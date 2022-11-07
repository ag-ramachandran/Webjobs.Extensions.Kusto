﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    /// <summary>
    /// Exposes Kusto bindings.
    /// </summary>
    [Extension("Kusto")]
    public class KustoExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="KustoBindingConfigProvider/>"/> class.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either parameter is null.
        /// </exception>
        public KustoExtensionConfigProvider(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Initializes the Kusto binding rules.
        /// </summary>
        /// <param name="context"> The config context.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if context is null.
        /// </exception>
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            ILogger logger = this._loggerFactory.CreateLogger(LogCategories.Bindings);
#pragma warning disable CS0618 // Cannot use var. FluentBindingRule is in Beta
            FluentBindingRule<KustoAttribute> rule = context.AddBindingRule<KustoAttribute>();
            // Validate the attributes we have
            rule.AddValidator(this.ValidateConnection);
            // Bind to the types
            rule.BindToCollector<KustoOpenType>(typeof(KustoAsyncCollectorBuilder<>), this._configuration, logger);
        }
        internal void ValidateConnection(KustoAttribute attribute, Type paramType)
        {
            if (string.IsNullOrEmpty(attribute.Connection))
            {
                string attributeProperty = $"{nameof(KustoAttribute)}.{nameof(KustoAttribute.Connection)}";
                throw new InvalidOperationException(
                    $"The {attributeProperty} property cannot be an empty value.");
            }

            if (string.IsNullOrEmpty(attribute.Database))
            {
                string attributeProperty = $"{nameof(KustoAttribute)}.{nameof(KustoAttribute.Database)}";
                throw new InvalidOperationException(
                    $"The {attributeProperty} property cannot be an empty value.");
            }

            if (string.IsNullOrEmpty(attribute.TableName))
            {
                string attributeProperty = $"{nameof(KustoAttribute)}.{nameof(KustoAttribute.TableName)}";
                throw new InvalidOperationException(
                    $"The {attributeProperty} property cannot be an empty value.");
            }
        }
    }
    /// <summary>
    /// Wrapper around OpenType to receive data correctly from output bindings (not as byte[])
    /// This can be used for general "T --> JObject" bindings. 
    /// The exact definition here comes from the WebJobs v1.0 Queue binding.
    /// refer https://github.com/Azure/azure-webjobs-sdk/blob/dev/src/Microsoft.Azure.WebJobs.Host/Bindings/OpenType.cs#L390.
    /// </summary>
    internal class KustoOpenType : OpenType.Poco
    {
        // return true when type is an "System.Object" to enable Object binding.
        public override bool IsMatch(Type type, OpenTypeMatchContext context)
        {
            if (type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return false;
            }

            if (type.FullName == "System.Object")
            {
                return true;
            }
            return base.IsMatch(type, context);
        }
    }
}