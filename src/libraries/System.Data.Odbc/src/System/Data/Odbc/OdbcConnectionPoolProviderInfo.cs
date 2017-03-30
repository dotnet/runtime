// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.ProviderBase;

namespace System.Data.Odbc
{
    internal sealed class OdbcConnectionPoolGroupProviderInfo : DbConnectionPoolGroupProviderInfo
    {
        private string _driverName;
        private string _driverVersion;
        private string _quoteChar;

        private char _escapeChar;
        private bool _hasQuoteChar;
        private bool _hasEscapeChar;

        private bool _isV3Driver;
        private int _supportedSQLTypes;
        private int _testedSQLTypes;
        private int _restrictedSQLBindTypes;   // These, otherwise supported types, are not available for binding

        // flags for unsupported Attributes
        private bool _noCurrentCatalog;
        private bool _noConnectionDead;

        private bool _noQueryTimeout;
        private bool _noSqlSoptSSNoBrowseTable;
        private bool _noSqlSoptSSHiddenColumns;

        // SSS_WARNINGS_OFF
        private bool _noSqlCASSColumnKey;
        // SSS_WARNINGS_ON

        // flags for unsupported Functions
        private bool _noSqlPrimaryKeys;

        internal string DriverName
        {
            get
            {
                return _driverName;
            }
            set
            {
                _driverName = value;
            }
        }

        internal string DriverVersion
        {
            get
            {
                return _driverVersion;
            }
            set
            {
                _driverVersion = value;
            }
        }

        internal bool HasQuoteChar
        {
            // the value is set together with the QuoteChar (see set_QuoteChar);
            get
            {
                return _hasQuoteChar;
            }
        }

        internal bool HasEscapeChar
        {
            // the value is set together with the EscapeChar (see set_EscapeChar);
            get
            {
                return _hasEscapeChar;
            }
        }


        internal string QuoteChar
        {
            get
            {
                return _quoteChar;
            }
            set
            {
                _quoteChar = value;
                _hasQuoteChar = true;
            }
        }

        internal char EscapeChar
        {
            get
            {
                return _escapeChar;
            }
            set
            {
                _escapeChar = value;
                _hasEscapeChar = true;
            }
        }

        internal bool IsV3Driver
        {
            get
            {
                return _isV3Driver;
            }
            set
            {
                _isV3Driver = value;
            }
        }

        internal int SupportedSQLTypes
        {
            get
            {
                return _supportedSQLTypes;
            }
            set
            {
                _supportedSQLTypes = value;
            }
        }

        internal int TestedSQLTypes
        {
            get
            {
                return _testedSQLTypes;
            }
            set
            {
                _testedSQLTypes = value;
            }
        }

        internal int RestrictedSQLBindTypes
        {
            get
            {
                return _restrictedSQLBindTypes;
            }
            set
            {
                _restrictedSQLBindTypes = value;
            }
        }


        internal bool NoCurrentCatalog
        {
            get
            {
                return _noCurrentCatalog;
            }
            set
            {
                _noCurrentCatalog = value;
            }
        }

        internal bool NoConnectionDead
        {
            get
            {
                return _noConnectionDead;
            }
            set
            {
                _noConnectionDead = value;
            }
        }


        internal bool NoQueryTimeout
        {
            get
            {
                return _noQueryTimeout;
            }
            set
            {
                _noQueryTimeout = value;
            }
        }

        internal bool NoSqlSoptSSNoBrowseTable
        {
            get
            {
                return _noSqlSoptSSNoBrowseTable;
            }
            set
            {
                _noSqlSoptSSNoBrowseTable = value;
            }
        }

        internal bool NoSqlSoptSSHiddenColumns
        {
            get
            {
                return _noSqlSoptSSHiddenColumns;
            }
            set
            {
                _noSqlSoptSSHiddenColumns = value;
            }
        }

        // SSS_WARNINGS_OFF
        internal bool NoSqlCASSColumnKey
        {
            get
            {
                return _noSqlCASSColumnKey;
            }
            set
            {
                _noSqlCASSColumnKey = value;
            }
        }
        // SSS_WARNINGS_ON

        internal bool NoSqlPrimaryKeys
        {
            get
            {
                return _noSqlPrimaryKeys;
            }
            set
            {
                _noSqlPrimaryKeys = value;
            }
        }
    }
}


