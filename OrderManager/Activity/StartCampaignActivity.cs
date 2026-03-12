using System;
using System.Threading.Tasks;
using Dapr.Workflow;
using Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Model;

namespace Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.Activity;

public class StartCampaignActivity : WorkflowActivity<StartCampaignInput, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, StartCampaignInput input)
    {
        await Task.Delay(TimeSpan.FromSeconds(60));

        return true;
    }
}
