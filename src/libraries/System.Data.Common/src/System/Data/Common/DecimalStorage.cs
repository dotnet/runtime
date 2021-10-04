// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common
{
    internal sealed class DecimalStorage : DataStorage
    {
        private const decimal DefaultValue = decimal.Zero;

        private decimal[] _values = default!; // Late-initialized

        internal DecimalStorage(DataColumn column)
        : base(column, typeof(decimal), DefaultValue, StorageType.Decimal)
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
                        decimal sum = DefaultValue;
                        foreach (int record in records)
                        {
                            if (HasValue(record))
                            {
                                checked { sum += _values[record]; }
                                hasData = true;
                            }
                        }
                        if (hasData)
                        {
                            return sum;
                        }
                        return _nullValue;

                    case AggregateType.Mean:
                        decimal meanSum = DefaultValue;
                        int meanCount = 0;
                        foreach (int record in records)
                        {
                            if (HasValue(record))
                            {
                                checked { meanSum += _values[record]; }
                                meanCount++;
                                hasData = true;
                            }
                        }
                        if (hasData)
                        {
                            decimal mean;
                            checked { mean = (meanSum / meanCount); }
                            return mean;
                        }
                        return _nullValue;

                    case AggregateType.Var:
                    case AggregateType.StDev:
                        int count = 0;
                        double var = (double)DefaultValue;
                        double prec = (double)DefaultValue;
                        double dsum = (double)DefaultValue;
                        double sqrsum = (double)DefaultValue;

                        foreach (int record in records)
                        {
                            if (HasValue(record))
                            {
                                dsum += (double)_values[record];
                                sqrsum += (double)_values[record] * (double)_values[record];
                                count++;
                            }
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
                                return Math.Sqrt(var);
                            }
                            return var;
                        }
                        return _nullValue;

                    case AggregateType.Min:
                        decimal min = decimal.MaxValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (HasValue(record))
                            {
                                min = Math.Min(_values[record], min);
                                hasData = true;
                            }
                        }
                        if (hasData)
                        {
                            return min;
                        }
                        return _nullValue;

                    case AggregateType.Max:
                        decimal max = decimal.MinValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (HasValue(record))
                            {
                                max = Math.Max(_values[record], max);
                                hasData = true;
                            }
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
                        return base.Aggregate(records, kind);
                }
            }
            catch (OverflowException)
            {
                throw ExprException.Overflow(typeof(decimal));
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            decimal valueNo1 = _values[recordNo1];
            decimal valueNo2 = _values[recordNo2];

            if (valueNo1 == DefaultValue || valueNo2 == DefaultValue)
            {
                int bitCheck = CompareBits(recordNo1, recordNo2);
                if (0 != bitCheck)
                    return bitCheck;
            }
            return decimal.Compare(valueNo1, valueNo2); // InternalCall
        }

        public override int CompareValueTo(int recordNo, object? value)
        {
            System.Diagnostics.Debug.Assert(0 <= recordNo, "Invalid record");
            System.Diagnostics.Debug.Assert(null != value, "null value");

            if (_nullValue == value)
            {
                return (HasValue(recordNo) ? 1 : 0);
            }

            decimal valueNo1 = _values[recordNo];
            if ((DefaultValue == valueNo1) && !HasValue(recordNo))
            {
                return -1;
            }
            return decimal.Compare(valueNo1, (decimal)value);
        }

        public override object ConvertValue(object? value)
        {
            if (_nullValue != value)
            {
                if (null != value)
                {
                    value = ((IConvertible)value).ToDecimal(FormatProvider);
                }
                else
                {
                    value = _nullValue;
                }
            }
            return value;
        }

        public override void Copy(int recordNo1, int recordNo2)
        {
            CopyBits(recordNo1, recordNo2);
            _values[recordNo2] = _values[recordNo1];
        }

        public override object Get(int record)
        {
            return (HasValue(record) ? _values[record] : _nullValue);
        }

        public override void Set(int record, object value)
        {
            System.Diagnostics.Debug.Assert(null != value, "null value");
            if (_nullValue == value)
            {
                _values[record] = DefaultValue;
                SetNullBit(record, true);
            }
            else
            {
                _values[record] = ((IConvertible)value).ToDecimal(FormatProvider);
                SetNullBit(record, false);
            }
        }

        public override void SetCapacity(int capacity)
        {
            decimal[] newValues = new decimal[capacity];
            if (null != _values)
            {
                Array.Copy(_values, newValues, Math.Min(capacity, _values.Length));
            }
            _values = newValues;
            base.SetCapacity(capacity);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override object ConvertXmlToObject(string s)
        {
            return XmlConvert.ToDecimal(s);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override string ConvertObjectToXml(object value)
        {
            return XmlConvert.ToString((decimal)value);
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new decimal[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            decimal[] typedStore = (decimal[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, !HasValue(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (decimal[])store;
            SetNullStorage(nullbits);
        }
    }
}
