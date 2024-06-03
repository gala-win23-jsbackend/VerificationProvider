
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using VerificationProvider.Models;
using VerificationProvider.Services;

namespace VerificationProvider.Functions;

public class GenereteVerificationCodeHTTP
{
    private readonly ILogger<GenereteVerificationCodeHTTP> _logger;
    private readonly IVerificationService _verificationService;
    private readonly QueueClient _queueClient;

    public GenereteVerificationCodeHTTP(ILogger<GenereteVerificationCodeHTTP> logger, IVerificationService verificationService)
    {
        _logger = logger;
        _verificationService = verificationService;
        string serviceBusConnection = Environment.GetEnvironmentVariable("ServiceBusConnection")!;
        string queueName = Environment.GetEnvironmentVariable("ServiceBusQueueName")!;
        _queueClient = new QueueClient(serviceBusConnection, queueName);
    }

    [Function("GenereteVerificationCodeHTTP")]
    public async Task<string> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {

                try
                {
                    var verificationRequest = JsonConvert.DeserializeObject<VerificationRequest>(body.ToString());
                    if (verificationRequest != null && !string.IsNullOrEmpty(verificationRequest.Email))
                    {
                        try
                        {
                            var code = _verificationService.GenerateCode();
                            if (!string.IsNullOrEmpty(code))
                            {
                                var result = await _verificationService.SaveVerificationRequest(verificationRequest, code);
                                if (result)
                                {
                                    var emailRequest = _verificationService.GenerateEmailRequest(verificationRequest, code);
                                    if (emailRequest != null)
                                    {
                                        try
                                        {
                                            var payload = _verificationService.GenerateServiceBusEmailRequest(emailRequest);
                                            if (!string.IsNullOrEmpty(payload))
                                            {
                                                var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                                                {
                                                    Email = verificationRequest.Email,
                                                })));
                                                await _queueClient.SendAsync(message);
                                                _logger.LogInformation("Message sent to Service Bus queue");
                                                return payload;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError($"ERROR : GenerateServiceBusEmailRequest.Run() :: {ex.Message}");
                                        }

                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"ERROR : GenerateCode.Run() :: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"ERROR : DeserializeObject.Run() :: {ex.Message}");
                }

            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : StreamReader.Run() :: {ex.Message}");
        }

        return null!;
    }
}
