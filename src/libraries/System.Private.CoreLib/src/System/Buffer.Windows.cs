// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public static partial class Buffer
    {
#if TARGET_ARM64
        // Determine optimal value for Windows.
        // https://github.com/dotnet/runtime/issues/8896
        private static nuint MemmoveNativeThreshold => nuint.MaxValue;
#else
        private const nuint MemmoveNativeThreshold = 2048;
#endif
    }
}
