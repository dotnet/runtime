// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Runtime_60035
{
    public static class Helper_System_Runtime_Intrinsics_X86_Sse3
    {
        public static bool IsSupported
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return System.Runtime.Intrinsics.X86.Sse3.IsSupported; }
        }
    }

    public static class Helper_System_Runtime_Intrinsics_X86_Sse3_X64
    {
        public static bool IsSupported
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return System.Runtime.Intrinsics.X86.Sse3.X64.IsSupported; }
        }
    }
}
