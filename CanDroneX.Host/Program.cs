using System.Diagnostics;
using Catalogue.Infrastructure;
using Commandes.Infrastructure;
using Drones.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Drones.Api.DroneController).Assembly)
    .AddApplicationPart(typeof(Commandes.Api.CommandesController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDroneModule(builder.Configuration);
builder.Services.AddCatalogueModule(builder.Configuration);
builder.Services.AddCommandesModule(builder.Configuration);

var app = builder.Build();

// Middleware global de gestion des erreurs inattendues (§4.2, §8.3, Scénario Q6)
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var ex = exceptionHandlerPathFeature?.Error;

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        logger.LogError(ex, "Une exception non gérée s'est produite lors de l'exécution de la requête. TraceId: {TraceId}", traceId);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "Une erreur technique inattendue est survenue. Veuillez contacter le support en fournissant l'identifiant de corrélation.",
            Instance = context.Request.Path
        };
        problemDetails.Extensions["traceId"] = traceId;

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Endpoint de santé pour Docker et la supervision (§7.2, §10.3)
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.MapControllers();

app.Run();
