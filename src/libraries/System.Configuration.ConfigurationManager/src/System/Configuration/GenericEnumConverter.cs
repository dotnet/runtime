// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.ComponentModel;
using System.Text;

namespace System.Configuration
{
    public sealed class GenericEnumConverter : ConfigurationConverterBase
    {
        private readonly Type _enumType;

        public GenericEnumConverter(Type typeEnum)
        {
            if (typeEnum == null)
                throw new ArgumentNullException(nameof(typeEnum));

            _enumType = typeEnum;
        }

        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
        {
            return value.ToString();
        }

        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
        {
            // For any error, throw the ArgumentException with SR.Invalid_enum_value
            if ((data is string value) && (value.Length > 0))
            {
                // Disallow numeric values and whitespace at start and end.
                if ((!char.IsDigit(value[0])) && (value[0] != '-') && (value[0] != '+') &&
                    (!char.IsWhiteSpace(value[0])) && (!char.IsWhiteSpace(value[value.Length - 1])))
                {
                    try
                    {
                        return Enum.Parse(_enumType, value);
                    }
                    catch
                    {
                        // Exception from parse. Will throw more appropriate exception below.
                    }
                }
            }
            throw CreateExceptionForInvalidValue();
        }

        private ArgumentException CreateExceptionForInvalidValue()
        {
            string names = string.Join(", ", Enum.GetNames(_enumType));
            return new ArgumentException(SR.Format(SR.Invalid_enum_value, names));
        }
    }
}
