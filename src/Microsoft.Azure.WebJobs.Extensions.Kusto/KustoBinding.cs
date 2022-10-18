// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Kusto
{
    /// <summary>
    /// Runs on every request and passes the function context (e.g. Http request and host configuration) to a value provider.
    /// </summary>
    public class KustoBinding : IBinding
    {
        public bool FromAttribute => throw new NotImplementedException();

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
        {
            throw new NotImplementedException();
        }

        public Task<IValueProvider> BindAsync(BindingContext context)
        {
            throw new NotImplementedException();
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            throw new NotImplementedException();
        }
    }
}
