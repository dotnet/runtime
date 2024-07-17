// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public static class Program
{
#region ExtendedUint_RiscVTests
	[DllImport("PrimitiveABI")]
	public static extern long Echo_ExtendedUint_RiscV(int a0, uint a1);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static long Echo_ExtendedUint_RiscV_Managed(int a0, uint a1) => unchecked((int)a1);

	[Fact]
	public static void Test_ExtendedUint_RiscV()
	{
		const uint arg = 0xB1ED0C1Eu;
		const long ret = unchecked((int)arg);
		long managed = Echo_ExtendedUint_RiscV_Managed(0, arg);
		long native = Echo_ExtendedUint_RiscV(0, arg);

		Assert.Equal(ret, managed);
		Assert.Equal(ret, native);
	}

	[Fact]
	public static void Test_ExtendedUint_ByReflection_RiscV()
	{
		const uint arg = 0xB1ED0C1Eu;
		const long ret = unchecked((int)arg);
		long managed = (long)typeof(Program).GetMethod("Echo_ExtendedUint_RiscV_Managed").Invoke(
			null, new object[] {0, arg});
		long native = (long)typeof(Program).GetMethod("Echo_ExtendedUint_RiscV").Invoke(
			null, new object[] {0, arg});

		Assert.Equal(ret, managed);
		Assert.Equal(ret, native);
	}

	[DllImport("PrimitiveABI")]
	public static extern long Echo_ExtendedUint_OnStack_RiscV(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, uint stack0);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static long Echo_ExtendedUint_OnStack_RiscV_Managed(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, uint stack0) => unchecked((int)stack0);

	[Fact]
	public static void Test_ExtendedUint_OnStack_RiscV()
	{
		const uint arg = 0xB1ED0C1Eu;
		const long ret = unchecked((int)arg);
		long managed = Echo_ExtendedUint_OnStack_RiscV_Managed(0, 0, 0, 0, 0, 0, 0, 0, arg);
		long native = Echo_ExtendedUint_OnStack_RiscV(0, 0, 0, 0, 0, 0, 0, 0, arg);

		Assert.Equal(ret, managed);
		Assert.Equal(ret, native);
	}

	[Fact]
	public static void Test_ExtendedUint_OnStack_ByReflection_RiscV()
	{
		const uint arg = 0xB1ED0C1Eu;
		const long ret = unchecked((int)arg);
		long managed = (long)typeof(Program).GetMethod("Echo_ExtendedUint_OnStack_RiscV_Managed").Invoke(
			null, new object[] {0, 0, 0, 0, 0, 0, 0, 0, arg});
		long native = (long)typeof(Program).GetMethod("Echo_ExtendedUint_OnStack_RiscV").Invoke(
			null, new object[] {0, 0, 0, 0, 0, 0, 0, 0, arg});

		Assert.Equal(ret, managed);
		Assert.Equal(ret, native);
	}
#endregion

#region Float_RiscVTests
	[DllImport("PrimitiveABI")]
	public static extern double Echo_Float_RiscV(float fa0, float fa1);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static double Echo_Float_RiscV_Managed(float fa0, float fa1) => fa1;

	[Fact]
	public static void Test_Float_RiscV()
	{
		const float arg = 3.14159f;
		const double ret = 3.14159f;
		double managed = Echo_Float_RiscV_Managed(0f, arg);
		double native = Echo_Float_RiscV(0f, arg);

		Assert.Equal(ret, managed);
		Assert.Equal(ret, native);
	}

	[Fact]
	public static void Test_Float_ByReflection_RiscV()
	{
		const float arg = 3.14159f;
		const double ret = 3.14159f;
		double managed = (double)typeof(Program).GetMethod("Echo_Float_RiscV_Managed").Invoke(
			null, new object[] {0f, arg});
		double native = (double)typeof(Program).GetMethod("Echo_Float_RiscV").Invoke(
			null, new object[] {0f, arg});

		Assert.Equal(ret, managed);
		Assert.Equal(ret, native);
	}

	[DllImport("PrimitiveABI")]
	public static extern double Echo_Float_InIntegerReg_RiscV(
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, float a0);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static double Echo_Float_InIntegerReg_RiscV_Managed(
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, float a0) => a0;

	[Fact]
	public static void Test_Float_InIntegerReg_RiscV()
	{
		const float arg = 3.14159f;
		const double ret = 3.14159f;
		double managed = Echo_Float_InIntegerReg_RiscV_Managed(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, arg);
		double native = Echo_Float_InIntegerReg_RiscV(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, arg);

		Assert.Equal(ret, managed);
		Assert.Equal(ret, native);
	}

	[Fact]
	public static void Test_Float_InIntegerReg_ByReflection_RiscV()
	{
		const float arg = 3.14159f;
		const double ret = 3.14159f;
		double managed = (double)typeof(Program).GetMethod("Echo_Float_InIntegerReg_RiscV_Managed").Invoke(
			null, new object[] {0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, arg});
		double native = (double)typeof(Program).GetMethod("Echo_Float_InIntegerReg_RiscV").Invoke(
			null, new object[] {0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, arg});

		Assert.Equal(ret, managed);
		Assert.Equal(ret, native);
	}

	[DllImport("PrimitiveABI")]
	public static extern double Echo_Float_OnStack_RiscV(
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, float stack0);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static double Echo_Float_OnStack_RiscV_Managed(
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, float stack0) => stack0;

	[Fact]
	public static void Test_Float_OnStack_RiscV()
	{
		const float arg = 3.14159f;
		const double ret = 3.14159f;
		double managed = Echo_Float_OnStack_RiscV_Managed(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0, 0, 0, 0, 0, 0, 0, 0, arg);
		double native = Echo_Float_OnStack_RiscV(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0, 0, 0, 0, 0, 0, 0, 0, arg);

		Assert.Equal(ret, managed);
		Assert.Equal(ret, native);
	}

	[Fact]
	public static void Test_Float_OnStack_ByReflection_RiscV()
	{
		const float arg = 3.14159f;
		const double ret = 3.14159f;
		double managed = (double)typeof(Program).GetMethod("Echo_Float_OnStack_RiscV_Managed").Invoke(
			null, new object[] {0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0, 0, 0, 0, 0, 0, 0, 0, arg});
		double native = (double)typeof(Program).GetMethod("Echo_Float_OnStack_RiscV").Invoke(
			null, new object[] {0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0, 0, 0, 0, 0, 0, 0, 0, arg});

		Assert.Equal(ret, managed);
		Assert.Equal(ret, native);
	}
#endregion
}