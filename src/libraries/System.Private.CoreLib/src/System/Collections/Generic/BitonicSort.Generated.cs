// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using static System.Runtime.Intrinsics.X86.Avx;
using static System.Runtime.Intrinsics.X86.Avx2;

namespace System.Collections.Generic
{
    using V = Vector256<int>;
    static unsafe partial class BitonicSort<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort02V(ref V d01, ref V d02)
        {
            V tmp;

            BitonicSort01V(ref d01);
            BitonicSort01V(ref d02);

            tmp = Shuffle(d02, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d02 = Max(d01, tmp);
            d01 = Min(d01, tmp);

            BitonicSort01VMerge(ref d01);
            BitonicSort01VMerge(ref d02);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort02VMerge(ref V d01, ref V d02)
        {
            V tmp;

            tmp = d01;
            d01 = Min(d02, d01);
            d02 = Max(d02, tmp);

            BitonicSort01VMerge(ref d01);
            BitonicSort01VMerge(ref d02);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort03V(ref V d01, ref V d02, ref V d03)
        {
            V tmp;

            BitonicSort02V(ref d01, ref d02);
            BitonicSort01V(ref d03);

            tmp = Shuffle(d03, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d03 = Max(d02, tmp);
            d02 = Min(d02, tmp);

            BitonicSort02VMerge(ref d01, ref d02);
            BitonicSort01VMerge(ref d03);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort03VMerge(ref V d01, ref V d02, ref V d03)
        {
            V tmp;

            tmp = d01;
            d01 = Min(d03, d01);
            d03 = Max(d03, tmp);

            BitonicSort02VMerge(ref d01, ref d02);
            BitonicSort01VMerge(ref d03);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort04V(ref V d01, ref V d02, ref V d03, ref V d04)
        {
            V tmp;

            BitonicSort02V(ref d01, ref d02);
            BitonicSort02V(ref d03, ref d04);

            tmp = Shuffle(d03, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d03 = Max(d02, tmp);
            d02 = Min(d02, tmp);

            tmp = Shuffle(d04, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d04 = Max(d01, tmp);
            d01 = Min(d01, tmp);

            BitonicSort02VMerge(ref d01, ref d02);
            BitonicSort02VMerge(ref d03, ref d04);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort04VMerge(ref V d01, ref V d02, ref V d03, ref V d04)
        {
            V tmp;

            tmp = d01;
            d01 = Min(d03, d01);
            d03 = Max(d03, tmp);

            tmp = d02;
            d02 = Min(d04, d02);
            d04 = Max(d04, tmp);

            BitonicSort02VMerge(ref d01, ref d02);
            BitonicSort02VMerge(ref d03, ref d04);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort05V(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05)
        {
            V tmp;

            BitonicSort04V(ref d01, ref d02, ref d03, ref d04);
            BitonicSort01V(ref d05);

            tmp = Shuffle(d05, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d05 = Max(d04, tmp);
            d04 = Min(d04, tmp);

            BitonicSort04VMerge(ref d01, ref d02, ref d03, ref d04);
            BitonicSort01VMerge(ref d05);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort05VMerge(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05)
        {
            V tmp;

            tmp = d01;
            d01 = Min(d05, d01);
            d05 = Max(d05, tmp);

            BitonicSort04VMerge(ref d01, ref d02, ref d03, ref d04);
            BitonicSort01VMerge(ref d05);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort06V(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06)
        {
            V tmp;

            BitonicSort04V(ref d01, ref d02, ref d03, ref d04);
            BitonicSort02V(ref d05, ref d06);

            tmp = Shuffle(d05, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d05 = Max(d04, tmp);
            d04 = Min(d04, tmp);

            tmp = Shuffle(d06, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d06 = Max(d03, tmp);
            d03 = Min(d03, tmp);

            BitonicSort04VMerge(ref d01, ref d02, ref d03, ref d04);
            BitonicSort02VMerge(ref d05, ref d06);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort06VMerge(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06)
        {
            V tmp;

            tmp = d01;
            d01 = Min(d05, d01);
            d05 = Max(d05, tmp);

            tmp = d02;
            d02 = Min(d06, d02);
            d06 = Max(d06, tmp);

            BitonicSort04VMerge(ref d01, ref d02, ref d03, ref d04);
            BitonicSort02VMerge(ref d05, ref d06);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort07V(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06, ref V d07)
        {
            V tmp;

            BitonicSort04V(ref d01, ref d02, ref d03, ref d04);
            BitonicSort03V(ref d05, ref d06, ref d07);

            tmp = Shuffle(d05, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d05 = Max(d04, tmp);
            d04 = Min(d04, tmp);

            tmp = Shuffle(d06, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d06 = Max(d03, tmp);
            d03 = Min(d03, tmp);

            tmp = Shuffle(d07, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d07 = Max(d02, tmp);
            d02 = Min(d02, tmp);

            BitonicSort04VMerge(ref d01, ref d02, ref d03, ref d04);
            BitonicSort03VMerge(ref d05, ref d06, ref d07);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort07VMerge(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06, ref V d07)
        {
            V tmp;

            tmp = d01;
            d01 = Min(d05, d01);
            d05 = Max(d05, tmp);

            tmp = d02;
            d02 = Min(d06, d02);
            d06 = Max(d06, tmp);

            tmp = d03;
            d03 = Min(d07, d03);
            d07 = Max(d07, tmp);

            BitonicSort04VMerge(ref d01, ref d02, ref d03, ref d04);
            BitonicSort03VMerge(ref d05, ref d06, ref d07);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort08V(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06, ref V d07, ref V d08)
        {
            V tmp;

            BitonicSort04V(ref d01, ref d02, ref d03, ref d04);
            BitonicSort04V(ref d05, ref d06, ref d07, ref d08);

            tmp = Shuffle(d05, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d05 = Max(d04, tmp);
            d04 = Min(d04, tmp);

            tmp = Shuffle(d06, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d06 = Max(d03, tmp);
            d03 = Min(d03, tmp);

            tmp = Shuffle(d07, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d07 = Max(d02, tmp);
            d02 = Min(d02, tmp);

            tmp = Shuffle(d08, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d08 = Max(d01, tmp);
            d01 = Min(d01, tmp);

            BitonicSort04VMerge(ref d01, ref d02, ref d03, ref d04);
            BitonicSort04VMerge(ref d05, ref d06, ref d07, ref d08);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort08VMerge(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06, ref V d07, ref V d08)
        {
            V tmp;

            tmp = d01;
            d01 = Min(d05, d01);
            d05 = Max(d05, tmp);

            tmp = d02;
            d02 = Min(d06, d02);
            d06 = Max(d06, tmp);

            tmp = d03;
            d03 = Min(d07, d03);
            d07 = Max(d07, tmp);

            tmp = d04;
            d04 = Min(d08, d04);
            d08 = Max(d08, tmp);

            BitonicSort04VMerge(ref d01, ref d02, ref d03, ref d04);
            BitonicSort04VMerge(ref d05, ref d06, ref d07, ref d08);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort09V(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06, ref V d07, ref V d08, ref V d09)
        {
            V tmp;

            BitonicSort08V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort01V(ref d09);

            tmp = Shuffle(d09, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d09 = Max(d08, tmp);
            d08 = Min(d08, tmp);

            BitonicSort08VMerge(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort01VMerge(ref d09);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort10V(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06, ref V d07, ref V d08, ref V d09, ref V d10)
        {
            V tmp;

            BitonicSort08V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort02V(ref d09, ref d10);

            tmp = Shuffle(d09, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d09 = Max(d08, tmp);
            d08 = Min(d08, tmp);

            tmp = Shuffle(d10, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d10 = Max(d07, tmp);
            d07 = Min(d07, tmp);

            BitonicSort08VMerge(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort02VMerge(ref d09, ref d10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort11V(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06, ref V d07, ref V d08, ref V d09, ref V d10, ref V d11)
        {
            V tmp;

            BitonicSort08V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort03V(ref d09, ref d10, ref d11);

            tmp = Shuffle(d09, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d09 = Max(d08, tmp);
            d08 = Min(d08, tmp);

            tmp = Shuffle(d10, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d10 = Max(d07, tmp);
            d07 = Min(d07, tmp);

            tmp = Shuffle(d11, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d11 = Max(d06, tmp);
            d06 = Min(d06, tmp);

            BitonicSort08VMerge(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort03VMerge(ref d09, ref d10, ref d11);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort12V(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06, ref V d07, ref V d08, ref V d09, ref V d10, ref V d11, ref V d12)
        {
            V tmp;

            BitonicSort08V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort04V(ref d09, ref d10, ref d11, ref d12);

            tmp = Shuffle(d09, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d09 = Max(d08, tmp);
            d08 = Min(d08, tmp);

            tmp = Shuffle(d10, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d10 = Max(d07, tmp);
            d07 = Min(d07, tmp);

            tmp = Shuffle(d11, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d11 = Max(d06, tmp);
            d06 = Min(d06, tmp);

            tmp = Shuffle(d12, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d12 = Max(d05, tmp);
            d05 = Min(d05, tmp);

            BitonicSort08VMerge(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort04VMerge(ref d09, ref d10, ref d11, ref d12);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort13V(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06, ref V d07, ref V d08, ref V d09, ref V d10, ref V d11, ref V d12, ref V d13)
        {
            V tmp;

            BitonicSort08V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort05V(ref d09, ref d10, ref d11, ref d12, ref d13);

            tmp = Shuffle(d09, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d09 = Max(d08, tmp);
            d08 = Min(d08, tmp);

            tmp = Shuffle(d10, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d10 = Max(d07, tmp);
            d07 = Min(d07, tmp);

            tmp = Shuffle(d11, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d11 = Max(d06, tmp);
            d06 = Min(d06, tmp);

            tmp = Shuffle(d12, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d12 = Max(d05, tmp);
            d05 = Min(d05, tmp);

            tmp = Shuffle(d13, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d13 = Max(d04, tmp);
            d04 = Min(d04, tmp);

            BitonicSort08VMerge(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort05VMerge(ref d09, ref d10, ref d11, ref d12, ref d13);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort14V(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06, ref V d07, ref V d08, ref V d09, ref V d10, ref V d11, ref V d12, ref V d13, ref V d14)
        {
            V tmp;

            BitonicSort08V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort06V(ref d09, ref d10, ref d11, ref d12, ref d13, ref d14);

            tmp = Shuffle(d09, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d09 = Max(d08, tmp);
            d08 = Min(d08, tmp);

            tmp = Shuffle(d10, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d10 = Max(d07, tmp);
            d07 = Min(d07, tmp);

            tmp = Shuffle(d11, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d11 = Max(d06, tmp);
            d06 = Min(d06, tmp);

            tmp = Shuffle(d12, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d12 = Max(d05, tmp);
            d05 = Min(d05, tmp);

            tmp = Shuffle(d13, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d13 = Max(d04, tmp);
            d04 = Min(d04, tmp);

            tmp = Shuffle(d14, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d14 = Max(d03, tmp);
            d03 = Min(d03, tmp);

            BitonicSort08VMerge(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort06VMerge(ref d09, ref d10, ref d11, ref d12, ref d13, ref d14);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort15V(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06, ref V d07, ref V d08, ref V d09, ref V d10, ref V d11, ref V d12, ref V d13, ref V d14, ref V d15)
        {
            V tmp;

            BitonicSort08V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort07V(ref d09, ref d10, ref d11, ref d12, ref d13, ref d14, ref d15);

            tmp = Shuffle(d09, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d09 = Max(d08, tmp);
            d08 = Min(d08, tmp);

            tmp = Shuffle(d10, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d10 = Max(d07, tmp);
            d07 = Min(d07, tmp);

            tmp = Shuffle(d11, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d11 = Max(d06, tmp);
            d06 = Min(d06, tmp);

            tmp = Shuffle(d12, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d12 = Max(d05, tmp);
            d05 = Min(d05, tmp);

            tmp = Shuffle(d13, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d13 = Max(d04, tmp);
            d04 = Min(d04, tmp);

            tmp = Shuffle(d14, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d14 = Max(d03, tmp);
            d03 = Min(d03, tmp);

            tmp = Shuffle(d15, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d15 = Max(d02, tmp);
            d02 = Min(d02, tmp);

            BitonicSort08VMerge(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort07VMerge(ref d09, ref d10, ref d11, ref d12, ref d13, ref d14, ref d15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort16V(ref V d01, ref V d02, ref V d03, ref V d04, ref V d05, ref V d06, ref V d07, ref V d08, ref V d09, ref V d10, ref V d11, ref V d12, ref V d13, ref V d14, ref V d15, ref V d16)
        {
            V tmp;

            BitonicSort08V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort08V(ref d09, ref d10, ref d11, ref d12, ref d13, ref d14, ref d15, ref d16);

            tmp = Shuffle(d09, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d09 = Max(d08, tmp);
            d08 = Min(d08, tmp);

            tmp = Shuffle(d10, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d10 = Max(d07, tmp);
            d07 = Min(d07, tmp);

            tmp = Shuffle(d11, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d11 = Max(d06, tmp);
            d06 = Min(d06, tmp);

            tmp = Shuffle(d12, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d12 = Max(d05, tmp);
            d05 = Min(d05, tmp);

            tmp = Shuffle(d13, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d13 = Max(d04, tmp);
            d04 = Min(d04, tmp);

            tmp = Shuffle(d14, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d14 = Max(d03, tmp);
            d03 = Min(d03, tmp);

            tmp = Shuffle(d15, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d15 = Max(d02, tmp);
            d02 = Min(d02, tmp);

            tmp = Shuffle(d16, X_R);
            tmp = Permute4x64(tmp.AsInt64(), P_X).AsInt32();
            d16 = Max(d01, tmp);
            d01 = Min(d01, tmp);

            BitonicSort08VMerge(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);
            BitonicSort08VMerge(ref d09, ref d10, ref d11, ref d12, ref d13, ref d14, ref d15, ref d16);
        }


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort01V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);

            BitonicSort01V(ref d01);

            Store(ptr + 00*N, d01);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort02V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);

            BitonicSort02V(ref d01, ref d02);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort03V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);

            BitonicSort03V(ref d01, ref d02, ref d03);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort04V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);

            BitonicSort04V(ref d01, ref d02, ref d03, ref d04);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort05V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);
            var d05 = LoadDquVector256(ptr + 04*N);

            BitonicSort05V(ref d01, ref d02, ref d03, ref d04, ref d05);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
            Store(ptr + 04*N, d05);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort06V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);
            var d05 = LoadDquVector256(ptr + 04*N);
            var d06 = LoadDquVector256(ptr + 05*N);

            BitonicSort06V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
            Store(ptr + 04*N, d05);
            Store(ptr + 05*N, d06);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort07V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);
            var d05 = LoadDquVector256(ptr + 04*N);
            var d06 = LoadDquVector256(ptr + 05*N);
            var d07 = LoadDquVector256(ptr + 06*N);

            BitonicSort07V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
            Store(ptr + 04*N, d05);
            Store(ptr + 05*N, d06);
            Store(ptr + 06*N, d07);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort08V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);
            var d05 = LoadDquVector256(ptr + 04*N);
            var d06 = LoadDquVector256(ptr + 05*N);
            var d07 = LoadDquVector256(ptr + 06*N);
            var d08 = LoadDquVector256(ptr + 07*N);

            BitonicSort08V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
            Store(ptr + 04*N, d05);
            Store(ptr + 05*N, d06);
            Store(ptr + 06*N, d07);
            Store(ptr + 07*N, d08);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort09V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);
            var d05 = LoadDquVector256(ptr + 04*N);
            var d06 = LoadDquVector256(ptr + 05*N);
            var d07 = LoadDquVector256(ptr + 06*N);
            var d08 = LoadDquVector256(ptr + 07*N);
            var d09 = LoadDquVector256(ptr + 08*N);

            BitonicSort09V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08, ref d09);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
            Store(ptr + 04*N, d05);
            Store(ptr + 05*N, d06);
            Store(ptr + 06*N, d07);
            Store(ptr + 07*N, d08);
            Store(ptr + 08*N, d09);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort10V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);
            var d05 = LoadDquVector256(ptr + 04*N);
            var d06 = LoadDquVector256(ptr + 05*N);
            var d07 = LoadDquVector256(ptr + 06*N);
            var d08 = LoadDquVector256(ptr + 07*N);
            var d09 = LoadDquVector256(ptr + 08*N);
            var d10 = LoadDquVector256(ptr + 09*N);

            BitonicSort10V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08, ref d09, ref d10);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
            Store(ptr + 04*N, d05);
            Store(ptr + 05*N, d06);
            Store(ptr + 06*N, d07);
            Store(ptr + 07*N, d08);
            Store(ptr + 08*N, d09);
            Store(ptr + 09*N, d10);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort11V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);
            var d05 = LoadDquVector256(ptr + 04*N);
            var d06 = LoadDquVector256(ptr + 05*N);
            var d07 = LoadDquVector256(ptr + 06*N);
            var d08 = LoadDquVector256(ptr + 07*N);
            var d09 = LoadDquVector256(ptr + 08*N);
            var d10 = LoadDquVector256(ptr + 09*N);
            var d11 = LoadDquVector256(ptr + 10*N);

            BitonicSort11V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08, ref d09, ref d10, ref d11);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
            Store(ptr + 04*N, d05);
            Store(ptr + 05*N, d06);
            Store(ptr + 06*N, d07);
            Store(ptr + 07*N, d08);
            Store(ptr + 08*N, d09);
            Store(ptr + 09*N, d10);
            Store(ptr + 10*N, d11);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort12V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);
            var d05 = LoadDquVector256(ptr + 04*N);
            var d06 = LoadDquVector256(ptr + 05*N);
            var d07 = LoadDquVector256(ptr + 06*N);
            var d08 = LoadDquVector256(ptr + 07*N);
            var d09 = LoadDquVector256(ptr + 08*N);
            var d10 = LoadDquVector256(ptr + 09*N);
            var d11 = LoadDquVector256(ptr + 10*N);
            var d12 = LoadDquVector256(ptr + 11*N);

            BitonicSort12V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08, ref d09, ref d10, ref d11, ref d12);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
            Store(ptr + 04*N, d05);
            Store(ptr + 05*N, d06);
            Store(ptr + 06*N, d07);
            Store(ptr + 07*N, d08);
            Store(ptr + 08*N, d09);
            Store(ptr + 09*N, d10);
            Store(ptr + 10*N, d11);
            Store(ptr + 11*N, d12);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort13V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);
            var d05 = LoadDquVector256(ptr + 04*N);
            var d06 = LoadDquVector256(ptr + 05*N);
            var d07 = LoadDquVector256(ptr + 06*N);
            var d08 = LoadDquVector256(ptr + 07*N);
            var d09 = LoadDquVector256(ptr + 08*N);
            var d10 = LoadDquVector256(ptr + 09*N);
            var d11 = LoadDquVector256(ptr + 10*N);
            var d12 = LoadDquVector256(ptr + 11*N);
            var d13 = LoadDquVector256(ptr + 12*N);

            BitonicSort13V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08, ref d09, ref d10, ref d11, ref d12, ref d13);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
            Store(ptr + 04*N, d05);
            Store(ptr + 05*N, d06);
            Store(ptr + 06*N, d07);
            Store(ptr + 07*N, d08);
            Store(ptr + 08*N, d09);
            Store(ptr + 09*N, d10);
            Store(ptr + 10*N, d11);
            Store(ptr + 11*N, d12);
            Store(ptr + 12*N, d13);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort14V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);
            var d05 = LoadDquVector256(ptr + 04*N);
            var d06 = LoadDquVector256(ptr + 05*N);
            var d07 = LoadDquVector256(ptr + 06*N);
            var d08 = LoadDquVector256(ptr + 07*N);
            var d09 = LoadDquVector256(ptr + 08*N);
            var d10 = LoadDquVector256(ptr + 09*N);
            var d11 = LoadDquVector256(ptr + 10*N);
            var d12 = LoadDquVector256(ptr + 11*N);
            var d13 = LoadDquVector256(ptr + 12*N);
            var d14 = LoadDquVector256(ptr + 13*N);

            BitonicSort14V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08, ref d09, ref d10, ref d11, ref d12, ref d13, ref d14);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
            Store(ptr + 04*N, d05);
            Store(ptr + 05*N, d06);
            Store(ptr + 06*N, d07);
            Store(ptr + 07*N, d08);
            Store(ptr + 08*N, d09);
            Store(ptr + 09*N, d10);
            Store(ptr + 10*N, d11);
            Store(ptr + 11*N, d12);
            Store(ptr + 12*N, d13);
            Store(ptr + 13*N, d14);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort15V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);
            var d05 = LoadDquVector256(ptr + 04*N);
            var d06 = LoadDquVector256(ptr + 05*N);
            var d07 = LoadDquVector256(ptr + 06*N);
            var d08 = LoadDquVector256(ptr + 07*N);
            var d09 = LoadDquVector256(ptr + 08*N);
            var d10 = LoadDquVector256(ptr + 09*N);
            var d11 = LoadDquVector256(ptr + 10*N);
            var d12 = LoadDquVector256(ptr + 11*N);
            var d13 = LoadDquVector256(ptr + 12*N);
            var d14 = LoadDquVector256(ptr + 13*N);
            var d15 = LoadDquVector256(ptr + 14*N);

            BitonicSort15V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08, ref d09, ref d10, ref d11, ref d12, ref d13, ref d14, ref d15);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
            Store(ptr + 04*N, d05);
            Store(ptr + 05*N, d06);
            Store(ptr + 06*N, d07);
            Store(ptr + 07*N, d08);
            Store(ptr + 08*N, d09);
            Store(ptr + 09*N, d10);
            Store(ptr + 10*N, d11);
            Store(ptr + 11*N, d12);
            Store(ptr + 12*N, d13);
            Store(ptr + 13*N, d14);
            Store(ptr + 14*N, d15);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void BitonicSort16V(int* ptr)
        {
            var N = V.Count;

            var d01 = LoadDquVector256(ptr + 00*N);
            var d02 = LoadDquVector256(ptr + 01*N);
            var d03 = LoadDquVector256(ptr + 02*N);
            var d04 = LoadDquVector256(ptr + 03*N);
            var d05 = LoadDquVector256(ptr + 04*N);
            var d06 = LoadDquVector256(ptr + 05*N);
            var d07 = LoadDquVector256(ptr + 06*N);
            var d08 = LoadDquVector256(ptr + 07*N);
            var d09 = LoadDquVector256(ptr + 08*N);
            var d10 = LoadDquVector256(ptr + 09*N);
            var d11 = LoadDquVector256(ptr + 10*N);
            var d12 = LoadDquVector256(ptr + 11*N);
            var d13 = LoadDquVector256(ptr + 12*N);
            var d14 = LoadDquVector256(ptr + 13*N);
            var d15 = LoadDquVector256(ptr + 14*N);
            var d16 = LoadDquVector256(ptr + 15*N);

            BitonicSort16V(ref d01, ref d02, ref d03, ref d04, ref d05, ref d06, ref d07, ref d08, ref d09, ref d10, ref d11, ref d12, ref d13, ref d14, ref d15, ref d16);

            Store(ptr + 00*N, d01);
            Store(ptr + 01*N, d02);
            Store(ptr + 02*N, d03);
            Store(ptr + 03*N, d04);
            Store(ptr + 04*N, d05);
            Store(ptr + 05*N, d06);
            Store(ptr + 06*N, d07);
            Store(ptr + 07*N, d08);
            Store(ptr + 08*N, d09);
            Store(ptr + 09*N, d10);
            Store(ptr + 10*N, d11);
            Store(ptr + 11*N, d12);
            Store(ptr + 12*N, d13);
            Store(ptr + 13*N, d14);
            Store(ptr + 14*N, d15);
            Store(ptr + 15*N, d16);
        }

        public const int MinBitonicSortSize = 8;
        public const int MaxBitonicSortSize = 128;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Sort(int* ptr, int length)
        {
            Debug.Assert(length % 8 == 0);
            Debug.Assert(length <= MaxBitonicSortSize);

            switch (length / 8) {
                case 01: BitonicSort01V(ptr); return;
                case 02: BitonicSort02V(ptr); return;
                case 03: BitonicSort03V(ptr); return;
                case 04: BitonicSort04V(ptr); return;
                case 05: BitonicSort05V(ptr); return;
                case 06: BitonicSort06V(ptr); return;
                case 07: BitonicSort07V(ptr); return;
                case 08: BitonicSort08V(ptr); return;
                case 09: BitonicSort09V(ptr); return;
                case 10: BitonicSort10V(ptr); return;
                case 11: BitonicSort11V(ptr); return;
                case 12: BitonicSort12V(ptr); return;
                case 13: BitonicSort13V(ptr); return;
                case 14: BitonicSort14V(ptr); return;
                case 15: BitonicSort15V(ptr); return;
                case 16: BitonicSort16V(ptr); return;

                default:
                    throw new NotSupportedException("length is not power a multiple of 8 && <= 128");
            }
        }
    }
}
