// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;

namespace System.Configuration
{
    public sealed class CommaDelimitedStringCollectionConverter : ConfigurationConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
        {
            ValidateType(value, typeof(CommaDelimitedStringCollection));
            CommaDelimitedStringCollection internalValue = value as CommaDelimitedStringCollection;
            return internalValue?.ToString();
        }

        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
        {
            CommaDelimitedStringCollection attributeCollection = new CommaDelimitedStringCollection();
            attributeCollection.FromString((string)data);
            return attributeCollection;
        }
    }
}
