// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
                    await this.IngestRowsAsync();
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
        private async Task IngestRowsAsync()
        {
            var upsertRowsAsyncSw = Stopwatch.StartNew();
            var kustoIngestProperties = new KustoIngestionProperties(this._attribute.Database, this._attribute.TableName);
            this._logger.LogDebug("Ingesting rows into table {} in database {}", this._attribute.TableName, this._attribute.Database);
            DataSourceFormat format = this.GetDataFormat();
            kustoIngestProperties.Format = format;
            kustoIngestProperties.TableName = this._attribute.TableName;
            if (!string.IsNullOrEmpty(this._attribute.MappingRef))
            {
                this._logger.LogDebug("Using ingestionRef {} with configuration {} ", this._attribute.MappingRef, this._configuration);
                var ingestionMapping = new IngestionMapping
                {
                    IngestionMappingReference = this._attribute.MappingRef
                };
                kustoIngestProperties.IngestionMapping = ingestionMapping;
            }
            var sourceId = Guid.NewGuid();
            var streamSourceOptions = new StreamSourceOptions()
            {
                SourceId = sourceId,
            };
            /*
                The expectation here is that user will provide a CSV mapping or a JSON/Multi-JSON mapping
             */
            string dataToIngest = (format == DataSourceFormat.multijson || format == DataSourceFormat.json) ? this.SerializeToIngestData() : string.Join(Environment.NewLine, this._rows); ;
            await this.IngestData(dataToIngest, kustoIngestProperties, streamSourceOptions);
            upsertRowsAsyncSw.Stop();
            this._logger.LogInformation("End IngestRowsAsync with SourceId {} , ingestion took {} ", sourceId, upsertRowsAsyncSw.ElapsedMilliseconds);
        }

        private async Task<IngestionStatus> IngestData(string dataToIngest, KustoIngestionProperties kustoIngestionProperties, StreamSourceOptions streamSourceOptions)
        {
            IKustoIngestionResult ingestionResult = await this._kustoIngestClient.IngestFromStreamAsync(KustoBindingUtilities.StreamFromString(dataToIngest), kustoIngestionProperties, streamSourceOptions);
            IngestionStatus ingestionStatus = ingestionResult.GetIngestionStatusBySourceId(streamSourceOptions.SourceId);
            return ingestionStatus;
        }

        private string SerializeToIngestData()
        {
            var sb = new StringBuilder();
            bool first = true;
            Formatting indent = this._rows.Count == 1 ? Formatting.None : Formatting.Indented;
            using (var textWriter = new StringWriter(sb))
            {
                foreach (T row in this._rows)
                {
                    if (!first)
                    {
                        textWriter.WriteLine("");
                    }
                    first = false;
                    using (var jsonWriter = new JsonTextWriter(textWriter) { QuoteName = false, Formatting = indent, CloseOutput = false })
                    {
                        if (typeof(T) == typeof(string))
                        {
                            textWriter.Write(row.ToString());
                        }
                        else
                        {
                            // POCO , JObject
                            JsonSerializer.CreateDefault().Serialize(jsonWriter, row);
                        }
                    }
                }
            }
            return sb.ToString();
        }

        private DataSourceFormat GetDataFormat()
        {
            DataSourceFormat returnFormat = DataSourceFormat.json;
            if (string.IsNullOrEmpty(this._attribute.DataFormat))
            {
                if (this._rows.Count > 1)
                {
                    returnFormat = DataSourceFormat.multijson;
                }
            }
            else
            {
                bool parseResult = Enum.TryParse(this._attribute.DataFormat, out DataSourceFormat ingestDataFormat);

                returnFormat = parseResult && ingestDataFormat == DataSourceFormat.json && this._rows.Count > 1
                    ? DataSourceFormat.multijson
                    : ingestDataFormat;

            }
            return returnFormat;
        }

        public void Dispose()
        {
            this._rows.Clear();
            this._rowLock.Dispose();
        }
    }
}
