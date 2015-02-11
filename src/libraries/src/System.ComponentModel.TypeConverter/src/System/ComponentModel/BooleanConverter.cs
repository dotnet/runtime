// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Provides a type converter to convert
    ///       Boolean objects to and from various other representations.</para>
    /// </devdoc>
    public class BooleanConverter : TypeConverter
    {
        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object in the given source type to a Boolean object using the
        ///       specified context.</para>
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
        ///    <para>Converts the given value
        ///       object to a Boolean object.</para>
        /// </devdoc>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string text = value as string;
            if (text != null)
            {
                text = text.Trim();
                try
                {
                    return Boolean.Parse(text);
                }
                catch (FormatException e)
                {
                    throw new FormatException(SR.Format(SR.ConvertInvalidPrimitive, (string)value, "Boolean"), e);
                }
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}
