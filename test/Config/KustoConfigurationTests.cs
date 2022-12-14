// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Config
{
    public class KustoConfigurationTests : IDisposable
    {
        private static readonly IConfiguration _baseConfig = KustoTestHelper.BuildConfiguration();
        private readonly ILogger _logger = new LoggerFactory().CreateLogger<KustoConfigurationTests>();

        [Fact]
        public void ConfigurationCachesClients()
        {
            // Given
            KustoExtensionConfigProvider kustoExtensionConfigProvider = InitializeCreatesClients();
            var attribute = new KustoAttribute("unittestdb")
            {
                TableName = "Items"
            };
            // When
            _ = kustoExtensionConfigProvider.CreateIngestionContext(attribute);
            _ = kustoExtensionConfigProvider.CreateIngestionContext(attribute);
            var asyncBuilder = new KustoAsyncCollectorBuilder<KustoOpenType>(this._logger, kustoExtensionConfigProvider);
            // Then
            Assert.NotNull(asyncBuilder);
            Assert.Single(kustoExtensionConfigProvider.IngestClientCache);
        }
        private static KustoExtensionConfigProvider InitializeCreatesClients()
        {
            var nameResolver = new KustoNameResolver();
            var mockIngestionClient = new Mock<IKustoIngestClient>(MockBehavior.Strict);
            var ingestClientFactory = new MockClientFactory(mockIngestionClient.Object);

            var kustoExtensionConfigProvider = new KustoExtensionConfigProvider(_baseConfig, NullLoggerFactory.Instance, ingestClientFactory);
            kustoExtensionConfigProvider.IngestClientCache.Clear();
            ExtensionConfigContext context = KustoTestHelper.CreateExtensionConfigContext(nameResolver);
            // Should Initialize and register the validators and should not throw an error
            kustoExtensionConfigProvider.Initialize(context);
            // Then
            Assert.NotNull(kustoExtensionConfigProvider);
            return kustoExtensionConfigProvider;
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}