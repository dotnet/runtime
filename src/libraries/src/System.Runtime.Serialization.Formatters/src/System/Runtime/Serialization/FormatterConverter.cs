// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization
{
    public class FormatterConverter : IFormatterConverter
    {
        public object Convert(object value, Type type)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        public object Convert(object value, TypeCode typeCode)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ChangeType(value, typeCode, CultureInfo.InvariantCulture);
        }

        public bool ToBoolean(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        public char ToChar(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToChar(value, CultureInfo.InvariantCulture);
        }

        [CLSCompliant(false)]
        public sbyte ToSByte(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToSByte(value, CultureInfo.InvariantCulture);
        }

        public byte ToByte(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToByte(value, CultureInfo.InvariantCulture);
        }

        public short ToInt16(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToInt16(value, CultureInfo.InvariantCulture);
        }

        [CLSCompliant(false)]
        public ushort ToUInt16(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToUInt16(value, CultureInfo.InvariantCulture);
        }

        public int ToInt32(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        [CLSCompliant(false)]
        public uint ToUInt32(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToUInt32(value, CultureInfo.InvariantCulture);
        }

        public long ToInt64(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        [CLSCompliant(false)]
        public ulong ToUInt64(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }

        public float ToSingle(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        public double ToDouble(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        public decimal ToDecimal(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        public DateTime ToDateTime(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        public string ToString(object value)
        {
            if (value == null) ThrowValueNullException();
            return System.Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static void ThrowValueNullException()
        {
            throw new ArgumentNullException("value");
        }
    }
}
