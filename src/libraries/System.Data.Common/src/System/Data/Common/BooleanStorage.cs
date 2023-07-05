// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data.Common
{
    internal sealed class BooleanStorage : DataStorage
    {
        private const bool defaultValue = false;

        private bool[] _values = default!; // Late-initialized

        internal BooleanStorage(DataColumn column) :
            base(column, typeof(bool), defaultValue, StorageType.Boolean)
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
                        bool min = true;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            min = _values[record] && min;
                            hasData = true;
                        }
                        if (hasData)
                        {
                            return min;
                        }
                        return _nullValue;

                    case AggregateType.Max:
                        bool max = false;
                        for (int i = 0; i < records.Length; i++)
                        {
                            int record = records[i];
                            if (IsNull(record))
                                continue;
                            max = _values[record] || max;
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
                throw ExprException.Overflow(typeof(bool));
            }
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            bool valueNo1 = _values[recordNo1];
            bool valueNo2 = _values[recordNo2];

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
            Debug.Assert(null != value, "null value");

            if (_nullValue == value)
            {
                if (IsNull(recordNo))
                {
                    return 0;
                }
                return 1;
            }

            bool valueNo1 = _values[recordNo];
            if ((defaultValue == valueNo1) && IsNull(recordNo))
            {
                return -1;
            }
            return valueNo1.CompareTo((bool)value);
        }

        public override object ConvertValue(object? value)
        {
            if (_nullValue != value)
            {
                if (null != value)
                {
                    value = ((IConvertible)value).ToBoolean(FormatProvider);
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
            bool value = _values[record];
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
                _values[record] = ((IConvertible)value).ToBoolean(FormatProvider);
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
            return XmlConvert.ToBoolean(s);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public override string ConvertObjectToXml(object value)
        {
            return XmlConvert.ToString((bool)value);
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new bool[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            bool[] typedStore = (bool[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, IsNull(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (bool[])store;
            SetNullStorage(nullbits);
        }
    }
}
