// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 BMI2 hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Bmi2 : X86Base
    {
        internal Bmi2() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the x86 BMI2 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : X86Base.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            /// unsigned __int64 _bzhi_u64 (unsigned __int64 a, unsigned int index)
            ///   BZHI r64a, r/m64, r64b
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ZeroHighBits(ulong value, ulong index) => ZeroHighBits(value, index);

            /// <summary>
            /// unsigned __int64 _mulx_u64 (unsigned __int64 a, unsigned __int64 b, unsigned __int64* hi)
            ///   MULX r64a, r64b, r/m64
            /// The above native signature does not directly correspond to the managed signature.
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong MultiplyNoFlags(ulong left, ulong right) => MultiplyNoFlags(left, right);

            /// <summary>
            /// unsigned __int64 _mulx_u64 (unsigned __int64 a, unsigned __int64 b, unsigned __int64* hi)
            ///   MULX r64a, r64b, r/m64
            /// The above native signature does not directly correspond to the managed signature.
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static unsafe ulong MultiplyNoFlags(ulong left, ulong right, ulong* low) => MultiplyNoFlags(left, right, low);

            /// <summary>
            /// unsigned __int64 _pdep_u64 (unsigned __int64 a, unsigned __int64 mask)
            ///   PDEP r64a, r64b, r/m64
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ParallelBitDeposit(ulong value, ulong mask) => ParallelBitDeposit(value, mask);

            /// <summary>
            /// unsigned __int64 _pext_u64 (unsigned __int64 a, unsigned __int64 mask)
            ///   PEXT r64a, r64b, r/m64
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ParallelBitExtract(ulong value, ulong mask) => ParallelBitExtract(value, mask);
        }

        /// <summary>
        /// unsigned int _bzhi_u32 (unsigned int a, unsigned int index)
        ///   BZHI r32a, r/m32, r32b
        /// </summary>
        public static uint ZeroHighBits(uint value, uint index) => ZeroHighBits(value, index);

        /// <summary>
        /// unsigned int _mulx_u32 (unsigned int a, unsigned int b, unsigned int* hi)
        ///   MULX r32a, r32b, r/m32
        /// The above native signature does not directly correspond to the managed signature.
        /// </summary>
        public static uint MultiplyNoFlags(uint left, uint right) => MultiplyNoFlags(left, right);

        /// <summary>
        /// unsigned int _mulx_u32 (unsigned int a, unsigned int b, unsigned int* hi)
        ///   MULX r32a, r32b, r/m32
        /// The above native signature does not directly correspond to the managed signature.
        /// </summary>
        public static unsafe uint MultiplyNoFlags(uint left, uint right, uint* low) => MultiplyNoFlags(left, right, low);

        /// <summary>
        /// unsigned int _pdep_u32 (unsigned int a, unsigned int mask)
        ///   PDEP r32a, r32b, r/m32
        /// </summary>
        public static uint ParallelBitDeposit(uint value, uint mask) => ParallelBitDeposit(value, mask);

        /// <summary>
        /// unsigned int _pext_u32 (unsigned int a, unsigned int mask)
        ///   PEXT r32a, r32b, r/m32
        /// </summary>
        public static uint ParallelBitExtract(uint value, uint mask) => ParallelBitExtract(value, mask);
    }
}
