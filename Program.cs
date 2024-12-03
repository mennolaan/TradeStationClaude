using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TradeStation.Interfaces;
using TradeStation.Configuration;
using TradeStation.Configuration.ValidationRules;
using TradeStation.Handlers;
using TradeStation.Models.Common;
using TradeStation.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// Configuration
builder.Services.Configure<TradeStationOptions>(
    builder.Configuration.GetSection(TradeStationOptions.ConfigurationSection));

// Add validator as singleton
builder.Services.AddSingleton<IValidator<TradeStationOptions>, TradeStationOptionsValidation>();

// Configure options validation at startup
builder.Services.PostConfigure<TradeStationOptions>(options =>
{
    var validator = builder.Services.BuildServiceProvider()
        .GetRequiredService<IValidator<TradeStationOptions>>();

    var result = validator.Validate(options);
    if (!result.IsValid)
    {
        throw new OptionsValidationException(
            nameof(TradeStationOptions),
            typeof(TradeStationOptions),
            result.Errors.Select(x => x.ErrorMessage));
    }
});

// Add HttpClient with base configuration
builder.Services.AddHttpClient<ITradeStationClient, TradeStationClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<TradeStationOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register services
builder.Services.AddScoped<ITradeStationClient, TradeStationClient>();
builder.Services.AddScoped<IResourceHandler, ResourceHandler>();
builder.Services.AddScoped<IToolHandler, ToolHandler>();
builder.Services.AddScoped<TradeStationServer>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// API Endpoints
app.MapGet("/api/resources", async (
    TradeStationServer server,
    CancellationToken cancellationToken) =>
{
    try
    {
        var resources = server.ListResources();
        return Results.Ok(resources);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error retrieving resources",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("GetResources")
.Produces<IEnumerable<Resource>>(200)
.ProducesProblem(500);

app.MapGet("/api/tools", async (
    TradeStationServer server,
    CancellationToken cancellationToken) =>
{
    try
    {
        var tools = server.ListTools();
        return Results.Ok(tools);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error retrieving tools",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("GetTools")
.Produces<IEnumerable<Tool>>(200)
.ProducesProblem(500);

app.MapPost("/api/resource", async (
    TradeStationServer server,
    Uri uri,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await server.HandleResourceCallAsync(uri, cancellationToken);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error handling resource call",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("CallResource")
.Produces<string>(200)
.ProducesProblem(500)
.ProducesValidationProblem(400);

app.MapPost("/api/tool", async (
    TradeStationServer server,
    [FromBody] ToolRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await server.HandleToolCallAsync(
            request.Name,
            request.Arguments,
            cancellationToken);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error handling tool call",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("CallTool")
.Produces<IEnumerable<ToolResponse>>(200)
.ProducesProblem(500)
.ProducesValidationProblem(400);

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }))
   .WithName("HealthCheck")
   .Produces<object>(200);

app.Run();