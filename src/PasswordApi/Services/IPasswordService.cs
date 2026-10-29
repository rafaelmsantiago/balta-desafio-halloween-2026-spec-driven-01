using PasswordApi.Data;

namespace PasswordApi.Services;

public interface IPasswordService
{
    Task<Password> CreateAsync(CancellationToken cancellationToken);
    Task<Password?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
