// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common
{
    internal sealed class SqlDecimalStorage : DataStorage
    {
        private SqlDecimal[] _values = default!; // Late-initialized

        public SqlDecimalStorage(DataColumn column)
        : base(column, typeof(SqlDecimal), SqlDecimal.Null, SqlDecimal.Null, StorageType.SqlDecimal)
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
                        SqlDecimal sum = 0;
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
                        SqlDecimal meanSum = 0;
                        int meanCount = 0;
                        foreach (int record in records)
                        {
                            if (IsNull(record))
                                continue;
                            checked { meanSum += _values[record]; }
                            meanCount++;
                            hasData = true;
                        }
                        if (hasData)
                        {
                            SqlDecimal mean = 0;
                            checked { mean = (meanSum / meanCount); }
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
                            dsum += _values[record].ToSqlDouble();
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
                        SqlDecimal min = SqlDecimal.MaxValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            if ((SqlDecimal.LessThan(_values[record], min)).IsTrue)
                                min = _values[record];
                            hasData = true;
                        }
                        if (hasData)
                        {
                            return min;
                        }
                        return _nullValue;

                    case AggregateType.Max:
                        SqlDecimal max = SqlDecimal.MinValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            if ((SqlDecimal.GreaterThan(_values[record], max)).IsTrue)
                                max = _values[record];
                            hasData = true;
                        }
                        if (hasData)
                        {
                            return max;
                        }
                        return _nullValue;

                    case AggregateType.First: // Does not seem to be implemented
                        if (records.Length > 0)
                        {
                            return _values[records[0]];
                        }
                        return null!;
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
                throw ExprException.Overflow(typeof(SqlDecimal));
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            return _values[recordNo1].CompareTo(_values[recordNo2]);
        }

        public override int CompareValueTo(int recordNo, object? value)
        {
            Debug.Assert(null != value, "null value");
            return _values[recordNo].CompareTo((SqlDecimal)value);
        }

        public override object ConvertValue(object? value)
        {
            if (null != value)
            {
                return SqlConvert.ConvertToSqlDecimal(value);
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
            _values[record] = SqlConvert.ConvertToSqlDecimal(value);
        }

        public override void SetCapacity(int capacity)
        {
            SqlDecimal[] newValues = new SqlDecimal[capacity];
            if (null != _values)
            {
                Array.Copy(_values, newValues, Math.Min(capacity, _values.Length));
            }
            _values = newValues;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override object ConvertXmlToObject(string s)
        {
            SqlDecimal newValue = default;
            string tempStr = string.Concat("<col>", s, "</col>"); // this is done since you can give fragmet to reader
            StringReader strReader = new StringReader(tempStr);

            IXmlSerializable tmp = newValue;

            using (XmlTextReader xmlTextReader = new XmlTextReader(strReader))
            {
                tmp.ReadXml(xmlTextReader);
            }
            return ((SqlDecimal)tmp);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override string ConvertObjectToXml(object value)
        {
            Debug.Assert(!DataStorage.IsObjectNull(value), "we shouldn't have null here");
            Debug.Assert((value.GetType() == typeof(SqlDecimal)), "wrong input type");

            StringWriter strwriter = new StringWriter(FormatProvider);

            using (XmlTextWriter xmlTextWriter = new XmlTextWriter(strwriter))
            {
                ((IXmlSerializable)value).WriteXml(xmlTextWriter);
            }
            return (strwriter.ToString());
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new SqlDecimal[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            SqlDecimal[] typedStore = (SqlDecimal[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, IsNull(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (SqlDecimal[])store;
            //SetNullStorage(nullbits);
        }
    }
}
