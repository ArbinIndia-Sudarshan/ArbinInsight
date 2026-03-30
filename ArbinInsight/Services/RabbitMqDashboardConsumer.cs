using System.Text;
using System.Text.Json;
using ArbinInsight.Data;
using ArbinInsight.Models.Configuration;
using ArbinInsight.Models.RemoteData;
using ArbinInsight.Models.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ArbinInsight.Services
{
    public class RabbitMqDashboardConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitMqOptions _options;
        private readonly ILogger<RabbitMqDashboardConsumer> _logger;
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMqDashboardConsumer(
            IServiceScopeFactory scopeFactory,
            IOptions<RabbitMqOptions> options,
            ILogger<RabbitMqDashboardConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.EnableDashboardConsumer)
            {
                _logger.LogInformation("RabbitMQ dashboard consumer is disabled.");
                return;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Direct, durable: true, autoDelete: false);
            _channel.QueueDeclare(_options.Queue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(_options.Queue, _options.Exchange, _options.RoutingKey);
            _channel.BasicQos(0, 1, false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnReceivedAsync;

            _channel.BasicConsume(_options.Queue, autoAck: false, consumer);
            _logger.LogInformation("RabbitMQ dashboard consumer started for queue {Queue}.", _options.Queue);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
        {
            if (_channel == null)
            {
                return;
            }

            var messageId = ParseMessageId(eventArgs.BasicProperties?.MessageId);
            var payloadJson = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            try
            {
                var database = JsonSerializer.Deserialize<RemoteDatabaseFetchResult>(payloadJson);
                if (database == null)
                {
                    throw new InvalidOperationException("RabbitMQ payload could not be deserialized to RemoteDatabaseFetchResult.");
                }

                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IDashboardSyncService>();
                await syncService.IngestRemoteDatabaseAsync(
                    database,
                    messageId,
                    eventArgs.BasicProperties?.Type ?? "RemoteDatabaseFetchResult",
                    eventArgs.RoutingKey,
                    CancellationToken.None);

                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process RabbitMQ dashboard message {MessageId}.", messageId);
                await SaveDeadLetterAsync(messageId, payloadJson, eventArgs, ex);
                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
        }

        private async Task SaveDeadLetterAsync(Guid messageId, string payloadJson, BasicDeliverEventArgs eventArgs, Exception ex)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (await dbContext.DeadLetterMessages.AnyAsync(x => x.MessageId == messageId))
                {
                    return;
                }

                dbContext.DeadLetterMessages.Add(new DeadLetterMessage
                {
                    MessageId = messageId,
                    PublisherNodeId = Guid.Empty,
                    MessageType = eventArgs.BasicProperties?.Type ?? "RemoteDatabaseFetchResult",
                    RoutingKey = eventArgs.RoutingKey,
                    PayloadJson = payloadJson,
                    RetryCount = 1,
                    ErrorText = ex.Message
                });

                await dbContext.SaveChangesAsync();
            }
            catch (Exception deadLetterEx)
            {
                _logger.LogError(deadLetterEx, "Failed to persist dead-letter message {MessageId}.", messageId);
            }
        }

        private static Guid ParseMessageId(string? value)
        {
            return Guid.TryParse(value, out var parsed) ? parsed : Guid.NewGuid();
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
