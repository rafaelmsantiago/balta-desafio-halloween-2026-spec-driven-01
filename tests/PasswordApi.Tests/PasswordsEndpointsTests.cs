using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PasswordApi.Data;

namespace PasswordApi.Tests;

public class PasswordsEndpointsTests
{
    [Fact]
    public async Task Api_StartsWithInMemoryDatabase()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(factory.ConnectionString, db.Database.GetConnectionString());
        Assert.Equal(0, await db.Passwords.CountAsync());
    }
}
