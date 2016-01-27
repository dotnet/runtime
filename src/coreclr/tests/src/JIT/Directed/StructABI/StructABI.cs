// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

struct SingleByte
{
	public byte Byte;

	public static SingleByte Get()
	{
		return new SingleByte { Byte = 42 };
	}

	public bool Equals(SingleByte other)
	{
		return Byte == other.Byte;
	}
}

struct SingleLong
{
	public ulong Long;

	public static SingleLong Get()
	{
		return new SingleLong { Long = 0xfeedfaceabadf00d };
	}

	public bool Equals(SingleLong other)
	{
		return Long == other.Long;
	}
}

struct SingleFloat
{
	public float Float;

	public static SingleFloat Get()
	{
		return new SingleFloat { Float = 3.14159f };
	}

	public bool Equals(SingleFloat other)
	{
		return Float == other.Float;
	}
}

struct SingleDouble
{
	public double Double;

	public static SingleDouble Get()
	{
		return new SingleDouble { Double = 3.14159d };
	}

	public bool Equals(SingleDouble other)
	{
		return Double == other.Double;
	}
}

struct ByteAndFloat
{
	public byte Byte;
	public float Float;

	public static ByteAndFloat Get()
	{
		return new ByteAndFloat { Byte = 42, Float = 3.14159f };
	}

	public bool Equals(ByteAndFloat other)
	{
		return Byte == other.Byte && Float == other.Float;
	}
}

struct FloatAndByte
{
	public float Float;
	public byte Byte;

	public static FloatAndByte Get()
	{
		return new FloatAndByte { Byte = 42, Float = 3.14159f };
	}

	public bool Equals(FloatAndByte other)
	{
		return Byte == other.Byte && Float == other.Float;
	}
}

struct LongAndFloat
{
	public ulong Long;
	public float Float;

	public static LongAndFloat Get()
	{
		return new LongAndFloat { Long = 0xfeedfaceabadf00d, Float = 3.14159f };
	}

	public bool Equals(LongAndFloat other)
	{
		return Long == other.Long && Float == other.Float;
	}
}

struct ByteAndDouble
{
	public byte Byte;
	public double Double;

	public static ByteAndDouble Get()
	{
		return new ByteAndDouble { Byte = 42, Double = 3.14159d };
	}

	public bool Equals(ByteAndDouble other)
	{
		return Byte == other.Byte && Double == other.Double;
	}
}

struct DoubleAndByte
{
	public double Double;
	public byte Byte;

	public static DoubleAndByte Get()
	{
		return new DoubleAndByte { Byte = 42, Double = 3.14159d };
	}

	public bool Equals(DoubleAndByte other)
	{
		return Byte == other.Byte && Double == other.Double;
	}
}

unsafe struct PointerAndByte
{
	public void* Pointer;
	public byte Byte;

	public static PointerAndByte Get()
	{
		byte unused;
		return new PointerAndByte { Pointer = &unused, Byte = 42 };
	}

	public bool Equals(PointerAndByte other)
	{
		return Pointer == other.Pointer && Byte == other.Byte;
	}
}

unsafe struct ByteAndPointer
{
	public byte Byte;
	public void* Pointer;

	public static ByteAndPointer Get()
	{
		byte unused;
		return new ByteAndPointer { Pointer = &unused, Byte = 42 };
	}

	public bool Equals(ByteAndPointer other)
	{
		return Pointer == other.Pointer && Byte == other.Byte;
	}
}

unsafe struct ByteFloatAndPointer
{
	public byte Byte;
	public float Float;
	public void* Pointer;

	public static ByteFloatAndPointer Get()
	{
		byte unused;
		return new ByteFloatAndPointer { Pointer = &unused, Float = 3.14159f, Byte = 42 };
	}

	public bool Equals(ByteFloatAndPointer other)
	{
		return Pointer == other.Pointer && Float == other.Float && Byte == other.Byte;
	}
}

unsafe struct PointerFloatAndByte
{
	public void* Pointer;
	public float Float;
	public byte Byte;

	public static PointerFloatAndByte Get()
	{
		byte unused;
		return new PointerFloatAndByte { Pointer = &unused, Float = 3.14159f, Byte = 42 };
	}

	public bool Equals(PointerFloatAndByte other)
	{
		return Pointer == other.Pointer && Float == other.Float && Byte == other.Byte;
	}
}

struct TwoLongs
{
	public ulong Long1;
	public ulong Long2;

	public static TwoLongs Get()
	{
		return new TwoLongs { Long1 = 0xb01dfaceddebac1e, Long2 = 0xfeedfaceabadf00d };
	}

	public bool Equals(TwoLongs other)
	{
		return Long1 == other.Long1 && Long2 == other.Long2;
	}
}

struct TwoFloats
{
	public float Float1;
	public float Float2;

	public static TwoFloats Get()
	{
		return new TwoFloats { Float1 = 3.14159f, Float2 = 2.71828f };
	}

	public bool Equals(TwoFloats other)
	{
		return Float1 == other.Float1 && Float2 == other.Float2;
	}
}

struct TwoDoubles
{
	public double Double1;
	public double Double2;

	public static TwoDoubles Get()
	{
		return new TwoDoubles { Double1 = 3.14159d, Double2 = 2.71828d };
	}

	public bool Equals(TwoDoubles other)
	{
		return Double1 == other.Double1 && Double2 == other.Double2;
	}
}

struct FourLongs
{
	public ulong Long1;
	public ulong Long2;
	public ulong Long3;
	public ulong Long4;

	public static FourLongs Get()
	{
		return new FourLongs { Long1 = 0xb01dfaceddebac1e, Long2 = 0xfeedfaceabadf00d, Long3 = 0xbeeff00fdeadcafe, Long4 = 0xabadf001ea75fee7 };
	}

	public bool Equals(FourLongs other)
	{
		return Long1 == other.Long1 && Long2 == other.Long2 && Long3 == other.Long3 && Long4 == other.Long4;
	}
}

struct FourDoubles
{
	public double Double1;
	public double Double2;
	public double Double3;
	public double Double4;

	public static FourDoubles Get()
	{
		return new FourDoubles { Double1 = 3.14159d, Double2 = 2.71828d, Double3 = 1.61803d, Double4 = 0.69314d };
	}

	public bool Equals(FourDoubles other)
	{
		return Double1 == other.Double1 && Double2 == other.Double2 && Double3 == other.Double3 && Double4 == other.Double4;
	}
}

unsafe struct InlineArray1
{
	public fixed byte Array[16];

	public static InlineArray1 Get()
	{
		var val = new InlineArray1();
		for (int i = 0; i < 16; i++)
		{
			val.Array[i] = (byte)(i + 1);
		}

		return val;
	}

	public bool Equals(InlineArray1 other)
	{
		fixed (byte* arr = Array)
		{
			for (int i = 0; i < 16; i++)
			{
				if (arr[i] != other.Array[i])
				{
					return false;
				}
			}
		}

		return true;
	}
}

unsafe struct InlineArray2
{
	public fixed float Array[4];

	public static InlineArray2 Get()
	{
		var val = new InlineArray2();
		for (int i = 0; i < 4; i++)
		{
			val.Array[i] = (float)(i + 1);
		}

		return val;
	}

	public bool Equals(InlineArray2 other)
	{
		fixed (float* arr = Array)
		{
			for (int i = 0; i < 4; i++)
			{
				if (arr[i] != other.Array[i])
				{
					return false;
				}
			}
		}

		return true;
	}
}

unsafe struct InlineArray3
{
	public fixed float Array[3];

	public static InlineArray3 Get()
	{
		var val = new InlineArray3();
		for (int i = 0; i < 3; i++)
		{
			val.Array[i] = (float)(i + 1);
		}

		return val;
	}

	public bool Equals(InlineArray3 other)
	{
		fixed (float* arr = Array)
		{
			for (int i = 0; i < 3; i++)
			{
				if (arr[i] != other.Array[i])
				{
					return false;
				}
			}
		}

		return true;
	}
}

unsafe struct InlineArray4
{
	public fixed ushort Array[5];

	public static InlineArray4 Get()
	{
		var val = new InlineArray4();
		for (int i = 0; i < 5; i++)
		{
			val.Array[i] = (ushort)(i + 1);
		}

		return val;
	}

	public bool Equals(InlineArray4 other)
	{
		fixed (ushort* arr = Array)
		{
			for (int i = 0; i < 5; i++)
			{
				if (arr[i] != other.Array[i])
				{
					return false;
				}
			}
		}

		return true;
	}
}

unsafe struct InlineArray5
{
	public fixed byte Array[9];

	public static InlineArray5 Get()
	{
		var val = new InlineArray5();
		for (int i = 0; i < 9; i++)
		{
			val.Array[i] = (byte)(i + 1);
		}

		return val;
	}

	public bool Equals(InlineArray5 other)
	{
		fixed (byte* arr = Array)
		{
			for (int i = 0; i < 9; i++)
			{
				if (arr[i] != other.Array[i])
				{
					return false;
				}
			}
		}

		return true;
	}
}

unsafe struct InlineArray6
{
	public fixed double Array[1];

	public static InlineArray6 Get()
	{
		var val = new InlineArray6();
		for (int i = 0; i < 1; i++)
		{
			val.Array[i] = (double)(i + 1);
		}

		return val;
	}

	public bool Equals(InlineArray6 other)
	{
		fixed (double* arr = Array)
		{
			for (int i = 0; i < 1; i++)
			{
				if (arr[i] != other.Array[i])
				{
					return false;
				}
			}
		}

		return true;
	}
}

struct Nested1
{
	public LongAndFloat Field1;
	public LongAndFloat Field2;

	public static Nested1 Get()
	{
		return new Nested1
		{
			Field1 = new LongAndFloat { Long = 0xfeedfaceabadf00d, Float = 3.14159f },
			Field2 = new LongAndFloat { Long = 0xbeeff00fdeadcafe, Float = 2.71928f }
		};
	}

	public bool Equals(Nested1 other)
	{
		return Field1.Equals(other.Field1) && Field2.Equals(other.Field2);
	}
}

struct Nested2
{
	public ByteAndFloat Field1;
	public FloatAndByte Field2;

	public static Nested2 Get()
	{
		return new Nested2
		{
			Field1 = new ByteAndFloat { Byte = 42, Float = 3.14159f },
			Field2 = new FloatAndByte { Byte = 24, Float = 2.71928f }
		};
	}

	public bool Equals(Nested2 other)
	{
		return Field1.Equals(other.Field1) && Field2.Equals(other.Field2);
	}
}

unsafe struct Nested3
{
	public void* Field1;
	public FloatAndByte Field2;

	public static Nested3 Get()
	{
		byte unused;
		return new Nested3 { Field1 = &unused, Field2 = FloatAndByte.Get() };
	}

	public bool Equals(Nested3 other)
	{
		return Field1 == other.Field1 && Field2.Equals(other.Field2);
	}
}

struct Nested4
{
	public InlineArray5 Field1;
	public ushort Field2;

	public static Nested4 Get()
	{
		return new Nested4 { Field1 = InlineArray5.Get(), Field2 = 0xcafe };
	}

	public bool Equals(Nested4 other)
	{
		return Field1.Equals(other.Field1) && Field2 == other.Field2;
	}
}

struct Nested5
{
	public ushort Field1;
	public InlineArray5 Field2;

	public static Nested5 Get()
	{
		return new Nested5 { Field2 = InlineArray5.Get(), Field1 = 0xcafe };
	}

	public bool Equals(Nested5 other)
	{
		return Field1 == other.Field1 && Field2.Equals(other.Field2);
	}
}

struct Nested6
{
	public InlineArray4 Field1;
	public uint Field2;

	public static Nested6 Get()
	{
		return new Nested6 { Field1 = InlineArray4.Get(), Field2 = 0xcafef00d };
	}

	public bool Equals(Nested6 other)
	{
		return Field1.Equals(other.Field1) && Field2 == other.Field2;
	}
}

struct Nested7
{
	public uint Field1;
	public InlineArray4 Field2;

	public static Nested7 Get()
	{
		return new Nested7 { Field2 = InlineArray4.Get(), Field1 = 0xcafef00d };
	}

	public bool Equals(Nested7 other)
	{
		return Field1 == other.Field1 && Field2.Equals(other.Field2);
	}
}

struct Nested8
{
	public InlineArray4 Field1;
	public ushort Field2;

	public static Nested8 Get()
	{
		return new Nested8 { Field1 = InlineArray4.Get(), Field2 = 0xcafe };
	}

	public bool Equals(Nested8 other)
	{
		return Field1.Equals(other.Field1) && Field2 == other.Field2;
	}
}

struct Nested9
{
	public ushort Field1;
	public InlineArray4 Field2;

	public static Nested9 Get()
	{
		return new Nested9 { Field2 = InlineArray4.Get(), Field1 = 0xcafe };
	}

	public bool Equals(Nested9 other)
	{
		return Field1 == other.Field1 && Field2.Equals(other.Field2);
	}
}

public static partial class StructABI
{
	[DllImport(StructABILib)]
	static extern SingleByte EchoSingleByte(SingleByte value);

	[DllImport(StructABILib)]
	static extern SingleLong EchoSingleLong(SingleLong value);

	[DllImport(StructABILib)]
	static extern SingleFloat EchoSingleFloat(SingleFloat value);

	[DllImport(StructABILib)]
	static extern SingleDouble EchoSingleDouble(SingleDouble value);

	[DllImport(StructABILib)]
	static extern ByteAndFloat EchoByteAndFloat(ByteAndFloat value);

	[DllImport(StructABILib)]
	static extern LongAndFloat EchoLongAndFloat(LongAndFloat value);

	[DllImport(StructABILib)]
	static extern ByteAndDouble EchoByteAndDouble(ByteAndDouble value);

	[DllImport(StructABILib)]
	static extern DoubleAndByte EchoDoubleAndByte(DoubleAndByte value);

	[DllImport(StructABILib)]
	static extern PointerAndByte EchoPointerAndByte(PointerAndByte value);

	[DllImport(StructABILib)]
	static extern ByteAndPointer EchoByteAndPointer(ByteAndPointer value);

	[DllImport(StructABILib)]
	static extern ByteFloatAndPointer EchoByteFloatAndPointer(ByteFloatAndPointer value);

	[DllImport(StructABILib)]
	static extern PointerFloatAndByte EchoPointerFloatAndByte(PointerFloatAndByte value);

	[DllImport(StructABILib)]
	static extern TwoLongs EchoTwoLongs(TwoLongs value);

	[DllImport(StructABILib)]
	static extern TwoFloats EchoTwoFloats(TwoFloats value);

	[DllImport(StructABILib)]
	static extern TwoDoubles EchoTwoDoubles(TwoDoubles value);

	[DllImport(StructABILib)]
	static extern FourLongs EchoFourLongs(FourLongs value);

	[DllImport(StructABILib)]
	static extern FourDoubles EchoFourDoubles(FourDoubles value);

	[DllImport(StructABILib)]
	static extern InlineArray1 EchoInlineArray1(InlineArray1 value);

	[DllImport(StructABILib)]
	static extern InlineArray2 EchoInlineArray2(InlineArray2 value);

	[DllImport(StructABILib)]
	static extern InlineArray3 EchoInlineArray3(InlineArray3 value);

	[DllImport(StructABILib)]
	static extern InlineArray4 EchoInlineArray4(InlineArray4 value);

	[DllImport(StructABILib)]
	static extern InlineArray5 EchoInlineArray5(InlineArray5 value);

	[DllImport(StructABILib)]
	static extern InlineArray6 EchoInlineArray6(InlineArray6 value);

	[DllImport(StructABILib)]
	static extern Nested1 EchoNested1(Nested1 value);

	[DllImport(StructABILib)]
	static extern Nested2 EchoNested2(Nested2 value);

	[DllImport(StructABILib)]
	static extern Nested3 EchoNested3(Nested3 value);

	[DllImport(StructABILib)]
	static extern Nested4 EchoNested4(Nested4 value);

	[DllImport(StructABILib)]
	static extern Nested5 EchoNested5(Nested5 value);

	[DllImport(StructABILib)]
	static extern Nested6 EchoNested6(Nested6 value);

	[DllImport(StructABILib)]
	static extern Nested7 EchoNested7(Nested7 value);

	[DllImport(StructABILib)]
	static extern Nested8 EchoNested8(Nested8 value);

	[DllImport(StructABILib)]
	static extern Nested9 EchoNested9(Nested9 value);

	[DllImport(StructABILib)]
	static extern TwoLongs NotEnoughRegistersSysV1(ulong a, ulong b, ulong c, ulong d, ulong e, ulong f, TwoLongs value);

	[DllImport(StructABILib)]
	static extern TwoLongs NotEnoughRegistersSysV2(ulong a, ulong b, ulong c, ulong d, ulong e, TwoLongs value);

	[DllImport(StructABILib)]
	static extern DoubleAndByte NotEnoughRegistersSysV3(ulong a, ulong b, ulong c, ulong d, ulong e, ulong f, DoubleAndByte value);

	[DllImport(StructABILib)]
	static extern TwoDoubles NotEnoughRegistersSysV4(double a, double b, double c, double d, double e, double f, double g, double h, TwoDoubles value);

	[DllImport(StructABILib)]
	static extern TwoDoubles NotEnoughRegistersSysV5(double a, double b, double c, double d, double e, double f, double g, TwoDoubles value);

	[DllImport(StructABILib)]
	static extern DoubleAndByte NotEnoughRegistersSysV6(double a, double b, double c, double d, double e, double f, double g, double h, DoubleAndByte value);

	[DllImport(StructABILib)]
	static extern TwoDoubles EnoughRegistersSysV1(ulong a, ulong b, ulong c, ulong d, ulong e, ulong f, TwoDoubles value);

	[DllImport(StructABILib)]
	static extern DoubleAndByte EnoughRegistersSysV2(ulong a, ulong b, ulong c, ulong d, ulong e, DoubleAndByte value);

	[DllImport(StructABILib)]
	static extern TwoLongs EnoughRegistersSysV3(double a, double b, double c, double d, double e, double f, double g, double h, TwoLongs value);

	[DllImport(StructABILib)]
	static extern DoubleAndByte EnoughRegistersSysV4(double a, double b, double c, double d, double e, double f, double g, DoubleAndByte value);

	static int Main()
	{
		var ok = true;
		SingleByte expectedSingleByte = SingleByte.Get();
		SingleByte actualSingleByte = EchoSingleByte(expectedSingleByte);
		if (!expectedSingleByte.Equals(actualSingleByte))
		{
			Console.WriteLine("EchoSingleByte failed");
			ok = false;
		}

		SingleLong expectedSingleLong = SingleLong.Get();
		SingleLong actualSingleLong = EchoSingleLong(expectedSingleLong);
		if (!expectedSingleLong.Equals(actualSingleLong))
		{
			Console.WriteLine("EchoSingleLong failed");
			ok = false;
		}

		SingleFloat expectedSingleFloat = SingleFloat.Get();
		SingleFloat actualSingleFloat = EchoSingleFloat(expectedSingleFloat);
		if (!expectedSingleFloat.Equals(actualSingleFloat))
		{
			Console.WriteLine("EchoSingleFloat failed");
			ok = false;
		}

		SingleDouble expectedSingleDouble = SingleDouble.Get();
		SingleDouble actualSingleDouble = EchoSingleDouble(expectedSingleDouble);
		if (!expectedSingleDouble.Equals(actualSingleDouble))
		{
			Console.WriteLine("EchoSingleDouble failed");
			ok = false;
		}

		ByteAndFloat expectedByteAndFloat = ByteAndFloat.Get();
		ByteAndFloat actualByteAndFloat = EchoByteAndFloat(expectedByteAndFloat);
		if (!expectedByteAndFloat.Equals(actualByteAndFloat))
		{
			Console.WriteLine("EchoByteAndFloat failed");
			ok = false;
		}

		LongAndFloat expectedLongAndFloat = LongAndFloat.Get();
		LongAndFloat actualLongAndFloat = EchoLongAndFloat(expectedLongAndFloat);
		if (!expectedLongAndFloat.Equals(actualLongAndFloat))
		{
			Console.WriteLine("EchoLongAndFloat failed");
			ok = false;
		}

		ByteAndDouble expectedByteAndDouble = ByteAndDouble.Get();
		ByteAndDouble actualByteAndDouble = EchoByteAndDouble(expectedByteAndDouble);
		if (!expectedByteAndDouble.Equals(actualByteAndDouble))
		{
			Console.WriteLine("EchoByteAndDouble failed");
			ok = false;
		}

		DoubleAndByte expectedDoubleAndByte = DoubleAndByte.Get();
		DoubleAndByte actualDoubleAndByte = EchoDoubleAndByte(expectedDoubleAndByte);
		if (!expectedDoubleAndByte.Equals(actualDoubleAndByte))
		{
			Console.WriteLine("EchoDoubleAndByte failed");
			ok = false;
		}

		PointerAndByte expectedPointerAndByte = PointerAndByte.Get();
		PointerAndByte actualPointerAndByte = EchoPointerAndByte(expectedPointerAndByte);
		if (!expectedPointerAndByte.Equals(actualPointerAndByte))
		{
			Console.WriteLine("EchoPointerAndByte failed");
			ok = false;
		}

		ByteAndPointer expectedByteAndPointer = ByteAndPointer.Get();
		ByteAndPointer actualByteAndPointer = EchoByteAndPointer(expectedByteAndPointer);
		if (!expectedByteAndPointer.Equals(actualByteAndPointer))
		{
			Console.WriteLine("EchoByteAndPointer failed");
			ok = false;
		}

		ByteFloatAndPointer expectedByteFloatAndPointer = ByteFloatAndPointer.Get();
		ByteFloatAndPointer actualByteFloatAndPointer = EchoByteFloatAndPointer(expectedByteFloatAndPointer);
		if (!expectedByteFloatAndPointer.Equals(actualByteFloatAndPointer))
		{
			Console.WriteLine("EchoByteFloatAndPointer failed");
			ok = false;
		}

		PointerFloatAndByte expectedPointerFloatAndByte = PointerFloatAndByte.Get();
		PointerFloatAndByte actualPointerFloatAndByte = EchoPointerFloatAndByte(expectedPointerFloatAndByte);
		if (!expectedPointerFloatAndByte.Equals(actualPointerFloatAndByte))
		{
			Console.WriteLine("EchoPointerFloatAndByte failed");
			ok = false;
		}

		TwoLongs expectedTwoLongs = TwoLongs.Get();
		TwoLongs actualTwoLongs = EchoTwoLongs(expectedTwoLongs);
		if (!expectedTwoLongs.Equals(actualTwoLongs))
		{
			Console.WriteLine("EchoTwoLongs failed");
			ok = false;
		}

		TwoFloats expectedTwoFloats = TwoFloats.Get();
		TwoFloats actualTwoFloats = EchoTwoFloats(expectedTwoFloats);
		if (!expectedTwoFloats.Equals(actualTwoFloats))
		{
			Console.WriteLine("EchoTwoFloats failed");
			ok = false;
		}

		TwoDoubles expectedTwoDoubles = TwoDoubles.Get();
		TwoDoubles actualTwoDoubles = EchoTwoDoubles(expectedTwoDoubles);
		if (!expectedTwoDoubles.Equals(actualTwoDoubles))
		{
			Console.WriteLine("EchoTwoDoubles failed");
			ok = false;
		}

		FourLongs expectedFourLongs = FourLongs.Get();
		FourLongs actualFourLongs = EchoFourLongs(expectedFourLongs);
		if (!expectedFourLongs.Equals(actualFourLongs))
		{
			Console.WriteLine("EchoFourLongs failed");
			ok = false;
		}

		FourDoubles expectedFourDoubles = FourDoubles.Get();
		FourDoubles actualFourDoubles = EchoFourDoubles(expectedFourDoubles);
		if (!expectedFourDoubles.Equals(actualFourDoubles))
		{
			Console.WriteLine("EchoFourDoubles failed");
			ok = false;
		}

		InlineArray1 expectedInlineArray1 = InlineArray1.Get();
		InlineArray1 actualInlineArray1 = EchoInlineArray1(expectedInlineArray1);
		if (!expectedInlineArray1.Equals(actualInlineArray1))
		{
			Console.WriteLine("EchoInlineArray1 failed");
			ok = false;
		}

		InlineArray2 expectedInlineArray2 = InlineArray2.Get();
		InlineArray2 actualInlineArray2 = EchoInlineArray2(expectedInlineArray2);
		if (!expectedInlineArray2.Equals(actualInlineArray2))
		{
			Console.WriteLine("EchoInlineArray2 failed");
			ok = false;
		}

		InlineArray3 expectedInlineArray3 = InlineArray3.Get();
		InlineArray3 actualInlineArray3 = EchoInlineArray3(expectedInlineArray3);
		if (!expectedInlineArray3.Equals(actualInlineArray3))
		{
			Console.WriteLine("EchoInlineArray3 failed");
			ok = false;
		}

		InlineArray4 expectedInlineArray4 = InlineArray4.Get();
		InlineArray4 actualInlineArray4 = EchoInlineArray4(expectedInlineArray4);
		if (!expectedInlineArray4.Equals(actualInlineArray4))
		{
			Console.WriteLine("EchoInlineArray4 failed");
			ok = false;
		}

		InlineArray5 expectedInlineArray5 = InlineArray5.Get();
		InlineArray5 actualInlineArray5 = EchoInlineArray5(expectedInlineArray5);
		if (!expectedInlineArray5.Equals(actualInlineArray5))
		{
			Console.WriteLine("EchoInlineArray5 failed");
			ok = false;
		}

		InlineArray6 expectedInlineArray6 = InlineArray6.Get();
		InlineArray6 actualInlineArray6 = EchoInlineArray6(expectedInlineArray6);
		if (!expectedInlineArray6.Equals(actualInlineArray6))
		{
			Console.WriteLine("EchoInlineArray6 failed");
			ok = false;
		}

		Nested1 expectedNested1 = Nested1.Get();
		Nested1 actualNested1 = EchoNested1(expectedNested1);
		if (!expectedNested1.Equals(actualNested1))
		{
			Console.WriteLine("EchoNested1 failed");
			ok = false;
		}

		Nested2 expectedNested2 = Nested2.Get();
		Nested2 actualNested2 = EchoNested2(expectedNested2);
		if (!expectedNested2.Equals(actualNested2))
		{
			Console.WriteLine("EchoNested2 failed");
			ok = false;
		}

		Nested3 expectedNested3 = Nested3.Get();
		Nested3 actualNested3 = EchoNested3(expectedNested3);
		if (!expectedNested3.Equals(actualNested3))
		{
			Console.WriteLine("EchoNested3 failed");
			ok = false;
		}

		Nested4 expectedNested4 = Nested4.Get();
		Nested4 actualNested4 = EchoNested4(expectedNested4);
		if (!expectedNested4.Equals(actualNested4))
		{
			Console.WriteLine("EchoNested4 failed");
			ok = false;
		}

		Nested5 expectedNested5 = Nested5.Get();
		Nested5 actualNested5 = EchoNested5(expectedNested5);
		if (!expectedNested5.Equals(actualNested5))
		{
			Console.WriteLine("EchoNested5 failed");
			ok = false;
		}

		Nested6 expectedNested6 = Nested6.Get();
		Nested6 actualNested6 = EchoNested6(expectedNested6);
		if (!expectedNested6.Equals(actualNested6))
		{
			Console.WriteLine("EchoNested6 failed");
			ok = false;
		}

		Nested7 expectedNested7 = Nested7.Get();
		Nested7 actualNested7 = EchoNested7(expectedNested7);
		if (!expectedNested7.Equals(actualNested7))
		{
			Console.WriteLine("EchoNested7 failed");
			ok = false;
		}

		Nested8 expectedNested8 = Nested8.Get();
		Nested8 actualNested8 = EchoNested8(expectedNested8);
		if (!expectedNested8.Equals(actualNested8))
		{
			Console.WriteLine("EchoNested8 failed");
			ok = false;
		}

		Nested9 expectedNested9 = Nested9.Get();
		Nested9 actualNested9 = EchoNested9(expectedNested9);
		if (!expectedNested9.Equals(actualNested9))
		{
			Console.WriteLine("EchoNested9 failed");
			ok = false;
		}

		TwoLongs expectedNotEnoughRegistersSysV1 = TwoLongs.Get();
		TwoLongs actualNotEnoughRegistersSysV1 = NotEnoughRegistersSysV1(1, 2, 3, 4, 5, 6, expectedNotEnoughRegistersSysV1);
		if (!expectedNotEnoughRegistersSysV1.Equals(actualNotEnoughRegistersSysV1))
		{
			Console.WriteLine("NotEnoughRegistersSysV1 failed");
			ok = false;
		}

		TwoLongs expectedNotEnoughRegistersSysV2 = TwoLongs.Get();
		TwoLongs actualNotEnoughRegistersSysV2 = NotEnoughRegistersSysV2(1, 2, 3, 4, 5, expectedNotEnoughRegistersSysV2);
		if (!expectedNotEnoughRegistersSysV2.Equals(actualNotEnoughRegistersSysV2))
		{
			Console.WriteLine("NotEnoughRegistersSysV2 failed");
			ok = false;
		}

		DoubleAndByte expectedNotEnoughRegistersSysV3 = DoubleAndByte.Get();
		DoubleAndByte actualNotEnoughRegistersSysV3 = NotEnoughRegistersSysV3(1, 2, 3, 4, 5, 6, expectedNotEnoughRegistersSysV3);
		if (!expectedNotEnoughRegistersSysV3.Equals(actualNotEnoughRegistersSysV3))
		{
			Console.WriteLine("NotEnoughRegistersSysV3 failed");
			ok = false;
		}

		TwoDoubles expectedNotEnoughRegistersSysV4 = TwoDoubles.Get();
		TwoDoubles actualNotEnoughRegistersSysV4 = NotEnoughRegistersSysV4(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, 8.0d, expectedNotEnoughRegistersSysV4);
		if (!expectedNotEnoughRegistersSysV4.Equals(actualNotEnoughRegistersSysV4))
		{
			Console.WriteLine("NotEnoughRegistersSysV4 failed");
			ok = false;
		}

		TwoDoubles expectedNotEnoughRegistersSysV5 = TwoDoubles.Get();
		TwoDoubles actualNotEnoughRegistersSysV5 = NotEnoughRegistersSysV5(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, expectedNotEnoughRegistersSysV5);
		if (!expectedNotEnoughRegistersSysV5.Equals(actualNotEnoughRegistersSysV5))
		{
			Console.WriteLine("NotEnoughRegistersSysV5 failed");
			ok = false;
		}

		DoubleAndByte expectedNotEnoughRegistersSysV6 = DoubleAndByte.Get();
		DoubleAndByte actualNotEnoughRegistersSysV6 = NotEnoughRegistersSysV6(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, 8.0d, expectedNotEnoughRegistersSysV6);
		if (!expectedNotEnoughRegistersSysV6.Equals(actualNotEnoughRegistersSysV6))
		{
			Console.WriteLine("NotEnoughRegistersSysV6 failed");
			ok = false;
		}

		TwoDoubles expectedEnoughRegistersSysV1 = TwoDoubles.Get();
		TwoDoubles actualEnoughRegistersSysV1 = EnoughRegistersSysV1(1, 2, 3, 4, 5, 6, expectedEnoughRegistersSysV1);
		if (!expectedEnoughRegistersSysV1.Equals(actualEnoughRegistersSysV1))
		{
			Console.WriteLine("EnoughRegistersSysV1 failed");
			ok = false;
		}

		DoubleAndByte expectedEnoughRegistersSysV2 = DoubleAndByte.Get();
		DoubleAndByte actualEnoughRegistersSysV2 = EnoughRegistersSysV2(1, 2, 3, 4, 5, expectedEnoughRegistersSysV2);
		if (!expectedEnoughRegistersSysV2.Equals(actualEnoughRegistersSysV2))
		{
			Console.WriteLine("EnoughRegistersSysV2 failed");
			ok = false;
		}

		TwoLongs expectedEnoughRegistersSysV3 = TwoLongs.Get();
		TwoLongs actualEnoughRegistersSysV3 = EnoughRegistersSysV3(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, 8.0d, expectedEnoughRegistersSysV3);
		if (!expectedEnoughRegistersSysV3.Equals(actualEnoughRegistersSysV3))
		{
			Console.WriteLine("EnoughRegistersSysV3 failed");
			ok = false;
		}

		DoubleAndByte expectedEnoughRegistersSysV4 = DoubleAndByte.Get();
		DoubleAndByte actualEnoughRegistersSysV4 = EnoughRegistersSysV4(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, expectedEnoughRegistersSysV4);
		if (!expectedEnoughRegistersSysV4.Equals(actualEnoughRegistersSysV4))
		{
			Console.WriteLine("EnoughRegistersSysV4 failed");
			ok = false;
		}

		return ok ? 100 : -1;
	}
}
