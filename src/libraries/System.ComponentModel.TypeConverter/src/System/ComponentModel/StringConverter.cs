// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Provides a type converter to convert string objects to and from various other
    ///       representations.</para>
    /// </summary>
    public class StringConverter : TypeConverter
    {
        /// <summary>
        ///    <para>Gets a value indicating whether this converter can convert an object in the
        ///       given source type to a string using the specified context.</para>
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        ///    <para>Converts the specified value object to a string object.</para>
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                return (string)value;
            }
            if (value == null)
            {
                return string.Empty;
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}
