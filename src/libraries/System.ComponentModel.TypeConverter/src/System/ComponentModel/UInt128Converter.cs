// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert 128-bit unsigned integer objects to and
    /// from various other representations.
    /// </summary>
    public class UInt128Converter : BaseNumberConverter
    {
        /// <summary>
        /// The Type this converter is targeting (e.g. Int16, UInt64, etc.)
        /// </summary>
        internal override Type TargetType => typeof(UInt128);

        /// <summary>
        /// Convert the given value string to UInt128 using the given radix
        /// </summary>
        internal override object FromString(string value, int radix)
        {
            Debug.Assert(radix == 16);
            Debug.Assert(value is not null);

            return UInt128.Parse(value, NumberStyles.HexNumber);
        }

        /// <summary>
        /// Convert the given value to a string using the given formatInfo
        /// </summary>
        internal override object FromString(string value, NumberFormatInfo? formatInfo) =>
            UInt128.Parse(value, formatInfo);

        /// <summary>
        /// Convert the given value from a string using the given formatInfo
        /// </summary>
        internal override string ToString(object value, NumberFormatInfo? formatInfo) =>
            ((UInt128)value).ToString(formatInfo);
    }
}
