

namespace VerificationProvider.Models;

public class ValidateRequest
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
}
