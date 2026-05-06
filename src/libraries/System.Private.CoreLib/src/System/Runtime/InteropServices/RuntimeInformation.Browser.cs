// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
#if TARGET_BROWSER
        public static string OSDescription => "Browser";
#elif TARGET_WASI
        public static string OSDescription => "WASI";
#else
    #error
#endif

        public static Architecture OSArchitecture => Architecture.Wasm;
    }
}
