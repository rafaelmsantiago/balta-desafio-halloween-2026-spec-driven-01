using Microsoft.EntityFrameworkCore;
using PasswordApi.Data;
using PasswordApi.Endpoints;
using PasswordApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IPasswordGenerator, PasswordGenerator>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        if (context.ProblemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            context.ProblemDetails.Title = "Erro interno.";
            context.ProblemDetails.Detail = null;
        }
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseExceptionHandler();

app.MapOpenApi();
app.MapPasswordsEndpoints();

app.Run();
