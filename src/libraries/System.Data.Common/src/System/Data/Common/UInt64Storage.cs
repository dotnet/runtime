// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data.Common
{
    internal sealed class UInt64Storage : DataStorage
    {
        private const ulong DefaultValue = ulong.MinValue;

        private ulong[] _values = default!; // Late-initialized

        public UInt64Storage(DataColumn column)
        : base(column, typeof(ulong), DefaultValue, StorageType.UInt64)
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
                        ulong sum = DefaultValue;
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
                            ulong mean;
                            checked { mean = (ulong)(meanSum / (decimal)meanCount); }
                            return mean;
                        }
                        return _nullValue;

                    case AggregateType.Var:
                    case AggregateType.StDev:
                        int count = 0;
                        double var = 0.0f;
                        double prec = 0.0f;
                        double dsum = 0.0f;
                        double sqrsum = 0.0f;

                        foreach (int record in records)
                        {
                            if (HasValue(record))
                            {
                                dsum += _values[record];
                                sqrsum += _values[record] * (double)_values[record];
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
                                var /= (count * (count - 1));

                            if (kind == AggregateType.StDev)
                            {
                                return Math.Sqrt(var);
                            }
                            return var;
                        }
                        return _nullValue;

                    case AggregateType.Min:
                        ulong min = ulong.MaxValue;
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
                        ulong max = ulong.MinValue;
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
                throw ExprException.Overflow(typeof(ulong));
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            ulong valueNo1 = _values[recordNo1];
            ulong valueNo2 = _values[recordNo2];

            if (valueNo1.Equals(DefaultValue) || valueNo2.Equals(DefaultValue))
            {
                int bitCheck = CompareBits(recordNo1, recordNo2);
                if (0 != bitCheck)
                    return bitCheck;
            }
            //return valueNo1.CompareTo(valueNo2);
            return (valueNo1 < valueNo2 ? -1 : (valueNo1 > valueNo2 ? 1 : 0)); // similar to UInt64.CompareTo(UInt64)
        }

        public override int CompareValueTo(int recordNo, object? value)
        {
            System.Diagnostics.Debug.Assert(0 <= recordNo, "Invalid record");
            System.Diagnostics.Debug.Assert(null != value, "null value");

            if (_nullValue == value)
            {
                return (HasValue(recordNo) ? 1 : 0);
            }

            ulong valueNo1 = _values[recordNo];
            if ((DefaultValue == valueNo1) && !HasValue(recordNo))
            {
                return -1;
            }
            return valueNo1.CompareTo((ulong)value);
            //return(valueNo1 < valueNo2 ? -1 : (valueNo1 > valueNo2 ? 1 : 0)); // similar to UInt64.CompareTo(UInt64)
        }

        public override object ConvertValue(object? value)
        {
            if (_nullValue != value)
            {
                if (null != value)
                {
                    value = ((IConvertible)value).ToUInt64(FormatProvider);
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
            ulong value = _values[record];
            if (!value.Equals(DefaultValue))
            {
                return value;
            }
            return GetBits(record);
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
                _values[record] = ((IConvertible)value).ToUInt64(FormatProvider);
                SetNullBit(record, false);
            }
        }

        public override void SetCapacity(int capacity)
        {
            Array.Resize(ref _values, capacity);
            base.SetCapacity(capacity);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override object ConvertXmlToObject(string s)
        {
            return XmlConvert.ToUInt64(s);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override string ConvertObjectToXml(object value)
        {
            return XmlConvert.ToString((ulong)value);
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new ulong[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            ulong[] typedStore = (ulong[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, !HasValue(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (ulong[])store;
            SetNullStorage(nullbits);
        }
    }
}
