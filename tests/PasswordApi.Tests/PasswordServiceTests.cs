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

    [Fact]
    public async Task CreateAsync_WhenSaveFails_PropagatesExceptionAndStoresNothing()
    {
        using (var db = new FailingAppDbContext(CreateOptions()))
        {
            var service = new PasswordService(db, new FixedPasswordGenerator());

            await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateAsync(CancellationToken.None));
        }

        using var readDb = CreateContext();
        Assert.Equal(0, await readDb.Passwords.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsStoredPassword()
    {
        Password created;
        using (var db = CreateContext())
        {
            created = await new PasswordService(db, new FixedPasswordGenerator()).CreateAsync(CancellationToken.None);
        }

        using var readDb = CreateContext();
        var found = await new PasswordService(readDb, new FixedPasswordGenerator()).GetByIdAsync(created.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found.Id);
        Assert.Equal(created.Value, found.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        using var db = CreateContext();
        var service = new PasswordService(db, new FixedPasswordGenerator());

        var found = await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(found);
    }

    private DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

    private AppDbContext CreateContext() => new(CreateOptions());

    private sealed class FixedPasswordGenerator : IPasswordGenerator
    {
        public string Generate() => FixedPassword;
    }

    private sealed class FailingAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("Falha simulada ao gravar.");
    }
}
