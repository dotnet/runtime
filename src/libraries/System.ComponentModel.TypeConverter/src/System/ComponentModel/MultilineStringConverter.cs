// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert multiline strings to a simple string.
    /// </summary>
    public class MultilineStringConverter : TypeConverter
    {
        /// <summary>
        /// Converts the given value object to the specified destination type.
        /// </summary>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (destinationType == typeof(string) && value is string)
            {
                return SR.GetResourceString(nameof(SR.Text), "(Text)");
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Gets a collection of properties for the type of array specified by the value
        /// parameter using the specified context and attributes.
        /// </summary>
        [RequiresUnreferencedCode("The Type of value cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public override PropertyDescriptorCollection? GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes)
        {
            return null;
        }

        /// <summary>
        /// Gets a value indicating whether this object supports properties.
        /// </summary>
        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => false;
    }
}
