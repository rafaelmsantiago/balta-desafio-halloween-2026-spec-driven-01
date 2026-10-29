using Microsoft.AspNetCore.Http.HttpResults;
using PasswordApi.Services;

namespace PasswordApi.Endpoints;

public static class PasswordsEndpoints
{
    public static IEndpointRouteBuilder MapPasswordsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/senhas");

        group.MapPost("/", CreateAsync);
        group.MapGet("/{id}", GetByIdAsync);

        return app;
    }

    private static async Task<Created<PasswordResponse>> CreateAsync(
        IPasswordService service,
        CancellationToken cancellationToken)
    {
        var password = await service.CreateAsync(cancellationToken);

        return TypedResults.Created($"/senhas/{password.Id}", new PasswordResponse(password.Id, password.Value));
    }

    // O id chega como string para que um valor inválido vire 400, e não 404 do roteamento (D12).
    private static async Task<Results<Ok<PasswordResponse>, ValidationProblem, ProblemHttpResult>> GetByIdAsync(
        string id,
        IPasswordService service,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["id"] = ["Identificador inválido."] },
                title: "Erro de validação.");
        }

        var password = await service.GetByIdAsync(guid, cancellationToken);
        if (password is null)
        {
            return TypedResults.Problem(title: "Senha não encontrada.", statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(new PasswordResponse(password.Id, password.Value));
    }

    // Contrato público do spec: os campos JSON são "id" e "senha".
    public record PasswordResponse(Guid Id, string Senha);
}
