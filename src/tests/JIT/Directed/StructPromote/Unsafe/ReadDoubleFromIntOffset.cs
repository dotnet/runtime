// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The test came from https://github.com/dotnet/runtime/issues/21860.
// It tests that we do access overlapping fields with the correct types.
// Especially if the struct was casted by 'Unsafe.As` from a promoted type
// and the promoted type had another field on the same offset but with a different type/size.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System;

class TestReadIntAsDouble
{
	private struct Dec
	{
		public int uflags;
		public int uhi;
		public int ulo;
		public int umid;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct DecCalc1
	{
		[FieldOffset(0)]
		public int uflags;
		[FieldOffset(4)]
		public int uhi;
		[FieldOffset(8)]
		public int ulo;
		[FieldOffset(12)]
		public int umid;
		[FieldOffset(8)]
		public double ulomidLE;
	}

	public struct Data
	{
		public int x, y, z;
		public double m;
	}


	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void TestDoubleAssignment(Data d)
	{
		Dec p = default;
		p.ulo = d.x;
		p.umid = d.y;
        // The jit gets field's type based on offset, so it will return `ulo` as int.
        d.m = Unsafe.As<Dec, DecCalc1>(ref p).ulomidLE;
	}

    [StructLayout(LayoutKind.Explicit)]
    private struct DecCalc2
    {
        [FieldOffset(0)]
        public int uflags;
        [FieldOffset(4)]
        public int uhi;
        [FieldOffset(8)]
        public double ulomidLE;
        [FieldOffset(8)]
        public int ulo;
        [FieldOffset(12)]
        public int umid;

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestIntAssignment(Data d)
    {
        Dec p = default;
        p.ulo = d.x;
        p.umid = d.y;
        // The jit gets field's type based on offset, so it will return `ulomidLE` as double.
        d.x = Unsafe.As<Dec, DecCalc2>(ref p).ulo;
    }

    static int Main()
    {
        TestDoubleAssignment(default);
        TestIntAssignment(default);
		return 100;
    }
}
