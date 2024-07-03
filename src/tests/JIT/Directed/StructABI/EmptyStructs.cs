// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public static class Program
{
	public static bool IsSystemV =>
		(RuntimeInformation.ProcessArchitecture == Architecture.X64) &&
		!RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
	public static bool IsRiscV64OrArm =>
		RuntimeInformation.ProcessArchitecture is Architecture.RiscV64 or Architecture.Arm;
	public static bool IsArm64 => RuntimeInformation.ProcessArchitecture is Architecture.Arm64;
	public static bool IsArm => RuntimeInformation.ProcessArchitecture is Architecture.Arm;
	public const string SystemVPassNoClassEightbytes = "https://github.com/dotnet/runtime/issues/104098";
	public const string ClangEmptyStructsIgnoredWhenPassing = "https://github.com/llvm/llvm-project/issues/97285";
	public const string ProblemsWithEmptyStructPassing = "https://github.com/dotnet/runtime/issues/104369";

	public struct Empty
	{
	}

#region Empty_SanityTests
	[DllImport("EmptyStructsLib")]
	public static extern int Echo_Empty_Sanity(int i0, float f0, Empty e, int i1, float f1);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int Echo_Empty_Sanity_Managed(int i0, float f0, Empty e, int i1, float f1)
	{
		return i0 + (int)f0 + i1 + (int)f1;
	}

	[Fact]
	[ActiveIssue(ClangEmptyStructsIgnoredWhenPassing, typeof(Program), nameof(IsRiscV64OrArm))]
	[ActiveIssue(SystemVPassNoClassEightbytes, typeof(Program), nameof(IsSystemV))]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64))]
	public static void Test_Empty_Sanity()
	{
		Empty empty = new Empty{};
		int native = Echo_Empty_Sanity(-2, 3f, empty, -3, 2f);
		int managed = Echo_Empty_Sanity_Managed(-2, 3f, empty, -3, 2f);

		Assert.Equal(0, native);
		Assert.Equal(0, managed);
	}
#endregion

#region IntEmpty_SystemVTests
	public struct IntEmpty
	{
		public int Int0;
		public Empty Empty0;

		public static IntEmpty Get()
			=> new IntEmpty { Int0 = 0xBabc1a };

		public bool Equals(IntEmpty other)
			=> Int0 == other.Int0;
		
		public override string ToString()
			=> $"{{Int0:{Int0:x}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern IntEmpty Echo_IntEmpty_SysV(int i0, float f0, IntEmpty val, int i1, float f1);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static IntEmpty Echo_IntEmpty_SysV_Managed(int i0, float f0, IntEmpty val, int i1, float f1)
	{
		val.Int0 += i1 + (int)f1;
		return val;
	}

	[Fact]
	public static void Test_IntEmpty_SysV()
	{
		IntEmpty expected = IntEmpty.Get();
		IntEmpty native = Echo_IntEmpty_SysV(0, 0f, expected, 1, -1f);
		IntEmpty managed = Echo_IntEmpty_SysV_Managed(0, 0f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region IntEmptyPair_SystemVTests
	public struct IntEmptyPair
	{
		public IntEmpty IntEmpty0;
		public IntEmpty IntEmpty1;

		public static IntEmptyPair Get()
			=> new IntEmptyPair { IntEmpty0 = IntEmpty.Get(), IntEmpty1 = IntEmpty.Get() };

		public bool Equals(IntEmptyPair other)
			=> IntEmpty0.Equals(other.IntEmpty0) && IntEmpty1.Equals(other.IntEmpty1);
		
		public override string ToString()
			=> $"{{IntEmpty0:{IntEmpty0}, IntEmpty1:{IntEmpty1}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern IntEmptyPair Echo_IntEmptyPair_SysV(int i0, float f0, IntEmptyPair val, int i1, float f1);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static IntEmptyPair Echo_IntEmptyPair_SysV_Managed(int i0, float f0, IntEmptyPair val, int i1, float f1)
	{
		val.IntEmpty0.Int0 += i1 + (int)f1;
		return val;
	}

	[Fact]
	public static void Test_IntEmptyPair_SysV()
	{
		IntEmptyPair expected = IntEmptyPair.Get();
		IntEmptyPair native = Echo_IntEmptyPair_SysV(0, 0f, expected, 1, -1f);
		IntEmptyPair managed = Echo_IntEmptyPair_SysV_Managed(0, 0f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region EmptyFloatIntInt_SystemVTests
	public struct EmptyFloatIntInt
	{
		public Empty Empty0;
		public float Float0;
		public int Int0;
		public int Int1;

		public static EmptyFloatIntInt Get()
			=> new EmptyFloatIntInt { Float0 = 2.71828f, Int0 = 0xBabc1a, Int1 = 0xC10c1a };

		public bool Equals(EmptyFloatIntInt other)
			=> Float0 == other.Float0 && Int0 == other.Int0 && Int1 == other.Int1;

		public override string ToString()
			=> $"{{Float0:{Float0}, Int0:{Int0:x}, Int1:{Int1:x}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern EmptyFloatIntInt Echo_EmptyFloatIntInt_SysV(
		int i0, float f0, EmptyFloatIntInt val, int i1, float f1);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static EmptyFloatIntInt Echo_EmptyFloatIntInt_SysV_Managed(
		int i0, float f0, EmptyFloatIntInt val, int i1, float f1)
	{
		val.Float0 += (float)i1 + f1;
		return val;
	}

	[Fact]
	public static void Test_EmptyFloatIntInt_SysV()
	{
		EmptyFloatIntInt expected = EmptyFloatIntInt.Get();
		EmptyFloatIntInt native = Echo_EmptyFloatIntInt_SysV(0, 0f, expected, 1, -1f);
		EmptyFloatIntInt managed = Echo_EmptyFloatIntInt_SysV_Managed(0, 0f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region FloatFloatEmptyFloat_SystemVTests
	public struct FloatFloatEmptyFloat
	{
		public float Float0;
		public float Float1;
		public Empty Empty0;
		public float Float2;

		public static FloatFloatEmptyFloat Get()
			=> new FloatFloatEmptyFloat { Float0 = 2.71828f, Float1 = 3.14159f, Float2 = 1.61803f };

		public bool Equals(FloatFloatEmptyFloat other)
			=> Float0 == other.Float0 && Float1 == other.Float1 && Float2 == other.Float2;

		public override string ToString()
			=> $"{{Float0:{Float0}, Float1:{Float1}, Float2:{Float2}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern FloatFloatEmptyFloat Echo_FloatFloatEmptyFloat_SysV(
		int i0, float f0, FloatFloatEmptyFloat val, int i1, float f1);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static FloatFloatEmptyFloat Echo_FloatFloatEmptyFloat_SysV_Managed(
		int i0, float f0, FloatFloatEmptyFloat val, int i1, float f1)
	{
		val.Float2 += (float)i1 + f1;
		return val;
	}

	[Fact]
	public static void Test_FloatFloatEmptyFloat_SysV()
	{
		FloatFloatEmptyFloat expected = FloatFloatEmptyFloat.Get();
		FloatFloatEmptyFloat native = Echo_FloatFloatEmptyFloat_SysV(0, 0f, expected, 1, -1f);
		FloatFloatEmptyFloat managed = Echo_FloatFloatEmptyFloat_SysV_Managed(0, 0f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

	public struct Eight<T>
	{
		public T E0, E1, E2, E3, E4, E5, E6, E7;
	}

#region Empty8Float_RiscVTests
	public struct Empty8Float
	{
		public Eight<Empty> EightEmpty0;
		public float Float0;

		public static Empty8Float Get()
			=> new Empty8Float { Float0 = 2.71828f };

		public bool Equals(Empty8Float other)
			=> Float0 == other.Float0;

		public override string ToString()
			=> $"{{Float0:{Float0}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern Empty8Float Echo_Empty8Float_RiscV(
		int a0, float fa0, Empty8Float fa1, int a1, float fa2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static Empty8Float Echo_Empty8Float_RiscV_Managed(
		int a0, float fa0, Empty8Float fa1, int a1, float fa2)
	{
		fa1.Float0 += (float)a1 + fa2;
		return fa1;
	}

	[Fact, ActiveIssue(SystemVPassNoClassEightbytes, typeof(Program), nameof(IsSystemV))]
	public static void Test_Empty8Float_RiscV()
	{
		Empty8Float expected = Empty8Float.Get();
		Empty8Float native = Echo_Empty8Float_RiscV(0, 0f, expected, -1, 1f);
		Empty8Float managed = Echo_Empty8Float_RiscV_Managed(0, 0f, expected, -1, 1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern Empty8Float Echo_Empty8Float_InIntegerRegs_RiscV(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		Empty8Float a1_a2, int a3, float a4);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static Empty8Float Echo_Empty8Float_InIntegerRegs_RiscV_Managed(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		Empty8Float a1_a2, int a3, float a4)
	{
		a1_a2.Float0 += (float)a3 + a4;
		return a1_a2;
	}

	[Fact, ActiveIssue(SystemVPassNoClassEightbytes, typeof(Program), nameof(IsSystemV))]
	public static void Test_Empty8Float_InIntegerRegs_RiscV()
	{
		Empty8Float expected = Empty8Float.Get();
		Empty8Float native = Echo_Empty8Float_InIntegerRegs_RiscV(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		Empty8Float managed = Echo_Empty8Float_InIntegerRegs_RiscV_Managed(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern Empty8Float Echo_Empty8Float_Split_RiscV(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		Empty8Float a7_stack0, int stack1, float stack2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static Empty8Float Echo_Empty8Float_Split_RiscV_Managed(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		Empty8Float a7_stack0, int stack1, float stack2)
	{
		a7_stack0.Float0 += (float)stack1 + stack2;
		return a7_stack0;
	}

	[Fact, ActiveIssue(SystemVPassNoClassEightbytes, typeof(Program), nameof(IsSystemV))]
	public static void Test_Empty8Float_Split_RiscV()
	{
		Empty8Float expected = Empty8Float.Get();
		Empty8Float native = Echo_Empty8Float_Split_RiscV(
			0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		Empty8Float managed = Echo_Empty8Float_Split_RiscV_Managed(
			0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern Empty8Float Echo_Empty8Float_OnStack_RiscV(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		Empty8Float stack0_stack1, int stack2, float stack3);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static Empty8Float Echo_Empty8Float_OnStack_RiscV_Managed(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		Empty8Float stack0_stack1, int stack2, float stack3)
	{
		stack0_stack1.Float0 += (float)stack2 + stack3;
		return stack0_stack1;
	}

	[Fact, ActiveIssue(SystemVPassNoClassEightbytes, typeof(Program), nameof(IsSystemV))]
	public static void Test_Empty8Float_OnStack_RiscV()
	{
		Empty8Float expected = Empty8Float.Get();
		Empty8Float native = Echo_Empty8Float_OnStack_RiscV(
			0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		Empty8Float managed = Echo_Empty8Float_OnStack_RiscV_Managed(
			0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region FloatEmpty8Float_RiscVTests
	public struct FloatEmpty8Float
	{
		public float Float0;
		public Eight<Empty> EightEmpty0;
		public float Float1;

		public static FloatEmpty8Float Get()
			=> new FloatEmpty8Float { Float0 = 2.71828f, Float1 = 3.14159f };

		public bool Equals(FloatEmpty8Float other)
			=> Float0 == other.Float0 && Float1 == other.Float1;

		public override string ToString()
			=> $"{{Float0:{Float0}, Float1:{Float1}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern FloatEmpty8Float Echo_FloatEmpty8Float_RiscV(
		int a0, float fa0, FloatEmpty8Float fa1_fa2, int a1, float fa3);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static FloatEmpty8Float Echo_FloatEmpty8Float_RiscV_Managed(
		int a0, float fa0, FloatEmpty8Float fa1_fa2, int a1, float fa3)
	{
		fa1_fa2.Float1 += (float)a1 + fa3;
		return fa1_fa2;
	}

	[Fact]
	public static void Test_FloatEmpty8Float_RiscV()
	{
		FloatEmpty8Float expected = FloatEmpty8Float.Get();
		FloatEmpty8Float native = Echo_FloatEmpty8Float_RiscV(0, 0f, expected, -1, 1f);
		FloatEmpty8Float managed = Echo_FloatEmpty8Float_RiscV_Managed(0, 0f, expected, -1, 1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern FloatEmpty8Float Echo_FloatEmpty8Float_InIntegerRegs_RiscV(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		FloatEmpty8Float a1_a2, int a3, float a4);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static FloatEmpty8Float Echo_FloatEmpty8Float_InIntegerRegs_RiscV_Managed(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		FloatEmpty8Float a1_a2, int a3, float a4)
	{
		a1_a2.Float0 += (float)a3 + a4;
		return a1_a2;
	}

	[Fact]
	public static void Test_FloatEmpty8Float_InIntegerRegs_RiscV()
	{
		FloatEmpty8Float expected = FloatEmpty8Float.Get();
		FloatEmpty8Float native = Echo_FloatEmpty8Float_InIntegerRegs_RiscV(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		FloatEmpty8Float managed = Echo_FloatEmpty8Float_InIntegerRegs_RiscV_Managed(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern FloatEmpty8Float Echo_FloatEmpty8Float_Split_RiscV(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		FloatEmpty8Float a7_stack0, int stack1, float stack2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static FloatEmpty8Float Echo_FloatEmpty8Float_Split_RiscV_Managed(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		FloatEmpty8Float a7_stack0, int stack1, float stack2)
	{
		a7_stack0.Float0 += (float)stack1 + stack2;
		return a7_stack0;
	}

	[Fact]
	public static void Test_FloatEmpty8Float_Split_RiscV()
	{
		FloatEmpty8Float expected = FloatEmpty8Float.Get();
		FloatEmpty8Float native = Echo_FloatEmpty8Float_Split_RiscV(
			0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, -2, 2f);
		FloatEmpty8Float managed = Echo_FloatEmpty8Float_Split_RiscV_Managed(
			0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, -2, 2f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern FloatEmpty8Float Echo_FloatEmpty8Float_OnStack_RiscV(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		FloatEmpty8Float stack0_stack1, int stack2, float stack3);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static FloatEmpty8Float Echo_FloatEmpty8Float_OnStack_RiscV_Managed(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		FloatEmpty8Float stack0_stack1, int stack2, float stack3)
	{
		stack0_stack1.Float1 += (float)stack2 + stack3;
		return stack0_stack1;
	}

	[Fact]
	public static void Test_FloatEmpty8Float_OnStack_RiscV()
	{
		FloatEmpty8Float expected = FloatEmpty8Float.Get();
		FloatEmpty8Float native = Echo_FloatEmpty8Float_OnStack_RiscV(
			0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 3, -3f);
		FloatEmpty8Float managed = Echo_FloatEmpty8Float_OnStack_RiscV_Managed(
			0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 3, -3f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region FloatEmptyShort_RiscVTests
	public struct FloatEmptyShort
	{
		public float Float0;
		public Empty Empty0;
		public short Short0;

		public static FloatEmptyShort Get()
			=> new FloatEmptyShort { Float0 = 2.71828f, Short0 = 0x1dea };

		public bool Equals(FloatEmptyShort other)
			=> Float0 == other.Float0 && Short0 == other.Short0;

		public override string ToString()
			=> $"{{Float0:{Float0}, Short0:{Short0}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern FloatEmptyShort Echo_FloatEmptyShort_RiscV(
		int a0, float fa0, FloatEmptyShort fa1_a1, int a1, float fa2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static FloatEmptyShort Echo_FloatEmptyShort_RiscV_Managed(
		int a0, float fa0, FloatEmptyShort fa1_a1, int a1, float fa2)
	{
		fa1_a1.Short0 += (short)(a1 + (int)fa2);
		return fa1_a1;
	}

	[Fact]
	public static void Test_FloatEmptyShort_RiscV()
	{
		FloatEmptyShort expected = FloatEmptyShort.Get();
		FloatEmptyShort native = Echo_FloatEmptyShort_RiscV(0, 0f, expected, -1, 1f);
		FloatEmptyShort managed = Echo_FloatEmptyShort_RiscV_Managed(0, 0f, expected, -1, 1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern FloatEmptyShort Echo_FloatEmptyShort_InIntegerRegs_RiscV(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		FloatEmptyShort a1_a2, int a3, float a4);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static FloatEmptyShort Echo_FloatEmptyShort_InIntegerRegs_RiscV_Managed(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		FloatEmptyShort a1_a2, int a3, float a4)
	{
		a1_a2.Short0 += (short)(a3 + (int)a4);
		return a1_a2;
	}

	[Fact]
	public static void Test_FloatEmptyShort_InIntegerRegs_RiscV()
	{
		FloatEmptyShort expected = FloatEmptyShort.Get();
		FloatEmptyShort native = Echo_FloatEmptyShort_InIntegerRegs_RiscV(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		FloatEmptyShort managed = Echo_FloatEmptyShort_InIntegerRegs_RiscV_Managed(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern FloatEmptyShort Echo_FloatEmptyShort_OnStack_RiscV(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		FloatEmptyShort stack0, int stack1, float stack2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static FloatEmptyShort Echo_FloatEmptyShort_OnStack_RiscV_Managed(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		FloatEmptyShort stack0, int stack1, float stack2)
	{
		stack0.Short0 += (short)(stack1 + (int)stack2);
		return stack0;
	}

	[Fact]
	public static void Test_FloatEmptyShort_OnStack_RiscV()
	{
		FloatEmptyShort expected = FloatEmptyShort.Get();
		FloatEmptyShort native = Echo_FloatEmptyShort_OnStack_RiscV(
			0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 2, -2f);
		FloatEmptyShort managed = Echo_FloatEmptyShort_OnStack_RiscV_Managed(
			0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 2, -2f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region EmptyFloatEmpty5Sbyte_RiscVTests
	public struct EmptyFloatEmpty5Sbyte
	{
		public Empty Empty0;
		public float Float0;
		public Empty Empty1, Empty2, Empty3, Empty4, Empty5;
		public sbyte Sbyte0;

		public static EmptyFloatEmpty5Sbyte Get()
			=> new EmptyFloatEmpty5Sbyte { Float0 = 2.71828f, Sbyte0 = -123 };

		public bool Equals(EmptyFloatEmpty5Sbyte other)
			=> Float0 == other.Float0 && Sbyte0 == other.Sbyte0;

		public override string ToString()
			=> $"{{Float0:{Float0}, Sbyte0:{Sbyte0}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern EmptyFloatEmpty5Sbyte Echo_EmptyFloatEmpty5Sbyte_RiscV(int a0, float fa0,
		EmptyFloatEmpty5Sbyte fa1_a1, int a2, float fa2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static EmptyFloatEmpty5Sbyte Echo_EmptyFloatEmpty5Sbyte_RiscV_Managed(int a0, float fa0,
		EmptyFloatEmpty5Sbyte fa1_a1, int a2, float fa2)
	{
		fa1_a1.Float0 += (float)a2 + fa2;
		return fa1_a1;
	}

	[Fact]
	public static void Test_EmptyFloatEmpty5Sbyte_RiscV()
	{
		EmptyFloatEmpty5Sbyte expected = EmptyFloatEmpty5Sbyte.Get();
		EmptyFloatEmpty5Sbyte native = Echo_EmptyFloatEmpty5Sbyte_RiscV(0, 0f, expected, -1, 1f);
		EmptyFloatEmpty5Sbyte managed = Echo_EmptyFloatEmpty5Sbyte_RiscV_Managed(0, 0f, expected, -1, 1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region EmptyFloatEmpty5Byte_RiscVTests
	public struct EmptyFloatEmpty5Byte
	{
		public Empty Empty0;
		public float Float0;
		public Empty Empty1, Empty2, Empty3, Empty4, Empty5;
		public byte Byte0;

		public static EmptyFloatEmpty5Byte Get()
			=> new EmptyFloatEmpty5Byte { Float0 = 2.71828f, Byte0 = 123 };

		public bool Equals(EmptyFloatEmpty5Byte other)
			=> Float0 == other.Float0 && Byte0 == other.Byte0;

		public override string ToString()
			=> $"{{Float0:{Float0}, Byte0:{Byte0}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern EmptyFloatEmpty5Byte Echo_EmptyFloatEmpty5Byte_RiscV(int a0, float fa0,
		EmptyFloatEmpty5Byte fa1_a1, int a2, float fa2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static EmptyFloatEmpty5Byte Echo_EmptyFloatEmpty5Byte_RiscV_Managed(int a0, float fa0,
		EmptyFloatEmpty5Byte fa1_a1, int a2, float fa2)
	{
		fa1_a1.Float0 += (float)a2 + fa2;
		return fa1_a1;
	}

	[Fact]
	public static void Test_EmptyFloatEmpty5Byte_RiscV()
	{
		EmptyFloatEmpty5Byte expected = EmptyFloatEmpty5Byte.Get();
		EmptyFloatEmpty5Byte native = Echo_EmptyFloatEmpty5Byte_RiscV(0, 0f, expected, -1, 1f);
		EmptyFloatEmpty5Byte managed = Echo_EmptyFloatEmpty5Byte_RiscV_Managed(0, 0f, expected, -1, 1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern EmptyFloatEmpty5Byte Echo_EmptyFloatEmpty5Byte_InIntegerRegs_RiscV(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		EmptyFloatEmpty5Byte a1_a2, int a3, float a4);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static EmptyFloatEmpty5Byte Echo_EmptyFloatEmpty5Byte_InIntegerRegs_RiscV_Managed(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		EmptyFloatEmpty5Byte a1_a2, int a3, float a4)
	{
		a1_a2.Float0 += (float)a3 + a4;
		return a1_a2;
	}

	[Fact]
	public static void Test_EmptyFloatEmpty5Byte_InIntegerRegs_RiscV()
	{
		EmptyFloatEmpty5Byte expected = EmptyFloatEmpty5Byte.Get();
		EmptyFloatEmpty5Byte native = Echo_EmptyFloatEmpty5Byte_InIntegerRegs_RiscV(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		EmptyFloatEmpty5Byte managed = Echo_EmptyFloatEmpty5Byte_InIntegerRegs_RiscV_Managed(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern EmptyFloatEmpty5Byte Echo_EmptyFloatEmpty5Byte_Split_RiscV(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		EmptyFloatEmpty5Byte a7_stack0, int stack1, float stack2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static EmptyFloatEmpty5Byte Echo_EmptyFloatEmpty5Byte_Split_RiscV_Managed(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		EmptyFloatEmpty5Byte a7_stack0, int stack1, float stack2)
	{
		a7_stack0.Float0 += (float)stack1 + stack2;
		return a7_stack0;
	}

	[Fact]
	public static void Test_EmptyFloatEmpty5Byte_Split_RiscV()
	{
		EmptyFloatEmpty5Byte expected = EmptyFloatEmpty5Byte.Get();
		EmptyFloatEmpty5Byte native = Echo_EmptyFloatEmpty5Byte_Split_RiscV(
			0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		EmptyFloatEmpty5Byte managed = Echo_EmptyFloatEmpty5Byte_Split_RiscV_Managed(
			0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern EmptyFloatEmpty5Byte Echo_EmptyFloatEmpty5Byte_OnStack_RiscV(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		EmptyFloatEmpty5Byte stack0_stack1, int stack2, float stack3);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static EmptyFloatEmpty5Byte Echo_EmptyFloatEmpty5Byte_OnStack_RiscV_Managed(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		EmptyFloatEmpty5Byte stack0_stack1, int stack2, float stack3)
	{
		stack0_stack1.Float0 += (float)stack2 + stack3;
		return stack0_stack1;
	}

	[Fact]
	public static void Test_EmptyFloatEmpty5Byte_OnStack_RiscV()
	{
		EmptyFloatEmpty5Byte expected = EmptyFloatEmpty5Byte.Get();
		EmptyFloatEmpty5Byte native = Echo_EmptyFloatEmpty5Byte_OnStack_RiscV(
			0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		EmptyFloatEmpty5Byte managed = Echo_EmptyFloatEmpty5Byte_OnStack_RiscV_Managed(
			0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region NestedEmpty_RiscVTests
	public struct NestedEmpty
	{
		public struct InnerEmpty
		{
			public Empty Empty0;
		}
		public InnerEmpty InnerEmpty0;
	}

	public struct DoubleFloatNestedEmpty
	{
		public double Double0;
		public float Float0;
		public NestedEmpty NestedEmpty0;

		public static DoubleFloatNestedEmpty Get()
			=> new DoubleFloatNestedEmpty { Double0 = 2.71828, Float0 = 3.14159f };

		public bool Equals(DoubleFloatNestedEmpty other)
			=> Double0 == other.Double0 && Float0 == other.Float0;

		public override string ToString()
			=> $"{{Double0:{Double0}, Float0:{Float0}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern DoubleFloatNestedEmpty Echo_DoubleFloatNestedEmpty_RiscV(int a0, float fa0,
		DoubleFloatNestedEmpty fa1_fa2, int a1, float fa3);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static DoubleFloatNestedEmpty Echo_DoubleFloatNestedEmpty_RiscV_Managed(int a0, float fa0,
		DoubleFloatNestedEmpty fa1_fa2, int a1, float fa3)
	{
		fa1_fa2.Float0 += (float)a1 + fa3;
		return fa1_fa2;
	}

	[Fact]
	public static void Test_DoubleFloatNestedEmpty_RiscV()
	{
		DoubleFloatNestedEmpty expected = DoubleFloatNestedEmpty.Get();
		DoubleFloatNestedEmpty native = Echo_DoubleFloatNestedEmpty_RiscV(0, 0f, expected, 1, -1f);
		DoubleFloatNestedEmpty managed = Echo_DoubleFloatNestedEmpty_RiscV_Managed(0, 0f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern DoubleFloatNestedEmpty Echo_DoubleFloatNestedEmpty_InIntegerRegs_RiscV(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6,
		DoubleFloatNestedEmpty a1_a2, int a3, float a4);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static DoubleFloatNestedEmpty Echo_DoubleFloatNestedEmpty_InIntegerRegs_RiscV_Managed(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6,
		DoubleFloatNestedEmpty a1_a2, int a3, float fa7)
	{
		a1_a2.Float0 += (float)a3 + fa7;
		return a1_a2;
	}

	[Fact]
	public static void Test_DoubleFloatNestedEmpty_InIntegerRegs_RiscV()
	{
		DoubleFloatNestedEmpty expected = DoubleFloatNestedEmpty.Get();
		DoubleFloatNestedEmpty native = Echo_DoubleFloatNestedEmpty_InIntegerRegs_RiscV(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, expected, 1, -1f);
		DoubleFloatNestedEmpty managed = Echo_DoubleFloatNestedEmpty_InIntegerRegs_RiscV_Managed(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region EmptyUshortAndDouble_RiscVTests
	public struct EmptyUshortAndDouble
	{
		public struct EmptyUshort
		{
			public Empty Empty0;
			public ushort Ushort0;
		};
		public EmptyUshort EmptyUshort0;
		public double Double0;

		public static EmptyUshortAndDouble Get() => new EmptyUshortAndDouble {
			EmptyUshort0 = new EmptyUshort { Ushort0 = 0xBaca }, Double0 = 2.71828
		};

		public bool Equals(EmptyUshortAndDouble other)
			=> EmptyUshort0.Ushort0 == other.EmptyUshort0.Ushort0 && Double0 == other.Double0;

		public override string ToString()
			=> $"{{EmptyUshort0.Ushort0:{EmptyUshort0.Ushort0}, Double0:{Double0}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern EmptyUshortAndDouble Echo_EmptyUshortAndDouble_RiscV(int a0, float fa0,
		EmptyUshortAndDouble a1_fa1, int a2, double fa2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static EmptyUshortAndDouble Echo_EmptyUshortAndDouble_RiscV_Managed(int a0, float fa0,
		EmptyUshortAndDouble a1_fa1, int a2, double fa2)
	{
		a1_fa1.Double0 += (double)a2 + fa2;
		return a1_fa1;
	}

	[Fact]
	public static void Test_EmptyUshortAndDouble_RiscV()
	{
		EmptyUshortAndDouble expected = EmptyUshortAndDouble.Get();
		EmptyUshortAndDouble native = Echo_EmptyUshortAndDouble_RiscV(0, 0f, expected, -1, 1f);
		EmptyUshortAndDouble managed = Echo_EmptyUshortAndDouble_RiscV_Managed(0, 0f, expected, -1, 1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region PackedEmptyFloatLong_RiscVTests
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	public struct PackedEmptyFloatLong
	{
		public Empty Empty0;
		public float Float0;
		public long Long0;

		public static PackedEmptyFloatLong Get()
			=> new PackedEmptyFloatLong { Float0 = 2.71828f, Long0 = 0xDadAddedC0ffee };

		public bool Equals(PackedEmptyFloatLong other)
			=> Float0 == other.Float0 && Long0 == other.Long0;

		public override string ToString()
			=> $"{{Float0:{Float0}, Long0:{Long0}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern PackedEmptyFloatLong Echo_PackedEmptyFloatLong_RiscV(int a0, float fa0,
		PackedEmptyFloatLong fa1_a1, int a2, float fa2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static PackedEmptyFloatLong Echo_PackedEmptyFloatLong_RiscV_Managed(int a0, float fa0,
		PackedEmptyFloatLong fa1_a1, int a2, float fa2)
	{
		fa1_a1.Float0 += (float)a2 + fa2;
		return fa1_a1;
	}

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm))]
	public static void Test_PackedEmptyFloatLong_RiscV()
	{
		PackedEmptyFloatLong expected = PackedEmptyFloatLong.Get();
		PackedEmptyFloatLong native = Echo_PackedEmptyFloatLong_RiscV(0, 0f, expected, 1, -1f);
		PackedEmptyFloatLong managed = Echo_PackedEmptyFloatLong_RiscV_Managed(0, 0f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern PackedEmptyFloatLong Echo_PackedEmptyFloatLong_InIntegerRegs_RiscV(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		PackedEmptyFloatLong a1_a2, int a3, float a4);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static PackedEmptyFloatLong Echo_PackedEmptyFloatLong_InIntegerRegs_RiscV_Managed(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		PackedEmptyFloatLong a1_a2, int a3, float a4)
	{
		a1_a2.Float0 += (float)a3 + a4;
		return a1_a2;
	}

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm))]
	public static void Test_PackedEmptyFloatLong_InIntegerRegs_RiscV()
	{
		PackedEmptyFloatLong expected = PackedEmptyFloatLong.Get();
		PackedEmptyFloatLong native = Echo_PackedEmptyFloatLong_InIntegerRegs_RiscV(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		PackedEmptyFloatLong managed = Echo_PackedEmptyFloatLong_InIntegerRegs_RiscV_Managed(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern PackedEmptyFloatLong Echo_PackedEmptyFloatLong_Split_RiscV(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		PackedEmptyFloatLong a7_stack0, int stack1, float stack2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static PackedEmptyFloatLong Echo_PackedEmptyFloatLong_Split_RiscV_Managed(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		PackedEmptyFloatLong a7_stack0, int stack1, float stack2)
	{
		a7_stack0.Float0 += (float)stack1 + stack2;
		return a7_stack0;
	}

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm))]
	public static void Test_PackedEmptyFloatLong_Split_RiscV()
	{
		PackedEmptyFloatLong expected = PackedEmptyFloatLong.Get();
		PackedEmptyFloatLong native = Echo_PackedEmptyFloatLong_Split_RiscV(
			0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		PackedEmptyFloatLong managed = Echo_PackedEmptyFloatLong_Split_RiscV_Managed(
			0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern PackedEmptyFloatLong Echo_PackedEmptyFloatLong_OnStack_RiscV(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		PackedEmptyFloatLong stack0_stack1, int stack2, float stack3);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static PackedEmptyFloatLong Echo_PackedEmptyFloatLong_OnStack_RiscV_Managed(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		PackedEmptyFloatLong stack0_stack1, int stack2, float stack3)
	{
		stack0_stack1.Float0 += (float)stack2 + stack3;
		return stack0_stack1;
	}

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm))]
	public static void Test_PackedEmptyFloatLong_OnStack_RiscV()
	{
		PackedEmptyFloatLong expected = PackedEmptyFloatLong.Get();
		PackedEmptyFloatLong native = Echo_PackedEmptyFloatLong_OnStack_RiscV(
			0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		PackedEmptyFloatLong managed = Echo_PackedEmptyFloatLong_OnStack_RiscV_Managed(
			0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion
}