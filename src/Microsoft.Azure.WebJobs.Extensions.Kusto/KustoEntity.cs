// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Kusto.Data.Common;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Kusto
{
    /// <summary>
    /// Defines an item to be ingested into a table.
    /// </summary>
    public class KustoEntity
    {
        /// <summary>
        /// Table to write to.
        /// </summary>
        [JsonProperty("tableName")]
        public string TableName { get; set; }

        /// <summary>
        /// Defines the value as text to be ingested.
        /// </summary>
        [JsonProperty("payload")]
        public byte[] Payload { get; set; }

        /// <summary>
        /// Defines the mappingReference (already defined in Kusto) to be used.
        /// </summary>
        [JsonProperty("mappingReference")]
        public object MappingReference { get; set; }

        /// <summary>
        /// Defines the data format to use.
        /// </summary>
        [JsonProperty("dataFormat")]
        public DataSourceFormat DataFormat { get; set; } = DataSourceFormat.json;
    }
}
