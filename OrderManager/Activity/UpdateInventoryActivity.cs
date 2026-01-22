using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dapr.Client;
using Dapr.Workflow;
using Diagrid.Labs.Catalyst.OrderWorkflow.Common.ServiceDefaults;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Activity;

public class UpdateInventoryActivity(DaprClient daprClient) : WorkflowActivity<InventoryUpdateRequest, InventoryUpdateResult>
{
    public override async Task<InventoryUpdateResult> RunAsync(WorkflowActivityContext context, InventoryUpdateRequest request)
    {
        var httpClient = daprClient.CreateInvokableHttpClient(ResourceNames.InventoryService);

        // Convert OrderItems to InventoryItems for the inventory service
        var inventoryItems = request.Items.Select((item) =>
            new InventoryItem(item.ProductId, item.Quantity)).ToList();

        var inventoryUpdateRequest = new
        {
            request.OrderId,
            Items = inventoryItems,
            request.Operation,
        };

        var httpResponse = await httpClient.PostAsJsonAsync("/inventory/update", inventoryUpdateRequest);

        if (!httpResponse.IsSuccessStatusCode) throw new("Inventory service returned error.");

        var responseContent = await httpResponse.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<InventoryUpdateResponse>(responseContent, JsonSerializerOptions.Default) ?? new InventoryUpdateResponse { Success = false, Message = "Failed to deserialize response" };

        if (! response.Success) return new(false, response.Message);

        return new(true, response.Message);
    }
}
