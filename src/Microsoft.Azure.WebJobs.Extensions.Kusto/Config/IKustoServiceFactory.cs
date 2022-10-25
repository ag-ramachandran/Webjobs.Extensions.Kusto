// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Kusto.Ingest;

namespace Microsoft.Azure.WebJobs.Kusto.Config
{
    public interface IKustoServiceFactory
    {
        IKustoIngestClient CreateService(string connectionString);
    }
}
