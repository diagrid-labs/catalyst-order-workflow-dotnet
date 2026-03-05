using System;
using System.Threading.Tasks;
using Dapr.Workflow;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Activity;

public class CustomerFeedbackDelay : WorkflowActivity<bool, int>
{
    public const string Name = "customer-feedback-delay";

    public override Task<int> RunAsync(WorkflowActivityContext context, bool input)
    {
        var random = new Random();

        var duration = random.Next(40, 91);

        return Task.FromResult(duration);
    }
}
