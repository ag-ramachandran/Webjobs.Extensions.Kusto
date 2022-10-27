// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Sql;
using System;

namespace Microsoft.Azure.WebJobs.Kusto
{
    public static class KustoBindingExtension
    {
        public static IWebJobsBuilder AddKusto(this IWebJobsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            builder.AddExtension<KustoBindingConfigProvider>();
            return builder;
        }
    }
}
