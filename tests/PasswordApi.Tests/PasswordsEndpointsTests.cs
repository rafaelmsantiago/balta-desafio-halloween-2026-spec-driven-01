using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PasswordApi.Data;
using PasswordApi.Services;

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

    [Fact]
    public async Task CA05_GetAfterPost_Returns200WithSamePassword()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var created = await PostAsync(client);

        var response = await client.GetAsync($"/senhas/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PasswordBody>();
        Assert.Equal(created, body);
    }

    [Fact]
    public async Task CA06_GetWithUnknownGuid_Returns404ProblemDetails()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/senhas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Senha não encontrada.", problem.Title);
    }

    [Fact]
    public async Task CA07_GetWithInvalidGuid_Returns400ProblemDetails()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/senhas/abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(["Identificador inválido."], problem.Errors["id"]);
    }

    [Fact]
    public async Task CA09_PasswordSurvivesApplicationRestart()
    {
        var file = Path.Combine(Path.GetTempPath(), $"senhas-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={file}";

        try
        {
            PasswordBody created;
            using (var factory = new TestApiFactory(connectionString))
            using (var client = factory.CreateClient())
            {
                created = await PostAsync(client);
            }

            using (var factory = new TestApiFactory(connectionString))
            using (var client = factory.CreateClient())
            {
                var found = await client.GetFromJsonAsync<PasswordBody>($"/senhas/{created.Id}");
                Assert.Equal(created, found);
            }
        }
        finally
        {
            // O pool do SQLite mantém o arquivo aberto; é preciso liberá-lo antes de apagar.
            SqliteConnection.ClearAllPools();
            foreach (var path in new[] { file, $"{file}-wal", $"{file}-shm" })
            {
                File.Delete(path);
            }
        }

        Assert.False(File.Exists(file));
    }

    [Fact]
    public async Task EdgeCase_GetWithUpperOrLowerCaseGuid_ReturnsSamePassword()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var created = await PostAsync(client);

        var upper = await client.GetFromJsonAsync<PasswordBody>($"/senhas/{created.Id.ToUpperInvariant()}");
        var lower = await client.GetFromJsonAsync<PasswordBody>($"/senhas/{created.Id.ToLowerInvariant()}");

        Assert.Equal(created.Senha, upper?.Senha);
        Assert.Equal(created.Senha, lower?.Senha);
    }

    [Fact]
    public async Task EdgeCase_PostWhenServiceFails_Returns500WithoutLeakingData()
    {
        using var factory = new TestApiFactory(services =>
            services.AddScoped<IPasswordService, ThrowingPasswordService>());
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/senhas", content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal("Erro interno.", root.GetProperty("title").GetString());
        Assert.False(root.TryGetProperty("id", out _));
        Assert.False(root.TryGetProperty("senha", out _));
        Assert.DoesNotContain(ThrowingPasswordService.Message, json);
        Assert.DoesNotContain(nameof(ThrowingPasswordService), json);
    }

    private sealed class ThrowingPasswordService : IPasswordService
    {
        public const string Message = "Falha simulada no serviço.";

        public Task<Password> CreateAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException(Message);

        public Task<Password?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(Message);
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
