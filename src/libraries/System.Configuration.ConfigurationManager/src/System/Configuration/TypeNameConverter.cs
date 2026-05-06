// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;

namespace System.Configuration
{
    public sealed class TypeNameConverter : ConfigurationConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
        {
            // Make the check here since for some reason value.GetType is not System.Type but RuntimeType
            if (!(value is Type)) ValidateType(value, typeof(Type));

            string result = null;

            if (value != null) result = ((Type)value).AssemblyQualifiedName;

            return result;
        }

        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
        {
            Type result = TypeUtil.GetType((string)data, false);

            if (result == null) throw new ArgumentException(SR.Format(SR.Type_cannot_be_resolved, (string)data));

            return result;
        }
    }
}
