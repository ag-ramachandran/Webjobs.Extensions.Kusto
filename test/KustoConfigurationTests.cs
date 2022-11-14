// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests
{
    public class KustoConfigurationTests
    {
        private static readonly IConfiguration _baseConfig = KustoTestHelper.BuildConfiguration();
        [Fact]
        public void ConfigurationCachesClients()
        {
            // Given
            KustoExtensionConfigProvider kustoExtensionConfigProvider = InitializeCreatesClients();
            var attribute = new KustoAttribute("unittestdb", "Items");
            // When
            _ = kustoExtensionConfigProvider.CreateContext(attribute);
            _ = kustoExtensionConfigProvider.CreateContext(attribute);
            var asyncBuilder = new KustoAsyncCollectorBuilder<KustoOpenType>(kustoExtensionConfigProvider);
            // Then
            Assert.NotNull(asyncBuilder);
            Assert.Single(kustoExtensionConfigProvider.IngestClientCache);
        }
        private static KustoExtensionConfigProvider InitializeCreatesClients()
        {
            var nameResolver = new KustoNameResolver();
            var kustoExtensionConfigProvider = new KustoExtensionConfigProvider(_baseConfig, NullLoggerFactory.Instance);
            kustoExtensionConfigProvider.IngestClientCache.Clear();
            ExtensionConfigContext context = KustoTestHelper.CreateExtensionConfigContext(nameResolver);
            // Should Initialize and register the validators and should not throw an error
            kustoExtensionConfigProvider.Initialize(context);
            // Then
            Assert.NotNull(kustoExtensionConfigProvider);
            return kustoExtensionConfigProvider;
        }
    }
}