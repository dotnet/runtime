// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Configuration
{
    public abstract class ConfigurationConverterBase : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext ctx, Type type)
        {
            return type == typeof(string);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext ctx, Type type)
        {
            return type == typeof(string);
        }

        internal static void ValidateType(object value, Type expected)
        {
            if ((value != null) && (value.GetType() != expected))
                throw new ArgumentException(SR.Format(SR.Converter_unsupported_value_type, expected.Name));
        }
    }
}
