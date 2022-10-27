// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Kusto
{
    internal class KustoAsyncCollectorBuilder<T> : IConverter<KustoAttribute, IAsyncCollector<T>>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public KustoAsyncCollectorBuilder(IConfiguration configuration, ILogger logger)
        {
            this._configuration = configuration;
            this._logger = logger;
        }

        IAsyncCollector<T> IConverter<KustoAttribute, IAsyncCollector<T>>.Convert(KustoAttribute attribute)
        {
            return new KustoAsyncCollector<T>(this._configuration, attribute, this._logger);
        }
    }
}