// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// <para>Provides a type converter to convert <see cref='System.DateTime'/>
    /// objects to and from various other representations.</para>
    /// </summary>
    public class DateTimeConverter : TypeConverter
    {
        /// <summary>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object in the given source type to a <see cref='System.DateTime'/>
        ///       object using the
        ///       specified context.</para>
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        ///    <para>
        ///        Gets a value indicating whether this converter can convert an object
        ///        to the given destination type using the context.
        ///    </para>
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// <para>Converts the given value object to a <see cref='System.DateTime'/>
        /// object.</para>
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string text = value as string;
            if (text != null)
            {
                text = text.Trim();
                if (text.Length == 0)
                {
                    return DateTime.MinValue;
                }

                try
                {
                    // See if we have a culture info to parse with.  If so, then use it.
                    //
                    DateTimeFormatInfo formatInfo = null;

                    if (culture != null)
                    {
                        formatInfo = (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo));
                    }

                    if (formatInfo != null)
                    {
                        return DateTime.Parse(text, formatInfo);
                    }
                    else
                    {
                        return DateTime.Parse(text, culture);
                    }
                }
                catch (FormatException e)
                {
                    throw new FormatException(SR.Format(SR.ConvertInvalidPrimitive, (string)value, nameof(DateTime)), e);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// <para>Converts the given value object to a <see cref='System.DateTime'/>
        /// object
        /// using the arguments.</para>
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is DateTime)
            {
                DateTime dt = (DateTime)value;
                if (dt == DateTime.MinValue)
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
                    if (dt.TimeOfDay.TotalSeconds == 0)
                    {
                        return dt.ToString("yyyy-MM-dd", culture);
                    }
                    else
                    {
                        return dt.ToString(culture);
                    }
                }
                if (dt.TimeOfDay.TotalSeconds == 0)
                {
                    format = formatInfo.ShortDatePattern;
                }
                else
                {
                    format = formatInfo.ShortDatePattern + " " + formatInfo.ShortTimePattern;
                }

                return dt.ToString(format, CultureInfo.CurrentCulture);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
