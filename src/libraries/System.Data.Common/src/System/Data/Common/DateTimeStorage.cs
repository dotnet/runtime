// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common
{
    internal sealed class DateTimeStorage : DataStorage
    {
        private static readonly DateTime s_defaultValue = DateTime.MinValue;

        private DateTime[] _values = default!; // Late-initialized

        internal DateTimeStorage(DataColumn column)
        : base(column, typeof(DateTime), s_defaultValue, StorageType.DateTime)
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
                        DateTime min = DateTime.MaxValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (HasValue(record))
                            {
                                min = (DateTime.Compare(_values[record], min) < 0) ? _values[record] : min;
                                hasData = true;
                            }
                        }
                        if (hasData)
                        {
                            return min;
                        }
                        return _nullValue;

                    case AggregateType.Max:
                        DateTime max = DateTime.MinValue;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (HasValue(record))
                            {
                                max = (DateTime.Compare(_values[record], max) >= 0) ? _values[record] : max;
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
                throw ExprException.Overflow(typeof(DateTime));
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            DateTime valueNo1 = _values[recordNo1];
            DateTime valueNo2 = _values[recordNo2];

            if (valueNo1 == s_defaultValue || valueNo2 == s_defaultValue)
            {
                int bitCheck = CompareBits(recordNo1, recordNo2);
                if (0 != bitCheck)
                {
                    return bitCheck;
                }
            }
            return DateTime.Compare(valueNo1, valueNo2);
        }

        public override int CompareValueTo(int recordNo, object? value)
        {
            System.Diagnostics.Debug.Assert(0 <= recordNo, "Invalid record");
            System.Diagnostics.Debug.Assert(null != value, "null value");

            if (_nullValue == value)
            {
                return (HasValue(recordNo) ? 1 : 0);
            }

            DateTime valueNo1 = _values[recordNo];
            if ((s_defaultValue == valueNo1) && !HasValue(recordNo))
            {
                return -1;
            }
            return DateTime.Compare(valueNo1, (DateTime)value);
        }

        public override object ConvertValue(object? value)
        {
            if (_nullValue != value)
            {
                if (null != value)
                {
                    value = ((IConvertible)value).ToDateTime(FormatProvider);
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
            DateTime value = _values[record];
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
                DateTime retVal;
                DateTime tmpValue = ((IConvertible)value).ToDateTime(FormatProvider);
                switch (DateTimeMode)
                {
                    case DataSetDateTime.Utc:
                        if (tmpValue.Kind == DateTimeKind.Utc)
                        {
                            retVal = tmpValue;
                        }
                        else if (tmpValue.Kind == DateTimeKind.Local)
                        {
                            retVal = tmpValue.ToUniversalTime();
                        }
                        else
                        {
                            retVal = DateTime.SpecifyKind(tmpValue, DateTimeKind.Utc);
                        }
                        break;
                    case DataSetDateTime.Local:
                        if (tmpValue.Kind == DateTimeKind.Local)
                        {
                            retVal = tmpValue;
                        }
                        else if (tmpValue.Kind == DateTimeKind.Utc)
                        {
                            retVal = tmpValue.ToLocalTime();
                        }
                        else
                        {
                            retVal = DateTime.SpecifyKind(tmpValue, DateTimeKind.Local);
                        }
                        break;
                    case DataSetDateTime.Unspecified:
                    case DataSetDateTime.UnspecifiedLocal:
                        retVal = DateTime.SpecifyKind(tmpValue, DateTimeKind.Unspecified);
                        break;
                    default:
                        throw ExceptionBuilder.InvalidDateTimeMode(DateTimeMode);
                }
                _values[record] = retVal;
                SetNullBit(record, false);
            }
        }

        public override void SetCapacity(int capacity)
        {
            DateTime[] newValues = new DateTime[capacity];
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
            object retValue;
            if (DateTimeMode == DataSetDateTime.UnspecifiedLocal)
            {
                retValue = XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.Unspecified);
            }
            else
            {
                retValue = XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.RoundtripKind);
            }
            return retValue;
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override string ConvertObjectToXml(object value)
        {
            string retValue;
            if (DateTimeMode == DataSetDateTime.UnspecifiedLocal)
            {
                retValue = XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.Local);
            }
            else
            {
                retValue = XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
            }
            return retValue;
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new DateTime[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            DateTime[] typedStore = (DateTime[])store;
            bool isnull = !HasValue(record);
            if (isnull || (0 == (DateTimeMode & DataSetDateTime.Local)))
            {
                typedStore[storeIndex] = _values[record]; // already universal time
            }
            else
            {
                typedStore[storeIndex] = _values[record].ToUniversalTime();
            }
            nullbits.Set(storeIndex, isnull);
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (DateTime[])store;
            SetNullStorage(nullbits);
            if (DateTimeMode == DataSetDateTime.UnspecifiedLocal)
            {
                for (int i = 0; i < _values.Length; i++)
                {
                    if (HasValue(i))
                    {
                        _values[i] = DateTime.SpecifyKind(_values[i].ToLocalTime(), DateTimeKind.Unspecified); //Strip the kind for UnspecifiedLocal.
                    }
                }
            }
            else if (DateTimeMode == DataSetDateTime.Local)
            {
                for (int i = 0; i < _values.Length; i++)
                {
                    if (HasValue(i))
                    {
                        _values[i] = _values[i].ToLocalTime();
                    }
                }
            }
        }
    }
}
