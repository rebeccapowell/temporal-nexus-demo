using Fulfillment.Worker.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShoppingBasket.NexusContracts;
using ShoppingBasket.StandardDefaults;
using Temporalio.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var temporalAddress = builder.Configuration["Temporal:Address"] ?? "localhost:7233";

builder.Services.AddHostedTemporalWorker(
    clientTargetHost: temporalAddress,
    clientNamespace: "fulfillment",
    taskQueue: "fulfillment-nexus-queue")
    .AddSingletonNexusService<FulfillmentNexusService>();

var app = builder.Build();
await app.RunAsync();
