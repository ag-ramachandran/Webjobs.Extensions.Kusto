// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    public static class AddProduct
    {
        [FunctionName("AddProduct")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "addproduct")]
            HttpRequest req, ILogger log,
            [Kusto("Products",Connection = "KustoConnectionString")]
            out Product product)
        {
            log.LogInformation($"C# function started");
            product = new Product
            {
                Name = req.Query["name"],
                ProductID = int.Parse(req.Query["productId"]),
                Cost = int.Parse(req.Query["cost"])
            };
            return new CreatedResult($"/api/product", product.Name);
        }
    }
}
