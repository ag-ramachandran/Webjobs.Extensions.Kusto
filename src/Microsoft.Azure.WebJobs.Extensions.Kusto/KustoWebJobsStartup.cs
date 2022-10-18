// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Azure.WebJobs.Kusto;

[assembly: WebJobsStartup(typeof(KustoWebJobsStartup))]

namespace Microsoft.Azure.WebJobs.Kusto
{
    public class KustoWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddKusto();
        }
    }
}
