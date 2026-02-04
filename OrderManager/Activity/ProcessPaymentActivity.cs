using System;
using System.Threading.Tasks;
using Dapr.Workflow;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Activity;

public class ProcessPaymentActivity : WorkflowActivity<PaymentRequest, PaymentResult>
{
    public override async Task<PaymentResult> RunAsync(WorkflowActivityContext context, PaymentRequest request)
    {
        Console.WriteLine($"Processing payment for Order ID: {request.OrderId}, Amount: ${request.Amount}");
        
        if (request.Amount <= 0)
        {
            Console.WriteLine($"Payment failed for Order ID: {request.OrderId} - Reason: Invalid payment amount");
            return new(false, "Invalid payment amount");
        }

        await Task.Delay(TimeSpan.FromSeconds(1));

        if (request.Amount > 1000)
        {
            Console.WriteLine($"Payment failed for Order ID: {request.OrderId} - Reason: Payment amount exceeds limit");
            return new(false, "Payment amount exceeds limit");
        }

        Console.WriteLine($"Payment processed successfully for Order ID: {request.OrderId}");
        return new(true, "Payment processed successfully");
    }
}
