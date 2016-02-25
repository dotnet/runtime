// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Data.Common;
using System.Globalization;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    /// <summary>
    /// represents a base class for SQL type random generation
    /// </summary>
    public abstract class SqlRandomTypeInfo
    {
        // max size on the row for large blob types to prevent row overflow, when creating the table:
        // "Warning: The table "TestTable" has been created, but its maximum row size exceeds the allowed maximum of 8060 bytes. INSERT or UPDATE to this table will fail if the resulting row exceeds the size limit."
        // tests show that the actual size is 36, I added 4 more bytes for extra
        protected const int LargeVarDataRowUsage = 40; // var types
        protected const int LargeDataRowUsage = 40; // text/ntext/image
        protected internal const int XmlRowUsage = 40; //
        protected const int VariantRowUsage = 40;

        public readonly SqlDbType Type;

        protected SqlRandomTypeInfo(SqlDbType t)
        {
            Type = t;
        }

        /// <summary>
        /// true if column of this type can be created as sparse column
        /// </summary>
        public virtual bool CanBeSparseColumn
        {
            get
            {
                return true;
            }
        }


        /// <summary>
        /// creates a default column instance for given type
        /// </summary>
        public SqlRandomTableColumn CreateDefaultColumn()
        {
            return CreateDefaultColumn(SqlRandomColumnOptions.None);
        }

        /// <summary>
        /// creates a default column instance for given type
        /// </summary>
        public virtual SqlRandomTableColumn CreateDefaultColumn(SqlRandomColumnOptions options)
        {
            return new SqlRandomTableColumn(this, options);
        }

        /// <summary>
        /// creates a column with random size/precision/scale values, where applicable.
        /// </summary>
        /// <remarks>this method is overriden for some types to create columns with random size/precision</remarks>
        public virtual SqlRandomTableColumn CreateRandomColumn(SqlRandomizer rand, SqlRandomColumnOptions options)
        {
            return CreateDefaultColumn(options);
        }

        /// <summary>
        /// helper method to check validity of input column
        /// </summary>
        protected void ValidateColumnInfo(SqlRandomTableColumn columnInfo)
        {
            if (columnInfo == null)
                throw new ArgumentNullException("columnInfo");

            if (Type != columnInfo.Type)
                throw new ArgumentException("Type mismatch");
        }

        /// <summary>
        /// Returns the size used by a column value within the row. This method is used when generating random table to ensure
        /// the row size does not overflow
        /// </summary>
        public double GetInRowSize(SqlRandomTableColumn columnInfo, object value)
        {
            ValidateColumnInfo(columnInfo);

            if (columnInfo.IsSparse)
            {
                if (value == DBNull.Value && value == null)
                {
                    // null values of sparse columns do not use in-row size
                    return 0;
                }
                else
                {
                    // if sparse column has non-null value, it has an additional penalty of 4 bytes added to its storage size
                    return 4 + GetInRowSizeInternal(columnInfo);
                }
            }
            else
            {
                // not a sparse column
                return GetInRowSizeInternal(columnInfo);
            }
        }

        protected abstract double GetInRowSizeInternal(SqlRandomTableColumn columnInfo);

        /// <summary>
        /// gets TSQL definition of the column
        /// </summary>
        public string GetTSqlTypeDefinition(SqlRandomTableColumn columnInfo)
        {
            ValidateColumnInfo(columnInfo);
            return GetTSqlTypeDefinitionInternal(columnInfo);
        }

        protected abstract string GetTSqlTypeDefinitionInternal(SqlRandomTableColumn columnInfo);

        /// <summary>
        /// creates random, but valud value for the type, based on the given column definition
        /// </summary>
        public object CreateRandomValue(SqlRandomizer rand, SqlRandomTableColumn columnInfo)
        {
            ValidateColumnInfo(columnInfo);
            return CreateRandomValueInternal(rand, columnInfo);
        }

        protected abstract object CreateRandomValueInternal(SqlRandomizer rand, SqlRandomTableColumn columnInfo);

        /// <summary>
        /// helper method to read character data from the reader
        /// </summary>
        protected object ReadCharData(DbDataReader reader, int ordinal, Type asType)
        {
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;

            if (asType == typeof(string))
                return reader.GetString(ordinal);
            else if (asType == typeof(char[]) || asType == typeof(DBNull))
                return reader.GetString(ordinal).ToCharArray();
            else
                throw new NotSupportedException("Wrong type: " + asType.FullName);
        }

        /// <summary>
        /// helper method to read byte-array data from the reader
        /// </summary>
        protected object ReadByteArray(DbDataReader reader, int ordinal, Type asType)
        {
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;

            if (asType == typeof(byte[]) || asType == typeof(DBNull))
                return (byte[])reader.GetValue(ordinal);
            else
                throw new NotSupportedException("Wrong type: " + asType.FullName);
        }

        protected bool IsNullOrDbNull(object value)
        {
            return DBNull.Value.Equals(value) || value == null;
        }

        /// <summary>
        /// helper method to check that actual test value has same type as expectedType or it is dbnull.
        /// </summary>
        /// <param name="bothDbNull">set to true if both values are DbNull</param>
        /// <returns>true if expected value is DbNull or has the expected type</returns>
        protected bool CompareDbNullAndType(Type expectedType, object expected, object actual, out bool bothDbNull)
        {
            bool isNullExpected = IsNullOrDbNull(expected);
            bool isNullActual = IsNullOrDbNull(actual);

            bothDbNull = isNullActual && isNullExpected;

            if (bothDbNull)
                return true;

            if (isNullActual || isNullExpected)
                return false; // only one is null, but not both

            if (expectedType == null)
                return true;

            // both not null
            if (expectedType != expected.GetType())
                throw new ArgumentException("Wrong type!");

            return (expectedType == actual.GetType());
        }

        /// <summary>
        /// helper method to compare two byte arrays
        /// </summary>
        /// <remarks>I considered use of Generics here, but switched to explicit typed version due to performace overhead.
        /// When using generic version, there is no way to quickly compare two values (expected[i] == actual[i]), and using
        /// Equals method performs boxing, increasing the time spent on this method.</remarks>
        private bool CompareByteArray(byte[] expected, byte[] actual, bool allowIncomplete = false, byte paddingValue = 0)
        {
            if (expected.Length > actual.Length)
            {
                return false;
            }
            else if (!allowIncomplete && expected.Length < actual.Length)
            {
                return false;
            }

            // check expected array values
            int end = expected.Length;
            for (int i = 0; i < end; i++)
            {
                if (expected[i] != actual[i])
                    return false;
            }

            // check for padding in actual values
            end = actual.Length;
            for (int i = expected.Length; i < end; i++)
            {
                // ensure rest of array are zeros
                if (paddingValue != actual[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// helper method to compare two char arrays
        /// </summary>
        /// <remarks>I considered use of Generics here, but switched to explicit typed version due to performace overhead.
        /// When using generic version, there is no way to quickly compare two values (expected[i] == actual[i]), and using
        /// Equals method performs boxing, increasing the time spent on this method.</remarks>
        private bool CompareCharArray(char[] expected, char[] actual, bool allowIncomplete = false, char paddingValue = ' ')
        {
            if (expected.Length > actual.Length)
            {
                return false;
            }
            else if (!allowIncomplete && expected.Length < actual.Length)
            {
                return false;
            }

            // check expected array values
            int end = expected.Length;
            for (int i = 0; i < end; i++)
            {
                if (expected[i] != actual[i])
                    return false;
            }

            // check for padding in actual values
            end = actual.Length;
            for (int i = expected.Length; i < end; i++)
            {
                // ensure rest of array are zeros
                if (paddingValue != actual[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// helper method to compare two non-array values.
        /// </summary>
        protected bool CompareValues<T>(object expected, object actual) where T : struct
        {
            bool bothDbNull;
            if (!CompareDbNullAndType(typeof(T), expected, actual, out bothDbNull) || bothDbNull)
                return bothDbNull;

            return expected.Equals(actual);
        }


        /// <summary>
        /// validates that the actual value is DbNull or byte array and compares it to expected
        /// </summary>
        protected bool CompareByteArray(object expected, object actual, bool allowIncomplete, byte paddingValue = 0)
        {
            bool bothDbNull;
            if (!CompareDbNullAndType(typeof(byte[]), expected, actual, out bothDbNull) || bothDbNull)
                return bothDbNull;
            return CompareByteArray((byte[])expected, (byte[])actual, allowIncomplete, paddingValue);
        }

        /// <summary>
        /// validates that the actual value is DbNull or char array and compares it to expected
        /// </summary>
        protected bool CompareCharArray(object expected, object actual, bool allowIncomplete, char paddingValue = ' ')
        {
            bool bothDbNull;
            if (!CompareDbNullAndType(typeof(char[]), expected, actual, out bothDbNull) || bothDbNull)
                return bothDbNull;
            return CompareCharArray((char[])expected, (char[])actual, allowIncomplete, paddingValue);
        }

        /// <summary>
        /// helper method to reads datetime from the reader
        /// </summary>
        protected object ReadDateTime(DbDataReader reader, int ordinal, Type asType)
        {
            ValidateReadType(typeof(DateTime), asType);
            if (reader.IsDBNull(ordinal))
                return DBNull.Value;
            return reader.GetDateTime(ordinal);
        }

        protected void ValidateReadType(Type expectedType, Type readAsType)
        {
            if (readAsType != expectedType && readAsType != typeof(DBNull))
                throw new ArgumentException("Wrong type: " + readAsType.FullName);
        }

        /// <summary>
        /// this method is called to read the value from the data reader
        /// </summary>
        public object Read(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType)
        {
            if (reader == null || asType == null)
                throw new ArgumentNullException("reader == null || asType == null");
            ValidateColumnInfo(columnInfo);
            return ReadInternal(reader, ordinal, columnInfo, asType);
        }

        protected abstract object ReadInternal(DbDataReader reader, int ordinal, SqlRandomTableColumn columnInfo, Type asType);

        /// <summary>
        /// used to check if this column can be compared; returns true by default (timestamp and column set columns return false)
        /// </summary>
        public virtual bool CanCompareValues(SqlRandomTableColumn columnInfo)
        {
            return true;
        }

        /// <summary>
        /// This method is called to compare the actual value read to expected. Expected value must be either dbnull or from the given type.
        /// Actual value can be any.
        /// </summary>
        public bool CompareValues(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            ValidateColumnInfo(columnInfo);
            return CompareValuesInternal(columnInfo, expected, actual);
        }

        protected abstract bool CompareValuesInternal(SqlRandomTableColumn columnInfo, object expected, object actual);

        public string BuildErrorMessage(SqlRandomTableColumn columnInfo, object expected, object actual)
        {
            ValidateColumnInfo(columnInfo);

            string expectedAsString = IsNullOrDbNull(expected) ? "null" : "\"" + ValueAsString(columnInfo, expected) + "\"";
            string actualAsString = IsNullOrDbNull(actual) ? "null" : "\"" + ValueAsString(columnInfo, actual) + "\"";

            return string.Format(CultureInfo.InvariantCulture,
                "  Column type: {0}\n" +
                "  Expected value: {1}\n" +
                "  Actual value: {2}",
                GetTSqlTypeDefinition(columnInfo),
                expectedAsString,
                actualAsString);
        }

        /// <summary>
        /// using ToString by default, supports arrays and many primitives;
        /// override as needed
        /// </summary>
        /// <param name="value">value is not null or dbnull(validated before)</param>
        protected virtual string ValueAsString(SqlRandomTableColumn columnInfo, object value)
        {
            Array a = value as Array;
            if (a == null)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", value.GetType().Name, PrimitiveValueAsString(value));
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}[{1}]", value.GetType().Name, a.Length);
                if (a.Length > 0)
                {
                    sb.Append(":");
                    for (int i = 0; i < a.Length; i++)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, " {0}", PrimitiveValueAsString(a.GetValue(i)));
                    }
                }

                return sb.ToString();
            }
        }

        public string PrimitiveValueAsString(object value)
        {
            if (value is char)
            {
                int c = (int)(char)value; // double-cast is needed from object
                return c.ToString("X4", CultureInfo.InvariantCulture);
            }
            else if (value is byte)
            {
                byte b = (byte)value;
                return b.ToString("X2", CultureInfo.InvariantCulture);
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", value.ToString());
            }
        }
    }
}
