// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel BMI2 hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public static class Bmi2
    {
        public static bool IsSupported { get => IsSupported; }

        /// <summary>
        /// unsigned int _bzhi_u32 (unsigned int a, unsigned int index)
        /// </summary>
        public static uint ZeroHighBits(uint value, uint index) => ZeroHighBits(value, index);
        /// <summary>
        /// unsigned __int64 _bzhi_u64 (unsigned __int64 a, unsigned int index)
        /// </summary>
        public static ulong ZeroHighBits(ulong value, ulong index) => ZeroHighBits(value, index);

        /// <summary>
        /// unsigned int _mulx_u32 (unsigned int a, unsigned int b, unsigned int* hi)
        /// </summary>
        public static unsafe uint MultiplyNoFlags(uint left, uint right, uint* high) => MultiplyNoFlags(left, right, high);
        /// <summary>
        /// unsigned __int64 _mulx_u64 (unsigned __int64 a, unsigned __int64 b, unsigned __int64* hi)
        /// </summary>
        public static unsafe ulong MultiplyNoFlags(ulong left, ulong right, ulong* high) => MultiplyNoFlags(left, right, high);

        /// <summary>
        /// unsigned int _pdep_u32 (unsigned int a, unsigned int mask)
        /// </summary>
        public static uint ParallelBitDeposit(uint value, uint mask) => ParallelBitDeposit(value, mask);
        /// <summary>
        /// unsigned __int64 _pdep_u64 (unsigned __int64 a, unsigned __int64 mask)
        /// </summary>
        public static ulong ParallelBitDeposit(ulong value, ulong mask) => ParallelBitDeposit(value, mask);

        /// <summary>
        /// unsigned int _pext_u32 (unsigned int a, unsigned int mask)
        /// </summary>
        public static uint ParallelBitExtract(uint value, uint mask) => ParallelBitExtract(value, mask);
        /// <summary>
        /// unsigned __int64 _pext_u64 (unsigned __int64 a, unsigned __int64 mask)
        /// </summary>
        public static ulong ParallelBitExtract(ulong value, ulong mask) => ParallelBitExtract(value, mask);        
    }
}
