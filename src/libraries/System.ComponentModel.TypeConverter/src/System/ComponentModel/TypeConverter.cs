// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.InteropServices;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Converts the value of an object into a different data type.</para>
    /// </devdoc>
    public class TypeConverter
    {
        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can convert an object in the
        ///       given source type to the native type of the converter.</para>
        /// </devdoc>
        public bool CanConvertFrom(Type sourceType)
        {
            return CanConvertFrom(null, sourceType);
        }

        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object in the given source type to the native type of the converter
        ///       using the context.</para>
        /// </devdoc>
        public virtual bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return false;
        }

        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object to the given destination type using the context.</para>
        /// </devdoc>
        public bool CanConvertTo(Type destinationType)
        {
            return CanConvertTo(null, destinationType);
        }

        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object to the given destination type using the context.</para>
        /// </devdoc>
        public virtual bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (destinationType == typeof(string));
        }

        /// <devdoc>
        ///    <para>Converts the given value
        ///       to the converter's native type.</para>
        /// </devdoc>
        public object ConvertFrom(object value)
        {
            return ConvertFrom(null, CultureInfo.CurrentCulture, value);
        }

        /// <devdoc>
        ///    <para>Converts the given object to the converter's native type.</para>
        /// </devdoc>
        public virtual object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            throw GetConvertFromException(value);
        }

        /// <devdoc>
        ///    Converts the given string to the converter's native type using the invariant culture.
        /// </devdoc>
        public object ConvertFromInvariantString(string text)
        {
            return ConvertFromString(null, CultureInfo.InvariantCulture, text);
        }

        /// <devdoc>
        ///    <para>Converts the specified text into an object.</para>
        /// </devdoc>
        public object ConvertFromString(string text)
        {
            return ConvertFrom(null, null, text);
        }

        /// <devdoc>
        ///    <para>Converts the specified text into an object.</para>
        /// </devdoc>
        public object ConvertFromString(ITypeDescriptorContext context, CultureInfo culture, string text)
        {
            return ConvertFrom(context, culture, text);
        }

        /// <devdoc>
        ///    <para>Converts the given
        ///       value object to the specified destination type using the arguments.</para>
        /// </devdoc>
        public object ConvertTo(object value, Type destinationType)
        {
            return ConvertTo(null, null, value, destinationType);
        }

        /// <devdoc>
        ///    <para>Converts the given value object to
        ///       the specified destination type using the specified context and arguments.</para>
        /// </devdoc>
        public virtual object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(string))
            {
                if (value == null)
                {
                    return String.Empty;
                }

                if (culture != null && culture != CultureInfo.CurrentCulture)
                {
                    IFormattable formattable = value as IFormattable;
                    if (formattable != null)
                    {
                        return formattable.ToString(/* format = */ null, /* formatProvider = */ culture);
                    }
                }
                return value.ToString();
            }
            throw GetConvertToException(value, destinationType);
        }

        /// <devdoc>
        ///    <para>Converts the specified value to a culture-invariant string representation.</para>
        /// </devdoc>
        public string ConvertToInvariantString(object value)
        {
            return ConvertToString(null, CultureInfo.InvariantCulture, value);
        }

        /// <devdoc>
        ///    <para>Converts the specified value to a string representation.</para>
        /// </devdoc>
        public string ConvertToString(object value)
        {
            return (string)ConvertTo(null, CultureInfo.CurrentCulture, value, typeof(string));
        }

        /// <devdoc>
        ///    <para>Converts the specified value to a string representation.</para>
        /// </devdoc>
        public string ConvertToString(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return (string)ConvertTo(context, culture, value, typeof(string));
        }

        /// <devdoc>
        ///    <para>
        ///       Gets a suitable exception to throw when a conversion cannot
        ///       be performed.
        ///    </para>
        /// </devdoc>
        protected Exception GetConvertFromException(object value)
        {
            string valueTypeName;

            if (value == null)
            {
                valueTypeName = SR.Null;
            }
            else
            {
                valueTypeName = value.GetType().FullName;
            }

            throw new NotSupportedException(SR.Format(SR.ConvertFromException, GetType().Name, valueTypeName));
        }

        /// <devdoc>
        ///    <para>Retrieves a suitable exception to throw when a conversion cannot
        ///       be performed.</para>
        /// </devdoc>
        protected Exception GetConvertToException(object value, Type destinationType)
        {
            string valueTypeName;

            if (value == null)
            {
                valueTypeName = SR.Null;
            }
            else
            {
                valueTypeName = value.GetType().FullName;
            }

            throw new NotSupportedException(SR.Format(SR.ConvertToException, GetType().Name, valueTypeName, destinationType.FullName));
        }
    }
}

