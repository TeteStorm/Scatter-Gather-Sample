using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.File.Protocol;
using Newtonsoft.Json;

namespace Scatter_Gather_Sample
{
    public static class RequestRideFunction
    {
        [FunctionName("RequestRide")]
        public static async Task<IActionResult> RequestRide(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient orchestrationClient)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<RideRequest>(requestBody);

            var instanceId = await orchestrationClient.StartNewAsync("OrchestrateRide", request);

            return orchestrationClient.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("OrchestrateRide")]
        public static async Task OrchestrateRide(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var request = context.GetInput<RideRequest>();
            var drivers = await context.CallActivityAsync<List<Driver>>("GetAvailableDrivers", null);

            var tasks = new List<Task>();

            foreach (var driver in drivers)
            {
                tasks.Add(context.CallActivityAsync("NotifyDriver", driver.Id));
            }

            await Task.WhenAny(tasks);

            var firstDriver = drivers.First();

            await context.CallActivityAsync("DispatchDriver", firstDriver);
            var driverConfirmation = await context.WaitForExternalEvent<bool>("DriverConfirmation");

            if (driverConfirmation)
            {
                await context.CallActivityAsync("ProcessPayment", request.PaymentDetails);
                await context.CallActivityAsync("NotifyUser", "Your ride has been confirmed!");
            }
            else
            {
                await context.CallActivityAsync("NotifyUser", "No drivers available at the moment. Please try again later.");
            }
        }

        [FunctionName("GetAvailableDrivers")]
        public static List<Driver> GetAvailableDrivers([ActivityTrigger] string input)
        {
            // Logic to fetch available drivers from a data source (e.g., database)
            // Return a list of available drivers
            List<Driver> availableDrivers = new()
            {
                // Adding drivers to the list
                new Driver("1", "John Doe"),
                new Driver("2", "Jane Smith"),
                new Driver("3", "Mike Johnson")
            };

            return availableDrivers;
        }

        [FunctionName("NotifyDriver")]
        public static void NotifyDriver([ActivityTrigger] string driverId)
        {
            // Logic to send a notification to the driver
        }

        [FunctionName("DispatchDriver")]
        public static void DispatchDriver([ActivityTrigger] Driver driver)
        {
            // Logic to dispatch the driver to the user's location
        }

        [FunctionName("ProcessPayment")]
        public static void ProcessPayment([ActivityTrigger] PaymentDetails payment)
        {
            // Logic to process the payment
        }

        [FunctionName("NotifyUser")]
        public static void NotifyUser([ActivityTrigger] string message)
        {
            // Logic to send a notification to the user
        }

        public record RideRequest(string UserId, string PickupLocation, string Destination, PaymentDetails PaymentDetails);

        public record Driver(string Id, string Name);

        public record PaymentDetails(string CardNumber, string ExpirationDate, string CVV);

        public static async Task Run(
            [QueueTrigger("accept-ride")] string instanceId,
            [DurableClient] IDurableOrchestrationClient client)
        {
            await client.RaiseEventAsync(instanceId, "DriverConfirmation", true);
        }
    }
}