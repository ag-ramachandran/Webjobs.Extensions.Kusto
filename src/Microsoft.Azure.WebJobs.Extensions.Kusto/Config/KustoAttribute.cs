// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Description;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// Setup an 'output' binding to an Kusto.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [Binding]
    public sealed class KustoAttribute : Attribute, IConnectionProvider
    {

        private readonly string _tableName;
        private readonly string _mappingRef;
        private readonly string _dataFormat;

        /// <summary>Initializes a new instance of the <see cref="KustoAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table to which to ingest data.</param>
        public KustoAttribute(string tableName)
        {
            _tableName = tableName;
        }

        /// <summary>Initializes a new instance of the <see cref="KustoAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table to which to ingest data.</param>
        /// <param name="mappingRef">The mapping reference to use.</param>
        public KustoAttribute(string tableName, string mappingRef)
        {
            _tableName = tableName;
            _mappingRef = mappingRef;
        }

        /// <summary>Initializes a new instance of the <see cref="KustoAttribute"/> class.</summary>
        /// <param name="tableName">The name of the table to which to ingest data.</param>
        /// <param name="mappingRef">The mapping reference to use.</param>
        /// <param name="dataFormat">Denotes the format of data. Can be one of JSON , CSV or other supported ingestion formats.</param>
        public KustoAttribute(string tableName, string mappingRef, string dataFormat)
        {
            _tableName = tableName;
            _mappingRef = mappingRef;
            _dataFormat = dataFormat;
        }

        [AutoResolve]
        public string TableName => _tableName;

        [AutoResolve]
        public string mappingRef => _mappingRef;

        [AutoResolve]
        public string dataFormat => _dataFormat;


        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private string DebuggerDisplay
        {
            get
            {
                if (_dataFormat == null)
                {
                    return _tableName;
                }
                else
                {
                    return string.Format(CultureInfo.InvariantCulture, "{0}(Mapping={1}, DataFormat={2})",
                        _tableName, _mappingRef, _dataFormat);
                }
            }
        }

        /// <summary>
        /// Gets or sets the app setting name that contains the Kusto connection string. Ref : https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto.
        /// </summary>
        public string Connection { get; set; }
    }
}