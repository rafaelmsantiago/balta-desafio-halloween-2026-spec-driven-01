using PasswordApi.Data;

namespace PasswordApi.Services;

public class PasswordService(AppDbContext db, IPasswordGenerator generator) : IPasswordService
{
    public async Task<Password> CreateAsync(CancellationToken cancellationToken)
    {
        var password = new Password
        {
            Id = Guid.NewGuid(),
            Value = generator.Generate(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        db.Passwords.Add(password);

        // Só retorna depois de gravar: se o banco falhar, a exceção sobe e nada é devolvido.
        await db.SaveChangesAsync(cancellationToken);

        return password;
    }

    public async Task<Password?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Passwords.FindAsync([id], cancellationToken);
}
