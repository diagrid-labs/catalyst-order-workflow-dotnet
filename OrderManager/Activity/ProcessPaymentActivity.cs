using System;
using System.Threading.Tasks;
using Dapr.Workflow;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Activity;

public class ProcessPaymentActivity : WorkflowActivity<PaymentRequest, PaymentResult>
{
    public override async Task<PaymentResult> RunAsync(WorkflowActivityContext context, PaymentRequest request)
    {
        if (request.Amount <= 0) return new(false, "Invalid payment amount");

        await Task.Delay(TimeSpan.FromSeconds(1));

        if (request.Amount > 1000) return new(false, "Payment amount exceeds limit");

        return new(true, "Payment processed successfully");
    }
}
