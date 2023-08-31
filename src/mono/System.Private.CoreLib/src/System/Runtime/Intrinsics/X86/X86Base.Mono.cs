// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    public abstract partial class X86Base
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void __cpuidex(int* cpuInfo, int functionId, int subFunctionId);

        [Intrinsic]
        private static void DivRemInternal(long lower, long upper, long divisor, out long quotient, out long remainder) => 
            DivRemInternal (lower, upper, divisor, out quotient, out remainder);

        [Intrinsic]
        private static void DivRemInternal(ulong lower, ulong upper, ulong divisor, out ulong quotient, out ulong remainder) => 
            DivRemInternal (lower, upper, divisor, out quotient, out remainder);

        [MethodImpl(AggressiveInlining)]
        (long quotient, long remainder) DivRem (long lower, long upper, long divisor) 
        {
            DivRemInternal(lower, upper, divisor, out long quotient, out long remainder);
            return (quotient, remainder);
        }

        [MethodImpl(AggressiveInlining)]
        (ulong quotient, ulong remainder) DivRem (ulong lower, ulong upper, ulong divisor) 
        {
            DivRemInternal(lower, upper, divisor, out ulong quotient, out ulong remainder);
            return (quotient, remainder);
        }
    }
}
