using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

using Message = Amazon.SQS.Model.Message;

namespace msmq_to_sqs
{
    public partial class Service1 : ServiceBase
    {        
        private static readonly Logger L = LogManager.GetCurrentClassLogger();        
        private static bool _isOn = true;
        private AmazonSQSClient _sqsClient = new AmazonSQSClient(RegionEndpoint.SAEast1);
        private const int MaxMessages = 10;
        private const int WaitTime = 20;

        private static readonly Dictionary<string, string> MsmqToSqs = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> SqsToMsmq = new Dictionary<string, string>();

        private async Task SqsToMSMQ(string queueFrom, string queueTo)
        {
            do
            {
                try
                {
                    L.Debug("waiting for sqs messages on {0}", queueFrom);
                    var msg = await GetMessage(_sqsClient, queueFrom, WaitTime);
                    foreach (Message m in msg.Messages)
                    {
                        ProcessMessage(m, queueTo);
                        await DeleteMessage(_sqsClient, m, queueFrom);
                    }
                }
                catch(Exception e)
                {
                    L.Error(e, e.Message + " sleeping 5 seconds...");
                    Thread.Sleep(5000);
                }
            } while (_isOn);
            L.Warn("sqs task finished");
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
            L.Debug($"{queue} MessageId: {message.MessageId}:");
            L.Debug($"{message.Body}");            
            QueueHelper.Send(message.Body, queue);
        }
        
        private static async Task DeleteMessage(
          IAmazonSQS sqsClient, Message message, string qUrl)
        {
            L.Debug($"Deleting message {message.MessageId} from queue...");
            await sqsClient.DeleteMessageAsync(qUrl, message.ReceiptHandle);
        }

        private async Task MsmqToSQS(string msmq, string sqs)
        {
            do
            {
                try
                {                   
                    L.Warn("waiting for msmq messages on {0}", msmq);
                    var s = QueueHelper.Receive(msmq).Body;
                    L.Debug($"{msmq} receiveid {s}");
                    await ProcessMessage((string)s, sqs);                    
                }
                catch (Exception ex)
                {
                    L.Error(ex);
                    Thread.Sleep(1000);
                }
            }
            while (_isOn);
            L.Warn("msmq task finished");
        }

        private async Task ProcessMessage(string m, string sqs)
        {
            L.Debug("sending {0}", m);
            await _sqsClient.SendMessageAsync(sqs, m.ToString());
        }

        public Service1()
        {
            InitializeComponent();
        }

        private void Start()
        {
            L.Warn("service start v5");
            try
            {
                ReadSettings();
                foreach (string msmq in MsmqToSqs.Keys)
                {
                    Task.Run(() => MsmqToSQS(msmq, MsmqToSqs[msmq]));
                }
                foreach (string sqs in SqsToMsmq.Keys)
                {
                    Task.Run(() => SqsToMSMQ(sqs, SqsToMsmq[sqs]));
                }
                
            }
            catch (Exception e)
            {
                L.Error(e, e.Message);
            }            
        }

        private void ReadSettings()
        {
            var appSettings = ConfigurationManager.AppSettings;

            if (appSettings.Count == 0)
            {
                L.Warn("AppSettings is empty.");
            }
            else
            {
                foreach (var key in appSettings.AllKeys)
                {
                    L.Warn("Key: {0} Value: {1}", key, appSettings[key]);
                    if (key.StartsWith("msmqToSqs"))
                    {
                        MsmqToSqs.Add($".\\private$\\{key.Split('|')[1]}", appSettings[key]);
                    }
                    else if(key.StartsWith("sqsToMsmq"))
                    {
                        SqsToMsmq.Add(key.Split('|')[1], appSettings[key]);
                    }
                    else if(key == "awsAccessKeyId")
                    {
                        L.Warn($"creating sqsClient {appSettings[key]}");
                        _sqsClient = new AmazonSQSClient(appSettings[key], appSettings["awsSecretAccessKey"],RegionEndpoint.SAEast1);
                    }
                }
            }
        }
        
        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
            L.Warn("service stop");
            _isOn = false;           
        }        
    }
}
