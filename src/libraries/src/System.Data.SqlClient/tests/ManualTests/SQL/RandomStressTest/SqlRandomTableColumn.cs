// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.SqlClient.ManualTesting.Tests
{
    /// <summary>
    /// holds an information used to create a column in SQL Server
    /// </summary>
    public sealed class SqlRandomTableColumn
    {
        public readonly SqlRandomTypeInfo TypeInfo;
        public readonly int? StorageSize;
        public readonly int? Precision;
        public readonly int? Scale;
        public readonly SqlRandomColumnOptions Options;

        // useful shortcuts
        public SqlDbType Type { get { return TypeInfo.Type; } }
        public string GetTSqlTypeDefinition() { return TypeInfo.GetTSqlTypeDefinition(this); }
        public object CreateRandomValue(SqlRandomizer rand) { return TypeInfo.CreateRandomValue(rand, this); }
        public object Read(SqlDataReader reader, int ordinal, Type asType) { return TypeInfo.Read(reader, ordinal, this, asType); }
        public bool CanCompareValues { get { return TypeInfo.CanCompareValues(this); } }
        public bool CompareValues(object expected, object actual) { return TypeInfo.CompareValues(this, expected, actual); }
        public string BuildErrorMessage(object expected, object actual) { return TypeInfo.BuildErrorMessage(this, expected, actual); }
        public double GetInRowSize(object value) { return TypeInfo.GetInRowSize(this, value); }
        public bool IsSparse { get { return (Options & SqlRandomColumnOptions.Sparse) != 0; } }
        public bool IsColumnSet { get { return (Options & SqlRandomColumnOptions.ColumnSet) != 0; } }

        public SqlRandomTableColumn(SqlRandomTypeInfo typeInfo, SqlRandomColumnOptions options)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            if ((options & SqlRandomColumnOptions.ColumnSet) != 0)
            {
                if ((options & SqlRandomColumnOptions.Sparse) != 0)
                {
                    throw new ArgumentException("Must not be sparse", nameof(options));
                }

                if (typeInfo.Type != SqlDbType.Xml)
                {
                    throw new ArgumentException("columnset column must be an XML column");
                }
            }

            TypeInfo = typeInfo;
            Options = options;
        }

        public SqlRandomTableColumn(SqlRandomTypeInfo typeInfo, SqlRandomColumnOptions options, int? storageSize)
            : this(typeInfo, options)
        {
            StorageSize = storageSize;
            Precision = null;
            Scale = null;
        }

        public SqlRandomTableColumn(SqlRandomTypeInfo typeInfo, SqlRandomColumnOptions options, int? precision, int? scale)
            : this(typeInfo, options)
        {
            StorageSize = null;
            Precision = precision;
            Scale = scale;
        }
    }
}
