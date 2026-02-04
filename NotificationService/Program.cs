using System.Text.Json;
using Diagrid.Labs.Catalyst.OrderWorkflow.Common.ServiceDefaults;
using Diagrid.Labs.Catalyst.OrderWorkflow.NotificationService;
using Diagrid.Labs.Catalyst.OrderWorkflow.NotificationService.Hubs;
using Microsoft.AspNetCore.Http.Json;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

builder.Services.Configure<JsonOptions>((options) =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddDaprClient((daprBuilder) =>
{
    daprBuilder.UseJsonSerializationOptions(new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    });
});

// Add SignalR for real-time notifications
builder.Services.AddSignalR();

// Add CORS for web UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCloudEvents();

app.UseCors("AllowAll");
app.UseRouting();
app.MapSubscribeHandler();
app.MapHealthChecks("/healthz");
app.MapOpenApi();
app.MapScalarApiReference();

// Map SignalR hub
app.MapHub<NotificationHub>("/notificationHub");

// Serve static files for the UI
app.UseDefaultFiles();
app.UseStaticFiles();

// Map notification endpoints
app.MapNotificationServiceEndpoints();

Console.WriteLine("Notification service started...");

app.Run();
