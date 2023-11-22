// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.General.NativeFormat
{
    internal static class DefaultValueParser
    {
        public static bool GetDefaultValueFromConstantIfAny(MetadataReader reader, Handle constantHandle, Type declaredType, bool raw, out object? defaultValue)
        {
            if (!(constantHandle.IsNull(reader)))
            {
                defaultValue = constantHandle.ParseConstantValue(reader);
                if ((!raw) && declaredType.IsEnum && defaultValue != null)
                    defaultValue = Enum.ToObject(declaredType, defaultValue);
                return true;
            }

            defaultValue = null;
            return false;
        }

        public static bool GetDefaultValueFromAttributeIfAny(IEnumerable<CustomAttributeData> customAttributes, bool raw, out object? defaultValue)
        {
            if (Helpers.GetCustomAttributeDefaultValueIfAny(customAttributes, raw, out defaultValue))
                return true;

            defaultValue = null;
            return false;
        }
    }
}
