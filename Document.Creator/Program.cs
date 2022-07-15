using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Document.Creator
{
    internal class Program
    {
        private static string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";

        private static IConnection _connection;
        private static readonly string createDocument = "crete_document_queue";
        private static readonly string documentCreated = "document_created_queue";
        private static readonly string documentCreateExchange = "document_create_exchange";

        private static IModel _channel = null;

        private static IModel channel => _channel ?? (_channel = CreateOrGetChannel());

        

        public static void Main(string[] args)
        {
            _connection = GetConnection();
            
            channel.ExchangeDeclare(documentCreateExchange, "direct");
            channel.QueueDeclare(createDocument, false, false, false);

            channel.QueueBind(createDocument, documentCreateExchange, createDocument);

            channel.QueueDeclare(documentCreated, false, false, false);
            channel.QueueBind(documentCreated, documentCreateExchange, documentCreated);

            
            var consumerEvent = new EventingBasicConsumer(channel);
            consumerEvent.Received += (ch, ea) =>
            {
                var modelJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                var model = JsonConvert.DeserializeObject<CreateDocumentModel>(modelJson);
                Console.WriteLine($"Received Data : {modelJson}");
                //create document
                Task.Delay(5000).Wait();
                //document goes to ftp
                model.Url = "https://zeynelsahin.com"; 
                
                WriteToQueue(documentCreated,model);
            };
            channel.BasicConsume(documentCreated, true, consumerEvent);
            Console.WriteLine($"{documentCreated} listening");
            Console.ReadLine();
        }
        
        private static IConnection GetConnection()
        {
            ConnectionFactory factory = new ConnectionFactory()
            {
                Uri = new Uri(ConnectionString, UriKind.RelativeOrAbsolute)
            };
            return factory.CreateConnection();
        }

        private static IModel CreateOrGetChannel()
        {
            var model = _connection.CreateModel();
            Console.WriteLine($"Channel created");
            return model;
        }

        private static void WriteToQueue(string queueName, CreateDocumentModel documentModel)
        {
            var messageArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(documentModel));

            channel.BasicPublish(documentCreateExchange, queueName, null, messageArray);
            Console.WriteLine("Message Published");
        }
    }
}