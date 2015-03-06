// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Provides a
    ///       type converter to convert 8-bit unsigned
    ///       integer objects to and from various other representations.</para>
    /// </devdoc>
    public class ByteConverter : BaseNumberConverter
    {
        /// <devdoc>
        /// The Type this converter is targeting (e.g. Int16, UInt32, etc.)
        /// </devdoc>
        internal override Type TargetType
        {
            get
            {
                return typeof(Byte);
            }
        }

        /// <devdoc>
        /// Convert the given string to a Byte using the given radix
        /// </devdoc>
        internal override object FromString(string value, int radix)
        {
            return Convert.ToByte(value, radix);
        }

        /// <devdoc>
        /// Convert the given string to a Byte using the given formatInfo
        /// </devdoc>
        internal override object FromString(string value, NumberFormatInfo formatInfo)
        {
            return Byte.Parse(value, NumberStyles.Integer, formatInfo);
        }


        /// <devdoc>
        /// Convert the given string to a Byte using the given CultureInfo
        /// </devdoc>
        internal override object FromString(string value, CultureInfo culture)
        {
            return Byte.Parse(value, culture);
        }

        /// <devdoc>
        /// Convert the given value to a string using the given formatInfo
        /// </devdoc>
        internal override string ToString(object value, NumberFormatInfo formatInfo)
        {
            return ((Byte)value).ToString("G", formatInfo);
        }
    }
}

