using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace EventGridDemo.Function
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Event Grid HTTP trigger function processed a request.");

            // Retrieve the contents of the request and deserialize it into a grid event object
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            var gridEvent = JsonConvert.DeserializeObject<List<GridEvent<Dictionary<string, string>>>>(requestBody)?.SingleOrDefault();
            if (gridEvent == null)
            {
                return new BadRequestObjectResult("Missing event details");
            }

            // Check the header to identify the type of request from Event Grid. 
            // A subscription validation request must echo back the validation code.
            var gridEventType = req.Headers["Aeg-Event-Type"];
            if (gridEventType == "SubscriptionValidation")
            {
                var code = gridEvent.Data["validationCode"];

                return (ActionResult)new OkObjectResult(new { validationResponse = code });
            }
            else if (gridEventType == "Notification")
            {
                // TODO: place message into a queue for further processing.
                log.LogInformation($"New employee received: {requestBody}");
                return (ActionResult)new OkObjectResult("TODO");
            }
            else
            {
                return new BadRequestObjectResult("Unknown request type");
            }
        }
    }

    public class GridEvent<T> where T : class
    {
        public string Id { get; set; }
        public string EventType { get; set; }
        public string Subject { get; set; }
        public DateTime EventTime { get; set; }
        public T Data { get; set; }
        public string Topic { get; set; }
    }
}
