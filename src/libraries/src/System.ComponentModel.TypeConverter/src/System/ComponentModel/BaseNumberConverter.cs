// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Provides a base type converter for integral types.</para>
    /// </devdoc>
    public abstract class BaseNumberConverter : TypeConverter
    {
        /// <devdoc>
        /// Determines whether this editor will attempt to convert hex (0x or #) strings
        /// </devdoc>
        internal virtual bool AllowHex
        {
            get
            {
                return true;
            }
        }

        /// <devdoc>
        /// The Type this converter is targeting (e.g. Int16, UInt32, etc.)
        /// </devdoc>
        internal abstract Type TargetType
        {
            get;
        }

        /// <devdoc>
        /// Convert the given value to a string using the given radix
        /// </devdoc>
        internal abstract object FromString(string value, int radix);

        /// <devdoc>
        /// Convert the given value to a string using the given formatInfo
        /// </devdoc>
        internal abstract object FromString(string value, NumberFormatInfo formatInfo);

        /// <devdoc>
        /// Convert the given value to a string using the given CultureInfo
        /// </devdoc>
        internal abstract object FromString(string value, CultureInfo culture);

        /// <devdoc>
        /// Create an error based on the failed text and the exception thrown.
        /// </devdoc>
        internal virtual Exception FromStringError(string failedText, Exception innerException)
        {
            return new Exception(SR.Format(SR.ConvertInvalidPrimitive, failedText, this.TargetType.Name), innerException);
        }

        /// <devdoc>
        /// Convert the given value from a string using the given formatInfo
        /// </devdoc>
        internal abstract string ToString(object value, NumberFormatInfo formatInfo);

        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can convert an object in the
        ///       given source type to the TargetType object using the specified context.</para>
        /// </devdoc>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <devdoc>
        ///    <para>Converts the given value object to an object of Type TargetType.</para>
        /// </devdoc>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string text = value as string;
            if (text != null)
            {
                text = text.Trim();

                try
                {
                    if (this.AllowHex && text[0] == '#')
                    {
                        return this.FromString(text.Substring(1), 16);
                    }
                    else if (this.AllowHex && text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                             || text.StartsWith("&h", StringComparison.OrdinalIgnoreCase))
                    {
                        return this.FromString(text.Substring(2), 16);
                    }
                    else
                    {
                        if (culture == null)
                        {
                            culture = CultureInfo.CurrentCulture;
                        }
                        NumberFormatInfo formatInfo = (NumberFormatInfo)culture.GetFormat(typeof(NumberFormatInfo));
                        return this.FromString(text, formatInfo);
                    }
                }
                catch (Exception e)
                {
                    throw FromStringError(text, e);
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <devdoc>
        ///    <para>Converts the given value object to the destination type.</para>
        /// </devdoc>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(string) && value != null && this.TargetType.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
            {
                if (culture == null)
                {
                    culture = CultureInfo.CurrentCulture;
                }
                NumberFormatInfo formatInfo = (NumberFormatInfo)culture.GetFormat(typeof(NumberFormatInfo));
                return this.ToString(value, formatInfo);
            }

            if (destinationType.GetTypeInfo().IsPrimitive)
            {
                return Convert.ChangeType(value, destinationType, culture);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (base.CanConvertTo(context, destinationType) || destinationType.GetTypeInfo().IsPrimitive)
            {
                return true;
            }
            return false;
        }
    }
}

