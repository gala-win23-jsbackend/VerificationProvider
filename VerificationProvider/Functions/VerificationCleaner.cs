
using Microsoft.Azure.Functions.Worker;

using Microsoft.Extensions.Logging;

using VerificationProvider.Services;

namespace VerificationProvider.Functions;

public class VerificationCleaner(ILogger<VerificationCleaner> logger, IVerificationCleanerService verificationCleanerService)
{
    private readonly ILogger<VerificationCleaner> _logger = logger;
    private readonly IVerificationCleanerService _verificationCleanerService = verificationCleanerService;

    [Function("VerificationCleaner")]
    public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
    {
        try
        {
            await _verificationCleanerService.RemoveExpiredRecordAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : VerificationCleaner.Run :: {ex.Message}");
        }

    }
}
