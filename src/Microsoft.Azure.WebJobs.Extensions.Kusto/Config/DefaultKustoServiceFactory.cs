// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Ingest;
using System;

namespace Microsoft.Azure.WebJobs.Kusto.Config
{
    internal class DefaultKustoServiceFactory : IKustoServiceFactory
    {
        public IKustoIngestClient CreateService(string dmConnectionString)
        {
            if (string.IsNullOrWhiteSpace(dmConnectionString))
            {
                throw new ArgumentNullException($"ConnectionString cannot be null.");
            }
            // Uses connection string based auth
            var engineConnectionString = dmConnectionString.ReplaceFirstOccurrence("ingest-", "");
            var dmKcsb = new KustoConnectionStringBuilder(dmConnectionString);
            var engineKcsb = new KustoConnectionStringBuilder(engineConnectionString);
            // Create a managed ingest connection            
            return KustoIngestFactory.CreateManagedStreamingIngestClient(engineKcsb, dmKcsb);
        }
    }
}
