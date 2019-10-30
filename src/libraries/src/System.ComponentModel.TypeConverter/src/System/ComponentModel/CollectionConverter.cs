// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert collection objects to and from various other
    /// representations.
    /// </summary>
    public class CollectionConverter : TypeConverter
    {
        /// <summary>
        /// Converts the given value object to the specified destination type.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is ICollection)
            {
                return SR.Collection;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Gets a collection of properties for the type of array specified by the value
        /// parameter using the specified context and attributes.
        /// </summary>
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            return new PropertyDescriptorCollection(null);
        }
    }
}
