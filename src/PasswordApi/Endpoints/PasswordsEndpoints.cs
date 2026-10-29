using Microsoft.AspNetCore.Http.HttpResults;
using PasswordApi.Services;

namespace PasswordApi.Endpoints;

public static class PasswordsEndpoints
{
    private const string LearningWarning =
        "Aviso: projeto de aprendizado; as senhas ficam em texto puro e não há autenticação. Não use em produção.";

    public static IEndpointRouteBuilder MapPasswordsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/senhas");

        group.MapPost("/", CreateAsync)
            .WithSummary("Gera e armazena uma senha forte.")
            .WithDescription(
                "Gera uma senha de 16 caracteres com maiúscula, minúscula, dígito e caractere especial, " +
                "grava no banco e retorna o GUID para consulta. " + LearningWarning)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id}", GetByIdAsync)
            .WithSummary("Consulta uma senha pelo GUID.")
            .WithDescription(
                "Retorna a senha associada ao GUID. Responde 400 se o id não for um GUID válido " +
                "e 404 se o GUID não existir. " + LearningWarning)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

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
