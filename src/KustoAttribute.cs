// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Kusto
{
    /// <summary>
    /// Setup an 'output' binding to an Kusto.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [Binding]
    public sealed class KustoAttribute : Attribute, IConnectionProvider
    {

        /// <summary>Initializes a new instance of the <see cref="KustoAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table to which to ingest data.</param>
        public KustoAttribute(string tableName)
        {
            this.TableName = tableName;
        }

        /// <summary>Initializes a new instance of the <see cref="KustoAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table to which to ingest data.</param>
        /// <param name="mappingRef">The mapping reference to use.</param>
        public KustoAttribute(string tableName, string mappingRef)
        {
            this.TableName = tableName;
            this.MappingRef = mappingRef;
        }

        /// <summary>Initializes a new instance of the <see cref="KustoAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table to which to ingest data.</param>
        /// <param name="mappingRef">The mapping reference to use.</param>
        /// <param name="dataFormat">Denotes the format of data. Can be one of JSON , CSV or other supported ingestion formats.</param>
        public KustoAttribute(string tableName, string mappingRef, string dataFormat)
        {
            this.TableName = tableName;
            this.MappingRef = mappingRef;
            this.DataFormat = dataFormat;
        }

        [AutoResolve]
        public string TableName { get; }

        [AutoResolve]
        public string MappingRef { get; }

        [AutoResolve]
        public string DataFormat { get; }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private string DebuggerDisplay
        {
            get
            {
                if (this.DataFormat == null)
                {
                    return this.TableName;
                }
                else
                {
                    return string.Format(CultureInfo.InvariantCulture, "{0}(Mapping={1}, DataFormat={2})",
                        this.TableName, this.MappingRef, this.DataFormat);
                }
            }
        }

        /// <summary>
        /// Gets or sets the app setting name that contains the Kusto connection string. Ref : https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto.
        /// </summary>
        public string Connection { get; set; }
    }
}