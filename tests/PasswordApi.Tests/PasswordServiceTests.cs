using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PasswordApi.Data;
using PasswordApi.Services;

namespace PasswordApi.Tests;

public class PasswordServiceTests : IDisposable
{
    private const string FixedPassword = "Aa1!Bb2@Cc3#Dd4$";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public PasswordServiceTests()
    {
        _connection.Open();
        using var db = CreateContext();
        db.Database.Migrate();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task CreateAsync_ReturnsPasswordFromGenerator()
    {
        using var db = CreateContext();
        var service = new PasswordService(db, new FixedPasswordGenerator());

        var password = await service.CreateAsync(CancellationToken.None);

        Assert.Equal(FixedPassword, password.Value);
    }

    [Fact]
    public async Task CreateAsync_ReturnsNonEmptyId()
    {
        using var db = CreateContext();
        var service = new PasswordService(db, new FixedPasswordGenerator());

        var password = await service.CreateAsync(CancellationToken.None);

        Assert.NotEqual(Guid.Empty, password.Id);
    }

    [Fact]
    public async Task CreateAsync_PersistsRowWithSameValues()
    {
        Password created;
        using (var db = CreateContext())
        {
            created = await new PasswordService(db, new FixedPasswordGenerator()).CreateAsync(CancellationToken.None);
        }

        using var readDb = CreateContext();
        var stored = await readDb.Passwords.SingleAsync();

        Assert.Equal(created.Id, stored.Id);
        Assert.Equal(created.Value, stored.Value);
        Assert.Equal(created.CreatedAtUtc, stored.CreatedAtUtc);
    }

    private AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    private sealed class FixedPasswordGenerator : IPasswordGenerator
    {
        public string Generate() => FixedPassword;
    }
}
