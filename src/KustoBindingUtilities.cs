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
        /// <summary>
        /// Builds a connection using the connection string attached to the app setting with name ConnectionString.
        /// </summary>
        /// <param name="kustoAttribute">Kusto attribute for the ingestion.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if ConnectionStringSetting is empty or null.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if configuration is null.
        /// </exception>
        /// <returns>The built connection.</returns>
        public static IKustoIngestClient CreateIngestClient(KustoAttribute kustoAttribute)
        {
            string dmConnectionString = kustoAttribute.Connection;
            string engineConnectionString = dmConnectionString.ReplaceFirstOccurrence("ingest-", "");
            var dmKcsb = new KustoConnectionStringBuilder(dmConnectionString);
            var engineKcsb = new KustoConnectionStringBuilder(engineConnectionString);
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
    }
}
