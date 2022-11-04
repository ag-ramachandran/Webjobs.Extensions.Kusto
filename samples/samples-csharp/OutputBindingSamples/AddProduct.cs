// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    public static class AddProduct
    {
        [FunctionName("AddProduct")]
        public static void Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "addproduct")]
            HttpRequest req, ILogger log,
            [Kusto(database:"sdktestsdb" ,
            tableName:"Products" ,
            Connection = "KustoConnectionString")] IAsyncCollector<Product> collector)
        {
            log.LogInformation($"C# function started");
            for (int i = 0; i < 10; i++)
            {
                collector.AddAsync(new Product()
                {
                    Name = req.Query["name"],
                    ProductID = int.Parse(req.Query["productId"]),
                    Cost = int.Parse(req.Query["cost"])
                });
            }
        }
    }
}
