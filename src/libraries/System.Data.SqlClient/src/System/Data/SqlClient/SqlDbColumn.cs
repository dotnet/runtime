// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Data.Common;

namespace System.Data.SqlClient
{
    internal class SqlDbColumn : DbColumn
    {
        private readonly bool _isBrowseModeInfoConsumed;
        private readonly _SqlMetaData _metadata;

        internal SqlDbColumn(_SqlMetaData md, bool isBrowseModeInfoConsumed)
        {
            _metadata = md;
            _isBrowseModeInfoConsumed = isBrowseModeInfoConsumed;
            Populate();
        }

        private void Populate()
        {
            AllowDBNull = _metadata.isNullable;
            BaseCatalogName = _metadata.catalogName;
            BaseColumnName = _metadata.baseColumn;
            BaseSchemaName = _metadata.schemaName;
            BaseServerName = _metadata.serverName;
            BaseTableName = _metadata.tableName;
            ColumnName = _metadata.column;
            ColumnOrdinal = _metadata.ordinal;
            ColumnSize = (_metadata.metaType.IsSizeInCharacters && (_metadata.length != 0x7fffffff)) ? (_metadata.length / 2) : _metadata.length;

            if (_isBrowseModeInfoConsumed)
            {
                IsAliased = _metadata.isDifferentName;
                IsKey = _metadata.isKey;
                IsHidden = _metadata.isHidden;
                IsExpression = _metadata.isExpression;
            }

            IsAutoIncrement = _metadata.isIdentity;
            IsIdentity = _metadata.isIdentity;
            IsLong = _metadata.metaType.IsLong;

            if (SqlDbType.Timestamp == _metadata.type)
            {
                IsUnique = true;
            }
            else
            {
                IsUnique = false;
            }

            if (TdsEnums.UNKNOWN_PRECISION_SCALE != _metadata.precision)
            {
                NumericPrecision = _metadata.precision;
            }
            else
            {
                NumericPrecision = _metadata.metaType.Precision;
            }

            IsReadOnly = (0 == _metadata.updatability);

            UdtAssemblyQualifiedName = null;

        }

        internal Type SqlDataType
        {
            set
            {
                DataType = value;
            }
        }

        internal string SqlDataTypeName
        {
            set
            {
                DataTypeName = value;
            }
        }

        internal int? SqlNumericScale
        {
            set
            {
                NumericScale = value;
            }
        }

    }
}
