using Microsoft.AspNetCore.Http.HttpResults;
using PasswordApi.Services;

namespace PasswordApi.Endpoints;

public static class PasswordsEndpoints
{
    public static IEndpointRouteBuilder MapPasswordsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/senhas");

        group.MapPost("/", CreateAsync);

        return app;
    }

    private static async Task<Created<PasswordResponse>> CreateAsync(
        IPasswordService service,
        CancellationToken cancellationToken)
    {
        var password = await service.CreateAsync(cancellationToken);

        return TypedResults.Created($"/senhas/{password.Id}", new PasswordResponse(password.Id, password.Value));
    }

    // Contrato público do spec: os campos JSON são "id" e "senha".
    public record PasswordResponse(Guid Id, string Senha);
}
