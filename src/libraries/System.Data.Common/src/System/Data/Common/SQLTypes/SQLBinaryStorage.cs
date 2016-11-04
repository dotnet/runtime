// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Collections;

namespace System.Data.Common
{
    internal sealed class SqlBinaryStorage : DataStorage
    {
        private SqlBinary[] _values;

        public SqlBinaryStorage(DataColumn column) : base(column, typeof(SqlBinary), SqlBinary.Null, SqlBinary.Null, StorageType.SqlBinary)
        {
        }

        public override object Aggregate(int[] records, AggregateType kind)
        {
            try
            {
                switch (kind)
                {
                    case AggregateType.First:
                        if (records.Length > 0)
                        {
                            return _values[records[0]];
                        }
                        return null;

                    case AggregateType.Count:
                        int count = 0;
                        for (int i = 0; i < records.Length; i++)
                        {
                            if (!IsNull(records[i]))
                                count++;
                        }
                        return count;
                }
            }
            catch (OverflowException)
            {
                throw ExprException.Overflow(typeof(SqlBinary));
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            return _values[recordNo1].CompareTo(_values[recordNo2]);
        }

        public override int CompareValueTo(int recordNo, object value)
        {
            return _values[recordNo].CompareTo((SqlBinary)value);
        }

        public override object ConvertValue(object value)
        {
            if (null != value)
            {
                return SqlConvert.ConvertToSqlBinary(value);
            }
            return _nullValue;
        }

        public override void Copy(int recordNo1, int recordNo2)
        {
            _values[recordNo2] = _values[recordNo1];
        }

        public override object Get(int record)
        {
            return _values[record];
        }

        public override bool IsNull(int record)
        {
            return (_values[record].IsNull);
        }

        public override void Set(int record, object value)
        {
            _values[record] = SqlConvert.ConvertToSqlBinary(value);
        }

        public override void SetCapacity(int capacity)
        {
            SqlBinary[] newValues = new SqlBinary[capacity];
            if (null != _values)
            {
                Array.Copy(_values, 0, newValues, 0, Math.Min(capacity, _values.Length));
            }
            _values = newValues;
        }

        public override object ConvertXmlToObject(string s)
        {
            SqlBinary newValue = new SqlBinary();
            string tempStr = string.Concat("<col>", s, "</col>"); // this is done since you can give fragmet to reader
            StringReader strReader = new StringReader(tempStr);

            IXmlSerializable tmp = newValue;

            using (XmlTextReader xmlTextReader = new XmlTextReader(strReader))
            {
                tmp.ReadXml(xmlTextReader);
            }
            return ((SqlBinary)tmp);
        }

        public override string ConvertObjectToXml(object value)
        {
            Debug.Assert(!DataStorage.IsObjectNull(value), "we should have null here");
            Debug.Assert((value.GetType() == typeof(SqlBinary)), "wrong input type");

            StringWriter strwriter = new StringWriter(FormatProvider);

            using (XmlTextWriter xmlTextWriter = new XmlTextWriter(strwriter))
            {
                ((IXmlSerializable)value).WriteXml(xmlTextWriter);
            }
            return (strwriter.ToString());
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new SqlBinary[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            SqlBinary[] typedStore = (SqlBinary[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, IsNull(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (SqlBinary[])store;
            //SetNullStorage(nullbits);
        }
    }
}
