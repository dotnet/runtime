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
    internal sealed class SqlInt32Storage : DataStorage
    {
        private SqlInt32[] _values;

        public SqlInt32Storage(DataColumn column)
        : base(column, typeof(SqlInt32), SqlInt32.Null, SqlInt32.Null, StorageType.SqlInt32)
        {
        }

        public override object Aggregate(int[] records, AggregateType kind)
        {
            bool hasData = false;
            try
            {
                switch (kind)
                {
                    case AggregateType.Sum:
                        SqlInt64 sum = 0;
                        foreach (int record in records)
                        {
                            if (IsNull(record))
                                continue;
                            checked { sum += _values[record]; }
                            hasData = true;
                        }
                        if (hasData)
                        {
                            return sum;
                        }
                        return _nullValue;

                    case AggregateType.Mean:
                        SqlInt64 meanSum = 0;
                        int meanCount = 0;
                        foreach (int record in records)
                        {
                            if (IsNull(record))
                                continue;
                            checked { meanSum += (_values[record]).ToSqlInt64(); }
                            meanCount++;
                            hasData = true;
                        }
                        if (hasData)
                        {
                            SqlInt32 mean = 0;
                            checked { mean = (meanSum / meanCount).ToSqlInt32(); }
                            return mean;
                        }
                        return _nullValue;

                    case AggregateType.Var:
                    case AggregateType.StDev:
                        int count = 0;
                        SqlDouble var = 0;
                        SqlDouble prec = 0;
                        SqlDouble dsum = 0;
                        SqlDouble sqrsum = 0;

                        foreach (int record in records)
                        {
                            if (IsNull(record))
                                continue;
                            dsum += (_values[record]).ToSqlDouble();
                            sqrsum += (_values[record]).ToSqlDouble() * (_values[record]).ToSqlDouble();
                            count++;
                        }

                        if (count > 1)
                        {
                            var = count * sqrsum - (dsum * dsum);
                            prec = var / (dsum * dsum);

                            // we are dealing with the risk of a cancellation error
                            // double is guaranteed only for 15 digits so a difference 
                            // with a result less than 1e-15 should be considered as zero

                            if ((prec < 1e-15) || (var < 0))
                                var = 0;
                            else
                                var = var / (count * (count - 1));

                            if (kind == AggregateType.StDev)
                            {
                                return Math.Sqrt(var.Value);
                            }
                            return var;
                        }
                        return _nullValue;

                    case AggregateType.Min:
                        SqlInt32 min = SqlInt32.MaxValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            if ((SqlInt32.LessThan(_values[record], min)).IsTrue)
                                min = _values[record];
                            hasData = true;
                        }
                        if (hasData)
                        {
                            return min;
                        }
                        return _nullValue;

                    case AggregateType.Max:
                        SqlInt32 max = SqlInt32.MinValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            if ((SqlInt32.GreaterThan(_values[record], max)).IsTrue)
                                max = _values[record];
                            hasData = true;
                        }
                        if (hasData)
                        {
                            return max;
                        }
                        return _nullValue;

                    case AggregateType.First:
                        if (records.Length > 0)
                        {
                            return _values[records[0]];
                        }
                        return null;

                    case AggregateType.Count:
                        count = 0;
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
                throw ExprException.Overflow(typeof(SqlInt32));
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            return _values[recordNo1].CompareTo(_values[recordNo2]);
        }

        public override int CompareValueTo(int recordNo, object value)
        {
            return _values[recordNo].CompareTo((SqlInt32)value);
        }

        public override object ConvertValue(object value)
        {
            if (null != value)
            {
                return SqlConvert.ConvertToSqlInt32(value);
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
            _values[record] = SqlConvert.ConvertToSqlInt32(value);
        }

        public override void SetCapacity(int capacity)
        {
            SqlInt32[] newValues = new SqlInt32[capacity];
            if (null != _values)
            {
                Array.Copy(_values, 0, newValues, 0, Math.Min(capacity, _values.Length));
            }
            _values = newValues;
        }

        public override object ConvertXmlToObject(string s)
        {
            SqlInt32 newValue = new SqlInt32();
            string tempStr = string.Concat("<col>", s, "</col>"); // this is done since you can give fragmet to reader
            StringReader strReader = new StringReader(tempStr);

            IXmlSerializable tmp = newValue;

            using (XmlTextReader xmlTextReader = new XmlTextReader(strReader))
            {
                tmp.ReadXml(xmlTextReader);
            }
            return ((SqlInt32)tmp);
        }

        public override string ConvertObjectToXml(object value)
        {
            Debug.Assert(!DataStorage.IsObjectNull(value), "we shouldn't have null here");
            Debug.Assert((value.GetType() == typeof(SqlInt32)), "wrong input type");

            StringWriter strwriter = new StringWriter(FormatProvider);

            using (XmlTextWriter xmlTextWriter = new XmlTextWriter(strwriter))
            {
                ((IXmlSerializable)value).WriteXml(xmlTextWriter);
            }
            return (strwriter.ToString());
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new SqlInt32[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            SqlInt32[] typedStore = (SqlInt32[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, IsNull(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (SqlInt32[])store;
            //SetNullStorage(nullbits);
        }
    }
}
