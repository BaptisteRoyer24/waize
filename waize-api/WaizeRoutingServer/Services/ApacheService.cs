namespace WaizeRoutingServer.Services;
using Apache.NMS;
using Apache.NMS.ActiveMQ;

public interface IApacheService
{
    public Task sendInformation(string message);
}

public class ApacheService : IApacheService
{
    public async Task sendInformation(string message)
    {
        await Task.Run(() =>
        {
            // Create a Connection Factory.
            Uri connecturi = new Uri("activemq:tcp://localhost:61616");
            ConnectionFactory connectionFactory = new ConnectionFactory(connecturi);

            // Create a single Connection from the Connection Factory.
            using IConnection connection = connectionFactory.CreateConnection();
            connection.Start();

            // Create a session from the Connection.
            using ISession session = connection.CreateSession();

            // Use the session to target a queue.
            IDestination destination = session.GetQueue("info");

            // Create a Producer targeting the selected queue.
            using IMessageProducer producer = session.CreateProducer(destination);

            // Configure the producer
            producer.DeliveryMode = MsgDeliveryMode.NonPersistent;

            // Create and send the message.
            ITextMessage textMessage = session.CreateTextMessage(message);
            producer.Send(textMessage);

            Console.WriteLine("Message sent, check ActiveMQ web interface to confirm.");
        });
    }
}