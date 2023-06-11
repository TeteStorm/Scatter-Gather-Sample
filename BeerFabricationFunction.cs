using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace Scatter_Gather_Sample
{
    public static class BeerFabricationFunction
    {
        [FunctionName("BeerFabrication")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Message to broadcast
            var message = "Beer fabrication in progress...";

            // Get the list of recipients (brewery stages)
            var recipients = new List<string>
        {
            "Mashing",
            "Boiling",
            "Fermenting",
            "Conditioning",
            "Packaging"
        };

            // Start the orchestration and pass the recipients as input
            string instanceId = await starter.StartNewAsync("BeerFabricationOrchestration", recipients);


            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("BeerFabricationOrchestration")]
        public static async Task<string> BeerFabricationOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            // Get the recipients (brewery stages) from the input
            var recipients = context.GetInput<List<string>>();

            // Fan-out: Send messages to all recipients asynchronously
            var tasks = new List<Task<string>>();
            foreach (var recipient in recipients)
            {
                tasks.Add(context.CallActivityAsync<string>("ProcessStageActivity", recipient));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Final message after aggregation
            var responses = tasks.Select(t => t.Result);
            var aggregatedResponse = string.Join(", ", responses);

            log.LogInformation($"Result '{aggregatedResponse}'");

            return aggregatedResponse;
        }

        [FunctionName("ProcessStageActivity")]
        public static async Task<string> ProcessStageActivity(
            [ActivityTrigger] string stage,
            ILogger log)
        {
            // Simulating processing time
            await Task.Delay(2000);

            log.LogInformation($"Stage '{stage}' completed");

            // Processing logic
            return $"Stage '{stage}' completed";


        }
    }
}