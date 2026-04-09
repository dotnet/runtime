// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert 8-bit unsigned integer objects
    /// to and from various other representations.
    /// </summary>
    public class ByteConverter : BaseNumberConverter
    {
        /// <summary>
        /// The Type this converter is targeting (e.g. Int16, UInt32, etc.)
        /// </summary>
        internal override Type TargetType => typeof(byte);

        /// <summary>
        /// Convert the given string to a Byte using the given radix
        /// </summary>
        internal override object FromString(string value, int radix) => Convert.ToByte(value, radix);

        /// <summary>
        /// Convert the given string to a Byte using the given formatInfo
        /// </summary>
        internal override object FromString(string value, NumberFormatInfo? formatInfo)
        {
            return byte.Parse(value, NumberStyles.Integer, formatInfo);
        }

        /// <summary>
        /// Convert the given value to a string using the given formatInfo
        /// </summary>
        internal override string ToString(object value, NumberFormatInfo? formatInfo)
        {
            return ((byte)value).ToString("G", formatInfo);
        }
    }
}
