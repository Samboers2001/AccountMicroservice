using System.Text;
using System.Text.Json;
using AccountMicroservice.AsyncDataServices.Interfaces;
using RabbitMQ.Client;

namespace AccountMicroservice.AsyncDataServices.Implementations
{
    public class MessageBusClient : IMessageBusClient
    {
        private readonly IConnection _connection;
        private readonly IConfiguration _configuration;

        private readonly IModel _channel;

        public MessageBusClient(IConfiguration configuration)
        {
            _configuration = configuration;
            ConnectionFactory factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQHost"],
                Port = int.Parse(_configuration["RabbitMQPort"])
            };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel(); 

                _channel.ExchangeDeclare(exchange: "topic-exchange", type: ExchangeType.Topic);

                _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;

                Console.WriteLine("--> Connected to Message Bus");
            } 
            catch(Exception ex)
            {
                Console.WriteLine($"--> Could not connect to Message Bus: {ex.Message}");
            }
        }

        public void PublishMessage<T>(T message, string routingKey)
        {
            string serializedMessage = JsonSerializer.Serialize(message);
            byte[] body = Encoding.UTF8.GetBytes(serializedMessage);
            
            _channel.BasicPublish(exchange: "topic-exchange", routingKey: routingKey, basicProperties: null, body: body);
            Console.WriteLine($"--> Message published: {serializedMessage}");
        }

        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("--> RabbitMQ Connection Shutdown");
        }
    }
}