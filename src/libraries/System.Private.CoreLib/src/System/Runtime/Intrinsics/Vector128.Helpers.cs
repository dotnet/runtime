// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Runtime.Intrinsics
{
    // Contains internal helper methods
    public static partial class Vector128
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<T> ConditionalSelectBitwise<T>(Vector128<T> selector, Vector128<T> ifTrue, Vector128<T> ifFalse)
            where T : struct
        {
            Debug.Assert(Sse.IsSupported || AdvSimd.IsSupported);

            if (Sse.IsSupported)
            {
                ifTrue = Sse.And(ifTrue.AsSingle(), selector.AsSingle());
                ifFalse = Sse.AndNot(selector.AsSingle(), ifFalse.AsSingle());

                return Sse.Or(ifFalse, ifTrue).As<float, T>();
            }
            else (AdvSimd.IsSupported)
            {
                return AdvSimd.BitwiseSelect(selector.AsByte(), ifTrue.AsByte(), ifFalse.AsByte()).As<byte, T>();
            }
        }
    }
}
