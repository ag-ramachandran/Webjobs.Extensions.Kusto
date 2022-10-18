// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Kusto
{
    public class KustoBindingProvider : IBindingProvider
    {
        private readonly ILogger _logger;

        public KustoBindingProvider(ILogger logger)
        {
            _logger = logger;
        }

        public Task<IBinding> TryCreateAsync(BindingProviderContext context)
        {
            return Task.FromResult(TryCreate(context));
        }

        private IBinding TryCreate(BindingProviderContext context)
        {
            // TODO
            return null;
        }
    }
}
