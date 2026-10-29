using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace PasswordApi.Tests;

public class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly SqliteConnection? _keepAlive;
    private readonly Action<IServiceCollection>? _configureServices;

    public TestApiFactory()
        : this(null, null)
    {
    }

    public TestApiFactory(Action<IServiceCollection> configureServices)
        : this(null, configureServices)
    {
    }

    public TestApiFactory(string connectionString)
        : this(connectionString, null)
    {
    }

    private TestApiFactory(string? connectionString, Action<IServiceCollection>? configureServices)
    {
        _configureServices = configureServices;

        if (connectionString is null)
        {
            // SQLite em memória compartilhado: o banco existe enquanto esta conexão ficar aberta.
            _connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
            _keepAlive = new SqliteConnection(_connectionString);
            _keepAlive.Open();
        }
        else
        {
            _connectionString = connectionString;
        }
    }

    public string ConnectionString => _connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _connectionString);

        if (_configureServices is not null)
        {
            builder.ConfigureTestServices(_configureServices);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _keepAlive?.Dispose();
        }
    }
}
