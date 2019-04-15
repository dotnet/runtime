// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a base type converter for integral types.
    /// </summary>
    public abstract class BaseNumberConverter : TypeConverter
    {
        /// <summary>
        /// Determines whether this editor will attempt to convert hex (0x or #) strings
        /// </summary>
        internal virtual bool AllowHex => true;

        /// <summary>
        /// The Type this converter is targeting (e.g. Int16, UInt32, etc.)
        /// </summary>
        internal abstract Type TargetType { get; }

        /// <summary>
        /// Convert the given value to a string using the given radix
        /// </summary>
        internal abstract object FromString(string value, int radix);

        /// <summary>
        /// Convert the given value to a string using the given formatInfo
        /// </summary>
        internal abstract object FromString(string value, NumberFormatInfo formatInfo);

        /// <summary>
        /// Convert the given value from a string using the given formatInfo
        /// </summary>
        internal abstract string ToString(object value, NumberFormatInfo formatInfo);

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object in the
        /// given source type to the TargetType object using the specified context.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Converts the given value object to an object of Type TargetType.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                text = text.Trim();

                try
                {
                    if (AllowHex && text[0] == '#')
                    {
                        return FromString(text.Substring(1), 16);
                    }
                    else if (AllowHex && (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                             || text.StartsWith("&h", StringComparison.OrdinalIgnoreCase)))
                    {
                        return FromString(text.Substring(2), 16);
                    }
                    else
                    {
                        if (culture == null)
                        {
                            culture = CultureInfo.CurrentCulture;
                        }

                        NumberFormatInfo formatInfo = (NumberFormatInfo)culture.GetFormat(typeof(NumberFormatInfo));
                        return FromString(text, formatInfo);
                    }
                }
                catch (Exception e)
                {
                    throw new ArgumentException(SR.Format(SR.ConvertInvalidPrimitive, text, TargetType.Name), nameof(value), e);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Converts the given value object to the destination type.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(string) && value != null && TargetType.IsInstanceOfType(value))
            {
                if (culture == null)
                {
                    culture = CultureInfo.CurrentCulture;
                }

                NumberFormatInfo formatInfo = (NumberFormatInfo)culture.GetFormat(typeof(NumberFormatInfo));
                return ToString(value, formatInfo);
            }

            if (destinationType.IsPrimitive)
            {
                return Convert.ChangeType(value, destinationType, culture);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType != null && destinationType.IsPrimitive)
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }
    }
}
