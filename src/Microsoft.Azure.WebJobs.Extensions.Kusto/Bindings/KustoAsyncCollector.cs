// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Kusto.Config;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Kusto.Bindings
{

    public class KustoAsyncCollector<KustoEntity> : IAsyncCollector<KustoEntity>
    {
        private readonly KustoAttribute _kustoAttribute;
        private readonly KustoExtensionConfigProvider _kustoExtensionConfigProvider;
        private List<KustoEntity> kustoEntities;

        public Task AddAsync(KustoEntity item, CancellationToken cancellationToken = default)
        {
            if (item == null) 
            {
                throw new ArgumentNullException(nameof(item));
            }
            else 
            {
                kustoEntities.Add(item);
            }
            throw new NotImplementedException();
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
