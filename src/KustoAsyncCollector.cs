// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Extensions.Kusto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Kusto
{
    internal class KustoAsyncCollector<T> : IAsyncCollector<T>, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly KustoAttribute _attribute;
        private readonly ILogger _logger;
        private readonly IKustoIngestClient _kustoIngestClient;
        private readonly List<T> _rows = new List<T>();
        private readonly SemaphoreSlim _rowLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="KustoAsyncCollector<typeparamref name="T"/>"/> class.
        /// </summary>
        /// <param name="connection">
        /// Contains the connection string used to establish connection to Kusto and ingest data.
        /// </param>
        /// <param name="attribute">
        /// Contains as attributes the database and the table to ingest the data into.
        /// </param>
        /// <param name="loggerFactory">
        /// Logger Factory for creating an ILogger.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either configuration or attribute is null.
        /// </exception>
        public KustoAsyncCollector(IConfiguration configuration, KustoAttribute attribute, ILogger logger)
        {
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            this._logger = logger;
            this._kustoIngestClient = KustoBindingUtilities.GetIngestClient(attribute, configuration);
        }

        /// <summary>
        /// Adds an item to this collector that is processed in a batch along with all other items added via
        /// AddAsync when <see cref="FlushAsync"/> is called. Each item is interpreted as a row to be added to the Kusto table
        /// specified in the Binding.
        /// </summary>
        /// <param name="item"> The item to add to the collector.</param>
        /// <param name="cancellationToken">The cancellationToken is not used in this method.</param>
        /// <returns> A CompletedTask if executed successfully.</returns>
        public async Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            if (item != null)
            {
                await this._rowLock.WaitAsync(cancellationToken);
                try
                {
                    this._rows.Add(item);
                }
                finally
                {
                    this._rowLock.Release();
                }
            }
        }

        /// <summary>
        /// Ingest rows to be added into the Kusto table. This uses managed streaming for the ingestion <see cref="AddAsync"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellationToken is not used in this method.</param>
        /// <returns> A CompletedTask if executed successfully. If no rows were added, this is returned
        /// automatically. </returns>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            await this._rowLock.WaitAsync(cancellationToken);
            try
            {
                if (this._rows.Count != 0)
                {
                    await this.IngestRowsAsync(this._rows, this._attribute, this._configuration);
                    this._rows.Clear();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this._rowLock.Release();
            }
        }

        /// <summary>
        /// Ingests the rows specified in "rows" to the table specified in "kusto-attribute"
        /// </summary>
        /// <param name="rows"> The rows to be ingested to Kusto.</param>
        /// <param name="attribute"> Contains the name of the table to be ingested into.</param>
        /// <param name="configuration"> Used to build up the connection.</param>
        private async Task IngestRowsAsync(List<T> rows, KustoAttribute attribute, IConfiguration configuration)
        {
            var upsertRowsAsyncSw = Stopwatch.StartNew();
            var kustoIngestProperties = new KustoIngestionProperties(attribute.Database, attribute.TableName);
            this._logger.LogDebug("Ingesting rows into table {} in database {}", attribute.TableName, attribute.Database);
            bool parseResult = Enum.TryParse(attribute.DataFormat, out DataSourceFormat ingestDataFormat);
            kustoIngestProperties.Format = parseResult ? ingestDataFormat : DataSourceFormat.json;
            kustoIngestProperties.TableName = attribute.TableName;
            if (!string.IsNullOrEmpty(attribute.MappingRef))
            {
                this._logger.LogDebug("Using ingestionRef {} with configuration {} ", attribute.MappingRef, configuration);
                var ingestionMapping = new IngestionMapping
                {
                    IngestionMappingReference = attribute.MappingRef
                };
                kustoIngestProperties.IngestionMapping = ingestionMapping;
            }

            var sourceId = Guid.NewGuid();
            var streamSourceOptions = new StreamSourceOptions()
            {
                SourceId = sourceId,
            };


            upsertRowsAsyncSw.Stop();
            this._logger.LogInformation("END IngestRowsAsync , ingestion took {} ", upsertRowsAsyncSw.ElapsedMilliseconds);
        }

        private async Task<IngestionStatus> IngestData(string dataToIngest, KustoIngestionProperties kustoIngestionProperties, StreamSourceOptions streamSourceOptions)
        {
            IKustoIngestionResult ingestionResult = await this._kustoIngestClient.IngestFromStreamAsync(KustoBindingUtilities.StreamFromString(dataToIngest), kustoIngestionProperties, streamSourceOptions);
            IngestionStatus ingestionStatus = ingestionResult.GetIngestionStatusBySourceId(streamSourceOptions.SourceId);
            return ingestionStatus;
        }


        public void Dispose()
        {
            this._rowLock.Dispose();
        }
    }
}
