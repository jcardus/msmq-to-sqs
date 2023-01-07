using NLog;
using System;
using System.Messaging;
using System.Runtime.CompilerServices;
namespace msmq_to_sqs
{
  public class QueueHelper
  {
    private static Logger logger = LogManager.GetCurrentClassLogger();
           

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void Send(string m, string queue)
    {
        MessageQueue sQueue = new MessageQueue(queue);
        sQueue.Formatter = new ActiveXMessageFormatter();
        MessageQueueTransaction transaction = new MessageQueueTransaction();
        transaction.Begin();            
        Message message = new Message();
        message.Body = m;
        message.Formatter = new ActiveXMessageFormatter();
        sQueue.Send(message, DateTime.Now.ToString(), transaction);
        transaction.Commit();
    }

    public static Message Receive(string queue)
    {
        MessageQueue rQueue = new MessageQueue(queue);
        rQueue.Formatter = new ActiveXMessageFormatter();
        logger.Info("checking {0}", queue);
        return rQueue.Receive();
    }        
  }
}
