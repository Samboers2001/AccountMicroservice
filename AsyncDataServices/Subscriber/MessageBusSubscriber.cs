using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AccountMicroservice.AsyncDataServices.Subscriber
{
    public class MessageBusSubscriber : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private IConnection _connection;
        private IModel _channel;
        private string _queueName;

        public MessageBusSubscriber(IConfiguration configuration)
        {
            _configuration = configuration;

            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            ConnectionFactory factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQHost"],
                Port = int.Parse(_configuration["RabbitMQPort"])
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: "trigger", type: ExchangeType.Fanout);
            _queueName = _channel.QueueDeclare().QueueName;
            _channel.QueueBind(queue: _queueName, exchange: "trigger", routingKey: "");

            Console.WriteLine("--> Listening on the Message Bus...");

            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
        }

private bool hasProcessedFirstMessage = false;

protected override Task ExecuteAsync(CancellationToken stoppingToken)
{
    stoppingToken.ThrowIfCancellationRequested();

    EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

    consumer.Received += (ModuleHandle, ea) =>
    {
        if (!hasProcessedFirstMessage)
        {
            Console.WriteLine("--> Event Received!");

            byte[] body = ea.Body.ToArray();
            string notificationMessage = Encoding.UTF8.GetString(body);

            // Process the message and return a hardcoded reply
            var reply = ProcessMessage(notificationMessage);

            // Send the reply message back to the messaging system
            SendMessage(reply);

            hasProcessedFirstMessage = true; // Stop listening after the first message is processed
        }
    };

    _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);
    return Task.CompletedTask;
}


        private void SendMessage(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(exchange: "trigger",
                                  routingKey: "",
                                  basicProperties: null,
                                  body: body);
            Console.WriteLine($"--> Sent Reply: {message}");
        }

        private string ProcessMessage(string message)
        {
            Console.WriteLine("--> Processing Message");
            Console.WriteLine($"--> Message Received: {message}");

            return "This is a hardcoded reply to the incoming message.";
        }

        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("--> RabbitMQ Connection Shutdown");
        }
    }
}
