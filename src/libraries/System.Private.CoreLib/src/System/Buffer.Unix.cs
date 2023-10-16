// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public static partial class Buffer
    {
#if TARGET_LOONGARCH64
        // Managed code is currently faster than glibc unoptimized memmove
        private static nuint MemmoveNativeThreshold => nuint.MaxValue;
#elif TARGET_ARM || TARGET_ARM64
        private const nuint MemmoveNativeThreshold = 512;
#else
        private const nuint MemmoveNativeThreshold = 2048;
#endif
    }
}
