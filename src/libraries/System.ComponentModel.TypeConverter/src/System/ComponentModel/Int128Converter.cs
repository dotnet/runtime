// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert 128-bit signed integer objects to and
    /// from various other representations.
    /// </summary>
    public class Int128Converter : BaseNumberConverter
    {
        /// <summary>
        /// The Type this converter is targeting (e.g. Int16, UInt32, etc.)
        /// </summary>
        internal override Type TargetType => typeof(Int128);

        /// <summary>
        /// Convert the given value string to Int128 using the given radix
        /// </summary>
        internal override object FromString(string value, int radix)
        {
            // We only support decimal and hex radix in the converter.
            if (radix != 10 && radix != 16)
            {
                throw new ArgumentException(SR.Arg_InvalidBase);
            }

            return value is null ? 0 : Int128.Parse(value, radix == 16 ? NumberStyles.HexNumber : NumberStyles.None);
        }

        /// <summary>
        /// Convert the given value string to Int128 using given formatInfo
        /// </summary>
        internal override object FromString(string value, NumberFormatInfo? formatInfo)
        {
            return Int128.Parse(value, NumberStyles.Integer, formatInfo);
        }

        /// <summary>
        /// Convert the given value from a string using the given formatInfo
        /// </summary>
        internal override string ToString(object value, NumberFormatInfo? formatInfo)
        {
            return ((Int128)value).ToString("G", formatInfo);
        }
    }
}
