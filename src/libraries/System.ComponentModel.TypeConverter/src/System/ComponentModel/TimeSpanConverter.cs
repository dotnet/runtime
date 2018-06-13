// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert <see cref='System.TimeSpan'/> objects to and from
    /// various other representations.
    /// </summary>
    public class TimeSpanConverter : TypeConverter
    {
        /// <summary>
        /// Gets a value indicating whether this converter can
        /// convert an object in the given source type to a <see cref='System.TimeSpan'/> object using the
        /// specified context.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object to the given
        /// destination type using the context.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given object to a <see cref='System.TimeSpan'/>
        /// object.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                text = text.Trim();
                try
                {
                    return TimeSpan.Parse(text, culture);
                }
                catch (FormatException e)
                {
                    throw new FormatException(SR.Format(SR.ConvertInvalidPrimitive, (string)value, nameof(TimeSpan)), e);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Converts the given object to another type. The most common types to convert
        /// are to and from a string object. The default implementation will make a call
        /// to ToString on the object if the object is valid and if the destination
        /// type is string. If this cannot convert to the destination type, this will
        /// throw a NotSupportedException.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(InstanceDescriptor) && value is TimeSpan)
            {
                MethodInfo method = typeof(TimeSpan).GetMethod("Parse", new Type[] { typeof(string) });
                if (method != null)
                {
                    return new InstanceDescriptor(method, new object[] { value.ToString() });
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
