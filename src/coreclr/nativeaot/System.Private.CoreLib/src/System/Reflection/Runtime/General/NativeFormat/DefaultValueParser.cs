// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.General.NativeFormat
{
    internal static class DefaultValueParser
    {
        public static bool GetDefaultValueFromConstantIfAny(MetadataReader reader, Handle constantHandle, Type declaredType, bool raw, out object? defaultValue)
        {
            if (!constantHandle.IsNil)
            {
                defaultValue = constantHandle.ParseConstantValue(reader);
                if ((!raw) && declaredType.IsEnum && defaultValue != null && !declaredType.ContainsGenericParameters)
                    defaultValue = Enum.ToObject(declaredType, defaultValue);
                return true;
            }

            defaultValue = null;
            return false;
        }
    }
}
