// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::Internal.Metadata.NativeFormat;
using global::System;

using Debug = System.Diagnostics.Debug;

namespace Internal.Reflection.Execution
{
    internal static class MetadataReaderExtensions
    {
        public static string GetString(this ConstantStringValueHandle handle, MetadataReader reader)
        {
            return reader.GetConstantStringValue(handle).Value;
        }

        public static MethodHandle AsMethodHandle(this int i)
        {
            unsafe
            {
                Debug.Assert((HandleType)((uint)i >> 24) == HandleType.Method);
                return *(MethodHandle*)&i;
            }
        }
    }
}
