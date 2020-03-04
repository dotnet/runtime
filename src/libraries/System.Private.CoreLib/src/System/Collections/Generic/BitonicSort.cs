// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using static System.Runtime.Intrinsics.X86.Avx2;

namespace System.Collections.Generic
{
    using V = Vector256<int>;
    internal static partial class BitonicSort<T> where T : unmanaged, IComparable<T>
    {

        // Legend:
        // X - shuffle/permute mask for generating a cross (X) shuffle
        //     the numbers (1,2,4) denote the stride of the shuffle operation
        // B - Blend mask, used for blending two vectors according to a given order
        //     the numbers (1,2,4) denote the "stride" of blending, e.g. 1 means switch vectors
        //     every element, 2 means switch vectors every two elements and so on...
        // P - Permute mask, read specific comment about it below...
        private const byte X_1 = 0b10_11_00_01;
        private const byte X_2 = 0b01_00_11_10;
        private const byte B_1 = 0b10_10_10_10;
        private const byte B_2 = 0b11_00_11_00;
        private const byte B_4 = 0b11_11_00_00;

        // Shuffle (X_R) + Permute (P_X) is a more efficient way
        // (copied shamelessly from LLVM through compiler explorer)
        // For implementing X_4, which requires a cross 128-bit lane operation.
        // A Shuffle (1c lat / 1c tp) + 64 bit permute (3c lat / 1c tp) take 1 more cycle to execute than the
        // the alternative: PermuteVar8x32 / VPERMD which takes (3c lat / 1c tp)
        // But, the latter requires loading the permutation entry from cache, which can take up to 5 cycles (when cached)
        // and costs one more register, which steals a register from us for high-count bitonic sorts.
        // In short, it's faster this way, from my attempts...
        private const byte X_R = 0b00_01_10_11;
        private const byte P_X = 0b01_00_11_10;

        // Basic 8-element bitonic sort
        // This will get composed and inlined throughout
        // the various bitonic-sort sizes:
        // BitonicSort1V will be directly embedded in BitonicSort{2,3,5,9}V
        // BitonicSort2V will be directly embedded in BitonicSort{3,4,6,10}V
        // BitonicSort3V will be directly embedded in BitonicSort{7,11}V
        // etc.
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort01V(ref V d)
        {
            // ReSharper disable JoinDeclarationAndInitializer
            V min, max, s;
            // ReSharper restore JoinDeclarationAndInitializer
            s   = Shuffle(d, X_1);
            min = Min(s, d);
            max = Max(s, d);
            d   = Blend(min, max, B_1);

            s   = Shuffle(d, X_R);
            min = Min(s, d);
            max = Max(s, d);
            d   = Blend(min, max, B_2);

            s   = Shuffle(d, X_1);
            min = Min(s, d);
            max = Max(s, d);
            d   = Blend(min, max, B_1);

            s   = Shuffle(d, X_R);
            s   = Permute4x64(s.AsInt64(), P_X).AsInt32();
            min = Min(s, d);
            max = Max(s, d);
            d   = Blend(min, max, B_4);

            s   = Shuffle(d, X_2);
            min = Min(s, d);
            max = Max(s, d);
            d   = Blend(min, max, B_2);

            s   = Shuffle(d, X_1);
            min = Min(s, d);
            max = Max(s, d);
            d   = Blend(min, max, B_1);
        }

        // Basic bitonic 8-element merge
        // This will get composed and inlined throughout
        // the code base for merging larger sized bitonic sorts temporary result states
        // BitonicSort1VFinish used for BitonicSort{2,3,5,9}V{,Finish}
        // BitonicSort2VFinish used for BitonicSort{3,4,6,10}V{,Finish}

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort01VMerge(ref V d)
        {
            // ReSharper disable JoinDeclarationAndInitializer
            V min, max, s;
            // ReSharper restore JoinDeclarationAndInitializer

            s   = Permute4x64(d.AsInt64(), P_X).AsInt32();
            min = Min(s, d);
            max = Max(s, d);
            d   = Blend(min, max, B_4);

            s   = Shuffle(d, X_2);
            min = Min(s, d);
            max = Max(s, d);
            d   = Blend(min, max, B_2);

            s   = Shuffle(d, X_1);
            min = Min(s, d);
            max = Max(s, d);
            d   = Blend(min, max, B_1);
        }
    }
}
