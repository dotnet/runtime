// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public static class Program
{
	public struct Empty
	{
	}


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
	public static extern IntEmpty EchoIntEmptySysV(int i0, IntEmpty val);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static IntEmpty EchoIntEmptySysVManaged(int i0, IntEmpty val) => val;

	[Fact]
	public static void TestIntEmptySysV()
	{
		IntEmpty expected = IntEmpty.Get();
		IntEmpty native = EchoIntEmptySysV(0, expected);
		IntEmpty managed = EchoIntEmptySysVManaged(0, expected);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}


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
	public static extern IntEmptyPair EchoIntEmptyPairSysV(int i0, IntEmptyPair val);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static IntEmptyPair EchoIntEmptyPairSysVManaged(int i0, IntEmptyPair val) => val;

	[Fact]
	public static void TestIntEmptyPairSysV()
	{
		IntEmptyPair expected = IntEmptyPair.Get();
		IntEmptyPair native = EchoIntEmptyPairSysV(0, expected);
		IntEmptyPair managed = EchoIntEmptyPairSysVManaged(0, expected);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}


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
	public static extern EmptyFloatIntInt EchoEmptyFloatIntIntSysV(int i0, float f0, EmptyFloatIntInt val);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static EmptyFloatIntInt EchoEmptyFloatIntIntSysVManaged(int i0, float f0, EmptyFloatIntInt val) => val;

	[Fact]
	public static void TestEmptyFloatIntIntSysV()
	{
		EmptyFloatIntInt expected = EmptyFloatIntInt.Get();
		EmptyFloatIntInt native = EchoEmptyFloatIntIntSysV(0, 0f, expected);
		EmptyFloatIntInt managed = EchoEmptyFloatIntIntSysVManaged(0, 0f, expected);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}


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
	public static extern FloatFloatEmptyFloat EchoFloatFloatEmptyFloatSysV(float f0, FloatFloatEmptyFloat val);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static FloatFloatEmptyFloat EchoFloatFloatEmptyFloatSysVManaged(float f0, FloatFloatEmptyFloat val) => val;

	[Fact]
	public static void TestFloatFloatEmptyFloatSysV()
	{
		FloatFloatEmptyFloat expected = FloatFloatEmptyFloat.Get();
		FloatFloatEmptyFloat native = EchoFloatFloatEmptyFloatSysV(0f, expected);
		FloatFloatEmptyFloat managed = EchoFloatFloatEmptyFloatSysVManaged(0f, expected);

		Assert.Equal(expected, native);
		Assert.Equal(expected, managed);
	}
}