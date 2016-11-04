// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data.Common
{
    internal sealed class SqlBytesStorage : DataStorage
    {
        private SqlBytes[] _values;

        public SqlBytesStorage(DataColumn column)
        : base(column, typeof(SqlBytes), SqlBytes.Null, SqlBytes.Null, StorageType.SqlBytes)
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
                        return null;// no data => null

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
                throw ExprException.Overflow(typeof(SqlBytes));
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            return 0;
        }

        public override int CompareValueTo(int recordNo, object value)
        {
            return 0;
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
            if ((value == DBNull.Value) || (value == null))
            {
                _values[record] = SqlBytes.Null;
            }
            else
            {
                _values[record] = (SqlBytes)value;
            }
        }

        public override void SetCapacity(int capacity)
        {
            SqlBytes[] newValues = new SqlBytes[capacity];
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
            return (new SqlBytes((SqlBinary)tmp));
        }

        public override string ConvertObjectToXml(object value)
        {
            Debug.Assert(!DataStorage.IsObjectNull(value), "we shouldn't have null here");
            Debug.Assert((value.GetType() == typeof(SqlBytes)), "wrong input type");

            StringWriter strwriter = new StringWriter(FormatProvider);

            using (XmlTextWriter xmlTextWriter = new XmlTextWriter(strwriter))
            {
                ((IXmlSerializable)value).WriteXml(xmlTextWriter);
            }
            return (strwriter.ToString());
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new SqlBytes[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            SqlBytes[] typedStore = (SqlBytes[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, IsNull(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (SqlBytes[])store;
            //SetNullStorage(nullbits);
        }
    }
}
