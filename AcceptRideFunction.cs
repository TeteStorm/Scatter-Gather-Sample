using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

public static class SimulateDriverConfirmation
{



[FunctionName("AcceptRide")]
public static async Task<IActionResult> AcceptRide(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
    [DurableClient] IDurableClient client)
{
    string instanceId = req.Query["instanceId"];
    bool confirmation = true; // Set the confirmation value based on your scenario

    await client.RaiseEventAsync(instanceId, "DriverConfirmation", confirmation);

    return new OkResult();
}
}
