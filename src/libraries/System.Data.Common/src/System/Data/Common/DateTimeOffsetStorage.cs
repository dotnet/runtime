// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common
{
    internal sealed class DateTimeOffsetStorage : DataStorage
    {
        private static readonly DateTimeOffset s_defaultValue = DateTimeOffset.MinValue;

        private DateTimeOffset[] _values = default!; // Late-initialized

        internal DateTimeOffsetStorage(DataColumn column)
        : base(column, typeof(DateTimeOffset), s_defaultValue, StorageType.DateTimeOffset)
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
                        DateTimeOffset min = DateTimeOffset.MaxValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (HasValue(record))
                            {
                                min = (DateTimeOffset.Compare(_values[record], min) < 0) ? _values[record] : min;
                                hasData = true;
                            }
                        }
                        if (hasData)
                        {
                            return min;
                        }
                        return _nullValue;

                    case AggregateType.Max:
                        DateTimeOffset max = DateTimeOffset.MinValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (HasValue(record))
                            {
                                max = (DateTimeOffset.Compare(_values[record], max) >= 0) ? _values[record] : max;
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
                        int count = 0;
                        for (int i = 0; i < records.Length; i++)
                        {
                            if (HasValue(records[i]))
                            {
                                count++;
                            }
                        }
                        return count;
                }
            }
            catch (OverflowException)
            {
                throw ExprException.Overflow(typeof(DateTimeOffset));
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            DateTimeOffset valueNo1 = _values[recordNo1];
            DateTimeOffset valueNo2 = _values[recordNo2];

            if (valueNo1 == s_defaultValue || valueNo2 == s_defaultValue)
            {
                int bitCheck = CompareBits(recordNo1, recordNo2);
                if (0 != bitCheck)
                {
                    return bitCheck;
                }
            }
            return DateTimeOffset.Compare(valueNo1, valueNo2);
        }

        public override int CompareValueTo(int recordNo, object? value)
        {
            System.Diagnostics.Debug.Assert(0 <= recordNo, "Invalid record");
            System.Diagnostics.Debug.Assert(null != value, "null value");

            if (_nullValue == value)
            {
                return (HasValue(recordNo) ? 1 : 0);
            }

            DateTimeOffset valueNo1 = _values[recordNo];
            if ((s_defaultValue == valueNo1) && !HasValue(recordNo))
            {
                return -1;
            }
            return DateTimeOffset.Compare(valueNo1, (DateTimeOffset)value);
        }

        public override object ConvertValue(object? value)
        {
            if (_nullValue != value)
            {
                if (null != value)
                {
                    value = ((DateTimeOffset)value);
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
            DateTimeOffset value = _values[record];
            if ((value != s_defaultValue) || HasValue(record))
            {
                return value;
            }
            return _nullValue;
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
                _values[record] = (DateTimeOffset)value;
                SetNullBit(record, false);
            }
        }

        public override void SetCapacity(int capacity)
        {
            DateTimeOffset[] newValues = new DateTimeOffset[capacity];
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
            return XmlConvert.ToDateTimeOffset(s);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override string ConvertObjectToXml(object value)
        {
            return XmlConvert.ToString((DateTimeOffset)value);
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new DateTimeOffset[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            DateTimeOffset[] typedStore = (DateTimeOffset[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, !HasValue(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (DateTimeOffset[])store;
            SetNullStorage(nullbits);
        }
    }
}
