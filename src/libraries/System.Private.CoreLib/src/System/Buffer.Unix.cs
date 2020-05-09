// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public static partial class Buffer
    {
#if TARGET_ARM64
        // Managed code is currently faster than glibc unoptimized memmove
        // TODO-ARM64-UNIX-OPT revisit when glibc optimized memmove is in Linux distros
        // https://github.com/dotnet/runtime/issues/8897
        private static nuint MemmoveNativeThreshold => nuint.MaxValue;
#elif TARGET_ARM
        private const nuint MemmoveNativeThreshold = 512;
#else
        private const nuint MemmoveNativeThreshold = 2048;
#endif
    }
}
