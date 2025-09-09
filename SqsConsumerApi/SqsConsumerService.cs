using Amazon.SQS;
using Amazon.SQS.Model;

public class SqsConsumerService : BackgroundService
{
    private readonly ILogger<SqsConsumerService> _logger;
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public SqsConsumerService(ILogger<SqsConsumerService> logger, IAmazonSQS sqsClient, IConfiguration configuration)
    {
        _logger = logger;
        _sqsClient = sqsClient;

        // Monta a URL da fila a partir do nome
        var queueName = configuration["AWS:QueueName"];
        var serviceUrl = configuration["AWS:ServiceURL"];
        _queueUrl = $"{serviceUrl}/000000000000/{queueName}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando o consumidor SQS...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var receiveRequest = new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 5,
                WaitTimeSeconds = 5 // Long-polling para evitar chamadas vazias constantes
            };

            try
            {
                var receiveResponse = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                if (receiveResponse.Messages != null && receiveResponse.Messages.Any())
                {
                    _logger.LogInformation($"Recebidas {receiveResponse.Messages.Count} mensagens.");
                    foreach (var message in receiveResponse.Messages)
                    {
                        // Simula um processamento que consome CPU
                        ProcessMessage(message);

                        // Deleta a mensagem da fila para não ser processada novamente
                        await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                    }
                }
                else
                {
                    _logger.LogInformation("Nenhuma mensagem na fila.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagens SQS.");
                // Em um cenário real, teríamos políticas de retry, dead-letter queue, etc.
                await Task.Delay(5000, stoppingToken); // Aguarda antes de tentar novamente
            }
        }
    }

    private void ProcessMessage(Message message)
    {
        _logger.LogInformation($"Processando mensagem ID: {message.MessageId}");
        _logger.LogInformation($"Corpo: {message.Body}");

        // SIMULAÇÃO DE TRABALHO PESADO (para testar o HPA)
        // Apenas para gastar CPU e fazer o Kubernetes escalar
        int result = 0;
        for (int i = 0; i < 10000000; i++)
        {
            result += i;
        }

        _logger.LogInformation("Mensagem processada.");
    }
}