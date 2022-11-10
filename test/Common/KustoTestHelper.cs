// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common
{
    internal class KustoTestHelper
    {
        public static KustoContext CreateContext(IKustoIngestClient ingestClientService, string database = "unittest", string tableName = "items", string mappingRef = "", string dataFormat = "json")
        {
            var attribute = new KustoAttribute(database, tableName, mappingRef, dataFormat);
            return new KustoContext
            {
                IngestService = ingestClientService,
                ResolvedAttribute = attribute
            };
        }

        public static List<Item> LoadItems(Stream stream)
        {
            var serializer = new JsonSerializer();
            var streamReader = new StreamReader(stream, new UTF8Encoding());
            var result = new List<Item>();
            using (var reader = new JsonTextReader(streamReader))
            {
                reader.CloseInput = false;
                reader.SupportMultipleContent = true;
                while (reader.Read())
                {
                    result.Add(serializer.Deserialize<Item>(reader));
                }
            }
            return result;
        }

        public static IConfiguration BuildConfiguration()
        {
            var initialConfig = new List<KeyValuePair<string, string>>()
            {
                KeyValuePair.Create(KustoConstants.DefaultConnectionStringName, "Data Source=https://kustofunctionscluster.eastus.dev.kusto.windows.net;Database=unittestdb;Fed=True;AppClientId=11111111-xxxx-xxxx-xxxx-111111111111;AppKey=appKey~appKey;Authority Id=1111111-1111-1111-1111-111111111111"),
                KeyValuePair.Create("Attribute", "database=unittestdb;tableName=Items;Connection=KustoConnectionString")
            };
            return new ConfigurationBuilder().AddInMemoryCollection(initialConfig).AddEnvironmentVariables().Build();
        }
    }
}
