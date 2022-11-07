// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Kusto;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Kusto
{
    internal class KustoAsyncCollectorBuilder<T> : IConverter<KustoAttribute, IAsyncCollector<T>>
    {
        private readonly KustoExtensionConfigProvider _configProvider;
        private readonly ILogger _logger;

        public KustoAsyncCollectorBuilder(KustoExtensionConfigProvider configProvider, ILogger logger)
        {
            this._configProvider = configProvider;
            this._logger = logger;
        }

        IAsyncCollector<T> IConverter<KustoAttribute, IAsyncCollector<T>>.Convert(KustoAttribute attribute)
        {
            KustoContext context = this._configProvider.CreateContext(attribute);
            return new KustoAsyncCollector<T>(this._logger, context);
        }
    }
}