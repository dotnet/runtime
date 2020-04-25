// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetICUVersion")]
#endif
        internal static extern int LoadICU();

#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetICUVersion")]
#endif
        internal static extern int GetICUVersion();
    }
}
