// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        public static Architecture ProcessArchitecture
#if TARGET_X86
            => Architecture.X86;
#elif TARGET_AMD64
            => Architecture.X64;
#elif TARGET_ARM
            => Architecture.Arm;
#elif TARGET_ARM64
            => Architecture.Arm64;
#elif TARGET_WASM
            => Architecture.Wasm;
#elif TARGET_S390X
            => Architecture.S390x;
#else
#error Unknown Architecture
#endif
    }
}
