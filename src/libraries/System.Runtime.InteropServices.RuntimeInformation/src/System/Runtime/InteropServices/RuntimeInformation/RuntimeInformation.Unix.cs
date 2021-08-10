// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static string? s_osDescription;

        public static string OSDescription => s_osDescription ??= Interop.Sys.GetUnixVersion();

        public static Architecture ProcessArchitecture { get; } =
#if TARGET_AMD64
            Architecture.X64;
#elif TARGET_ARM
            Architecture.Arm;
#elif TARGET_ARM64
            Architecture.Arm64;
#elif TARGET_S390X
            Architecture.S390x;
#elif TARGET_WASM
            Architecture.Wasm;
#elif TARGET_X86
            Architecture.X86;
#else
#error Unidentified Architecture
#endif

        public static Architecture OSArchitecture { get; } = ProcessArchitecture;
    }
}
