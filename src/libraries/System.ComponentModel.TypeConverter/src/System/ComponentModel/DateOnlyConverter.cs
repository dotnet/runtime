// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert <see cref='System.DateOnly'/> objects to and from various other representations.
    /// </summary>
    public class DateOnlyConverter : TypeConverter
    {
        /// <summary>
        /// Gets a value indicating whether this converter can convert an object in the given source type to a <see cref='System.DateOnly'/>
        /// object using the specified context.
        /// </summary>
        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <inheritdoc />
        public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given value object to a <see cref='System.DateOnly'/> object.
        /// </summary>
        /// <inheritdoc />
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string text)
            {
                text = text.Trim();
                if (text.Length == 0)
                {
                    return DateOnly.MinValue;
                }

                try
                {
                    // See if we have a culture info to parse with. If so, then use it.
                    DateTimeFormatInfo? formatInfo = null;

                    if (culture != null)
                    {
                        formatInfo = (DateTimeFormatInfo?)culture.GetFormat(typeof(DateTimeFormatInfo));
                    }

                    if (formatInfo != null)
                    {
                        return DateOnly.Parse(text, formatInfo);
                    }
                    else
                    {
                        return DateOnly.Parse(text, culture);
                    }
                }
                catch (FormatException e)
                {
                    throw new FormatException(SR.Format(SR.ConvertInvalidPrimitive, (string)value, nameof(DateOnly)), e);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Converts the given value object from a <see cref='System.DateOnly'/> object using the arguments.
        /// </summary>
        /// <inheritdoc />
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is DateOnly dateOnly)
            {
                if (dateOnly == DateOnly.MinValue)
                {
                    return string.Empty;
                }

                culture ??= CultureInfo.CurrentCulture;

                DateTimeFormatInfo? formatInfo = (DateTimeFormatInfo?)culture.GetFormat(typeof(DateTimeFormatInfo));

                if (culture == CultureInfo.InvariantCulture)
                {
                    return dateOnly.ToString("yyyy-MM-dd", culture);
                }

                string format = formatInfo!.ShortDatePattern;

                return dateOnly.ToString(format, CultureInfo.CurrentCulture);
            }

            if (destinationType == typeof(InstanceDescriptor) && value is DateOnly date)
            {
                return new InstanceDescriptor(typeof(DateOnly).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int) }), new object[] { date.Year, date.Month, date.Day });
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
