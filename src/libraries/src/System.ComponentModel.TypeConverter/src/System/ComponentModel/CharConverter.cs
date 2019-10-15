// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert Unicode character objects to and from various
    /// other representations.
    /// </summary>
    public class CharConverter : TypeConverter
    {
        /// <summary>
        /// Gets a value indicating whether this converter can convert an object in the given
        /// source type to a Unicode character object using the specified context.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Converts the given object to another type.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is char charValue)
            {
                if (charValue == '\0')
                {
                    return string.Empty;
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Converts the given object to a Unicode character object.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                if (text.Length > 1)
                {
                    text = text.Trim();
                }

                if (text.Length > 0)
                {
                    if (text.Length != 1)
                    {
                        throw new FormatException(SR.Format(SR.ConvertInvalidPrimitive, text, nameof(Char)));
                    }

                    return text[0];
                }

                return '\0';
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
