using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dapr.Client;
using Dapr.Workflow;
using Diagrid.Labs.Catalyst.OrderWorkflow.Common.Domain;
using Diagrid.Labs.Catalyst.OrderWorkflow.Common.ServiceDefaults;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Activity;

public class CheckInventoryActivity(DaprClient daprClient) : WorkflowActivity<SearchInventoryRequest, InventorySearchResult>
{
    public override async Task<InventorySearchResult> RunAsync(WorkflowActivityContext context, SearchInventoryRequest request)
    {
        Console.WriteLine($"Checking inventory for Order ID: {context.InstanceId}");
        
        var httpClient = daprClient.CreateInvokableHttpClient(ResourceNames.InventoryService);

        var httpResponse = await httpClient.PostAsJsonAsync("/inventory/search", request);

        if (! httpResponse.IsSuccessStatusCode) throw new HttpRequestException($"Inventory service returned error: {httpResponse.StatusCode}");

        var response = await httpResponse.Content.ReadFromJsonAsync<InventorySearchResult>()
            ?? throw new("Failed to deserialize inventory search response.");

        if (response.OutOfStockItems is { Count: >= 1 } outOfStockItems)
        {
            Console.WriteLine($"Items out of stock for Order ID: {context.InstanceId} - Items: {string.Join(", ", outOfStockItems.Select(item => item.ProductId))}");
        }

        return response;
    }
}
