using InfinityFlow.Aspire.Temporal;

var builder = DistributedApplication.CreateBuilder(args);

var temporal = builder.AddTemporalServerContainer("temporal")
    .WithNamespace("storefront", "inventory", "payment", "fulfillment");

var mailpit = builder.AddMailPit("mailpit");

var api = builder.AddProject<Projects.Storefront_Api>("storefront-api")
    .WithReference(temporal)
    .WithReference(mailpit)
    .WithEnvironment("Temporal__Address", temporal.Resource.ConnectionStringExpression)
    .WaitFor(temporal)
    .WaitFor(mailpit);

builder.AddProject<Projects.PurchaseIntent_Worker>("purchase-intent-worker")
    .WithReference(temporal)
    .WithReference(mailpit)
    .WithEnvironment("Temporal__Address", temporal.Resource.ConnectionStringExpression)
    .WithEnvironment("MailPit__Host", mailpit.Resource.Host)
    .WithEnvironment("MailPit__Port", mailpit.Resource.Port)
    .WaitFor(temporal)
    .WaitFor(mailpit);

builder.AddProject<Projects.Checkout_Worker>("checkout-worker")
    .WithReference(temporal)
    .WithReference(mailpit)
    .WithEnvironment("Temporal__Address", temporal.Resource.ConnectionStringExpression)
    .WithEnvironment("MailPit__Host", mailpit.Resource.Host)
    .WithEnvironment("MailPit__Port", mailpit.Resource.Port)
    .WithEnvironment("Storefront__ApiBase", api.GetEndpoint("http"))
    .WaitFor(temporal)
    .WaitFor(mailpit)
    .WaitFor(api);

builder.AddProject<Projects.Inventory_Worker>("inventory-worker")
    .WithReference(temporal)
    .WithEnvironment("Temporal__Address", temporal.Resource.ConnectionStringExpression)
    .WaitFor(temporal);

builder.AddProject<Projects.Payment_Worker>("payment-worker")
    .WithReference(temporal)
    .WithEnvironment("Temporal__Address", temporal.Resource.ConnectionStringExpression)
    .WaitFor(temporal);

builder.AddProject<Projects.Fulfillment_Worker>("fulfillment-worker")
    .WithReference(temporal)
    .WithEnvironment("Temporal__Address", temporal.Resource.ConnectionStringExpression)
    .WaitFor(temporal);

builder.AddNpmApp("storefront-ui", @"..\Storefront.Ui", "dev", ["--", "--host", "0.0.0.0", "--port", "5173"])
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
    .WithHttpEndpoint(targetPort: 5173, name: "http")
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
