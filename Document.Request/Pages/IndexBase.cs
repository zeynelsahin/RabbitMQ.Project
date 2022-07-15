using System.Text;
using EDevlet.Document.Common;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Document.Request.Pages;

public class IndexBase : ComponentBase
{
    protected List<MyAlert> Log = new List<MyAlert>();
    [Inject] private ISnackbar Snackbar { get; set; }
    protected string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";
    protected bool isConnectionOpen = false;

    private IConnection _connection;
    protected string ConnectName = "Connect";
    private readonly string createDocument = "crete_document_queue";
    private readonly string documentCreated = "document_created_queue";
    private readonly string documentCreateExchange = "document_create_exchange";

    private IModel _channel = null;

    protected IModel? channel
    {
        get
        {
            if (_channel == null)
            {
                _channel = CreateOrGetChannel();
            }

            return _channel;
        }
    }

    private IConnection GetConnection()
    {
        ConnectionFactory factory = new ConnectionFactory()
        {
            Uri = new Uri(ConnectionString, UriKind.RelativeOrAbsolute)
        };
        return factory.CreateConnection();
    }

    private IModel CreateOrGetChannel()
    {
        var model = _connection.CreateModel();
        AddLog($"Channel created", Severity.Normal);
        return model;
    }

    protected void CreateDocument()
    {
        var model = new CreateDocumentModel()
        {
            UserId = 1, DocumentType = DocumentType.Pdf
        };
        WriteToQueue(createDocument, model);

        var consumerEvent = new EventingBasicConsumer(channel);
        consumerEvent.Received += (ch, ea) =>
        {
            var modelReceived = JsonConvert.DeserializeObject<CreateDocumentModel>(Encoding.UTF8.GetString(ea.Body.ToArray()));
            AddLog($"Received Data Url : {modelReceived.Url}",Severity.Normal);
        };
        channel.BasicConsume(documentCreated, true, consumerEvent);
    }

    private void WriteToQueue(string queueName, CreateDocumentModel documentModel)
    {
        var messageArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(documentModel));

        channel.BasicPublish(documentCreateExchange, queueName, null, messageArray);
        AddLog("Message Published", Severity.Success);
    }

    protected void Connect()
    {
        if (!isConnectionOpen || _connection == null)
        {
            _connection = GetConnection();
        }
        else
        {
            _connection.Close();
        }

        //declare queue exchange bind
        channel.ExchangeDeclare(documentCreateExchange, "direct");
        channel.QueueDeclare(createDocument, false, false, false);

        channel.QueueBind(createDocument, documentCreateExchange, createDocument);

        channel.QueueDeclare(documentCreated, false, false, false);
        channel.QueueBind(documentCreated, documentCreateExchange, documentCreated);


        isConnectionOpen = _connection.IsOpen;
        ConnectName = isConnectionOpen ? "Disconnect" : "Connect";
        var logMessage = isConnectionOpen ? "Bağlantı açıldı" : "Bağlantı kapandı";
        var severity = isConnectionOpen ? Severity.Success : Severity.Error;
        AddLog(logMessage, severity);
    }

    public void AddLog(string logStr, Severity severity)
    {
        logStr = $"[{DateTime.Now:dd.MM.yyy HH:MM:ss}] - {logStr}";
        Log.Add(new MyAlert() { Message = logStr, Severity = severity });
        Snackbar.Add(logStr, severity);
    }

    protected class MyAlert
    {
        public string Message { get; set; }
        public Severity Severity { get; set; }
    }
}