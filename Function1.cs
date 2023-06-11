using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Scatter_Gather_Sample
{
    public static class BeerProductionOrchestrator
    {

        [FunctionName("BeerProduction_HttpStart")]
        public static async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
        [DurableClient] IDurableOrchestrationClient orchestrationClient,
        ILogger log)
        {
            int beerQuantity;

            if (!int.TryParse(req.Query["beerQuantity"], out beerQuantity))
            {
                return new BadRequestObjectResult("Invalid beerQuantity parameter");
            }

            var data = new BeerProductionInput { BeerQuantity = beerQuantity };

            string instanceId = await orchestrationClient.StartNewAsync("BeerProductionOrchestrator", data);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return orchestrationClient.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("BeerProductionOrchestrator")]
        public static async Task<List<BeerBatch>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var productionInput = context.GetInput<BeerProductionInput>();

            // Scatter phase
            var fermentationTasks = new List<Task<BeerBatch>>();
            var mashingTasks = new List<Task<BeerBatch>>();
            var hoppingTasks = new List<Task<BeerBatch>>();

            for (int i = 0; i < productionInput.BeerQuantity; i++)
            {
                fermentationTasks.Add(context.CallActivityAsync<BeerBatch>("FermentationFunction", i));
                mashingTasks.Add(context.CallActivityAsync<BeerBatch>("MashingFunction", i));
                hoppingTasks.Add(context.CallActivityAsync<BeerBatch>("HoppingFunction", i));
            }
            var tasks = fermentationTasks.Concat(mashingTasks).Concat(hoppingTasks);
            await Task.WhenAll(fermentationTasks.Concat(mashingTasks).Concat(hoppingTasks));

            // Gather phase
            var fermentationResults = fermentationTasks.Select(t => t.Result).ToList();
            var mashingResults = mashingTasks.Select(t => t.Result).ToList();
            var hoppingResults = hoppingTasks.Select(t => t.Result).ToList();

            // Further processing and packaging
            // ...

            // Final message after aggregation
            var responses = tasks.Select(t => t.Result.ToString());
            var aggregatedResponse = string.Join(", ", responses);

            log.LogInformation($"Result '{aggregatedResponse}'");

            return fermentationResults; // Or return any relevant result
        }

        [FunctionName("FermentationFunction")]
        public static BeerBatch FermentationFunction(
            [ActivityTrigger] int batchNumber)
        {
            // Perform fermentation for the given batchNumber
            // ...

            return new BeerBatch(batchNumber, "Fermented");
        }

        [FunctionName("MashingFunction")]
        public static BeerBatch MashingFunction(
            [ActivityTrigger] int batchNumber)
        {
            // Perform mashing for the given batchNumber
            // ...

            return new BeerBatch(batchNumber, "Mashed");
        }

        [FunctionName("HoppingFunction")]
        public static BeerBatch HoppingFunction(
            [ActivityTrigger] int batchNumber)
        {
            // Perform hopping for the given batchNumber
            // ...

            return new BeerBatch(batchNumber, "Hopped");
        }
    }

    public record BeerBatch
    {
        public int BatchNumber { get; set; }
        public string Status { get; set; }

        public BeerBatch(int batchNumber, string status)
        {
            BatchNumber = batchNumber;
            Status = status;
        }

        public sealed override string ToString()
        {
            return $"Beer Batch: {BatchNumber} Status: {Status}";
        }
    }

    public record BeerProductionInput
    {
        public int BeerQuantity { get; set; }
    }
}