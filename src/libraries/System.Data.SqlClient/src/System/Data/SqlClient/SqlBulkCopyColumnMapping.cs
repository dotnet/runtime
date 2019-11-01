// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;


namespace System.Data.SqlClient
{
    // -------------------------------------------------------------------------------------------------
    // this class helps allows the user to create association between source- and targetcolumns
    //
    //

    public sealed class SqlBulkCopyColumnMapping
    {
        internal string _destinationColumnName;
        internal int _destinationColumnOrdinal;
        internal string _sourceColumnName;
        internal int _sourceColumnOrdinal;

        // devnote: we don't want the user to detect the columnordinal after WriteToServer call.
        // _sourceColumnOrdinal(s) will be copied to _internalSourceColumnOrdinal when WriteToServer executes.
        internal int _internalDestinationColumnOrdinal;
        internal int _internalSourceColumnOrdinal;   // -1 indicates an undetermined value

        public string DestinationColumn
        {
            get
            {
                if (_destinationColumnName != null)
                {
                    return _destinationColumnName;
                }
                return string.Empty;
            }
            set
            {
                _destinationColumnOrdinal = _internalDestinationColumnOrdinal = -1;
                _destinationColumnName = value;
            }
        }

        public int DestinationOrdinal
        {
            get
            {
                return _destinationColumnOrdinal;
            }
            set
            {
                if (value >= 0)
                {
                    _destinationColumnName = null;
                    _destinationColumnOrdinal = _internalDestinationColumnOrdinal = value;
                }
                else
                {
                    throw ADP.IndexOutOfRange(value);
                }
            }
        }

        public string SourceColumn
        {
            get
            {
                if (_sourceColumnName != null)
                {
                    return _sourceColumnName;
                }
                return string.Empty;
            }
            set
            {
                _sourceColumnOrdinal = _internalSourceColumnOrdinal = -1;
                _sourceColumnName = value;
            }
        }

        public int SourceOrdinal
        {
            get
            {
                return _sourceColumnOrdinal;
            }
            set
            {
                if (value >= 0)
                {
                    _sourceColumnName = null;
                    _sourceColumnOrdinal = _internalSourceColumnOrdinal = value;
                }
                else
                {
                    throw ADP.IndexOutOfRange(value);
                }
            }
        }

        public SqlBulkCopyColumnMapping()
        {
            _internalSourceColumnOrdinal = -1;
        }

        public SqlBulkCopyColumnMapping(string sourceColumn, string destinationColumn)
        {
            SourceColumn = sourceColumn;
            DestinationColumn = destinationColumn;
        }

        public SqlBulkCopyColumnMapping(int sourceColumnOrdinal, string destinationColumn)
        {
            SourceOrdinal = sourceColumnOrdinal;
            DestinationColumn = destinationColumn;
        }

        public SqlBulkCopyColumnMapping(string sourceColumn, int destinationOrdinal)
        {
            SourceColumn = sourceColumn;
            DestinationOrdinal = destinationOrdinal;
        }

        public SqlBulkCopyColumnMapping(int sourceColumnOrdinal, int destinationOrdinal)
        {
            SourceOrdinal = sourceColumnOrdinal;
            DestinationOrdinal = destinationOrdinal;
        }
    }
}
