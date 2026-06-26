using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Temporalio.Api.Nexus.V1;
using Temporalio.Api.OperatorService.V1;
using Temporalio.Client;

namespace Storefront.Api.Infrastructure;

public sealed class NexusEndpointInitializer(
    ITemporalClient temporalClient,
    ILogger<NexusEndpointInitializer> logger) : IHostedService
{
    private static readonly (string Name, string Namespace, string TaskQueue)[] Endpoints =
    [
        ("inventory-service", "inventory", "inventory-nexus-queue"),
        ("payment-service", "payment", "payment-nexus-queue"),
        ("fulfillment-service", "fulfillment", "fulfillment-nexus-queue")
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var endpoint in Endpoints)
        {
            var existing = await temporalClient.OperatorService.ListNexusEndpointsAsync(
                new ListNexusEndpointsRequest
                {
                    Name = endpoint.Name,
                    PageSize = 1
                });

            if (existing.Endpoints.Count > 0)
            {
                logger.LogInformation("Nexus endpoint {EndpointName} already exists", endpoint.Name);
                continue;
            }

            await temporalClient.OperatorService.CreateNexusEndpointAsync(
                new CreateNexusEndpointRequest
                {
                    Spec = new EndpointSpec
                    {
                        Name = endpoint.Name,
                        Target = new EndpointTarget
                        {
                            Worker = new EndpointTarget.Types.Worker
                            {
                                Namespace = endpoint.Namespace,
                                TaskQueue = endpoint.TaskQueue
                            }
                        }
                    }
                });

            logger.LogInformation(
                "Created Nexus endpoint {EndpointName} for namespace {Namespace} task queue {TaskQueue}",
                endpoint.Name,
                endpoint.Namespace,
                endpoint.TaskQueue);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
