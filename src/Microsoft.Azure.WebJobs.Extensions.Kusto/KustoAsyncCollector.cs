// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
        /// Contains the SQL connection that will be used by the collector when it inserts SQL rows into the user's table.
        /// </param>
        /// <param name="attribute">
        /// Contains as one of its attributes the SQL table that rows will be inserted into.
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
            this._kustoIngestClient = KustoBindingUtilities.CreateIngestClient(attribute);
        }

        /// <summary>
        /// Adds an item to this collector that is processed in a batch along with all other items added via
        /// AddAsync when <see cref="FlushAsync"/> is called. Each item is interpreted as a row to be added to the SQL table
        /// specified in the SQL Binding.
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
        /// Processes all items added to the collector via <see cref="AddAsync"/>. Each item is interpreted as a row to be added
        /// to the SQL table specified in the SQL Binding. All rows are added in one transaction. Nothing is done
        /// if no items were added via AddAsync.
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
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                this._rowLock.Release();
            }
        }

        /// <summary>
        /// Upserts the rows specified in "rows" to the table specified in "attribute"
        /// If a primary key in "rows" already exists in the table, the row is interpreted as an update rather than an insert.
        /// The column values associated with that primary key in the table are updated to have the values specified in "rows".
        /// If a new primary key is encountered in "rows", the row is simply inserted into the table.
        /// </summary>
        /// <param name="rows"> The rows to be upserted.</param>
        /// <param name="attribute"> Contains the name of the table to be modified and SQL connection information.</param>
        /// <param name="configuration"> Used to build up the connection.</param>
        private async Task IngestRowsAsync(IEnumerable<T> rows, KustoAttribute attribute, IConfiguration configuration)
        {
            var upsertRowsAsyncSw = Stopwatch.StartNew();
            var kustoIngestProperties = new KustoIngestionProperties();
            this._logger.LogDebug("BEGIN IngestRowsAsync");
            var parseResult = Enum.TryParse(attribute.DataFormat, out DataSourceFormat ingestDataFormat);
            var isJson = DataSourceFormat.json.Equals(ingestDataFormat);
            kustoIngestProperties.Format = parseResult ? ingestDataFormat : DataSourceFormat.json;
            kustoIngestProperties.TableName = attribute.TableName;
            if (!string.IsNullOrEmpty(attribute.MappingRef))
            {
                this._logger.LogDebug("Using ingestionRef {}", attribute.MappingRef);
                IngestionMapping ingestionMapping = new IngestionMapping();
                ingestionMapping.IngestionMappingReference = attribute.MappingRef;
                kustoIngestProperties.IngestionMapping = ingestionMapping;
            }

            var sourceId = Guid.NewGuid();
            var streamSourceOptions = new StreamSourceOptions()
            {
                SourceId = sourceId,
            };

            // IList<string> rowsToUpsert = new List<string>();
            /*
                Can be a POCO to be serialized to a JSON
                May be a string (CSV)
                May be a string (JSON)
                TODO: Also see how we can support binary data
            */
            foreach (T row in rows)
            {
                var serializedJson = "";
                if (typeof(T) == typeof(JObject))
                {
                    // Is a JavaScript JSON object
                    serializedJson = JsonConvert.SerializeObject(row);
                }
                else
                {
                    serializedJson = row.ToString();
                }
                /*
                    if (typeof(T) == typeof(JArray))
                    {
                        var jsons = row as JArray;
                        foreach (JObject json in jsons)
                        {
                            serializedJson = JsonConvert.SerializeObject(json);
                        }
                    }
                */
                await IngestData(serializedJson, kustoIngestProperties, streamSourceOptions);
            }
            upsertRowsAsyncSw.Stop();
            this._logger.LogInformation("END IngestRowsAsync , ingestion took {} ", upsertRowsAsyncSw.ElapsedMilliseconds);
        }

        private async Task<IngestionStatus> IngestData(string dataToIngest, KustoIngestionProperties kustoIngestionProperties, StreamSourceOptions streamSourceOptions)
        {
            var ingestionResult = await this._kustoIngestClient.IngestFromStreamAsync(KustoBindingUtilities.StreamFromString(dataToIngest), kustoIngestionProperties, streamSourceOptions);
            var ingestionStatus = ingestionResult.GetIngestionStatusBySourceId(streamSourceOptions.SourceId);
            return ingestionStatus;
        }


        public void Dispose()
        {
            this._rowLock.Dispose();
        }
    }
}
