using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerificationProvider.Models;
using VerificationProvider.Services;

namespace VerificationProvider.Functions;

public class GenerateVerificationCodeHttp(ILogger<GenerateVerificationCodeHttp> logger, IVerificationService verificationService)
{
    private readonly ILogger<GenerateVerificationCodeHttp> _logger = logger;
    private readonly IVerificationService _verificationService = verificationService;

    [Function("GenerateVerificationCodeHttp")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        try
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var verificationRequest = JsonConvert.DeserializeObject<VerificationRequest>(requestBody);

            if (verificationRequest != null)
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
                            string payload = _verificationService.GenerateServiceBusEmailRequest(emailRequest);
                            if (!string.IsNullOrEmpty(payload))
                            {
                                return new OkObjectResult(payload);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCodeHttp.Run :: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        return new BadRequestResult();
    }
}
