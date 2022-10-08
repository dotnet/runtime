// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection.Runtime.TypeInfos.NativeFormat;

namespace System.Reflection.Runtime.General
{
    internal static partial class Helpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeFormatRuntimeNamedTypeInfo CastToNativeFormatRuntimeNamedTypeInfo(this Type type)
        {
            Debug.Assert(type is NativeFormatRuntimeNamedTypeInfo);
            return (NativeFormatRuntimeNamedTypeInfo)type;
        }
    }
}
