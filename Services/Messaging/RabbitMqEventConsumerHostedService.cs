using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WebDevStd2531.Services.Messaging;

public sealed class RabbitMqEventConsumerHostedService : BackgroundService
{
    private const string DefaultExchangeName = "webdevstd2531.events";
    private const string DefaultQueueName = "webdevstd2531.web";

    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqEventConsumerHostedService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public RabbitMqEventConsumerHostedService(
        IConfiguration configuration,
        ILogger<RabbitMqEventConsumerHostedService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await CreateFactory().CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(options: null, cancellationToken: stoppingToken);

                var exchangeName = GetExchangeName();
                var queueName = GetQueueName();

                await channel.ExchangeDeclareAsync(
                    exchange: exchangeName,
                    type: ExchangeType.Fanout,
                    durable: true,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                await channel.QueueBindAsync(
                    queue: queueName,
                    exchange: exchangeName,
                    routingKey: string.Empty,
                    arguments: null,
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    var json = Encoding.UTF8.GetString(ea.Body.Span);
                    try
                    {
                        using var document = JsonDocument.Parse(json);
                        var root = document.RootElement;
                        var eventType = root.TryGetProperty("eventType", out var eventTypeNode)
                            ? eventTypeNode.GetString() ?? "Unknown"
                            : "Unknown";
                        var source = root.TryGetProperty("source", out var sourceNode)
                            ? sourceNode.GetString() ?? "Unknown"
                            : "Unknown";

                        _logger.LogInformation("RabbitMQ event received from {Source}: {EventType}. Payload: {Payload}", source, eventType, json);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse RabbitMQ event payload: {Payload}", json);
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                };

                await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, cancellationToken: stoppingToken);
                _logger.LogInformation("RabbitMQ consumer listening on exchange {ExchangeName}, queue {QueueName}.", exchangeName, queueName);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ consumer connection failed or dropped. Retrying in 5 seconds.");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private ConnectionFactory CreateFactory()
    {
        return new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:Host"] ?? "rabbitmq",
            Port = int.TryParse(_configuration["RabbitMq:Port"], out var port) ? port : 5672,
            UserName = _configuration["RabbitMq:UserName"] ?? "guest",
            Password = _configuration["RabbitMq:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            ClientProvidedName = "webdevstd2531-web-consumer"
        };
    }

    private string GetExchangeName() => _configuration["RabbitMq:Exchange"] ?? DefaultExchangeName;

    private string GetQueueName() => _configuration["RabbitMq:Queue"] ?? DefaultQueueName;
}
