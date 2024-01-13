// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert <see cref='System.TimeOnly'/> objects to and from various other representations.
    /// </summary>
    public class TimeOnlyConverter : TypeConverter
    {
        /// <summary>
        /// Gets a value indicating whether this converter can convert an object in the given source type to a <see cref='System.TimeOnly'/>
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
        /// Converts the given value object to a <see cref='System.TimeOnly'/> object.
        /// </summary>
        /// <inheritdoc />
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string text)
            {
                text = text.Trim();
                if (text.Length == 0)
                {
                    return TimeOnly.MinValue;
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
                        return TimeOnly.Parse(text, formatInfo);
                    }
                    else
                    {
                        return TimeOnly.Parse(text, culture);
                    }
                }
                catch (FormatException e)
                {
                    throw new FormatException(SR.Format(SR.ConvertInvalidPrimitive, (string)value, nameof(TimeOnly)), e);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Converts the given value object from a <see cref='System.TimeOnly'/> object using the arguments.
        /// </summary>
        /// <inheritdoc />
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is TimeOnly timeOnly)
            {
                if (timeOnly == TimeOnly.MinValue)
                {
                    return string.Empty;
                }

                culture ??= CultureInfo.CurrentCulture;

                return timeOnly.ToString(culture.DateTimeFormat.ShortTimePattern, culture);
            }

            if (destinationType == typeof(InstanceDescriptor) && value is TimeOnly time)
            {
                if (time.Ticks == 0)
                {
                    return new InstanceDescriptor(typeof(TimeOnly).GetConstructor(new Type[] { typeof(long) }), new object[] { time.Ticks });
                }

                return new InstanceDescriptor(typeof(TimeOnly).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) }),
                                                new object[] { time.Hour, time.Minute, time.Second, time.Millisecond, time.Microsecond });
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
