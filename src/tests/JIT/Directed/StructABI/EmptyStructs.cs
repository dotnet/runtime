// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Note: this test checks passing empty struct fields in .NET; confronting it against C++ on native compilers is just
// a means to assert compliance to the platform calling convention. The native part is using C++ because it defines
// empty structs as 1 byte like in .NET. Empty structs in C are undefined (it's a GCC extension to define them as 0
// bytes) and .NET managed/unmanaged interop follows the C ABI, not C++, so signatures with empty struct fields should
// not be used in any real-world interop calls.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public static class Program
{
	public static bool IsSystemV =>
		(RuntimeInformation.ProcessArchitecture == Architecture.X64) &&
		!RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
	public static bool IsRiscV64 => RuntimeInformation.ProcessArchitecture is Architecture.RiscV64;
	public static bool IsArm32 => RuntimeInformation.ProcessArchitecture is Architecture.Arm;
	public static bool IsArm64Or32 =>
		RuntimeInformation.ProcessArchitecture is Architecture.Arm64 or Architecture.Arm;
	public static bool IsArm64 => RuntimeInformation.ProcessArchitecture is Architecture.Arm64;
	public const string SystemVPassNoClassEightbytes = "https://github.com/dotnet/runtime/issues/104098";
	public const string RiscVClangEmptyStructsIgnored = "https://github.com/llvm/llvm-project/issues/97285";
	public const string Arm32ClangEmptyStructsIgnored = "https://github.com/llvm/llvm-project/issues/98159";
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
	[ActiveIssue(RiscVClangEmptyStructsIgnored, typeof(Program), nameof(IsRiscV64))]
	[ActiveIssue(Arm32ClangEmptyStructsIgnored, typeof(Program), nameof(IsArm32))]
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

	[Fact]
	[ActiveIssue(RiscVClangEmptyStructsIgnored, typeof(Program), nameof(IsRiscV64))]
	[ActiveIssue(Arm32ClangEmptyStructsIgnored, typeof(Program), nameof(IsArm32))]
	[ActiveIssue(SystemVPassNoClassEightbytes, typeof(Program), nameof(IsSystemV))]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64))]
	public static void Test_Empty_ByReflection_Sanity()
	{
		Empty empty = new Empty{};
		int native = (int)typeof(Program).GetMethod("Echo_Empty_Sanity").Invoke(
			null, new object[] {-2, 3f, empty, -3, 2f});
		int managed = (int)typeof(Program).GetMethod("Echo_Empty_Sanity_Managed").Invoke(
			null, new object[] {-2, 3f, empty, -3, 2f});

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

		public override bool Equals(object other)
			=> other is IntEmpty o && Int0 == o.Int0;
		
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

	[Fact]
	public static void Test_IntEmpty_ByReflection_SysV()
	{
		var expected = (IntEmpty)typeof(IntEmpty).GetMethod("Get").Invoke(null, new object[] {});
		var native = (IntEmpty)typeof(Program).GetMethod("Echo_IntEmpty_SysV").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});
		var managed = (IntEmpty)typeof(Program).GetMethod("Echo_IntEmpty_SysV_Managed").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});

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

		public override bool Equals(object other)
			=> other is IntEmptyPair o && IntEmpty0.Equals(o.IntEmpty0) && IntEmpty1.Equals(o.IntEmpty1);
		
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

	[Fact]
	public static void Test_IntEmptyPair_ByReflection_SysV()
	{
		var expected = IntEmptyPair.Get();
		var native = (IntEmptyPair)typeof(Program).GetMethod("Echo_IntEmptyPair_SysV").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});
		var managed = (IntEmptyPair)typeof(Program).GetMethod("Echo_IntEmptyPair_SysV_Managed").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});

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

		public override bool Equals(object other)
			=> other is EmptyFloatIntInt o && Float0 == o.Float0 && Int0 == o.Int0 && Int1 == o.Int1;

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

	[Fact]
	public static void Test_EmptyFloatIntInt_ByReflection_SysV()
	{
		var expected = EmptyFloatIntInt.Get();
		var native = (EmptyFloatIntInt)typeof(Program).GetMethod("Echo_EmptyFloatIntInt_SysV").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});
		var managed = (EmptyFloatIntInt)typeof(Program).GetMethod("Echo_EmptyFloatIntInt_SysV_Managed").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});

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

		public override bool Equals(object other)
			=> other is FloatFloatEmptyFloat o && Float0 == o.Float0 && Float1 == o.Float1 && Float2 == o.Float2;

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

	[Fact]
	public static void Test_FloatFloatEmptyFloat_ByReflection_SysV()
	{
		var expected = FloatFloatEmptyFloat.Get();
		var native = (FloatFloatEmptyFloat)typeof(Program).GetMethod("Echo_FloatFloatEmptyFloat_SysV").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});
		var managed = (FloatFloatEmptyFloat)typeof(Program).GetMethod("Echo_FloatFloatEmptyFloat_SysV_Managed").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});

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

		public override bool Equals(object other)
			=> other is Empty8Float o && Float0 == o.Float0;

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

	[Fact, ActiveIssue(SystemVPassNoClassEightbytes, typeof(Program), nameof(IsSystemV))]
	public static void Test_Empty8Float_ByReflection_RiscV()
	{
		var expected = Empty8Float.Get();
		var native = (Empty8Float)typeof(Program).GetMethod("Echo_Empty8Float_RiscV").Invoke(
			null, new object[] {0, 0f, expected, -1, 1f});
		var managed = (Empty8Float)typeof(Program).GetMethod("Echo_Empty8Float_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, expected, -1, 1f});

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

	[Fact, ActiveIssue(SystemVPassNoClassEightbytes, typeof(Program), nameof(IsSystemV))]
	public static void Test_Empty8Float_InIntegerRegs_ByReflection_RiscV()
	{
		var expected = Empty8Float.Get();
		var native = (Empty8Float)typeof(Program).GetMethod("Echo_Empty8Float_InIntegerRegs_RiscV").Invoke(
			null, new object[] {0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});
		var managed = (Empty8Float)typeof(Program).GetMethod("Echo_Empty8Float_InIntegerRegs_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});

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

	[Fact, ActiveIssue(SystemVPassNoClassEightbytes, typeof(Program), nameof(IsSystemV))]
	public static void Test_Empty8Float_Split_ByReflection_RiscV()
	{
		var expected = Empty8Float.Get();
		var native = (Empty8Float)typeof(Program).GetMethod("Echo_Empty8Float_Split_RiscV").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});
		var managed = (Empty8Float)typeof(Program).GetMethod("Echo_Empty8Float_Split_RiscV_Managed").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});

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

	[Fact, ActiveIssue(SystemVPassNoClassEightbytes, typeof(Program), nameof(IsSystemV))]
	public static void Test_Empty8Float_OnStack_ByReflection_RiscV()
	{
		var expected = Empty8Float.Get();
		var native = (Empty8Float)typeof(Program).GetMethod("Echo_Empty8Float_OnStack_RiscV").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});
		var managed = (Empty8Float)typeof(Program).GetMethod("Echo_Empty8Float_OnStack_RiscV_Managed").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});

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

		public override bool Equals(object other)
			=> other is FloatEmpty8Float o && Float0 == o.Float0 && Float1 == o.Float1;

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

	[Fact]
	public static void Test_FloatEmpty8Float_ByReflection_RiscV()
	{
		var expected = FloatEmpty8Float.Get();
		var native = (FloatEmpty8Float)typeof(Program).GetMethod("Echo_FloatEmpty8Float_RiscV").Invoke(
			null, new object[] {0, 0f, expected, -1, 1f});
		var managed = (FloatEmpty8Float)typeof(Program).GetMethod("Echo_FloatEmpty8Float_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, expected, -1, 1f});

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

	[Fact]
	public static void Test_FloatEmpty8Float_InIntegerRegs_ByReflection_RiscV()
	{
		var expected = FloatEmpty8Float.Get();
		var native = (FloatEmpty8Float)typeof(Program).GetMethod("Echo_FloatEmpty8Float_InIntegerRegs_RiscV").Invoke(
			null, new object[] {0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});
		var managed = (FloatEmpty8Float)typeof(Program).GetMethod("Echo_FloatEmpty8Float_InIntegerRegs_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});

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

	[Fact]
	public static void Test_FloatEmpty8Float_Split_ByReflection_RiscV()
	{
		var expected = FloatEmpty8Float.Get();
		var native = (FloatEmpty8Float)typeof(Program).GetMethod("Echo_FloatEmpty8Float_Split_RiscV").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, -2, 2f});
		var managed = (FloatEmpty8Float)typeof(Program).GetMethod("Echo_FloatEmpty8Float_Split_RiscV_Managed").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, -2, 2f});

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

	[Fact]
	public static void Test_FloatEmpty8Float_OnStack_ByReflection_RiscV()
	{
		var expected = FloatEmpty8Float.Get();
		var native = (FloatEmpty8Float)typeof(Program).GetMethod("Echo_FloatEmpty8Float_OnStack_RiscV").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 3, -3f});
		var managed = (FloatEmpty8Float)typeof(Program).GetMethod("Echo_FloatEmpty8Float_OnStack_RiscV_Managed").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 3, -3f});

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

		public override bool Equals(object other)
			=> other is FloatEmptyShort o && Float0 == o.Float0 && Short0 == o.Short0;

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

	[Fact]
	public static void Test_FloatEmptyShort_ByReflection_RiscV()
	{
		var expected = FloatEmptyShort.Get();
		var native = (FloatEmptyShort)typeof(Program).GetMethod("Echo_FloatEmptyShort_RiscV").Invoke(
			null, new object[] {0, 0f, expected, -1, 1f});
		var managed = (FloatEmptyShort)typeof(Program).GetMethod("Echo_FloatEmptyShort_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, expected, -1, 1f});

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

	[Fact]
	public static void Test_FloatEmptyShort_InIntegerRegs_ByReflection_RiscV()
	{
		var expected = FloatEmptyShort.Get();
		var native = (FloatEmptyShort)typeof(Program).GetMethod("Echo_FloatEmptyShort_InIntegerRegs_RiscV").Invoke(
			null, new object[] {0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});
		var managed = (FloatEmptyShort)typeof(Program).GetMethod("Echo_FloatEmptyShort_InIntegerRegs_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});

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

	[Fact]
	public static void Test_FloatEmptyShort_OnStack_ByReflection_RiscV()
	{
		var expected = FloatEmptyShort.Get();
		var native = (FloatEmptyShort)typeof(Program).GetMethod("Echo_FloatEmptyShort_OnStack_RiscV").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 2, -2f});
		var managed = (FloatEmptyShort)typeof(Program).GetMethod("Echo_FloatEmptyShort_OnStack_RiscV_Managed").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 2, -2f});

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

		public override bool Equals(object other)
			=> other is EmptyFloatEmpty5Sbyte o && Float0 == o.Float0 && Sbyte0 == o.Sbyte0;

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

	[Fact]
	public static void Test_EmptyFloatEmpty5Sbyte_ByReflection_RiscV()
	{
		var expected = EmptyFloatEmpty5Sbyte.Get();
		var native = (EmptyFloatEmpty5Sbyte)typeof(Program).GetMethod("Echo_EmptyFloatEmpty5Sbyte_RiscV").Invoke(
			null, new object[] {0, 0f, expected, -1, 1f});
		var managed = (EmptyFloatEmpty5Sbyte)typeof(Program).GetMethod("Echo_EmptyFloatEmpty5Sbyte_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, expected, -1, 1f});

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

		public override bool Equals(object other)
			=> other is EmptyFloatEmpty5Byte o && Float0 == o.Float0 && Byte0 == o.Byte0;

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

	[Fact]
	public static void Test_EmptyFloatEmpty5Byte_ByReflection_RiscV()
	{
		var expected = EmptyFloatEmpty5Byte.Get();
		var native = (EmptyFloatEmpty5Byte)typeof(Program).GetMethod("Echo_EmptyFloatEmpty5Byte_RiscV").Invoke(
			null, new object[] {0, 0f, expected, -1, 1f});
		var managed = (EmptyFloatEmpty5Byte)typeof(Program).GetMethod("Echo_EmptyFloatEmpty5Byte_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, expected, -1, 1f});

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

	[Fact]
	public static void Test_EmptyFloatEmpty5Byte_InIntegerRegs_ByReflection_RiscV()
	{
		var expected = EmptyFloatEmpty5Byte.Get();
		var native = (EmptyFloatEmpty5Byte)typeof(Program).GetMethod("Echo_EmptyFloatEmpty5Byte_InIntegerRegs_RiscV").Invoke(
			null, new object[] {0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});
		var managed = (EmptyFloatEmpty5Byte)typeof(Program).GetMethod("Echo_EmptyFloatEmpty5Byte_InIntegerRegs_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});

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

	[Fact]
	public static void Test_EmptyFloatEmpty5Byte_Split_ByReflection_RiscV()
	{
		var expected = EmptyFloatEmpty5Byte.Get();
		var native = (EmptyFloatEmpty5Byte)typeof(Program).GetMethod("Echo_EmptyFloatEmpty5Byte_Split_RiscV").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});
		var managed = (EmptyFloatEmpty5Byte)typeof(Program).GetMethod("Echo_EmptyFloatEmpty5Byte_Split_RiscV_Managed").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});

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

	[Fact]
	public static void Test_EmptyFloatEmpty5Byte_OnStack_ByReflection_RiscV()
	{
		var expected = EmptyFloatEmpty5Byte.Get();
		var native = (EmptyFloatEmpty5Byte)typeof(Program).GetMethod("Echo_EmptyFloatEmpty5Byte_OnStack_RiscV").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});
		var managed = (EmptyFloatEmpty5Byte)typeof(Program).GetMethod("Echo_EmptyFloatEmpty5Byte_OnStack_RiscV_Managed").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});

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

		public override bool Equals(object other)
			=> other is DoubleFloatNestedEmpty o && Double0 == o.Double0 && Float0 == o.Float0;

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

	[Fact]
	public static void Test_DoubleFloatNestedEmpty_ByReflection_RiscV()
	{
		var expected = DoubleFloatNestedEmpty.Get();
		var native = (DoubleFloatNestedEmpty)typeof(Program).GetMethod("Echo_DoubleFloatNestedEmpty_RiscV").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});
		var managed = (DoubleFloatNestedEmpty)typeof(Program).GetMethod("Echo_DoubleFloatNestedEmpty_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});

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

	[Fact]
	public static void Test_DoubleFloatNestedEmpty_InIntegerRegs_ByReflection_RiscV()
	{
		var expected = DoubleFloatNestedEmpty.Get();
		var native = (DoubleFloatNestedEmpty)typeof(Program).GetMethod("Echo_DoubleFloatNestedEmpty_InIntegerRegs_RiscV").Invoke(
			null, new object[] {0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, expected, 1, -1f});
		var managed = (DoubleFloatNestedEmpty)typeof(Program).GetMethod("Echo_DoubleFloatNestedEmpty_InIntegerRegs_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, expected, 1, -1f});

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region ArrayOfEmptiesFloatDouble_RiscVTests
	[InlineArray(1)]
	public struct ArrayOfEmpties
	{
		public Empty e;
	}

	public struct ArrayOfEmptiesFloatDouble
	{
		public ArrayOfEmpties ArrayOfEmpties0;
		public float Float0;
		public double Double0;

		public static ArrayOfEmptiesFloatDouble Get()
			=> new ArrayOfEmptiesFloatDouble { Float0 = 3.14159f, Double0 = 2.71828 };

		public override bool Equals(object other)
			=> other is ArrayOfEmptiesFloatDouble o && Float0 == o.Float0 && Double0 == o.Double0;

		public override string ToString()
			=> $"{{Float0:{Float0}, Double0:{Double0}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern ArrayOfEmptiesFloatDouble Echo_ArrayOfEmptiesFloatDouble_RiscV(int a0, float fa0,
		ArrayOfEmptiesFloatDouble a1_a2, int a3, float fa1);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static ArrayOfEmptiesFloatDouble Echo_ArrayOfEmptiesFloatDouble_RiscV_Managed(int a0, float fa0,
		ArrayOfEmptiesFloatDouble a1_a2, int a3, float fa1)
	{
		a1_a2.Double0 += (double)a3 + fa1;
		return a1_a2;
	}

	[Fact]
	public static void Test_ArrayOfEmptiesFloatDouble_RiscV()
	{
		ArrayOfEmptiesFloatDouble expected = ArrayOfEmptiesFloatDouble.Get();
		ArrayOfEmptiesFloatDouble native = Echo_ArrayOfEmptiesFloatDouble_RiscV(0, 0f, expected, 1, -1f);
		ArrayOfEmptiesFloatDouble managed = Echo_ArrayOfEmptiesFloatDouble_RiscV_Managed(0, 0f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[Fact]
	public static void Test_ArrayOfEmptiesFloatDouble_ByReflection_RiscV()
	{
		var expected = ArrayOfEmptiesFloatDouble.Get();
		var native = (ArrayOfEmptiesFloatDouble)typeof(Program).GetMethod("Echo_ArrayOfEmptiesFloatDouble_RiscV").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});
		var managed = (ArrayOfEmptiesFloatDouble)typeof(Program).GetMethod("Echo_ArrayOfEmptiesFloatDouble_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});

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

		public override bool Equals(object other)
			=> other is EmptyUshortAndDouble o && EmptyUshort0.Ushort0 == o.EmptyUshort0.Ushort0 && Double0 == o.Double0;

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

	[Fact]
	public static void Test_EmptyUshortAndDouble_ByReflection_RiscV()
	{
		var expected = EmptyUshortAndDouble.Get();
		var native = (EmptyUshortAndDouble)typeof(Program).GetMethod("Echo_EmptyUshortAndDouble_RiscV").Invoke(
			null, new object[] {0, 0f, expected, -1, 1f});
		var managed = (EmptyUshortAndDouble)typeof(Program).GetMethod("Echo_EmptyUshortAndDouble_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, expected, -1, 1f});

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

		public override bool Equals(object other)
			=> other is PackedEmptyFloatLong o && Float0 == o.Float0 && Long0 == o.Long0;

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
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
	public static void Test_PackedEmptyFloatLong_RiscV()
	{
		PackedEmptyFloatLong expected = PackedEmptyFloatLong.Get();
		PackedEmptyFloatLong native = Echo_PackedEmptyFloatLong_RiscV(0, 0f, expected, 1, -1f);
		PackedEmptyFloatLong managed = Echo_PackedEmptyFloatLong_RiscV_Managed(0, 0f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
	public static void Test_PackedEmptyFloatLong_ByReflection_RiscV()
	{
		var expected = PackedEmptyFloatLong.Get();
		var native = (PackedEmptyFloatLong)typeof(Program).GetMethod("Echo_PackedEmptyFloatLong_RiscV").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});
		var managed = (PackedEmptyFloatLong)typeof(Program).GetMethod("Echo_PackedEmptyFloatLong_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f});

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
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
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

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
	public static void Test_PackedEmptyFloatLong_InIntegerRegs_ByReflection_RiscV()
	{
		var expected = PackedEmptyFloatLong.Get();
		var native = (PackedEmptyFloatLong)typeof(Program).GetMethod("Echo_PackedEmptyFloatLong_InIntegerRegs_RiscV").Invoke(
			null, new object[] {0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});
		var managed = (PackedEmptyFloatLong)typeof(Program).GetMethod("Echo_PackedEmptyFloatLong_InIntegerRegs_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});

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
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
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

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
	public static void Test_PackedEmptyFloatLong_Split_ByReflection_RiscV()
	{
		var expected = PackedEmptyFloatLong.Get();
		var native = (PackedEmptyFloatLong)typeof(Program).GetMethod("Echo_PackedEmptyFloatLong_Split_RiscV").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});
		var managed = (PackedEmptyFloatLong)typeof(Program).GetMethod("Echo_PackedEmptyFloatLong_Split_RiscV_Managed").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});

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
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
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

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
	public static void Test_PackedEmptyFloatLong_OnStack_ByReflection_RiscV()
	{
		var expected = PackedEmptyFloatLong.Get();
		var native = (PackedEmptyFloatLong)typeof(Program).GetMethod("Echo_PackedEmptyFloatLong_OnStack_RiscV").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});
		var managed = (PackedEmptyFloatLong)typeof(Program).GetMethod("Echo_PackedEmptyFloatLong_OnStack_RiscV_Managed").Invoke(
			null, new object[] {0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f});

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region PackedFloatEmptyByte_RiscVTests
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	public struct PackedFloatEmptyByte
	{
		public float Float0;
		public Empty Empty0;
		public byte Byte0;

		public static PackedFloatEmptyByte Get()
			=> new PackedFloatEmptyByte { Float0 = 2.71828f, Byte0 = 0xba };

		public override bool Equals(object other)
			=> other is PackedFloatEmptyByte o && Float0 == o.Float0 && Byte0 == o.Byte0;

		public override string ToString()
			=> $"{{Float0:{Float0}, Byte0:{Byte0}}}";
	}

	[DllImport("EmptyStructsLib")]
	public static extern PackedFloatEmptyByte Echo_PackedFloatEmptyByte_RiscV(int a0, float fa0,
		PackedFloatEmptyByte fa1_a1, int a2, float fa2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static PackedFloatEmptyByte Echo_PackedFloatEmptyByte_RiscV_Managed(int a0, float fa0,
		PackedFloatEmptyByte fa1_a1, int a2, float fa2)
	{
		fa1_a1.Float0 += (float)a2 + fa2;
		return fa1_a1;
	}

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
	public static void Test_PackedFloatEmptyByte_RiscV()
	{
		PackedFloatEmptyByte expected = PackedFloatEmptyByte.Get();
		PackedFloatEmptyByte native = Echo_PackedFloatEmptyByte_RiscV(0, 0f, expected, 1, -1f);
		PackedFloatEmptyByte managed = Echo_PackedFloatEmptyByte_RiscV_Managed(0, 0f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
	public static void Test_PackedFloatEmptyByte_ByReflection_RiscV()
	{
		var expected = PackedFloatEmptyByte.Get();
		var native = (PackedFloatEmptyByte)typeof(Program).GetMethod("Echo_PackedFloatEmptyByte_RiscV").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f });
		var managed = (PackedFloatEmptyByte)typeof(Program).GetMethod("Echo_PackedFloatEmptyByte_RiscV_Managed").Invoke(
			null, new object[] {0, 0f, expected, 1, -1f });

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern PackedFloatEmptyByte Echo_PackedFloatEmptyByte_InIntegerRegs_RiscV(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		PackedFloatEmptyByte a1, int a2, float a3);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static PackedFloatEmptyByte Echo_PackedFloatEmptyByte_InIntegerRegs_RiscV_Managed(
		int a0,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		PackedFloatEmptyByte a1, int a2, float a3)
	{
		a1.Float0 += (float)a2 + a3;
		return a1;
	}

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
	public static void Test_PackedFloatEmptyByte_InIntegerRegs_RiscV()
	{
		PackedFloatEmptyByte expected = PackedFloatEmptyByte.Get();
		PackedFloatEmptyByte native = Echo_PackedFloatEmptyByte_InIntegerRegs_RiscV(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		PackedFloatEmptyByte managed = Echo_PackedFloatEmptyByte_InIntegerRegs_RiscV_Managed(
			0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
	public static void Test_PackedFloatEmptyByte_InIntegerRegs_ByReflection_RiscV()
	{
		var expected = PackedFloatEmptyByte.Get();
		var native = (PackedFloatEmptyByte)typeof(Program).GetMethod("Echo_PackedFloatEmptyByte_InIntegerRegs_RiscV").Invoke(
			null, new object[] { 0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f });
		var managed = (PackedFloatEmptyByte)typeof(Program).GetMethod("Echo_PackedFloatEmptyByte_InIntegerRegs_RiscV_Managed").Invoke(
			null, new object[] { 0, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f });

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[DllImport("EmptyStructsLib")]
	public static extern PackedFloatEmptyByte Echo_PackedFloatEmptyByte_OnStack_RiscV(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		PackedFloatEmptyByte stack0, int stack1, float stack2);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static PackedFloatEmptyByte Echo_PackedFloatEmptyByte_OnStack_RiscV_Managed(
		int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
		PackedFloatEmptyByte stack0, int stack1, float stack2)
	{
		stack0.Float0 += (float)stack1 + stack2;
		return stack0;
	}

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
	public static void Test_PackedFloatEmptyByte_OnStack_RiscV()
	{
		PackedFloatEmptyByte expected = PackedFloatEmptyByte.Get();
		PackedFloatEmptyByte native = Echo_PackedFloatEmptyByte_OnStack_RiscV(
			0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);
		PackedFloatEmptyByte managed = Echo_PackedFloatEmptyByte_OnStack_RiscV_Managed(
			0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}

	[Fact]
	[ActiveIssue(ProblemsWithEmptyStructPassing, typeof(Program), nameof(IsArm64Or32))]
	public static void Test_PackedFloatEmptyByte_OnStack_ByReflection_RiscV()
	{
		var expected = PackedFloatEmptyByte.Get();
		var native = (PackedFloatEmptyByte)typeof(Program).GetMethod("Echo_PackedFloatEmptyByte_OnStack_RiscV").Invoke(
			null, new object[] { 0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f });
		var managed = (PackedFloatEmptyByte)typeof(Program).GetMethod("Echo_PackedFloatEmptyByte_OnStack_RiscV_Managed").Invoke(
			null, new object[] { 0, 1, 2, 3, 4, 5, 6, 7, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, expected, 1, -1f });

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
#endregion

#region ShufflingThunks_RiscVTests
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ShufflingThunk_EmptyFloatEmpty5Byte_RiscV(
		int a1_to_a0, int a2_to_a1, int a3_to_a2, int a4_to_a3, int a5_to_a4, int a6_to_a5, int a7_to_a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6,
		EmptyFloatEmpty5Byte stack0_stack1_to_fa7_a7,
		int stack2_to_stack0, float fa7_to_stack1)
	{
		Assert.Equal(0, a1_to_a0);
		Assert.Equal(1, a2_to_a1);
		Assert.Equal(2, a3_to_a2);
		Assert.Equal(3, a4_to_a3);
		Assert.Equal(4, a5_to_a4);
		Assert.Equal(5, a6_to_a5);
		Assert.Equal(6, a7_to_a6);
		Assert.Equal(0f, fa0);
		Assert.Equal(1f, fa1);
		Assert.Equal(2f, fa2);
		Assert.Equal(3f, fa3);
		Assert.Equal(4f, fa4);
		Assert.Equal(5f, fa5);
		Assert.Equal(6f, fa6);
		Assert.Equal(EmptyFloatEmpty5Byte.Get(), stack0_stack1_to_fa7_a7);
		Assert.Equal(7, stack2_to_stack0);
		Assert.Equal(7f, fa7_to_stack1);
	}

	[Fact]
	public static void Test_ShufflingThunk_EmptyFloatEmpty5Byte_RiscV()
	{
		var getDelegate = [MethodImpl(MethodImplOptions.NoOptimization)] ()
			=> ShufflingThunk_EmptyFloatEmpty5Byte_RiscV;
		getDelegate()(0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f,
			EmptyFloatEmpty5Byte.Get(), 7, 7f);
	}


	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ShufflingThunk_EmptyFloatEmpty5Sbyte_Empty8Float_RiscV(
		int a1_to_a0, int a2_to_a1, int a3_to_a2, int a4_to_a3, int a5_to_a4, int a6_to_a5, int a7_to_a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6,
		EmptyFloatEmpty5Sbyte stack0_stack1_to_fa7_a7,
		int stack2_to_stack0,
		Empty8Float fa7_to_stack1_stack2)
	{
		Assert.Equal(0, a1_to_a0);
		Assert.Equal(1, a2_to_a1);
		Assert.Equal(2, a3_to_a2);
		Assert.Equal(3, a4_to_a3);
		Assert.Equal(4, a5_to_a4);
		Assert.Equal(5, a6_to_a5);
		Assert.Equal(6, a7_to_a6);
		Assert.Equal(0f, fa0);
		Assert.Equal(1f, fa1);
		Assert.Equal(2f, fa2);
		Assert.Equal(3f, fa3);
		Assert.Equal(4f, fa4);
		Assert.Equal(5f, fa5);
		Assert.Equal(6f, fa6);
		Assert.Equal(EmptyFloatEmpty5Sbyte.Get(), stack0_stack1_to_fa7_a7);
		Assert.Equal(7, stack2_to_stack0);
		Assert.Equal(Empty8Float.Get(), fa7_to_stack1_stack2);
	}

	[Fact]
	public static void Test_ShufflingThunk_EmptyFloatEmpty5Sbyte_Empty8Float_RiscV()
	{
		var getDelegate = [MethodImpl(MethodImplOptions.NoOptimization)] ()
			=> ShufflingThunk_EmptyFloatEmpty5Sbyte_Empty8Float_RiscV;
		getDelegate()(0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f,
			EmptyFloatEmpty5Sbyte.Get(), 7, Empty8Float.Get());
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ShufflingThunk_EmptyUshortAndDouble_FloatEmpty8Float_Empty8Float_RiscV(
		int a1_to_a0, int a2_to_a1, int a3_to_a2, int a4_to_a3, int a5_to_a4, int a6_to_a5, int a7_to_a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5,
		EmptyUshortAndDouble stack0_stack1_to_a7_fa6, // 1st lowering
		FloatEmpty8Float fa6_fa7_to_stack0_stack1, // delowering
		Empty8Float stack1_stack2_to_fa7, // 2nd lowering
		int stack3_to_stack2)
	{
		Assert.Equal(0, a1_to_a0);
		Assert.Equal(1, a2_to_a1);
		Assert.Equal(2, a3_to_a2);
		Assert.Equal(3, a4_to_a3);
		Assert.Equal(4, a5_to_a4);
		Assert.Equal(5, a6_to_a5);
		Assert.Equal(6, a7_to_a6);
		Assert.Equal(0f, fa0);
		Assert.Equal(1f, fa1);
		Assert.Equal(2f, fa2);
		Assert.Equal(3f, fa3);
		Assert.Equal(4f, fa4);
		Assert.Equal(5f, fa5);
		Assert.Equal(EmptyUshortAndDouble.Get(), stack0_stack1_to_a7_fa6);
		Assert.Equal(FloatEmpty8Float.Get(), fa6_fa7_to_stack0_stack1);
		Assert.Equal(Empty8Float.Get(), stack1_stack2_to_fa7);
		Assert.Equal(7, stack3_to_stack2);
	}

	[Fact]
	public static void Test_ShufflingThunk_EmptyUshortAndDouble_FloatEmpty8Float_Empty8Float_RiscV()
	{
		var getDelegate = [MethodImpl(MethodImplOptions.NoOptimization)] ()
			=> ShufflingThunk_EmptyUshortAndDouble_FloatEmpty8Float_Empty8Float_RiscV;
		getDelegate()(0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f,
			EmptyUshortAndDouble.Get(), FloatEmpty8Float.Get(), Empty8Float.Get(), 7);
	}


	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ShufflingThunk_FloatEmptyShort_DoubleFloatNestedEmpty_Float_RiscV(
		int a1_to_a0, int a2_to_a1, int a3_to_a2, int a4_to_a3, int a5_to_a4, int a6_to_a5, int a7_to_a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5,
		FloatEmptyShort stack0_to_fa6_a7, // frees 1 stack slot
		int stack1_to_stack0,
		DoubleFloatNestedEmpty fa6_fa7_to_stack1_stack2, // takes 2 stack slots
		int stack2_to_stack3, // shuffle stack slots to the right
		int stack3_to_stack4,
		Empty8Float stack4_stack5_to_fa7, // frees 2 stack slots
		int stack6_to_stack5, // shuffle stack slots to the left
		int stack7_to_stack6)
	{
		Assert.Equal(0, a1_to_a0);
		Assert.Equal(1, a2_to_a1);
		Assert.Equal(2, a3_to_a2);
		Assert.Equal(3, a4_to_a3);
		Assert.Equal(4, a5_to_a4);
		Assert.Equal(5, a6_to_a5);
		Assert.Equal(6, a7_to_a6);
		Assert.Equal(0f, fa0);
		Assert.Equal(1f, fa1);
		Assert.Equal(2f, fa2);
		Assert.Equal(3f, fa3);
		Assert.Equal(4f, fa4);
		Assert.Equal(5f, fa5);
		Assert.Equal(FloatEmptyShort.Get(), stack0_to_fa6_a7);
		Assert.Equal(7, stack1_to_stack0);
		Assert.Equal(DoubleFloatNestedEmpty.Get(), fa6_fa7_to_stack1_stack2);
		Assert.Equal(8, stack2_to_stack3);
		Assert.Equal(9, stack3_to_stack4);
		Assert.Equal(Empty8Float.Get(), stack4_stack5_to_fa7);
		Assert.Equal(10, stack6_to_stack5);
		Assert.Equal(11, stack7_to_stack6);
	}

	[Fact]
	public static void Test_ShufflingThunk_FloatEmptyShort_DoubleFloatNestedEmpty_Float_RiscV()
	{
		var getDelegate = [MethodImpl(MethodImplOptions.NoOptimization)] ()
			=> ShufflingThunk_FloatEmptyShort_DoubleFloatNestedEmpty_Float_RiscV;
		getDelegate()(0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f,
			FloatEmptyShort.Get(), 7, DoubleFloatNestedEmpty.Get(), 8, 9, Empty8Float.Get(), 10, 11);
	}


	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ShufflingThunk_FloatEmptyShort_DoubleFloatNestedEmpty_RiscV(
		int a1_to_a0, int a2_to_a1, int a3_to_a2, int a4_to_a3, int a5_to_a4, int a6_to_a5, int a7_to_a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5,
		FloatEmptyShort stack0_to_fa6_a7, // frees 1 stack slot
		int stack1_to_stack0,
		DoubleFloatNestedEmpty fa6_fa7_to_stack1_stack2, // takes 2 stack slots
		int stack2_to_stack3, // shuffle stack slots to the right
		int stack3_to_stack4) // shuffling thunk must grow stack
	{
		Assert.Equal(0, a1_to_a0);
		Assert.Equal(1, a2_to_a1);
		Assert.Equal(2, a3_to_a2);
		Assert.Equal(3, a4_to_a3);
		Assert.Equal(4, a5_to_a4);
		Assert.Equal(5, a6_to_a5);
		Assert.Equal(6, a7_to_a6);
		Assert.Equal(0f, fa0);
		Assert.Equal(1f, fa1);
		Assert.Equal(2f, fa2);
		Assert.Equal(3f, fa3);
		Assert.Equal(4f, fa4);
		Assert.Equal(5f, fa5);
		Assert.Equal(FloatEmptyShort.Get(), stack0_to_fa6_a7);
		Assert.Equal(7, stack1_to_stack0);
		Assert.Equal(DoubleFloatNestedEmpty.Get(), fa6_fa7_to_stack1_stack2);
		Assert.Equal(8, stack2_to_stack3);
		Assert.Equal(9, stack3_to_stack4);
	}

	[Fact]
	public static void Test_ShufflingThunk_FloatEmptyShort_DoubleFloatNestedEmpty_RiscV()
	{
		var getDelegate = [MethodImpl(MethodImplOptions.NoOptimization)] ()
			=> ShufflingThunk_FloatEmptyShort_DoubleFloatNestedEmpty_RiscV;
		getDelegate()(0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f,
			FloatEmptyShort.Get(), 7, DoubleFloatNestedEmpty.Get(), 8, 9);
	}


	public struct FloatFloat
	{
		public float Float0;
		public float Float1;

		public static FloatFloat Get()
			=> new FloatFloat { Float0 = 2.71828f, Float1 = 1.61803f };

		public bool Equals(FloatFloat other)
			=> Float0 == other.Float0 && Float1 == other.Float1;

		public override string ToString()
			=> $"{{Float0:{Float0}, Float1:{Float1}}}";
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ShufflingThunk_PackedEmptyFloatLong_FloatFloat_RiscV(
		int a1_to_a0, int a2_to_a1, int a3_to_a2, int a4_to_a3, int a5_to_a4, int a6_to_a5, int a7_to_a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5,
		PackedEmptyFloatLong stack0_stack1_to_fa7_a7,
		int stack2_to_stack0,
		FloatFloat fa6_fa7_to_stack1)
	{
		Assert.Equal(0, a1_to_a0);
		Assert.Equal(1, a2_to_a1);
		Assert.Equal(2, a3_to_a2);
		Assert.Equal(3, a4_to_a3);
		Assert.Equal(4, a5_to_a4);
		Assert.Equal(5, a6_to_a5);
		Assert.Equal(6, a7_to_a6);
		Assert.Equal(0f, fa0);
		Assert.Equal(1f, fa1);
		Assert.Equal(2f, fa2);
		Assert.Equal(3f, fa3);
		Assert.Equal(4f, fa4);
		Assert.Equal(5f, fa5);
		Assert.Equal(PackedEmptyFloatLong.Get(), stack0_stack1_to_fa7_a7);
		Assert.Equal(7, stack2_to_stack0);
		Assert.Equal(FloatFloat.Get(), fa6_fa7_to_stack1);
	}

	[Fact]
	public static void Test_ShufflingThunk_PackedEmptyFloatLong_FloatFloat_RiscV()
	{
		var getDelegate = [MethodImpl(MethodImplOptions.NoOptimization)] ()
			=> ShufflingThunk_PackedEmptyFloatLong_FloatFloat_RiscV;
		getDelegate()(0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f,
			PackedEmptyFloatLong.Get(), 7, FloatFloat.Get());
	}


	[StructLayout(LayoutKind.Sequential, Pack=1)]
	public struct PackedEmptyUintEmptyFloat
	{
		public Empty Empty0;
		public uint Uint0;
		public Empty Empty1;
		public float Float0;

		public static PackedEmptyUintEmptyFloat Get()
			=> new PackedEmptyUintEmptyFloat { Uint0 = 0xB1ed0c1e, Float0 = 2.71828f };

		public bool Equals(PackedEmptyUintEmptyFloat other)
			=> Uint0 == other.Uint0 && Float0 == other.Float0;

		public override string ToString()
			=> $"{{Uint0:{Uint0}, Float0:{Float0}}}";
	}

	[StructLayout(LayoutKind.Sequential, Pack=1)]
	public struct PackedEmptyDouble
	{
		public Empty Empty0;
		public double Double0;

		public static PackedEmptyDouble Get()
			=> new PackedEmptyDouble { Double0 = 1.61803 };

		public bool Equals(PackedEmptyDouble other)
			=> Double0 == other.Double0;

		public override string ToString()
			=> $"{{Double0:{Double0}}}";
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ShufflingThunk_PackedEmptyUintEmptyFloat_PackedEmptyDouble(
		int a1_to_a0, int a2_to_a1, int a3_to_a2, int a4_to_a3, int a5_to_a4, int a6_to_a5, int a7_to_a6,
		float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6,
		PackedEmptyUintEmptyFloat stack0_stack1_to_a7_fa7,
		int stack2_to_stack0,
		PackedEmptyDouble fa7_to_stack1_stack2)
	{
		Assert.Equal(0, a1_to_a0);
		Assert.Equal(1, a2_to_a1);
		Assert.Equal(2, a3_to_a2);
		Assert.Equal(3, a4_to_a3);
		Assert.Equal(4, a5_to_a4);
		Assert.Equal(5, a6_to_a5);
		Assert.Equal(6, a7_to_a6);
		Assert.Equal(0f, fa0);
		Assert.Equal(1f, fa1);
		Assert.Equal(2f, fa2);
		Assert.Equal(3f, fa3);
		Assert.Equal(4f, fa4);
		Assert.Equal(5f, fa5);
		Assert.Equal(6f, fa6);
		Assert.Equal(PackedEmptyUintEmptyFloat.Get(), stack0_stack1_to_a7_fa7);
		Assert.Equal(7, stack2_to_stack0);
		Assert.Equal(PackedEmptyDouble.Get(), fa7_to_stack1_stack2);
	}

	[Fact]
	public static void Test_ShufflingThunk_PackedEmptyUintEmptyFloat_PackedEmptyDouble()
	{
		var getDelegate = [MethodImpl(MethodImplOptions.NoOptimization)] ()
			=> ShufflingThunk_PackedEmptyUintEmptyFloat_PackedEmptyDouble;
		getDelegate()(0, 1, 2, 3, 4, 5, 6, 0f, 1f, 2f, 3f, 4f, 5f, 6f,
			PackedEmptyUintEmptyFloat.Get(), 7, PackedEmptyDouble.Get());
	}
#endregion
}