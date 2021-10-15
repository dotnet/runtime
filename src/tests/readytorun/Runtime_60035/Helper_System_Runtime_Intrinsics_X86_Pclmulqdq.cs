// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Runtime_60035
{
    public static class Helper_System_Runtime_Intrinsics_X86_Pclmulqdq
    {
        public static bool IsSupported
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported; }
        }
    }

    public static class Helper_System_Runtime_Intrinsics_X86_Pclmulqdq_X64
    {
        public static bool IsSupported
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { return System.Runtime.Intrinsics.X86.Pclmulqdq.X64.IsSupported; }
        }
    }
}
