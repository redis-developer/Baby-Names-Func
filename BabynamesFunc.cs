using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Reflection;

namespace Redis.Functions
{
    public static class BabynamesFunc
    {
        private static Lazy<ConnectionMultiplexer> lazyConnection = CreateConnection();
        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }
        
        private static Lazy<ConnectionMultiplexer> CreateConnection()
        {
            return new Lazy<ConnectionMultiplexer>(() =>
            {
            var HOST_NAME = System.Environment.GetEnvironmentVariable("REDIS_HOST_NAME");
            var PASSWORD = System.Environment.GetEnvironmentVariable("REDIS_PASSWORD");
            return ConnectionMultiplexer.Connect($"{HOST_NAME},password={PASSWORD}");
            });
        }

        [FunctionName("CountBabyNames")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "getCount")] HttpRequest req,
            ILogger log)
        {
            string name = req.Query["name"];
            var db = Connection.GetDatabase();

            var result = await db.ExecuteAsync("CMS.QUERY", $"baby-names", name);

            var fi = result.GetType().GetField("_value", BindingFlags.NonPublic|BindingFlags.Instance);
            var res = (RedisResult[])fi.GetValue(result);
            var count = int.Parse(res[0].ToString());

            var responseMessage = $"{{count:{count}}}";
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("IncrementBabyName")]
        public static async Task<IActionResult> Increment(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "increment")] HttpRequest req,
            ILogger log)
        {
            string name = req.Query["name"];
            var db = Connection.GetDatabase();            

            await db.ExecuteAsync("CMS.INCRBY", "baby-names", name, 1);

            return new OkObjectResult("OK");
        }
    }
}
