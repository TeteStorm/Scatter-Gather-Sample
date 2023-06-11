//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Extensions.Http;
//using Microsoft.Azure.WebJobs.Extensions.Storage;
//using Microsoft.Extensions.Logging;
//using Microsoft.WindowsAzure.Storage.Queue;

//public static class BeerFabricationFunction
//{
//    [FunctionName("BeerFabrication")]
//    public static async Task<IActionResult> Run(
//        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
//        [Queue("beer-stages")] IAsyncCollector<string> queue,
//        [Queue("beer-responses")] CloudQueue responseQueue,
//        ILogger log)
//    {
//        // Message to broadcast
//        var message = "Beer fabrication in progress...";

//        // Get the list of recipients (brewery stages)
//        var recipients = new List<string>
//        {
//            "Mashing",
//            "Boiling",
//            "Fermenting",
//            "Conditioning",
//            "Packaging"
//        };

//        // Fan-out: Send the message to all recipients asynchronously
//        var tasks = new List<Task>();
//        foreach (var recipient in recipients)
//        {
//            await queue.AddAsync(recipient);
//        }

//        // Fan-in: Wait for all responses to be processed
//        while (tasks.Count < recipients.Count)
//        {
//            var response = await responseQueue.GetMessageAsync();
//            if (response != null)
//            {
//                tasks.Add(ProcessStageResponse(response.AsString));
//                await responseQueue.DeleteMessageAsync(response);
//            }
//        }

//        // Wait for all tasks to complete
//        await Task.WhenAll(tasks);


//        // Final message after aggregation
//        var responses = tasks.to(t => t.GetAwaiter().GetResult());
//        var aggregatedResponse = string.Join(", ", responses);
//        return new OkObjectResult(aggregatedResponse);
//    }

//    [FunctionName("ProcessStage")]
//    public static async Task ProcessStage(
//        [QueueTrigger("beer-stages")] string stage,
//        [Queue("beer-responses")] IAsyncCollector<string> responseQueue,
//        ILogger log)
//    {
//        // Simulating processing time
//        await Task.Delay(2000);

//        // Processing logic
//        var response = $"Stage '{stage}' completed";
//        await responseQueue.AddAsync(response);
//    }

//    private static async Task<string> ProcessStageResponse(string response)
//    {
//        // Additional processing on the response if needed
//        await Task.Delay(0);

//        return response;
//    }
//}
