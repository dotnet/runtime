// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.ComponentModel;

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