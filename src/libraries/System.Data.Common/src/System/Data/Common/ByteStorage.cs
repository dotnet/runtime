// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data.Common
{
    internal sealed class ByteStorage : DataStorage
    {
        private const byte defaultValue = 0;

        private byte[] _values = default!; // Late-initialized

        internal ByteStorage(DataColumn column) : base(column, typeof(byte), defaultValue, StorageType.Byte)
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
                        ulong sum = defaultValue;
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
                        long meanSum = defaultValue;
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
                            byte mean;
                            checked { mean = (byte)(meanSum / meanCount); }
                            return mean;
                        }
                        return _nullValue;

                    case AggregateType.Var:
                    case AggregateType.StDev:
                        int count = 0;
                        double var = defaultValue;
                        double prec = defaultValue;
                        double dsum = defaultValue;
                        double sqrsum = defaultValue;

                        foreach (int record in records)
                        {
                            if (IsNull(record))
                                continue;
                            dsum += _values[record];
                            sqrsum += _values[record] * (double)_values[record];
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
                                var /= (count * (count - 1));

                            if (kind == AggregateType.StDev)
                            {
                                return Math.Sqrt(var);
                            }
                            return var;
                        }
                        return _nullValue;

                    case AggregateType.Min:
                        byte min = byte.MaxValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            min = Math.Min(_values[record], min);
                            hasData = true;
                        }
                        if (hasData)
                        {
                            return min;
                        }
                        return _nullValue;

                    case AggregateType.Max:
                        byte max = byte.MinValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            max = Math.Max(_values[record], max);
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
                        return base.Aggregate(records, kind);
                }
            }
            catch (OverflowException)
            {
                throw ExprException.Overflow(typeof(byte));
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            byte valueNo1 = _values[recordNo1];
            byte valueNo2 = _values[recordNo2];

            if (valueNo1 == defaultValue || valueNo2 == defaultValue)
            {
                int bitCheck = CompareBits(recordNo1, recordNo2);
                if (0 != bitCheck)
                    return bitCheck;
            }
            return valueNo1.CompareTo(valueNo2);
        }

        public override int CompareValueTo(int recordNo, object? value)
        {
            Debug.Assert(0 <= recordNo, "Invalid record");
            System.Diagnostics.Debug.Assert(null != value, "null value");

            if (_nullValue == value)
            {
                if (IsNull(recordNo))
                {
                    return 0;
                }
                return 1;
            }

            byte valueNo1 = _values[recordNo];
            if ((defaultValue == valueNo1) && IsNull(recordNo))
            {
                return -1;
            }
            return valueNo1.CompareTo((byte)value);
        }

        public override object ConvertValue(object? value)
        {
            if (_nullValue != value)
            {
                if (null != value)
                {
                    value = ((IConvertible)value).ToByte(FormatProvider);
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
            byte value = _values[record];
            if (value != defaultValue)
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
                _values[record] = defaultValue;
                SetNullBit(record, true);
            }
            else
            {
                _values[record] = ((IConvertible)value).ToByte(FormatProvider);
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
            return XmlConvert.ToByte(s);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override string ConvertObjectToXml(object value)
        {
            return XmlConvert.ToString((byte)value);
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new byte[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            byte[] typedStore = (byte[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, IsNull(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (byte[])store;
            SetNullStorage(nullbits);
        }
    }
}
