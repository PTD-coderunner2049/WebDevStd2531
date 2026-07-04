using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace CatalogService.Services.Messaging;

public sealed class RabbitMqEventPublisher
{
    private const string DefaultExchangeName = "webdevstd2531.events";
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public RabbitMqEventPublisher(IConfiguration configuration, ILogger<RabbitMqEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = CreateConnectionFactory();
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(options: null, cancellationToken: cancellationToken);

            await channel.ExchangeDeclareAsync(
                exchange: GetExchangeName(),
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            var envelope = new
            {
                eventType,
                source = "CatalogService",
                occurredAtUtc = DateTime.UtcNow,
                payload
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, _jsonOptions));
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await channel.BasicPublishAsync(
                exchange: GetExchangeName(),
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Published {EventType} event to RabbitMQ.", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish {EventType} event to RabbitMQ.", eventType);
        }
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:Host"] ?? "rabbitmq",
            Port = int.TryParse(_configuration["RabbitMq:Port"], out var port) ? port : 5672,
            UserName = _configuration["RabbitMq:UserName"] ?? "guest",
            Password = _configuration["RabbitMq:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            ClientProvidedName = "webdevstd2531-catalog-service-publisher"
        };
    }

    private string GetExchangeName()
    {
        return _configuration["RabbitMq:Exchange"] ?? DefaultExchangeName;
    }
}
