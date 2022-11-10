// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs.Kusto;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests
{
    public class KustoConfigurationTests
    {

        private static readonly IConfiguration _baseConfig = KustoTestHelper.BuildConfiguration();

        [Fact]
        public void ConfigurationCachesClients()
        {
            // Given
            var kustoExtensionConfigProvider = new KustoExtensionConfigProvider(_baseConfig, NullLoggerFactory.Instance);
            var attribute = new KustoAttribute("unittestdb", "Items");
            // When
            _ = kustoExtensionConfigProvider.CreateContext(attribute);
            _ = kustoExtensionConfigProvider.CreateContext(attribute);
            var asyncBuilder = new KustoAsyncCollectorBuilder<KustoOpenType>(kustoExtensionConfigProvider);
            // Then
            Assert.NotNull(asyncBuilder);
            Assert.Single(kustoExtensionConfigProvider.IngestClientCache);
        }
    }
}