// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Provides a type converter to convert 32-bit signed integer objects to and
    ///       from various other representations.</para>
    /// </devdoc>
    public class Int32Converter : BaseNumberConverter
    {
        /// <devdoc>
        /// The Type this converter is targeting (e.g. Int16, UInt32, etc.)
        /// </devdoc>
        internal override Type TargetType
        {
            get
            {
                return typeof(Int32);
            }
        }

        /// <devdoc>
        /// Convert the given value to a string using the given radix
        /// </devdoc>
        internal override object FromString(string value, int radix)
        {
            return Convert.ToInt32(value, radix);
        }

        /// <devdoc>
        /// Convert the given value to a string using the given formatInfo
        /// </devdoc>
        internal override object FromString(string value, NumberFormatInfo formatInfo)
        {
            return Int32.Parse(value, NumberStyles.Integer, formatInfo);
        }


        /// <devdoc>
        /// Convert the given value to a string using the given CultureInfo
        /// </devdoc>
        internal override object FromString(string value, CultureInfo culture)
        {
            return Int32.Parse(value, culture);
        }



        /// <devdoc>
        /// Convert the given value from a string using the given formatInfo
        /// </devdoc>
        internal override string ToString(object value, NumberFormatInfo formatInfo)
        {
            return ((Int32)value).ToString("G", formatInfo);
        }
    }
}

