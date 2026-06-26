using Microsoft.Extensions.DependencyInjection;
using PurchaseIntent.Worker.Activities;
using PurchaseIntent.Worker.Workflows;
using Microsoft.Extensions.Hosting;
using ShoppingBasket.StandardDefaults;
using Temporalio.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var temporalAddress = builder.Configuration["Temporal:Address"] ?? "localhost:7233";

builder.Services.AddHostedTemporalWorker(
    clientTargetHost: temporalAddress,
    clientNamespace: "storefront",
    taskQueue: "purchase-intent-queue")
    .AddWorkflow<PurchaseIntentWorkflow>()
    .AddScopedActivities<PurchaseIntentActivities>()
    .AddScopedActivities<RecoveryEmailActivities>();

var app = builder.Build();
await app.RunAsync();
