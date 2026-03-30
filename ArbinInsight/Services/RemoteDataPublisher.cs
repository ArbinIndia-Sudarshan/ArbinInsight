using System.Text;
using System.Text.Json;
using ArbinInsight.Models.Configuration;
using ArbinInsight.Models.RemoteData;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ArbinInsight.Services
{
    public class RemoteDataPublisher : IRemoteDataPublisher
    {
        private readonly RabbitMqOptions _options;

        public RemoteDataPublisher(IOptions<RabbitMqOptions> options)
        {
            _options = options.Value;
        }

        public Task<RemoteDataPublishResult> PublishAsync(RemoteDataFetchResponse payload, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(_options.Exchange, ExchangeType.Direct, durable: true, autoDelete: false);
            channel.QueueDeclare(_options.Queue, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(_options.Queue, _options.Exchange, _options.RoutingKey);

            var result = new RemoteDataPublishResult
            {
                PublishedAtUtc = DateTime.UtcNow
            };

            foreach (var database in payload.Databases.Where(x => x.Success))
            {
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(database));
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.Type = "RemoteDatabaseFetchResult";

                channel.BasicPublish(_options.Exchange, _options.RoutingKey, properties, body);

                result.PublishedMessageCount++;
                result.PublishedConnections.Add(database.ConnectionName);
            }

            return Task.FromResult(result);
        }
    }
}
