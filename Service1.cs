using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Messaging;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

using Message = Amazon.SQS.Model.Message;

namespace msmq_to_sqs
{
    public partial class Service1 : ServiceBase
    {        
        private static readonly Logger l = LogManager.GetCurrentClassLogger();        
        private static bool isOn = true;
        AmazonSQSClient sqsClient = new AmazonSQSClient(RegionEndpoint.SAEast1);
        private const int MaxMessages = 10;
        private const int WaitTime = 20;

        private static readonly Dictionary<string, string> msmqToSqs = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> sqsToMsmq = new Dictionary<string, string>();

        public async Task SqsToMSMQ(string queueFrom, string queueTo)
        {
            do
            {
                try
                {
                    l.Debug("waiting for sqs messages on {0}", queueFrom);
                    var msg = await GetMessage(sqsClient, queueFrom, WaitTime);
                    foreach (Message m in msg.Messages)
                    {
                        ProcessMessage(m, queueTo);
                        await DeleteMessage(sqsClient, m, queueFrom);
                    }
                }
                catch(Exception e)
                {
                    l.Error(e, e.Message + " sleeping 5 seconds...");
                    Thread.Sleep(5000);
                }
            } while (isOn);
            l.Warn("sqs task finished");
        }


        private static async Task<ReceiveMessageResponse> GetMessage(
          IAmazonSQS sqsClient, string qUrl, int waitTime = 0)
        {
            return await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = qUrl,
                MaxNumberOfMessages = MaxMessages,
                WaitTimeSeconds = waitTime
            });
        }


        private static void ProcessMessage(Message message, string queue)
        {
            l.Debug($"{queue} MessageId: {message.MessageId}:");
            l.Debug($"{message.Body}");            
            QueueHelper.Send(message.Body, queue);
        }


        private static async Task DeleteMessage(
          IAmazonSQS sqsClient, Message message, string qUrl)
        {
            l.Debug($"Deleting message {message.MessageId} from queue...");
            await sqsClient.DeleteMessageAsync(qUrl, message.ReceiptHandle);
        }


        private async Task MsmqToSQS(string msmq, string sqs)
        {
            do
            {
                try
                {                   
                    l.Warn("waiting for msmq messages on {0}", msmq);
                    var s = QueueHelper.Receive(msmq).Body;
                    l.Debug($"{msmq} receiveid {s}");
                    await ProcessMessage((string)s, sqs);                    
                }
                catch (Exception ex)
                {
                    l.Error(ex);
                    Thread.Sleep(1000);
                }
            }
            while (isOn);
            l.Warn("msmq task finished");
        }

        private async Task ProcessMessage(string m, string sqs)
        {
            l.Debug("sending {0}", m);
            await sqsClient.SendMessageAsync(sqs, m.ToString());
        }

        public Service1()
        {
            InitializeComponent();
        }

        public void Start()
        {
            
            l.Warn("service start v5");
            try
            {
                ReadSettings();
                foreach (string msmq in msmqToSqs.Keys)
                {
                    if (!MessageQueue.Exists(msmq))
                    {
                        l.Warn("creating queue {0}", msmq);
                        MessageQueue.Create(msmq, true);
                    }
                    SetQueuePermissions(msmq);
                    Task.Run(() => MsmqToSQS(msmq, msmqToSqs[msmq]));
                    
                }
                foreach (string sqs in sqsToMsmq.Keys)
                {
                    Task.Run(() => SqsToMSMQ(sqs, sqsToMsmq[sqs]));
                }
                
            }
            catch (Exception e)
            {
                l.Error(e, e.Message);
            }            
        }

        private void ReadSettings()
        {
            var appSettings = ConfigurationManager.AppSettings;

            if (appSettings.Count == 0)
            {
                l.Warn("AppSettings is empty.");
            }
            else
            {
                foreach (var key in appSettings.AllKeys)
                {
                    l.Warn("Key: {0} Value: {1}", key, appSettings[key]);
                    if (key.StartsWith("msmqToSqs"))
                    {
                        msmqToSqs.Add($".\\private$\\{key.Split('|')[1]}", appSettings[key]);
                    }
                    else if(key.StartsWith("sqsToMsmq"))
                    {
                        sqsToMsmq.Add(key.Split('|')[1], appSettings[key]);
                    }
                    else if(key == "awsAccessKeyId")
                    {
                        l.Warn($"creating sqsClient {appSettings[key]}");
                        sqsClient = new AmazonSQSClient(appSettings[key], appSettings["awsSecretAccessKey"],RegionEndpoint.SAEast1);
                    }
                }
            }
        }

        private static void SetQueuePermissions(string msmq)
        {
            try {        
                l.Warn("setting queue owner");
                MessageQueue q = new MessageQueue(msmq);
                MessageQueueExtensions.SetOwner(q, "Administrator");
                AccessControlList list = new AccessControlList();

                Trustee tr = new Trustee("Everyone");

                AccessControlEntry entry = new AccessControlEntry(
                    tr, GenericAccessRights.All,
                    StandardAccessRights.All,
                    AccessControlEntryType.Allow);

                list.Add(entry);
                // Apply the AccessControlList to the queue.
                l.Warn("setting permissions to {0}", msmq);
                q.SetPermissions(list);
            } catch(Exception e) {
                l.Error(e.Message);
            }
        }

        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
            l.Warn("service stop");
            isOn = false;           
        }        
    }
}
