using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VerificationProvider.Services;

namespace VerificationProvider.Functions;

public class GenerateVerificationCode(ILogger<GenerateVerificationCode> logger, IVerificationService verificationService)
{
   
    private readonly ILogger<GenerateVerificationCode> _logger = logger;
    private readonly IVerificationService _verificationService = verificationService;

    [Function(nameof(GenerateVerificationCode))]
    [ServiceBusOutput("email_request", Connection = "ServiceBusConnection")]
    public async Task<string> Run(
        [ServiceBusTrigger("verification_request", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        try
        {
            var verficationToken = verificationService.UnpackVerificationRequest(message);
            if (verficationToken != null)
            {
                var code = verificationService.GenerateCode();
                if (!string.IsNullOrEmpty(code))
                {
                    var result = await verificationService.SaveVerificationRequest(verficationToken, code);
                    if (result)
                    {
                        var emailRequest = verificationService.GenerateEmailRequest(verficationToken, code);
                        if (emailRequest != null)
                        {
                            var payload = verificationService.GenerateServiceBusEmailRequest(emailRequest);
                            if (!string.IsNullOrEmpty(payload))
                            {
                                await messageActions.CompleteMessageAsync(message);
                                return payload;
                            }
                        }
                    }
                }
            }
        }

        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.Run() :: {ex.Message}");
        }

        return null!;
    }

}
