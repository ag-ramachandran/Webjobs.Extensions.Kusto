// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.



using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.TestCommon;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests
{
    public class KustoBindingE2ETests : IDisposable
    {
        private const string DatabaseName = "TestDatabase";
        private const string TableName = "TestTable";
        private static readonly IConfiguration _baseConfig = KustoTestHelper.BuildConfiguration();
        private readonly ILoggerFactory _loggerFactory = new LoggerFactory();
        private readonly TestLoggerProvider _loggerProvider = new TestLoggerProvider();
        public KustoBindingE2ETests()
        {
            this._loggerFactory.AddProvider(this._loggerProvider);
        }

        [Fact]
        public async Task OutputBindings()
        {
            Assert.NotNull(_baseConfig);
            // Given
            var mockIngestionClient = new Mock<IKustoIngestClient>();
            var mockIngestionResult = new Mock<IKustoIngestionResult>();
            var ingestionStatus = new IngestionStatus()
            {
                Status = Status.Succeeded,
            };
            var actualIngestDataStreams = new List<Stream>();
            var actualKustoIngestionProps = new List<KustoIngestionProperties>();
            var actualStreamSourceOptions = new List<StreamSourceOptions>();
            // Ingestion results
            mockIngestionResult.Setup(m => m.GetIngestionStatusCollection()).Returns(Enumerable.Repeat(ingestionStatus, 1));
            mockIngestionResult.Setup(m => m.GetIngestionStatusBySourceId(It.IsAny<Guid>())).Returns(ingestionStatus);
            // set the ingestion behavior
            mockIngestionClient.Setup(m => m.IngestFromStreamAsync(
                Capture.In(actualIngestDataStreams),
                Capture.In(actualKustoIngestionProps),
                Capture.In(actualStreamSourceOptions))).ReturnsAsync(mockIngestionResult.Object);

            var ingestClientFactory = new MockManagedStreamingClientFactory(mockIngestionClient.Object);
            await this.RunTestAsync(typeof(KustoEndToEndFunctions), ingestClientFactory, "Outputs");
            KustoIngestionProperties actualKustoIngestionProp = actualKustoIngestionProps.First();
            Assert.Equal(TableName, actualKustoIngestionProp.TableName);
            Assert.Equal(DatabaseName, actualKustoIngestionProp.DatabaseName);
            //            Assert.Equal("multijson", actualKustoIngestionProp.Format.ToString());
            //            List<Item> actualResultItems = KustoTestHelper.LoadItems(actualIngestDataStreams.First());
            //            Assert.Equal("Outputs", this._loggerProvider.GetAllUserLogMessages().Single().FormattedMessage);

            mockIngestionClient.VerifyAll();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._loggerFactory != null)
                {
                    this._loggerFactory.Dispose();
                }
            }
        }

        private async Task RunTestAsync(Type testType, IKustoClientFactory kustoIngestClientFactory, string testName, object argument = null, bool includeDefaultConnectionString = true)
        {
            var locator = new ExplicitTypeLocator(testType);
            var arguments = new Dictionary<string, object>
            {
                { "triggerData", argument }
            };
            IHost host = new HostBuilder()
                .ConfigureWebJobs(builder =>
                {
                    builder.AddBuiltInBindings();
                    builder.AddKusto();
                })
                .ConfigureAppConfiguration(c =>
                {
                    c.Sources.Clear();
                    if (includeDefaultConnectionString)
                    {
                        c.AddInMemoryCollection(new Dictionary<string, string>
                        {
                            { ConnectionStringNames.Storage, "UseDevelopmentStorage=true" },
                            { KustoConstants.DefaultConnectionStringName, KustoTestHelper.DefaultTestConnectionString }
                        });
                    }
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ITypeLocator>(locator);
                    services.AddSingleton(kustoIngestClientFactory);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(this._loggerProvider);
                })
                .Build();

            await host.StartAsync();
            await host.GetJobHost().CallAsync(testType.GetMethod(testName), arguments);
            await host.StopAsync();
        }
        private class KustoEndToEndFunctions
        {
            [NoAutomaticTrigger]
            public static void Outputs(
                [Kusto(database: DatabaseName, tableName: TableName, Connection = KustoConstants.DefaultConnectionStringName)] out object newItem,
                [Kusto(database: DatabaseName, tableName: TableName, Connection = KustoConstants.DefaultConnectionStringName)] out string newItemString,
                [Kusto(database: DatabaseName, tableName: TableName, Connection = KustoConstants.DefaultConnectionStringName)] out object[] arrayItem,
                [Kusto(database: DatabaseName, tableName: TableName, Connection = KustoConstants.DefaultConnectionStringName)] IAsyncCollector<object> asyncCollector,
                [Kusto(database: DatabaseName, tableName: TableName, Connection = KustoConstants.DefaultConnectionStringName)] ICollector<object> collector,
                TraceWriter trace)
            {
                newItem = new { };
                newItemString = "{}";
                arrayItem = new Item[]
                {
                    new Item(),
                    new Item()
                };
                Task.WaitAll(new[]
                {
                    asyncCollector.AddAsync(new { }),
                    asyncCollector.AddAsync(new { })
                });
                collector.Add(new { });
                collector.Add(new { });
                trace.Warning("Outputs");
            }
        }
    }
}
