using System.Linq;
using System.Threading.Tasks;
using Dapr.Workflow;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Activity;

public class ValidateOrderActivity : WorkflowActivity<OrderValidationRequest, ValidationResult>
{
    public override Task<ValidationResult> RunAsync(WorkflowActivityContext context, OrderValidationRequest request)
    {
        if (string.IsNullOrEmpty(request.CustomerId)) return Task.FromResult(new ValidationResult(false, "Customer ID is required"));

        if (! request.Items.Any()) return Task.FromResult(new ValidationResult(false, "Order must contain at least one item"));

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0) return Task.FromResult(new ValidationResult(false, $"Invalid quantity for product {item.ProductId}"));

            if (item.Price <= 0) return Task.FromResult(new ValidationResult(false, $"Invalid price for product {item.ProductId}"));
        }

        return Task.FromResult(new ValidationResult(true, "Order validation successful"));
    }
}
