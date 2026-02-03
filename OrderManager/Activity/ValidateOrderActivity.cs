using System;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Workflow;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Activity;

public class ValidateOrderActivity : WorkflowActivity<OrderValidationRequest, ValidationResult>
{
    public override Task<ValidationResult> RunAsync(WorkflowActivityContext context, OrderValidationRequest request)
    {
        Console.WriteLine($"Validating order for Order ID: {request.OrderId}");
        
        if (string.IsNullOrEmpty(request.CustomerId))
        {
            Console.WriteLine($"Order validation failed for Order ID: {request.OrderId} - Reason: Customer ID is required");
            return Task.FromResult(new ValidationResult(false, "Customer ID is required"));
        }

        if (! request.Items.Any())
        {
            Console.WriteLine($"Order validation failed for Order ID: {request.OrderId} - Reason: Order must contain at least one item");
            return Task.FromResult(new ValidationResult(false, "Order must contain at least one item"));
        }

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                Console.WriteLine($"Order validation failed for Order ID: {request.OrderId} - Reason: Invalid quantity for product {item.ProductId}");
                return Task.FromResult(new ValidationResult(false, $"Invalid quantity for product {item.ProductId}"));
            }

            if (item.Price <= 0)
            {
                Console.WriteLine($"Order validation failed for Order ID: {request.OrderId} - Reason: Invalid price for product {item.ProductId}");
                return Task.FromResult(new ValidationResult(false, $"Invalid price for product {item.ProductId}"));
            }
        }

        Console.WriteLine($"Order validation successful for Order ID: {request.OrderId}");
        return Task.FromResult(new ValidationResult(true, "Order validation successful"));
    }
}
