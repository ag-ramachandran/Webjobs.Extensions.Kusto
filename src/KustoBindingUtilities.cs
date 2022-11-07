// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Ingest;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs.Kusto
{
    internal static class KustoBindingUtilities
    {
        internal static ConcurrentDictionary<string, IKustoIngestClient> IngestClientCache { get; } = new ConcurrentDictionary<string, IKustoIngestClient>();
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
        public static IKustoIngestClient GetIngestClient(KustoAttribute kustoAttribute, IConfiguration configuration)
        {
            string engineConnectionString = GetConnectionString(kustoAttribute.Connection, configuration);
            string cacheKey = BuildCacheKey(engineConnectionString);
            return IngestClientCache.GetOrAdd(cacheKey, (c) => CreateIngestClient(engineConnectionString));
        }

        public static IKustoIngestClient CreateIngestClient(string engineConnectionString)
        {
            var engineKcsb = new KustoConnectionStringBuilder(engineConnectionString);
            /*
                We expect minimal input from the user.The end user can just pass a connection string, we need to decipher the DM
                ingest endpoint as well from this. Both the engine and DM endpoint are needed for the managed ingest to happen
             */
            string dmConnectionStringEndpoint = engineKcsb.Hostname.Contains(ingestPrefix) ? engineConnectionString : engineConnectionString.ReplaceFirstOccurrence(protocolSuffix, protocolSuffix + ingestPrefix);
            var dmKcsb = new KustoConnectionStringBuilder(dmConnectionStringEndpoint);
            // Create a managed ingest connection            
            return KustoIngestFactory.CreateManagedStreamingIngestClient(engineKcsb, dmKcsb);

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

        private static string GetConnectionString(string connectionStringSetting, IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(connectionStringSetting))
            {
                throw new ArgumentException("Must specify ConnectionString, which should refer to the name of an app setting that " +
                    "contains a Kusto connection string");
            }
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            return configuration.GetConnectionStringOrSetting(connectionStringSetting);
        }

        internal static string BuildCacheKey(string connectionString)
        {
            return $"C-{connectionString.GetHashCode()}";
        }
    }
}
