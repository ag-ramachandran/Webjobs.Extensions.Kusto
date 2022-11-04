// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Ingest;

namespace Microsoft.Azure.WebJobs.Kusto
{
    internal static class KustoBindingUtilities
    {
        private static readonly string ingestPrefix = "ingest-";
        private static readonly string protocolSuffix = "://";
        /// <summary>
        /// Builds a connection using the connection string attached to the app setting with name ConnectionString.
        /// </summary>
        /// <param name="kustoAttribute">Kusto attribute for the ingestion.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if configuration is null.
        /// </exception>
        /// <returns>The built connection.</returns>
        public static IKustoIngestClient CreateIngestClient(KustoAttribute kustoAttribute)
        {
            string envConnectionString = Environment.GetEnvironmentVariable("KustoConnectionString");
            string engineConnectionString = kustoAttribute.Connection;

            if (envConnectionString == null && engineConnectionString == null)
            {
                throw new ArgumentNullException(nameof(kustoAttribute), "Connection string attribute is empty." +
                    "Please pass KustoConnectionString through config or through the env-var KustoConnectionString");
            }

            if (engineConnectionString == null)
            {
                engineConnectionString = envConnectionString;
            }
            var engineKcsb = new KustoConnectionStringBuilder(engineConnectionString);
            /*
                We expect minimal input from the user.The end user can just pass a connection string, we need to decipher the DM
                ingest endpoint as well from this. Both the engine and DM endpoint are needed for the managed ingest to happen
             */
            string dmConnectionStringEndpoint = engineKcsb.Hostname.Contains(ingestPrefix) ? engineConnectionString : engineConnectionString.ReplaceFirstOccurrence(protocolSuffix, protocolSuffix + ingestPrefix);
            var dmKcsb = new KustoConnectionStringBuilder(dmConnectionStringEndpoint);
            // Create a managed ingest connection            
            IKustoIngestClient kustoStreamingClient = KustoIngestFactory.CreateManagedStreamingIngestClient(engineKcsb, dmKcsb);
            return kustoStreamingClient;
        }

        public static Stream StreamFromString(string dataToIngest)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(dataToIngest);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
