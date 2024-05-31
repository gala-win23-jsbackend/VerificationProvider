

using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerificationProvider.Data.Entities;
using VerificationProvider.Models;
using VerificationProvider.Services;

namespace VerificationProvider.Functions;

public class GenerateUsingHttp(ILogger<GenerateUsingHttp> logger, IVerificationService verificationService)
{
    private readonly ILogger<GenerateUsingHttp> _logger = logger;
    private readonly IVerificationService _verificationService = verificationService;

    [Function("GenerateUsingHttp")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "Verify")] HttpRequest req)
    {
        try
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<SendEmailRequest>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.Email))
            {
                return new BadRequestObjectResult("Please provide an email address.");
            }

            var verificationRequest = new VerificationRequest { Email = data.Email };
            var code = _verificationService.GenerateCode();

            var result = await _verificationService.SaveVerificationRequest(verificationRequest, code);
            if (result)
            {
                var emailRequest = _verificationService.GenerateEmailRequest(verificationRequest, code);
                if (emailRequest != null)
                {
                    var payload = _verificationService.GenerateServiceBusEmailRequest(emailRequest);
                    if (!string.IsNullOrEmpty(payload))
                    {
                        var client = new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnection"));
                        var sender = client.CreateSender("email_request");
                        await sender.SendMessageAsync(new ServiceBusMessage(payload));
                        return new OkObjectResult(payload);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateUsingHttp.Run() :: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
    }
}

