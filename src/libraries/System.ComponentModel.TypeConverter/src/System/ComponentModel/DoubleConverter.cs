// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert double-precision, floating point number objects
    /// to and from various other representations.
    /// </summary>
    public class DoubleConverter : BaseNumberConverter
    {
        /// <summary>
        /// Determines whether this editor will attempt to convert hex (0x or #) strings
        /// </summary>
        internal override bool AllowHex => false;

        /// <summary>
        /// The Type this converter is targeting (e.g. Int16, UInt32, etc.)
        /// </summary>
        internal override Type TargetType => typeof(double);

        /// <summary>
        /// Convert the given value to a string using the given radix
        /// </summary>
        internal override object FromString(string value, int radix)
        {
            return Convert.ToDouble(value, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Convert the given value to a string using the given formatInfo
        /// </summary>
        internal override object FromString(string value, NumberFormatInfo? formatInfo)
        {
            return double.Parse(value, NumberStyles.Float, formatInfo);
        }

        /// <summary>
        /// Convert the given value from a string using the given formatInfo
        /// </summary>
        internal override string ToString(object value, NumberFormatInfo? formatInfo)
        {
            return ((double)value).ToString("R", formatInfo);
        }
    }
}
