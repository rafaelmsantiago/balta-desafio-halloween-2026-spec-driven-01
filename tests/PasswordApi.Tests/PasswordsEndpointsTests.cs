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

    [Fact]
    public async Task CA02_Post_ReturnsPasswordWithAtLeast16Characters()
    {
        var senha = await PostAndGetPasswordAsync();

        Assert.True(senha.Length >= 16, $"Senha com {senha.Length} caracteres.");
    }

    [Fact]
    public async Task CA03_Post_ReturnsPasswordWithSpecialUpperLowerAndDigit()
    {
        var senha = await PostAndGetPasswordAsync();

        Assert.Contains(senha, char.IsAsciiLetterUpper);
        Assert.Contains(senha, char.IsAsciiLetterLower);
        Assert.Contains(senha, char.IsAsciiDigit);
        Assert.Contains(senha, c => !char.IsAsciiLetterOrDigit(c) && !char.IsWhiteSpace(c));
    }

    [Fact]
    public async Task CA04_Post_ReturnsPasswordWithoutWhitespace()
    {
        var senha = await PostAndGetPasswordAsync();

        Assert.DoesNotContain(senha, char.IsWhiteSpace);
    }

    [Fact]
    public async Task CA08_TwoPosts_ReturnDifferentIdsAndPasswords()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var first = await PostAsync(client);
        var second = await PostAsync(client);

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.Senha, second.Senha);
    }

    private static async Task<PasswordBody> PostAsync(HttpClient client)
    {
        var response = await client.PostAsync("/senhas", content: null);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PasswordBody>())!;
    }

    private static async Task<string> PostAndGetPasswordAsync()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/senhas", content: null);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PasswordBody>();
        return body!.Senha;
    }

    private sealed record PasswordBody(string Id, string Senha);
}
