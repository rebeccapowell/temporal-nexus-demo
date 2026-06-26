using ShoppingBasket.StandardDefaults;
using Storefront.Api.Endpoints;
using Storefront.Api.Infrastructure;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                builder.Configuration["services:storefront-ui:http:0"] ?? "http://localhost:5173",
                builder.Configuration["services:storefront-ui:https:0"] ?? "https://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var temporalAddress = builder.Configuration["Temporal:Address"] ?? "localhost:7233";

builder.Services.AddTemporalClient(options =>
{
    options.TargetHost = temporalAddress;
    options.Namespace = "storefront";
});

builder.Services.AddSingleton<PurchaseIntentCookieManager>();
builder.Services.AddHostedService<NexusEndpointInitializer>();

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();
app.MapSessionEndpoints();
app.MapProductEndpoints();
app.MapPurchaseIntentEndpoints();
app.MapEmailVerificationEndpoints();
app.MapCheckoutEndpoints();

app.Run();
