using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VerificationProvider.Services;

namespace VerificationProvider.Functions;

public class ValidateVerificationCode(ILogger<ValidateVerificationCode> logger, IValidateVerificationCodeService validationVerificationService)
{
    private readonly ILogger<ValidateVerificationCode> _logger = logger;
    private readonly IValidateVerificationCodeService _validationVerificationService = validationVerificationService;


    [Function("ValidateVerification")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "verification")] HttpRequest req)
    {
        try
        {
            var validateRequest = await _validationVerificationService.UnpackValidateRequestAsync(req);
            if (validateRequest != null)
            {
                var validateResult = await _validationVerificationService.ValidateCodeAsync(validateRequest);
                if (validateResult)
                {
                    return new OkResult();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : ValidateVerification.Run() :: {ex.Message}");
        }

        return new UnauthorizedResult();
    }

}

