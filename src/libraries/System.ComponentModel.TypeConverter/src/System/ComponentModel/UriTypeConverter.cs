// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;

namespace System
{
    /// <summary>
    ///    <para>Provides a type converter to convert Uri objects to and
    ///       from various other representations.</para>
    /// </summary>
    public class UriTypeConverter : TypeConverter
    {
        /// <summary>
        ///    <para>Gets a value indicating whether this converter can convert an object in the
        ///       given source type to a Uri.</para>
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == null)
                throw new ArgumentNullException(nameof(sourceType));

            return (sourceType == typeof(string) || sourceType == typeof(Uri));
        }

        /// <summary>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object to the given destination type using the context.</para>
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (destinationType == typeof(string) || destinationType == typeof(Uri));
        }

        /// <summary>
        ///    <para>Converts the given object to the a Uri.</para>
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string uriString = value as string;
            if (uriString != null)
            {
                if (string.IsNullOrEmpty(uriString))
                    return null;

                // Let the Uri constructor throw any informative exceptions
                return new Uri(uriString, UriKind.RelativeOrAbsolute);
            }

            Uri uri = value as Uri;
            if (uri != null)
            {
                return new Uri(uri.OriginalString); // return new instance
            }

            throw GetConvertFromException(value);
        }

        /// <summary>
        ///    <para>Converts the given value object to
        ///       the specified destination type using the specified context and arguments.</para>
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            Uri uri = value as Uri;
            if (uri != null)
            {
                if (destinationType == typeof(string))
                    return uri.OriginalString;

                if (destinationType == typeof(Uri))
                    return new Uri(uri.OriginalString, UriKind.RelativeOrAbsolute);
            }

            throw GetConvertToException(value, destinationType);
        }

        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            string text = value as string;
            if (text != null)
            {
                Uri uri;
                return Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out uri);
            }
            return value is Uri;
        }        
    }
}
