// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Runtime.Serialization
{
    public class FormatterConverter : IFormatterConverter
    {
        public object Convert(object value!!, Type type)
        {
            return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        public object Convert(object value!!, TypeCode typeCode)
        {
            return System.Convert.ChangeType(value, typeCode, CultureInfo.InvariantCulture);
        }

        public bool ToBoolean(object value!!)
        {
            return System.Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        public char ToChar(object value!!)
        {
            return System.Convert.ToChar(value, CultureInfo.InvariantCulture);
        }

        [CLSCompliant(false)]
        public sbyte ToSByte(object value!!)
        {
            return System.Convert.ToSByte(value, CultureInfo.InvariantCulture);
        }

        public byte ToByte(object value!!)
        {
            return System.Convert.ToByte(value, CultureInfo.InvariantCulture);
        }

        public short ToInt16(object value!!)
        {
            return System.Convert.ToInt16(value, CultureInfo.InvariantCulture);
        }

        [CLSCompliant(false)]
        public ushort ToUInt16(object value!!)
        {
            return System.Convert.ToUInt16(value, CultureInfo.InvariantCulture);
        }

        public int ToInt32(object value!!)
        {
            return System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        [CLSCompliant(false)]
        public uint ToUInt32(object value!!)
        {
            return System.Convert.ToUInt32(value, CultureInfo.InvariantCulture);
        }

        public long ToInt64(object value!!)
        {
            return System.Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        [CLSCompliant(false)]
        public ulong ToUInt64(object value!!)
        {
            return System.Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }

        public float ToSingle(object value!!)
        {
            return System.Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        public double ToDouble(object value!!)
        {
            return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        public decimal ToDecimal(object value!!)
        {
            return System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        public DateTime ToDateTime(object value!!)
        {
            return System.Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        public string? ToString(object value!!)
        {
            return System.Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
