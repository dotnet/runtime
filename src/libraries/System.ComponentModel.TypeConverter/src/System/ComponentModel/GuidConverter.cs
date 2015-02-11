// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Provides a
    ///       type converter to convert globally unique identifier objects to and from various
    ///       other representations.</para>
    /// </devdoc>
    public class GuidConverter : TypeConverter
    {
        /// <devdoc>
        ///    <para>Gets a value indicating whether this
        ///       converter can convert an object in the given source type to a globally unique identifier object
        ///       using the context.</para>
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
        ///    <para>Converts
        ///       the given object to a globally unique identifier object.</para>
        /// </devdoc>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                string text = ((string)value).Trim();
                return new Guid(text);
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}

