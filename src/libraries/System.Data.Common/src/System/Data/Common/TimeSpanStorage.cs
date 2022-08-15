// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data.Common
{
    internal sealed class TimeSpanStorage : DataStorage
    {
        private static readonly TimeSpan s_defaultValue = TimeSpan.Zero;

        private TimeSpan[] _values = default!; // Late-initialized

        public TimeSpanStorage(DataColumn column)
        : base(column, typeof(TimeSpan), s_defaultValue, StorageType.TimeSpan)
        {
        }

        public override object Aggregate(int[] records, AggregateType kind)
        {
            bool hasData = false;
            try
            {
                switch (kind)
                {
                    case AggregateType.Min:
                        TimeSpan min = TimeSpan.MaxValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            min = (TimeSpan.Compare(_values[record], min) < 0) ? _values[record] : min;
                            hasData = true;
                        }
                        if (hasData)
                        {
                            return min;
                        }
                        return _nullValue;

                    case AggregateType.Max:
                        TimeSpan max = TimeSpan.MinValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            max = (TimeSpan.Compare(_values[record], max) >= 0) ? _values[record] : max;
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

                    case AggregateType.Sum:
                        {
                            decimal sum = 0;
                            foreach (int record in records)
                            {
                                if (IsNull(record))
                                    continue;
                                sum += _values[record].Ticks;
                                hasData = true;
                            }
                            if (hasData)
                            {
                                return TimeSpan.FromTicks((long)Math.Round(sum));
                            }
                            return null!; // TODO: This is incorrect, should be DBNull.Value
                        }

                    case AggregateType.Mean:
                        {
                            decimal meanSum = 0;
                            int meanCount = 0;
                            foreach (int record in records)
                            {
                                if (IsNull(record))
                                    continue;
                                meanSum += _values[record].Ticks;
                                meanCount++;
                            }
                            if (meanCount > 0)
                            {
                                return TimeSpan.FromTicks((long)Math.Round(meanSum / meanCount));
                            }
                            return null!; // TODO: This is incorrect, should be DBNull.Value
                        }

                    case AggregateType.StDev:
                        {
                            int count = 0;
                            decimal meanSum = 0;

                            foreach (int record in records)
                            {
                                if (IsNull(record))
                                    continue;
                                meanSum += _values[record].Ticks;
                                count++;
                            }

                            if (count > 1)
                            {
                                double varSum = 0;
                                decimal mean = meanSum / count;
                                foreach (int record in records)
                                {
                                    if (IsNull(record))
                                        continue;
                                    double x = (double)(_values[record].Ticks - mean);
                                    varSum += x * x;
                                }
                                ulong stDev = (ulong)Math.Round(Math.Sqrt(varSum / (count - 1)));
                                if (stDev > long.MaxValue)
                                {
                                    stDev = long.MaxValue;
                                }
                                return TimeSpan.FromTicks((long)stDev);
                            }
                            return null!; // TODO: This is incorrect, should be DBNull.Value
                        }
                }
            }
            catch (OverflowException)
            {
                throw ExprException.Overflow(typeof(TimeSpan));
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            TimeSpan valueNo1 = _values[recordNo1];
            TimeSpan valueNo2 = _values[recordNo2];

            if (valueNo1 == s_defaultValue || valueNo2 == s_defaultValue)
            {
                int bitCheck = CompareBits(recordNo1, recordNo2);
                if (0 != bitCheck)
                    return bitCheck;
            }
            return TimeSpan.Compare(valueNo1, valueNo2);
        }

        public override int CompareValueTo(int recordNo, object? value)
        {
            System.Diagnostics.Debug.Assert(0 <= recordNo, "Invalid record");
            System.Diagnostics.Debug.Assert(null != value, "null value");

            if (_nullValue == value)
            {
                if (IsNull(recordNo))
                {
                    return 0;
                }
                return 1;
            }

            TimeSpan valueNo1 = _values[recordNo];
            if ((s_defaultValue == valueNo1) && IsNull(recordNo))
            {
                return -1;
            }
            return valueNo1.CompareTo((TimeSpan)value);
        }

        private static TimeSpan ConvertToTimeSpan(object value)
        {
            // Do not change these checks
            Type typeofValue = value.GetType();

            if (typeofValue == typeof(string))
            {
                return TimeSpan.Parse((string)value);
            }
            else if (typeofValue == typeof(int))
            {
                return new TimeSpan((int)value);
            }
            else if (typeofValue == typeof(long))
            {
                return new TimeSpan((long)value);
            }
            else
            {
                return (TimeSpan)value;
            }
        }

        public override object ConvertValue(object? value)
        {
            if (_nullValue != value)
            {
                if (null != value)
                {
                    value = ConvertToTimeSpan(value);
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
            TimeSpan value = _values[record];
            if (value != s_defaultValue)
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
                _values[record] = s_defaultValue;
                SetNullBit(record, true);
            }
            else
            {
                _values[record] = ConvertToTimeSpan(value);
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
            return XmlConvert.ToTimeSpan(s);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override string ConvertObjectToXml(object value)
        {
            return XmlConvert.ToString((TimeSpan)value);
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new TimeSpan[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            TimeSpan[] typedStore = (TimeSpan[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, IsNull(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (TimeSpan[])store;
            SetNullStorage(nullbits);
        }
    }
}
