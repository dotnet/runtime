// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
#if NETCOREAPP
using System.Runtime.Intrinsics.Arm;
#endif
#if NETCOREAPP5_0
using System.Runtime.Intrinsics.X86;
#endif

namespace System.Text.Encodings.Web
{
    internal static class AdvSimdHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSse2OrAdvSimdArm64Supported()
        {
#if NETCOREAPP5_0
            return Sse2.IsSupported || AdvSimd.Arm64.IsSupported;
#elif NETCOREAPP
            return Sse2.IsSupported;
#else
            return false;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAdvSimdArm64Supported()
        {
#if NETCOREAPP5_0
            return AdvSimd.Arm64.IsSupported;
#else
            return false;
#endif
        }
    }
}
