// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace Microsoft.Azure.WebJobs.Kusto.Config
{
    /// <summary>
    /// Defines the configuration options for the Kusto binding.
    /// </summary>
    [Extension("Kusto")]
    public class KustoExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IKustoServiceFactory _kustoServiceFactory;
        private readonly ILoggerFactory _loggerFactory;

        public KustoExtensionConfigProvider(IKustoServiceFactory kustoServiceFactory, ILoggerFactory loggerFactory)
        {
            this._kustoServiceFactory = kustoServiceFactory;
            this._loggerFactory = loggerFactory;
        }

        // If we have a managed client cache that can be reused 
        internal ConcurrentDictionary<string, IKustoIngestClient> ClientCache { get; } = new ConcurrentDictionary<string, IKustoIngestClient>();

        /// <inheritdoc />
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            // Apply ValidateConnection to all on this rule. 
            var rule = context.AddBindingRule<KustoAttribute>();
            rule.AddValidator(ValidateConnection);
            
            
            // Set up the output binding 
            rule.BindToCollector<string>(null, this);
            throw new NotImplementedException();
        }

        internal void ValidateConnection(KustoAttribute kustoAttribute, Type paramType)
        {
            if (string.IsNullOrEmpty(kustoAttribute.Connection))
            {
                string attributeProperty = $"{nameof(KustoAttribute)}.{nameof(KustoAttribute.Connection)}";
                throw new InvalidOperationException(
                    $"The {attributeProperty} property cannot be an empty value.");
            }
            // TODO decide if the attributes table can be empty ?
        }

        // Cache the client and use
        internal IKustoIngestClient GetService(string dmConnectionString)
        {
            return ClientCache.GetOrAdd(dmConnectionString, (c) => _kustoServiceFactory.CreateService(dmConnectionString));
        }
    }
}
