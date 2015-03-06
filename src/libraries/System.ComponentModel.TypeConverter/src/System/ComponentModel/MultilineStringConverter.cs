// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.ComponentModel
{
    /// <devdoc>
    /// Provides a type converter to convert multiline strings to a simple string.
    /// </devdoc>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public class MultilineStringConverter : TypeConverter
    {
        /// <devdoc>
        /// Converts the given value object to the specified destination type.
        /// </devdoc>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            if (destinationType == typeof(string))
            {
                if (value is string)
                {
                    return SR.MultilineStringConverterText;
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
