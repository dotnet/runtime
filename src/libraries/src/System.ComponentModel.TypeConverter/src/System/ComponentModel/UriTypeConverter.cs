// (c) Copyright Microsoft Corporation.
// This source is subject to [###LICENSE_NAME###].
// Please see [###LICENSE_LINK###] for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using System.Globalization;

namespace System
{
    /// <summary>
    /// Converts a String type to a Uri type.
    /// </summary>
    public sealed class UriTypeConverter : TypeConverter
    {
        /// <summary>
        /// Initializes a new instance of the UriTypeConverter class. 
        /// </summary>
        public UriTypeConverter()
        {
        }

        /// <summary>
        /// Returns whether this converter can convert an object in the given source type to the type of the converter
        /// using the context.
        /// </summary>
        /// <param name="sourceType">
        /// A type that represents the type that you want to convert from.
        /// </param>
        /// <returns>
        /// true if sourceType is a String type or a Uri type that can be
        /// assigned from sourceType; otherwise, false.
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == null)
                throw new ArgumentNullException("sourceType");

            if (sourceType == typeof(string))
                return true;

            if (typeof(Uri).IsAssignableFrom(sourceType))
                return true;

            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
                return true;

            if (destinationType == typeof(Uri))
                return true;

            return false;
        }

        /// <summary>
        /// Converts the specified object to a Uri.
        /// </summary>
        /// <param name="value">Object to convert into a Uri.</param>
        /// <returns>A Uri that represents the converted text.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string uriString = value as string;
            if (uriString != null)
            {
                if (String.IsNullOrEmpty(uriString))
                    return null;

                // Let the Uri constructor throw any informative exceptions
                return new Uri(uriString, UriKind.RelativeOrAbsolute);
            }

            Uri valueAsUri = value as Uri;
            if (valueAsUri != null)
                return new Uri(valueAsUri.OriginalString); // return new instance like desktop fx

            throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture,
                SR.GetString(SR.UriTypeConverter_ConvertFrom_CannotConvert), typeof(UriTypeConverter).Name,
                value != null ? value.GetType().FullName : "null"));
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            Uri uri = value as Uri;

            if (uri != null)
            {
                if (destinationType == typeof(string))
                    return uri.OriginalString;

                if (destinationType == typeof(Uri))
                    return new Uri(uri.OriginalString, UriKind.RelativeOrAbsolute);
            }

            throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture,
                SR.GetString(SR.UriTypeConverter_ConvertTo_CannotConvert), typeof(UriTypeConverter).Name,
                value != null ? value.GetType().FullName : "null",
                destinationType != null ? destinationType.FullName : "null"));
        }
    }
}
