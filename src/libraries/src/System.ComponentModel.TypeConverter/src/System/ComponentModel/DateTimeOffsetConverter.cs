// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert <see cref='System.DateTimeOffset'/>
    /// objects to and from various other representations.
    /// </summary>
    public class DateTimeOffsetConverter : TypeConverter
    {
        /// <summary>
        /// Gets a value indicating whether this converter can convert an
        /// object in the given source type to a <see cref='System.DateTimeOffset'/>
        /// object using the specified context.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an
        /// object to the given destination type using the context.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given value object to a <see cref='System.DateTime'/>
        /// object.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                text = text.Trim();
                if (text.Length == 0)
                {
                    return DateTimeOffset.MinValue;
                }

                try
                {
                    // See if we have a culture info to parse with. If so, then use it.
                    DateTimeFormatInfo formatInfo = null;

                    if (culture != null)
                    {
                        formatInfo = (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo));
                    }

                    if (formatInfo != null)
                    {
                        return DateTimeOffset.Parse(text, formatInfo);
                    }
                    else
                    {
                        return DateTimeOffset.Parse(text, culture);
                    }
                }
                catch (FormatException e)
                {
                    throw new FormatException(SR.Format(SR.ConvertInvalidPrimitive, (string)value, nameof(DateTimeOffset)), e);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Converts the given value object to a <see cref='System.DateTimeOffset'/>
        /// object using the arguments.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            // logic is exactly as in DateTimeConverter, only the offset pattern ' zzz' is added to the default
            // ConvertToString pattern.
            if (destinationType == typeof(string) && value is DateTimeOffset dto)
            {
                if (dto == DateTimeOffset.MinValue)
                {
                    return string.Empty;
                }

                if (culture == null)
                {
                    culture = CultureInfo.CurrentCulture;
                }

                DateTimeFormatInfo formatInfo = null;
                formatInfo = (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo));

                string format;
                if (culture == CultureInfo.InvariantCulture)
                {
                    // note: the y/m/d format used when there is or isn't a time component is not consistent
                    // when the culture is invariant, because for some reason InvariantCulture's date format is
                    // MM/dd/yyyy. However, this matches the behavior of DateTimeConverter so it is preserved here.

                    if (dto.TimeOfDay.TotalSeconds == 0)
                    {
                        // pattern just like DateTimeConverter when DateTime.TimeOfDay.TotalSeconds==0
                        // but with ' zzz' offset pattern added.
                        return dto.ToString("yyyy-MM-dd zzz", culture);
                    }
                    else
                    {
                        return dto.ToString(culture);
                    }
                }
                if (dto.TimeOfDay.TotalSeconds == 0)
                {
                    // pattern just like DateTimeConverter when DateTime.TimeOfDay.TotalSeconds==0
                    // but with ' zzz' offset pattern added.
                    format = formatInfo.ShortDatePattern + " zzz";
                }
                else
                {
                    // pattern just like DateTimeConverter when DateTime.TimeOfDay.TotalSeconds!=0
                    // but with ' zzz' offset pattern added.
                    format = formatInfo.ShortDatePattern + " " + formatInfo.ShortTimePattern + " zzz";
                }

                return dto.ToString(format, CultureInfo.CurrentCulture);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
