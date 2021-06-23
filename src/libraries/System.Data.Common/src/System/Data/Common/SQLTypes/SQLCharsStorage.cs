// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common
{
    internal sealed class SqlCharsStorage : DataStorage
    {
        private SqlChars[] _values = default!; // Late-initialized

        public SqlCharsStorage(DataColumn column)
        : base(column, typeof(SqlChars), SqlChars.Null, SqlChars.Null, StorageType.SqlChars)
        {
        }

        public override object Aggregate(int[] records, AggregateType kind)
        {
            try
            {
                switch (kind)
                {
                    case AggregateType.First: // Does not seem to be implemented
                        if (records.Length > 0)
                        {
                            return _values[records[0]];
                        }
                        return null!; // no data => null

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
                throw ExprException.Overflow(typeof(SqlChars));
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            //            throw ExceptionBuilder.IComparableNotDefined;
            return 0;
        }

        public override int CompareValueTo(int recordNo, object? value)
        {
            //            throw ExceptionBuilder.IComparableNotDefined;
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
                _values[record] = SqlChars.Null;
            }
            else
            {
                _values[record] = (SqlChars)value;
            }
        }

        public override void SetCapacity(int capacity)
        {
            SqlChars[] newValues = new SqlChars[capacity];
            if (null != _values)
            {
                Array.Copy(_values, newValues, Math.Min(capacity, _values.Length));
            }
            _values = newValues;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override object ConvertXmlToObject(string s)
        {
            SqlString newValue = default;

            string tempStr = string.Concat("<col>", s, "</col>"); // this is done since you can give fragmet to reader
            StringReader strReader = new StringReader(tempStr);

            IXmlSerializable tmp = newValue;

            using (XmlTextReader xmlTextReader = new XmlTextReader(strReader))
            {
                tmp.ReadXml(xmlTextReader);
            }
            return (new SqlChars((SqlString)tmp));
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override string ConvertObjectToXml(object value)
        {
            Debug.Assert(!DataStorage.IsObjectNull(value), "we shouldn't have null here");
            Debug.Assert((value.GetType() == typeof(SqlChars)), "wrong input type");

            StringWriter strwriter = new StringWriter(FormatProvider);

            using (XmlTextWriter xmlTextWriter = new XmlTextWriter(strwriter))
            {
                ((IXmlSerializable)value).WriteXml(xmlTextWriter);
            }
            return (strwriter.ToString());
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new SqlChars[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            SqlChars[] typedStore = (SqlChars[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, IsNull(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (SqlChars[])store;
            //SetNullStorage(nullbits);
        }
    }
}
