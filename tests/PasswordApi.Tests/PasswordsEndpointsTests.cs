using System.Net;
using System.Net.Http.Json;
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

    [Fact]
    public async Task CA01_Post_Returns201WithGuidAndPassword()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/senhas", content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PasswordBody>();
        Assert.NotNull(body);
        Assert.True(Guid.TryParse(body.Id, out _), $"'{body.Id}' não é um GUID válido.");
        Assert.False(string.IsNullOrEmpty(body.Senha));
        Assert.Equal($"/senhas/{body.Id}", response.Headers.Location?.OriginalString);
    }

    private sealed record PasswordBody(string Id, string Senha);
}
