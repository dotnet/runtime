// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.Intrinsics.X86
{
   [System.CLSCompliantAttribute(false)]
    public abstract partial class AvxVnni : System.Runtime.Intrinsics.X86.Avx2
    {
        internal AvxVnni() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> MultiplyWideningAndAdd(System.Runtime.Intrinsics.Vector256<int> addend, System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> MultiplyWideningAndAdd(System.Runtime.Intrinsics.Vector256<int> addend, System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningAndAddSaturate(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningAndAddSaturate(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> MultiplyWideningAndAddSaturate(System.Runtime.Intrinsics.Vector256<int> addend, System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> MultiplyWideningAndAddSaturate(System.Runtime.Intrinsics.Vector256<int> addend, System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Avx2.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
   
}
