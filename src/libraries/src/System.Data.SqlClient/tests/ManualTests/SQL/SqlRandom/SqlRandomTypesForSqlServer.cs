// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Diagnostics;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    internal sealed class SqlVarBinaryTypeInfo : SqlRandomTypeInfo
    {
        private const int MaxDefinedSize = 8000;
        private const string TypePrefix = "varbinary";
        private const int DefaultSize = 1;

        internal SqlVarBinaryTypeInfo()
            : base(SqlDbType.VarBinary)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return LargeVarDataRowUsage;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            string sizeSuffix;
            if (!columnInfo.StorageSize.HasValue)
            {
                sizeSuffix = DefaultSize.ToString();
            }
            else
            {
                int size = columnInfo.StorageSize.Value;
                if (size > MaxDefinedSize)
                {
                    sizeSuffix = "max";
                }
                else
                {
                    Debug.Assert(size > 0, "wrong size");
                    sizeSuffix = size.ToString();
                }
            }
            return string.Format("{0}({1})", TypePrefix, sizeSuffix);
        }

        public override SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            int size = rand.NextAllocationSizeBytes(1);
            return new SqlRandomTableColumn(this, options, size);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            int size = columnInfo.StorageSize.HasValue ? columnInfo.StorageSize.Value : DefaultSize;
            return rand.NextByteArray(0, size);
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadByteArray(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareByteArray(expected, actual, allowIncomplete: false);
        }
    }

    internal sealed class SqlVarCharTypeInfo : SqlRandomTypeInfo
    {
        private const int MaxDefinedCharSize = 8000;
        private const string TypePrefix = "varchar";
        private const int DefaultCharSize = 1;

        internal SqlVarCharTypeInfo()
            : base(SqlDbType.VarChar)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return LargeVarDataRowUsage;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            string sizeSuffix;
            if (!columnInfo.StorageSize.HasValue)
            {
                sizeSuffix = DefaultCharSize.ToString();
            }
            else
            {
                int size = columnInfo.StorageSize.Value;
                if (size > MaxDefinedCharSize)
                {
                    sizeSuffix = "max";
                }
                else
                {
                    Debug.Assert(size > 0, "wrong size");
                    sizeSuffix = size.ToString();
                }
            }

            return string.Format("{0}({1})", TypePrefix, sizeSuffix);
        }

        public override SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            int size = rand.NextAllocationSizeBytes(1);
            return new SqlRandomTableColumn(this, options, size);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            int size = columnInfo.StorageSize.HasValue ? columnInfo.StorageSize.Value : DefaultCharSize;
            return rand.NextAnsiArray(0, size);
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadCharData(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareCharArray(expected, actual, allowIncomplete: false);
        }
    }

    internal sealed class SqlNVarCharTypeInfo : SqlRandomTypeInfo
    {
        private const int MaxDefinedCharSize = 4000;
        private const string TypePrefix = "nvarchar";
        private const int DefaultCharSize = 1;

        internal SqlNVarCharTypeInfo()
            : base(SqlDbType.NVarChar)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return LargeVarDataRowUsage;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            string sizeSuffix;
            if (!columnInfo.StorageSize.HasValue)
            {
                sizeSuffix = DefaultCharSize.ToString();
            }
            else
            {
                int charSize = columnInfo.StorageSize.Value / 2;
                if (charSize > MaxDefinedCharSize)
                {
                    sizeSuffix = "max";
                }
                else
                {
                    Debug.Assert(charSize > 0, "wrong size");
                    sizeSuffix = charSize.ToString();
                }
            }

            return string.Format("{0}({1})", TypePrefix, sizeSuffix);
        }

        public override SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            int size = rand.NextAllocationSizeBytes(2);
            size = size & 0xFFFE; // clean last bit to make it even
            return new SqlRandomTableColumn(this, options, storageSize: size);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            int storageSize = columnInfo.StorageSize.HasValue ? columnInfo.StorageSize.Value : DefaultCharSize * 2;
            return rand.NextUcs2Array(0, storageSize);
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadCharData(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareCharArray(expected, actual, allowIncomplete: false);
        }
    }

    internal sealed class SqlBigIntTypeInfo : SqlRandomTypeInfo
    {
        private const string TypeTSqlName = "bigint";
        private const int StorageSize = 8;

        public SqlBigIntTypeInfo()
            : base(SqlDbType.BigInt)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return StorageSize;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return TypeTSqlName;
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextBigInt();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(Int64), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return reader.GetInt64(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<Int64>(expected, actual);
        }
    }

    internal sealed class SqlIntTypeInfo : SqlRandomTypeInfo
    {
        private const string TypeTSqlName = "int";
        private const int StorageSize = 4;

        public SqlIntTypeInfo()
            : base(SqlDbType.Int)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return StorageSize;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return TypeTSqlName;
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextIntInclusive();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(int), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return reader.GetInt32(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<Int32>(expected, actual);
        }
    }

    internal sealed class SqlSmallIntTypeInfo : SqlRandomTypeInfo
    {
        private const string TypeTSqlName = "smallint";
        private const int StorageSize = 2;

        public SqlSmallIntTypeInfo()
            : base(SqlDbType.SmallInt)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return StorageSize;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return TypeTSqlName;
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextSmallInt();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(short), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return reader.GetInt16(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<Int16>(expected, actual);
        }
    }

    internal sealed class SqlTinyIntTypeInfo : SqlRandomTypeInfo
    {
        private const string TypeTSqlName = "tinyint";
        private const int StorageSize = 1;

        public SqlTinyIntTypeInfo()
            : base(SqlDbType.TinyInt)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return StorageSize;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return TypeTSqlName;
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextTinyInt();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(byte), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return reader.GetByte(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<byte>(expected, actual);
        }
    }

    internal sealed class SqlTextTypeInfo : SqlRandomTypeInfo
    {
        private const string TypeTSqlName = "text";

        public SqlTextTypeInfo()
            : base(SqlDbType.Text)
        {
        }

        public override bool CanBeSparseColumn
        {
            get { return false; }
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return LargeDataRowUsage;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return TypeTSqlName;
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextAnsiArray(0, columnInfo.StorageSize);
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadCharData(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareCharArray(expected, actual, allowIncomplete: false);
        }
    }

    internal sealed class SqlNTextTypeInfo : SqlRandomTypeInfo
    {
        private const string TypeTSqlName = "ntext";

        public SqlNTextTypeInfo()
            : base(SqlDbType.NText)
        {
        }

        public override bool CanBeSparseColumn
        {
            get { return false; }
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return LargeDataRowUsage;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return TypeTSqlName;
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextUcs2Array(0, columnInfo.StorageSize);
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadCharData(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareCharArray(expected, actual, allowIncomplete: false);
        }
    }

    internal sealed class SqlImageTypeInfo : SqlRandomTypeInfo
    {
        private const string TypeTSqlName = "image";

        public SqlImageTypeInfo()
            : base(SqlDbType.Image)
        {
        }

        public override bool CanBeSparseColumn
        {
            get { return false; }
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return LargeDataRowUsage;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return TypeTSqlName;
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextByteArray(0, columnInfo.StorageSize);
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadByteArray(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareByteArray(expected, actual, allowIncomplete: false);
        }
    }

    internal sealed class SqlBinaryTypeInfo : SqlRandomTypeInfo
    {
        private const int MaxStorageSize = 8000;
        private const string TypePrefix = "binary";
        private const int DefaultSize = 1;

        public SqlBinaryTypeInfo()
            : base(SqlDbType.Binary)
        {
        }

        private int GetSize(SqlRandomTableColumn columnInfo)
        {
            ValidateColumnInfo(columnInfo);

            int size = columnInfo.StorageSize.HasValue ? columnInfo.StorageSize.Value : DefaultSize;
            if (size < 1 || size > MaxStorageSize)
                throw new NotSupportedException("wrong size");

            return size;
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return GetSize(columnInfo);
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return string.Format("{0}({1})", TypePrefix, GetSize(columnInfo));
        }

        public override SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            int size = rand.NextAllocationSizeBytes(1, MaxStorageSize);
            return new SqlRandomTableColumn(this, options, size);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            int size = columnInfo.StorageSize.HasValue ? columnInfo.StorageSize.Value : DefaultSize;
            return rand.NextByteArray(0, size);
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadByteArray(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareByteArray(expected, actual, allowIncomplete: true);
        }
    }

    internal sealed class SqlCharTypeInfo : SqlRandomTypeInfo
    {
        private const int MaxCharSize = 8000;
        private const string TypePrefix = "char";
        private const int DefaultCharSize = 1;

        public SqlCharTypeInfo()
            : base(SqlDbType.Char)
        {
        }

        private int GetCharSize(SqlRandomTableColumn columnInfo)
        {
            ValidateColumnInfo(columnInfo);

            int size = columnInfo.StorageSize.HasValue ? columnInfo.StorageSize.Value : DefaultCharSize;
            if (size < 1 || size > MaxCharSize)
                throw new NotSupportedException("wrong size");

            return size;
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return GetCharSize(columnInfo);
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return string.Format("{0}({1})", TypePrefix, GetCharSize(columnInfo));
        }

        public override SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            int size = rand.NextAllocationSizeBytes(1, MaxCharSize);
            return new SqlRandomTableColumn(this, options, size);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            int size = columnInfo.StorageSize.HasValue ? columnInfo.StorageSize.Value : DefaultCharSize;
            return rand.NextAnsiArray(0, size);
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadCharData(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareCharArray(expected, actual, allowIncomplete: true);
        }
    }

    internal sealed class SqlNCharTypeInfo : SqlRandomTypeInfo
    {
        private const int MaxCharSize = 4000;
        private const string TypePrefix = "nchar";
        private const int DefaultCharSize = 1;

        public SqlNCharTypeInfo()
            : base(SqlDbType.NChar)
        {
        }

        private int GetCharSize(SqlRandomTableColumn columnInfo)
        {
            ValidateColumnInfo(columnInfo);

            int charSize = columnInfo.StorageSize.HasValue ? columnInfo.StorageSize.Value / 2 : DefaultCharSize;
            if (charSize < 1 || charSize > MaxCharSize)
                throw new NotSupportedException("wrong size");

            return charSize;
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return GetCharSize(columnInfo) * 2; // nchar is not stored in row
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return string.Format("{0}({1})", TypePrefix, GetCharSize(columnInfo));
        }

        public override SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            int size = rand.NextAllocationSizeBytes(2, MaxCharSize * 2);
            size = size & 0xFFFE; // clean last bit to make it even
            return new SqlRandomTableColumn(this, options, size);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            int storageSize = GetCharSize(columnInfo) * 2;
            return rand.NextUcs2Array(0, storageSize);
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadCharData(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareCharArray(expected, actual, allowIncomplete: true);
        }
    }

    internal sealed class SqlBitTypeInfo : SqlRandomTypeInfo
    {
        public SqlBitTypeInfo()
            : base(SqlDbType.Bit)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return 0.125; // 8 bits => 1 byte
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return "bit";
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextBit();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(Boolean), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return reader.GetBoolean(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<Boolean>(expected, actual);
        }
    }

    internal sealed class SqlDecimalTypeInfo : SqlRandomTypeInfo
    {
        private int _defaultPrecision = 18;

        public SqlDecimalTypeInfo()
            : base(SqlDbType.Decimal)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            int precision = columnInfo.Precision.HasValue ? columnInfo.Precision.Value : _defaultPrecision;
            if (precision < 1 || precision > 38)
            {
                throw new ArgumentOutOfRangeException("wrong precision");
            }

            if (precision < 10)
            {
                return 5;
            }
            else if (precision < 20)
            {
                return 9;
            }
            else if (precision < 28)
            {
                return 13;
            }
            else
            {
                return 17;
            }
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return "decimal";
        }

        public override SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            return CreateDefaultColumn(options);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return (decimal)Math.Round(rand.NextDouble());
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(decimal), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return reader.GetDecimal(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<decimal>(expected, actual);
        }
    }

    internal sealed class SqlMoneyTypeInfo : SqlRandomTypeInfo
    {
        private const string TypeTSqlName = "money";
        private const int StorageSize = 8;

        public SqlMoneyTypeInfo()
            : base(SqlDbType.Money)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return StorageSize;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return TypeTSqlName;
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextMoney();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(decimal), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return reader.GetDecimal(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<decimal>(expected, actual);
        }
    }

    internal sealed class SqlSmallMoneyTypeInfo : SqlRandomTypeInfo
    {
        private const string TypeTSqlName = "smallmoney";
        private const int StorageSize = 4;

        public SqlSmallMoneyTypeInfo()
            : base(SqlDbType.SmallMoney)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return StorageSize;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return TypeTSqlName;
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextSmallMoney();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(decimal), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return reader.GetDecimal(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<decimal>(expected, actual);
        }
    }

    internal sealed class SqRealTypeInfo : SqlRandomTypeInfo
    {
        // simplify to double-precision or real only
        private const int RealPrecision = 7;
        private const int RealMantissaBits = 24;
        private const string TSqlTypeName = "real";

        public SqRealTypeInfo()
            : base(SqlDbType.Real)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return 4;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return TSqlTypeName;
        }

        public override SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            return CreateDefaultColumn(options);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextReal();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(float), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return reader.GetFloat(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<Single>(expected, actual);
        }
    }


    internal sealed class SqFloatTypeInfo : SqlRandomTypeInfo
    {
        // simplify to double-precision or real only
        private const int MaxFloatMantissaBits = 53;
        private const int MaxFloatPrecision = 15;
        private const int RealPrecision = 7;
        private const int RealMantissaBits = 24;
        private const string TypePrefix = "float";

        public SqFloatTypeInfo()
            : base(SqlDbType.Float)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            // float
            if (!columnInfo.Precision.HasValue)
            {
                return 8;
            }
            else
            {
                int precision = columnInfo.Precision.Value;
                if (precision != RealPrecision && precision != MaxFloatPrecision)
                    throw new ArgumentException("wrong precision");
                return (precision <= RealPrecision) ? 4 : 8;
            }
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            int precision = columnInfo.Precision.HasValue ? columnInfo.Precision.Value : MaxFloatPrecision;
            if (precision != RealPrecision && precision != MaxFloatPrecision)
                throw new ArgumentException("wrong precision");
            int mantissaBits = (precision <= RealPrecision) ? 24 : 53;
            return string.Format("{0}({1})", TypePrefix, mantissaBits);
        }

        public override SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            return CreateDefaultColumn(options);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            int precision = columnInfo.Precision.HasValue ? columnInfo.Precision.Value : MaxFloatPrecision;
            if (precision <= RealPrecision)
                return rand.NextDouble(float.MinValue, float.MaxValue, precision);
            else
                return rand.NextDouble(double.MinValue, double.MaxValue, precision);
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(double), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return reader.GetDouble(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<Double>(expected, actual);
        }
    }

    internal sealed class SqlRowVersionTypeInfo : SqlRandomTypeInfo
    {
        public SqlRowVersionTypeInfo()
            : base(SqlDbType.Timestamp)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return 8;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return "rowversion";
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextRowVersion();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadByteArray(reader, ordinal, asType);
        }

        public override bool CanBeSparseColumn
        {
            get
            {
                return false;
            }
        }

        public override bool CanCompareValues(SqlRandomTableColumn columnInfo)
        {
            // completely ignore TIMESTAMP value comparison
            return false;
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            throw new InvalidOperationException("should not be used for timestamp - use CanCompareValues before calling this method");
        }
    }

    internal sealed class SqlUniqueIdentifierTypeInfo : SqlRandomTypeInfo
    {
        public SqlUniqueIdentifierTypeInfo()
            : base(SqlDbType.UniqueIdentifier)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return 16;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return "uniqueidentifier";
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            // this method does not use Guid.NewGuid since it is not based on the given rand object
            return rand.NextUniqueIdentifier();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(Guid), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return reader.GetGuid(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<Guid>(expected, actual);
        }
    }

    internal sealed class SqlDateTypeInfo : SqlRandomTypeInfo
    {
        public SqlDateTypeInfo()
            : base(SqlDbType.Date)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return 3;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return "date";
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextDate();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadDateTime(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<DateTime>(expected, actual);
        }
    }

    internal sealed class SqlDateTimeTypeInfo : SqlRandomTypeInfo
    {
        public SqlDateTimeTypeInfo()
            : base(SqlDbType.DateTime)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return 8;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return "datetime";
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextDateTime();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadDateTime(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<DateTime>(expected, actual);
        }
    }

    internal sealed class SqlDateTime2TypeInfo : SqlRandomTypeInfo
    {
        private const int DefaultPrecision = 7;

        public SqlDateTime2TypeInfo()
            : base(SqlDbType.DateTime2)
        {
        }

        private static int GetPrecision(SqlRandomTableColumn columnInfo)
        {
            int precision;

            if (columnInfo.Precision.HasValue)
            {
                precision = columnInfo.Precision.Value;
                if (precision < 0 || precision > 7)
                    throw new ArgumentOutOfRangeException("columnInfo.Precision");
            }
            else
            {
                precision = DefaultPrecision;
            }

            return precision;
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            int precision = GetPrecision(columnInfo);
            if (precision < 3)
                return 6;
            else if (precision < 5)
                return 7;
            else
                return 8;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            int precision = GetPrecision(columnInfo);
            if (precision == DefaultPrecision)
                return "datetime2";
            else
            {
                Debug.Assert(precision > 0, "wrong precision");
                return string.Format("datetime2({0})", precision);
            }
        }

        public override SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            return CreateDefaultColumn(options);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextDateTime2();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadDateTime(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<DateTime>(expected, actual);
        }
    }

    internal sealed class SqlDateTimeOffsetTypeInfo : SqlRandomTypeInfo
    {
        public SqlDateTimeOffsetTypeInfo()
            : base(SqlDbType.DateTimeOffset)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return 10;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return "datetimeoffset";
        }

        public override SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            return CreateDefaultColumn(options);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextDateTimeOffset();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(DateTimeOffset), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return ((System.Data.SqlClient.SqlDataReader)reader).GetDateTimeOffset(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<DateTimeOffset>(expected, actual);
        }
    }

    internal sealed class SqlSmallDateTimeTypeInfo : SqlRandomTypeInfo
    {
        public SqlSmallDateTimeTypeInfo()
            : base(SqlDbType.SmallDateTime)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return 4;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return "smalldatetime";
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextSmallDateTime();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadDateTime(reader, ordinal, asType);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<DateTime>(expected, actual);
        }
    }

    internal sealed class SqlTimeTypeInfo : SqlRandomTypeInfo
    {
        public SqlTimeTypeInfo()
            : base(SqlDbType.Time)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return 5;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return "time ";
        }

        public override SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            return CreateDefaultColumn(options);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            return rand.NextTime();
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            ValidateReadType(typeof(TimeSpan), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return ((System.Data.SqlClient.SqlDataReader)reader).GetTimeSpan(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            return CompareValues<TimeSpan>(expected, actual);
        }
    }

    internal sealed class SqlVariantTypeInfo : SqlRandomTypeInfo
    {
        private static readonly SqlRandomTypeInfo[] s_variantSubTypes =
        {
            // var types
            new SqlVarBinaryTypeInfo(),
            new SqlVarCharTypeInfo(),
            new SqlNVarCharTypeInfo(),

            // integer data types
            new SqlBigIntTypeInfo(),
            new SqlIntTypeInfo(),
            new SqlSmallIntTypeInfo(),
            new SqlTinyIntTypeInfo(),

            // fixed length blobs
            new SqlCharTypeInfo(),
            new SqlNCharTypeInfo(),
            new SqlBinaryTypeInfo(),

            // large blobs
            new SqlTextTypeInfo(),
            new SqlNTextTypeInfo(),
            new SqlImageTypeInfo(),

            // bit
            new SqlBitTypeInfo(),

            // decimal
            new SqlDecimalTypeInfo(),

            // money types
            new SqlMoneyTypeInfo(),
            new SqlSmallMoneyTypeInfo(),

            // float types
            new SqRealTypeInfo(),
            new SqFloatTypeInfo(),
            
            // unique identifier (== guid)
            new SqlUniqueIdentifierTypeInfo(),

            // date/time types
            new SqlDateTimeTypeInfo(),
            new SqlSmallDateTimeTypeInfo(),
        };

        public SqlVariantTypeInfo()
            : base(SqlDbType.Variant)
        {
        }


        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return VariantRowUsage;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return "sql_variant";
        }

        private static bool IsUnicodeType(SqlDbType t)
        {
            return (t == SqlDbType.NChar || t == SqlDbType.NText || t == SqlDbType.NVarChar);
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            SqlRandomTypeInfo subType = s_variantSubTypes[rand.NextIntInclusive(0, maxValueInclusive: s_variantSubTypes.Length - 1)];
            object val = subType.CreateRandomValue(rand, new SqlRandomTableColumn(subType, SqlRandomColumnOptions.None, 8000));
            char[] cval = val as char[];
            if (cval != null)
            {
                int maxLength = IsUnicodeType(subType.Type) ? 4000 : 8000;
                Debug.Assert(cval.Length < maxLength, "char array length cannot be greater than " + maxLength);
                // cannot insert char[] into variant
                val = new string((char[])val);
            }
            else
            {
                byte[] bval = val as byte[];
                if (bval != null)
                    Debug.Assert(bval.Length < 8000, "byte array length cannot be greater than 8000");
            }

            return val;
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return reader.GetValue(ordinal);
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            bool bothDbNull;
            if (!CompareDbNullAndType(null, expected, actual, out bothDbNull) || bothDbNull)
                return bothDbNull;

            Type expectedType = expected.GetType();

            if (expectedType == typeof(byte[]))
            {
                return CompareByteArray(expected, actual, allowIncomplete: false);
            }
            else if (expectedType == typeof(char[]))
            {
                return CompareCharArray(expected, actual, allowIncomplete: false);
            }
            else if (expectedType == typeof(string))
            {
                // special handling for strings, only in variant
                if (actual.GetType() == typeof(string))
                    return string.Equals((string)expected, (string)actual, StringComparison.Ordinal);
                else
                    return false;
            }
            else
            {
                return expected.Equals(actual);
            }
        }
    }

    internal sealed class SqlXmlTypeInfo : SqlRandomTypeInfo
    {
        public SqlXmlTypeInfo()
            : base(SqlDbType.Xml)
        {
        }

        protected override double GetInRowSizeInternal(SqlRandomTableColumn columnInfo)
        {
            return XmlRowUsage;
        }

        protected override string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo)
        {
            return "xml";
        }

        protected override object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            if ((columnInfo.Options & SqlRandomColumnOptions.ColumnSet) != 0)
            {
                // use the sparse columns themselves to insert values
                // testing column set column correctness is not a goal for the stress test
                throw new NotImplementedException("should not use this method for column set columns");
            }

            int charSize = rand.NextAllocationSizeBytes(0, columnInfo.StorageSize);
            const string prefix = "<x>";
            const string suffix = "</x>";
            if (charSize > (prefix.Length + suffix.Length))
            {
                string randValue = new string('a', charSize - (prefix.Length + suffix.Length));
                return string.Format("<x>{0}</x>", randValue).ToCharArray();
            }
            else
            {
                // for accurate comparison, use the short form
                return "<x />".ToCharArray();
            }
        }

        protected override object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            return ReadCharData(reader, ordinal, asType);
        }

        public override bool CanCompareValues(SqlRandomTableColumn columnInfo)
        {
            return (columnInfo.Options & SqlRandomColumnOptions.ColumnSet) == 0;
        }

        protected override bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            if ((columnInfo.Options & SqlRandomColumnOptions.ColumnSet) != 0)
            {
                throw new InvalidOperationException("should not be used for ColumnSet columns - use CanCompareValues before calling this method");
            }
            return CompareCharArray(expected, actual, allowIncomplete: false);
        }
    }
}
