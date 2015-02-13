// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;

namespace System.ComponentModel
{
    /// <devdoc>
    /// <para>Provides a type converter to convert <see cref='System.Array'/>
    /// objects to and from various other representations.</para>
    /// </devdoc>
    public class ArrayConverter : CollectionConverter
    {
        /// <devdoc>
        ///    <para>Converts the given value object to the specified destination type.</para>
        /// </devdoc>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            if (destinationType == typeof(string))
            {
                if (value is Array)
                {
                    return SR.Format(SR.ArrayConverterText, value.GetType().Name);
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}


