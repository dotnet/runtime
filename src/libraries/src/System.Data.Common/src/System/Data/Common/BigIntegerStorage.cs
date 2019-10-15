// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using System.Collections;
using System.Diagnostics;

namespace System.Data.Common
{
    internal sealed class BigIntegerStorage : DataStorage
    {
        private BigInteger[] _values;

        internal BigIntegerStorage(DataColumn column) :
            base(column, typeof(BigInteger), BigInteger.Zero, StorageType.BigInteger)
        {
        }

        public override object Aggregate(int[] records, AggregateType kind)
        {
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            BigInteger valueNo1 = _values[recordNo1];
            BigInteger valueNo2 = _values[recordNo2];

            if (valueNo1.IsZero || valueNo2.IsZero)
            {
                int bitCheck = CompareBits(recordNo1, recordNo2);
                if (0 != bitCheck)
                {
                    return bitCheck;
                }
            }

            return valueNo1.CompareTo(valueNo2);
        }

        public override int CompareValueTo(int recordNo, object value)
        {
            Debug.Assert(0 <= recordNo, "Invalid record");
            Debug.Assert(null != value, "null value");

            if (_nullValue == value)
            {
                return (HasValue(recordNo) ? 1 : 0);
            }

            BigInteger valueNo1 = _values[recordNo];
            if (valueNo1.IsZero && !HasValue(recordNo))
            {
                return -1;
            }

            return valueNo1.CompareTo((BigInteger)value);
        }

        // supported implicit casts
        internal static BigInteger ConvertToBigInteger(object value, IFormatProvider formatProvider)
        {
            if (value.GetType() == typeof(BigInteger)) { return (BigInteger)value; }
            else if (value.GetType() == typeof(string)) { return BigInteger.Parse((string)value, formatProvider); }
            else if (value.GetType() == typeof(long)) { return (long)value; }
            else if (value.GetType() == typeof(int)) { return (int)value; }
            else if (value.GetType() == typeof(short)) { return (short)value; }
            else if (value.GetType() == typeof(sbyte)) { return (sbyte)value; }
            else if (value.GetType() == typeof(ulong)) { return (ulong)value; }
            else if (value.GetType() == typeof(uint)) { return (uint)value; }
            else if (value.GetType() == typeof(ushort)) { return (ushort)value; }
            else if (value.GetType() == typeof(byte)) { return (byte)value; }
            else { throw ExceptionBuilder.ConvertFailed(value.GetType(), typeof(System.Numerics.BigInteger)); }
        }

        internal static object ConvertFromBigInteger(BigInteger value, Type type, IFormatProvider formatProvider)
        {
            if (type == typeof(string)) { return value.ToString("D", formatProvider); }
            else if (type == typeof(sbyte)) { return checked((sbyte)value); }
            else if (type == typeof(short)) { return checked((short)value); }
            else if (type == typeof(int)) { return checked((int)value); }
            else if (type == typeof(long)) { return checked((long)value); }
            else if (type == typeof(byte)) { return checked((byte)value); }
            else if (type == typeof(ushort)) { return checked((ushort)value); }
            else if (type == typeof(uint)) { return checked((uint)value); }
            else if (type == typeof(ulong)) { return checked((ulong)value); }
            else if (type == typeof(float)) { return checked((float)value); }
            else if (type == typeof(double)) { return checked((double)value); }
            else if (type == typeof(decimal)) { return checked((decimal)value); }
            else if (type == typeof(System.Numerics.BigInteger)) { return value; }
            else { throw ExceptionBuilder.ConvertFailed(typeof(System.Numerics.BigInteger), type); }
        }

        public override object ConvertValue(object value)
        {
            if (_nullValue != value)
            {
                if (null != value)
                {
                    value = ConvertToBigInteger(value, FormatProvider);
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
            BigInteger value = _values[record];
            if (!value.IsZero)
            {
                return value;
            }
            return GetBits(record);
        }

        public override void Set(int record, object value)
        {
            Debug.Assert(null != value, "null value");
            if (_nullValue == value)
            {
                _values[record] = BigInteger.Zero;
                SetNullBit(record, true);
            }
            else
            {
                _values[record] = ConvertToBigInteger(value, FormatProvider);
                SetNullBit(record, false);
            }
        }

        public override void SetCapacity(int capacity)
        {
            BigInteger[] newValues = new BigInteger[capacity];
            if (null != _values)
            {
                Array.Copy(_values, 0, newValues, 0, Math.Min(capacity, _values.Length));
            }
            _values = newValues;
            base.SetCapacity(capacity);
        }

        public override object ConvertXmlToObject(string s)
        {
            return BigInteger.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        }

        public override string ConvertObjectToXml(object value)
        {
            return ((BigInteger)value).ToString("D", System.Globalization.CultureInfo.InvariantCulture);
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new BigInteger[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            BigInteger[] typedStore = (BigInteger[])store;
            typedStore[storeIndex] = _values[record];
            nullbits.Set(storeIndex, !HasValue(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (BigInteger[])store;
            SetNullStorage(nullbits);
        }
    }
}
