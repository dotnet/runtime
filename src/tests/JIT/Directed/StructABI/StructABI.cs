// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

struct SingleByte
{
	public byte Byte;

	public static SingleByte Get()
	{
		return new SingleByte { Byte = 42 };
	}

	public bool Equals(SingleByte other)
	{
		return Byte == 42 && Byte == other.Byte;
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
		return Long == other.Long && Long == 0xfeedfaceabadf00d;
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
		return Double == other.Double && Double == 3.14159d;
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
		return Byte == other.Byte && Float == other.Float &&
				Byte == 42 && Float == 3.14159f;
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
		return Byte == other.Byte && Float == other.Float &&
				Byte == 42 && Float == 3.14159f;
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
		return Long == other.Long && Float == other.Float &&
				Long == 0xfeedfaceabadf00d && Float == 3.14159f;
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

struct ShortIntFloatIntPtr
{
	public short Short;
	public int Int;
	public float Float;
	public IntPtr Pointer;

	public static ShortIntFloatIntPtr Get()
	{
		IntPtr unused = new IntPtr(42);
		return new ShortIntFloatIntPtr { Short = 10, Int = 11, Float = 3.14f, Pointer = unused };
	}

	public bool Equals(ShortIntFloatIntPtr other)
	{
		return Short == other.Short && Int == other.Int && Float == other.Float && Pointer == other.Pointer;
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
		return Field1.Equals(other.Field1) && Field2.Long == other.Field2.Long && Field2.Float == other.Field2.Float;
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
		return Field1.Equals(other.Field1) && Field2.Byte == other.Field2.Byte && Field2.Float == other.Field2.Float;
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
	[DllImport("StructABILib")]
	static extern SingleByte EchoSingleByte(SingleByte value);

	[DllImport("StructABILib")]
	static extern SingleLong EchoSingleLong(SingleLong value);

	[DllImport("StructABILib")]
	static extern SingleFloat EchoSingleFloat(SingleFloat value);

	[DllImport("StructABILib")]
	static extern SingleDouble EchoSingleDouble(SingleDouble value);

	[DllImport("StructABILib")]
	static extern ByteAndFloat EchoByteAndFloat(ByteAndFloat value);

	[DllImport("StructABILib")]
	static extern LongAndFloat EchoLongAndFloat(LongAndFloat value);

	[DllImport("StructABILib")]
	static extern ByteAndDouble EchoByteAndDouble(ByteAndDouble value);

	[DllImport("StructABILib")]
	static extern DoubleAndByte EchoDoubleAndByte(DoubleAndByte value);

	[DllImport("StructABILib")]
	static extern PointerAndByte EchoPointerAndByte(PointerAndByte value);

	[DllImport("StructABILib")]
	static extern ByteAndPointer EchoByteAndPointer(ByteAndPointer value);

	[DllImport("StructABILib")]
	static extern ByteFloatAndPointer EchoByteFloatAndPointer(ByteFloatAndPointer value);

	[DllImport("StructABILib")]
	static extern PointerFloatAndByte EchoPointerFloatAndByte(PointerFloatAndByte value);

	[DllImport("StructABILib")]
	static extern ShortIntFloatIntPtr EchoShortIntFloatIntPtr(ShortIntFloatIntPtr value);

	[DllImport("StructABILib")]
	static extern TwoLongs EchoTwoLongs(TwoLongs value);

	[DllImport("StructABILib")]
	static extern TwoFloats EchoTwoFloats(TwoFloats value);

	[DllImport("StructABILib")]
	static extern TwoDoubles EchoTwoDoubles(TwoDoubles value);

	[DllImport("StructABILib")]
	static extern FourLongs EchoFourLongs(FourLongs value);

	[DllImport("StructABILib")]
	static extern FourDoubles EchoFourDoubles(FourDoubles value);

	[DllImport("StructABILib")]
	static extern InlineArray1 EchoInlineArray1(InlineArray1 value);

	[DllImport("StructABILib")]
	static extern InlineArray2 EchoInlineArray2(InlineArray2 value);

	[DllImport("StructABILib")]
	static extern InlineArray3 EchoInlineArray3(InlineArray3 value);

	[DllImport("StructABILib")]
	static extern InlineArray4 EchoInlineArray4(InlineArray4 value);

	[DllImport("StructABILib")]
	static extern InlineArray5 EchoInlineArray5(InlineArray5 value);

	[DllImport("StructABILib")]
	static extern InlineArray6 EchoInlineArray6(InlineArray6 value);

	[DllImport("StructABILib")]
	static extern Nested1 EchoNested1(Nested1 value);

	[DllImport("StructABILib")]
	static extern Nested2 EchoNested2(Nested2 value);

	[DllImport("StructABILib")]
	static extern Nested3 EchoNested3(Nested3 value);

	[DllImport("StructABILib")]
	static extern Nested4 EchoNested4(Nested4 value);

	[DllImport("StructABILib")]
	static extern Nested5 EchoNested5(Nested5 value);

	[DllImport("StructABILib")]
	static extern Nested6 EchoNested6(Nested6 value);

	[DllImport("StructABILib")]
	static extern Nested7 EchoNested7(Nested7 value);

	[DllImport("StructABILib")]
	static extern Nested8 EchoNested8(Nested8 value);

	[DllImport("StructABILib")]
	static extern Nested9 EchoNested9(Nested9 value);

	[DllImport("StructABILib")]
	static extern TwoLongs NotEnoughRegistersSysV1(ulong a, ulong b, ulong c, ulong d, ulong e, ulong f, TwoLongs value);

	[DllImport("StructABILib")]
	static extern TwoLongs NotEnoughRegistersSysV2(ulong a, ulong b, ulong c, ulong d, ulong e, TwoLongs value);

	[DllImport("StructABILib")]
	static extern DoubleAndByte NotEnoughRegistersSysV3(ulong a, ulong b, ulong c, ulong d, ulong e, ulong f, DoubleAndByte value);

	[DllImport("StructABILib")]
	static extern TwoDoubles NotEnoughRegistersSysV4(double a, double b, double c, double d, double e, double f, double g, double h, TwoDoubles value);

	[DllImport("StructABILib")]
	static extern TwoDoubles NotEnoughRegistersSysV5(double a, double b, double c, double d, double e, double f, double g, TwoDoubles value);

	[DllImport("StructABILib")]
	static extern DoubleAndByte NotEnoughRegistersSysV6(double a, double b, double c, double d, double e, double f, double g, double h, DoubleAndByte value);

	[DllImport("StructABILib")]
	static extern TwoDoubles EnoughRegistersSysV1(ulong a, ulong b, ulong c, ulong d, ulong e, ulong f, TwoDoubles value);

	[DllImport("StructABILib")]
	static extern DoubleAndByte EnoughRegistersSysV2(ulong a, ulong b, ulong c, ulong d, ulong e, DoubleAndByte value);

	[DllImport("StructABILib")]
	static extern TwoLongs EnoughRegistersSysV3(double a, double b, double c, double d, double e, double f, double g, double h, TwoLongs value);

	[DllImport("StructABILib")]
	static extern DoubleAndByte EnoughRegistersSysV4(double a, double b, double c, double d, double e, double f, double g, DoubleAndByte value);

	////////////////////////////////////////////////////////////////////////////
	// Managed echo tests.
	////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.NoInlining)]
	static SingleByte EchoSingleByteManaged(SingleByte value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static SingleLong EchoSingleLongManaged(SingleLong value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static SingleFloat EchoSingleFloatManaged(SingleFloat value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static SingleDouble EchoSingleDoubleManaged(SingleDouble value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static ByteAndFloat EchoByteAndFloatManaged(ByteAndFloat value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static LongAndFloat EchoLongAndFloatManaged(LongAndFloat value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static ByteAndDouble EchoByteAndDoubleManaged(ByteAndDouble value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static DoubleAndByte EchoDoubleAndByteManaged(DoubleAndByte value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static PointerAndByte EchoPointerAndByteManaged(PointerAndByte value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static ByteAndPointer EchoByteAndPointerManaged(ByteAndPointer value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static ByteFloatAndPointer EchoByteFloatAndPointerManaged(ByteFloatAndPointer value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static PointerFloatAndByte EchoPointerFloatAndByteManaged(PointerFloatAndByte value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static ShortIntFloatIntPtr EchoShortIntFloatIntPtrManaged(ShortIntFloatIntPtr value)
	{

		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static TwoLongs EchoTwoLongsManaged(TwoLongs value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static TwoFloats EchoTwoFloatsManaged(TwoFloats value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static TwoDoubles EchoTwoDoublesManaged(TwoDoubles value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static FourLongs EchoFourLongsManaged(FourLongs value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static FourDoubles EchoFourDoublesManaged(FourDoubles value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static InlineArray1 EchoInlineArray1Managed(InlineArray1 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static InlineArray2 EchoInlineArray2Managed(InlineArray2 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static InlineArray3 EchoInlineArray3Managed(InlineArray3 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static InlineArray4 EchoInlineArray4Managed(InlineArray4 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static InlineArray5 EchoInlineArray5Managed(InlineArray5 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static InlineArray6 EchoInlineArray6Managed(InlineArray6 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Nested1 EchoNested1Managed(Nested1 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Nested2 EchoNested2Managed(Nested2 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Nested3 EchoNested3Managed(Nested3 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Nested4 EchoNested4Managed(Nested4 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Nested5 EchoNested5Managed(Nested5 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Nested6 EchoNested6Managed(Nested6 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Nested7 EchoNested7Managed(Nested7 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Nested8 EchoNested8Managed(Nested8 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Nested9 EchoNested9Managed(Nested9 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static TwoLongs NotEnoughRegistersSysV1Managed(ulong a, ulong b, ulong c, ulong d, ulong e, ulong f, TwoLongs value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static TwoLongs NotEnoughRegistersSysV2Managed(ulong a, ulong b, ulong c, ulong d, ulong e, TwoLongs value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static DoubleAndByte NotEnoughRegistersSysV3Managed(ulong a, ulong b, ulong c, ulong d, ulong e, ulong f, DoubleAndByte value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static TwoDoubles NotEnoughRegistersSysV4Managed(double a, double b, double c, double d, double e, double f, double g, double h, TwoDoubles value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static TwoDoubles NotEnoughRegistersSysV5Managed(double a, double b, double c, double d, double e, double f, double g, TwoDoubles value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static DoubleAndByte NotEnoughRegistersSysV6Managed(double a, double b, double c, double d, double e, double f, double g, double h, DoubleAndByte value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static TwoDoubles EnoughRegistersSysV1Managed(ulong a, ulong b, ulong c, ulong d, ulong e, ulong f, TwoDoubles value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static DoubleAndByte EnoughRegistersSysV2Managed(ulong a, ulong b, ulong c, ulong d, ulong e, DoubleAndByte value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static TwoLongs EnoughRegistersSysV3Managed(double a, double b, double c, double d, double e, double f, double g, double h, TwoLongs value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static DoubleAndByte EnoughRegistersSysV4Managed(double a, double b, double c, double d, double e, double f, double g, DoubleAndByte value)
	{
		return value;
	}

	////////////////////////////////////////////////////////////////////////////
	// Wrapper methods
	//
	// This will allow us to call both the PInvoke native function and
	// managed method.
	////////////////////////////////////////////////////////////////////////////

	static bool EchoSingleByteWrapper()
	{
		bool ok = true;
		SingleByte expectedSingleByte = SingleByte.Get();
		SingleByte nativeSingleByte = EchoSingleByte(expectedSingleByte);
		SingleByte managedSingleByte = EchoSingleByteManaged(expectedSingleByte);
		
		if (!expectedSingleByte.Equals(nativeSingleByte))
		{
			Console.WriteLine("Native call for EchoSingleByte failed");
			ok = false;
		}

		if (!expectedSingleByte.Equals(managedSingleByte))
		{
			Console.WriteLine("Managed call for EchoSingleByte failed");
			ok = false;
		}

		SingleByte expectedSingleByte2 = new SingleByte();
		expectedSingleByte2.Byte = 42;

		nativeSingleByte = EchoSingleByte(expectedSingleByte2);
		managedSingleByte = EchoSingleByteManaged(expectedSingleByte2);
		
		if (!expectedSingleByte2.Equals(nativeSingleByte))
		{
			Console.WriteLine("Native call for EchoSingleByte failed");
			ok = false;
		}

		if (!expectedSingleByte2.Equals(managedSingleByte))
		{
			Console.WriteLine("Managed call for EchoSingleByte failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoSingleLongWrapper()
	{
		bool ok = true;
		SingleLong expectedSingleLong = SingleLong.Get();
		SingleLong nativeSingleLong = EchoSingleLong(expectedSingleLong);
		SingleLong managedSingleLong = EchoSingleLongManaged(expectedSingleLong);
		
		if (!expectedSingleLong.Equals(nativeSingleLong))
		{
			Console.WriteLine("Native call for EchoSingleLong failed");
			ok = false;
		}

		if (!expectedSingleLong.Equals(managedSingleLong))
		{
			Console.WriteLine("Managed call for EchoSingleLong failed");
			ok = false;
		}

		SingleLong expectedSingleLong2 = new SingleLong();
		expectedSingleLong2.Long = 0xfeedfaceabadf00d;

		nativeSingleLong = EchoSingleLong(expectedSingleLong2);
		managedSingleLong = EchoSingleLongManaged(expectedSingleLong2);
		
		if (!expectedSingleLong2.Equals(nativeSingleLong))
		{
			Console.WriteLine("Native call for EchoSingleByte failed");
			ok = false;
		}

		if (!expectedSingleLong2.Equals(managedSingleLong))
		{
			Console.WriteLine("Managed call for EchoSingleByte failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoSingleFloatWrapper()
	{
		bool ok = true;
		SingleFloat expectedSingleFloat = SingleFloat.Get();
		SingleFloat nativeSingleFloat = EchoSingleFloat(expectedSingleFloat);
		SingleFloat managedSingleFloat = EchoSingleFloatManaged(expectedSingleFloat);
		
		if (!expectedSingleFloat.Equals(nativeSingleFloat))
		{
			Console.WriteLine("Native call for EchoSingleFloat failed");
			ok = false;
		}

		if (!expectedSingleFloat.Equals(managedSingleFloat))
		{
			Console.WriteLine("Managed call for EchoSingleFloat failed");
			ok = false;
		}

		SingleFloat expectedSingleFloat2 = new SingleFloat();
		expectedSingleFloat2.Float = 3.14159f;

		nativeSingleFloat = EchoSingleFloat(expectedSingleFloat2);
		managedSingleFloat = EchoSingleFloatManaged(expectedSingleFloat2);
		
		if (!expectedSingleFloat2.Equals(nativeSingleFloat))
		{
			Console.WriteLine("Native call for EchoSingleFloat failed");
			ok = false;
		}

		if (!expectedSingleFloat2.Equals(managedSingleFloat))
		{
			Console.WriteLine("Managed call for EchoSingleFloat failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoSingleDoubleWrapper()
	{
		bool ok = true;
		SingleDouble expectedSingleDouble = SingleDouble.Get();
		SingleDouble nativeSingleDouble = EchoSingleDouble(expectedSingleDouble);
		SingleDouble managedSingleDouble = EchoSingleDoubleManaged(expectedSingleDouble);
		
		if (!expectedSingleDouble.Equals(nativeSingleDouble))
		{
			Console.WriteLine("Native call for EchoSingleDouble failed");
			ok = false;
		}

		if (!expectedSingleDouble.Equals(managedSingleDouble))
		{
			Console.WriteLine("Managed call for EchoSingleDouble failed");
			ok = false;
		}

		SingleDouble expectedSingleDouble2 = new SingleDouble();
		expectedSingleDouble2.Double = 3.14159d;

		nativeSingleDouble = EchoSingleDouble(expectedSingleDouble2);
		managedSingleDouble = EchoSingleDoubleManaged(expectedSingleDouble2);
		
		if (!expectedSingleDouble2.Equals(nativeSingleDouble))
		{
			Console.WriteLine("Native call for EchoSingleDouble failed");
			ok = false;
		}

		if (!expectedSingleDouble2.Equals(managedSingleDouble))
		{
			Console.WriteLine("Managed call for EchoSingleDouble failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoByteAndFloatWrapper()
	{
		bool ok = true;
		ByteAndFloat expectedByteAndFloat = ByteAndFloat.Get();
		ByteAndFloat nativeByteAndFloat = EchoByteAndFloat(expectedByteAndFloat);
		ByteAndFloat managedByteAndFloat = EchoByteAndFloatManaged(expectedByteAndFloat);
		
		if (!expectedByteAndFloat.Equals(nativeByteAndFloat))
		{
			Console.WriteLine("Native call for EchoByteAndFloat failed");
			ok = false;
		}

		if (!expectedByteAndFloat.Equals(managedByteAndFloat))
		{
			Console.WriteLine("Managed call for EchoByteAndFloat failed");
			ok = false;
		}

		ByteAndFloat expectedByteAndFloat2 = new ByteAndFloat();
		expectedByteAndFloat2.Byte = 42;
		expectedByteAndFloat2.Float = 3.14159f;

		nativeByteAndFloat = EchoByteAndFloat(expectedByteAndFloat2);
		managedByteAndFloat = EchoByteAndFloatManaged(expectedByteAndFloat2);
		
		if (!expectedByteAndFloat2.Equals(nativeByteAndFloat))
		{
			Console.WriteLine("Native call for EchoByteAndFloat failed");
			ok = false;
		}

		if (!expectedByteAndFloat2.Equals(managedByteAndFloat))
		{
			Console.WriteLine("Managed call for EchoByteAndFloat failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoLongAndFloatWrapper()
	{
		bool ok = true;
		LongAndFloat expectedLongAndFloat = LongAndFloat.Get();
		LongAndFloat nativeLongAndFloat = EchoLongAndFloat(expectedLongAndFloat);
		LongAndFloat managedLongAndFloat = EchoLongAndFloatManaged(expectedLongAndFloat);
		
		if (!expectedLongAndFloat.Equals(nativeLongAndFloat))
		{
			Console.WriteLine("Native call for EchoLongAndFloat failed");
			ok = false;
		}

		if (!expectedLongAndFloat.Equals(managedLongAndFloat))
		{
			Console.WriteLine("Managed call for EchoLongAndFloat failed");
			ok = false;
		}

		LongAndFloat expectedLongAndFloat2 = new LongAndFloat();
		expectedLongAndFloat2.Long = 0xfeedfaceabadf00d;
		expectedLongAndFloat2.Float = 3.14159f;

		nativeLongAndFloat = EchoLongAndFloat(expectedLongAndFloat2);
		managedLongAndFloat = EchoLongAndFloatManaged(expectedLongAndFloat2);
		
		if (!expectedLongAndFloat2.Equals(nativeLongAndFloat))
		{
			Console.WriteLine("Native call for EchoLongAndFloat failed");
			ok = false;
		}

		if (!expectedLongAndFloat2.Equals(managedLongAndFloat))
		{
			Console.WriteLine("Managed call for EchoLongAndFloat failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoByteAndDoubleWrapper()
	{
		bool ok = true;
		ByteAndDouble expectedByteAndDouble = ByteAndDouble.Get();
		ByteAndDouble nativeByteAndDouble = EchoByteAndDouble(expectedByteAndDouble);
		ByteAndDouble managedByteAndDouble = EchoByteAndDoubleManaged(expectedByteAndDouble);
		
		if (!expectedByteAndDouble.Equals(nativeByteAndDouble))
		{
			Console.WriteLine("Native call for EchoByteAndDouble failed");
			ok = false;
		}

		if (!expectedByteAndDouble.Equals(managedByteAndDouble))
		{
			Console.WriteLine("Managed call for EchoByteAndDouble failed");
			ok = false;
		}

		ByteAndDouble expectedByteAndDouble2 = new ByteAndDouble();
		expectedByteAndDouble2.Byte = 42;
		expectedByteAndDouble2.Double = 3.14159d;

		nativeByteAndDouble = EchoByteAndDouble(expectedByteAndDouble2);
		managedByteAndDouble = EchoByteAndDoubleManaged(expectedByteAndDouble2);
		
		if (!expectedByteAndDouble2.Equals(nativeByteAndDouble))
		{
			Console.WriteLine("Native call for EchoByteAndDouble failed");
			ok = false;
		}

		if (!expectedByteAndDouble2.Equals(managedByteAndDouble))
		{
			Console.WriteLine("Managed call for EchoByteAndDouble failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoDoubleAndByteWrapper()
	{
		bool ok = true;
		DoubleAndByte expectedDoubleAndByte = DoubleAndByte.Get();
		DoubleAndByte nativeDoubleAndByte = EchoDoubleAndByte(expectedDoubleAndByte);
		DoubleAndByte managedDoubleAndByte = EchoDoubleAndByteManaged(expectedDoubleAndByte);
		
		if (!expectedDoubleAndByte.Equals(nativeDoubleAndByte))
		{
			Console.WriteLine("Native call for EchoDoubleAndByte failed");
			ok = false;
		}

		if (!expectedDoubleAndByte.Equals(managedDoubleAndByte))
		{
			Console.WriteLine("Managed call for EchoDoubleAndByte failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoPointerAndByteWrapper()
	{
		bool ok = true;
		PointerAndByte expectedPointerAndByte = PointerAndByte.Get();
		PointerAndByte nativePointerAndByte = EchoPointerAndByte(expectedPointerAndByte);
		PointerAndByte managedPointerAndByte = EchoPointerAndByteManaged(expectedPointerAndByte);
		
		if (!expectedPointerAndByte.Equals(nativePointerAndByte))
		{
			Console.WriteLine("Native call for EchoPointerAndByte failed");
			ok = false;
		}

		if (!expectedPointerAndByte.Equals(managedPointerAndByte))
		{
			Console.WriteLine("Managed call for EchoPointerAndByte failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoByteAndPointerWrapper()
	{
		bool ok = true;
		ByteAndPointer expectedByteAndPointer = ByteAndPointer.Get();
		ByteAndPointer nativeByteAndPointer = EchoByteAndPointer(expectedByteAndPointer);
		ByteAndPointer managedByteAndPointer = EchoByteAndPointerManaged(expectedByteAndPointer);
		
		if (!expectedByteAndPointer.Equals(nativeByteAndPointer))
		{
			Console.WriteLine("Native call for EchoByteAndPointer failed");
			ok = false;
		}

		if (!expectedByteAndPointer.Equals(managedByteAndPointer))
		{
			Console.WriteLine("Managed call for EchoByteAndPointer failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoByteFloatAndPointerWrapper()
	{
		bool ok = true;
		ByteFloatAndPointer expectedByteFloatAndPointer = ByteFloatAndPointer.Get();
		ByteFloatAndPointer nativeByteFloatAndPointer = EchoByteFloatAndPointer(expectedByteFloatAndPointer);
		ByteFloatAndPointer managedByteFloatAndPointer = EchoByteFloatAndPointerManaged(expectedByteFloatAndPointer);
		
		if (!expectedByteFloatAndPointer.Equals(nativeByteFloatAndPointer))
		{
			Console.WriteLine("Native call for EchoByteFloatAndPointer failed");
			ok = false;
		}

		if (!expectedByteFloatAndPointer.Equals(managedByteFloatAndPointer))
		{
			Console.WriteLine("Managed call for EchoByteFloatAndPointer failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoPointerFloatAndByteWrapper()
	{
		bool ok = true;
		PointerFloatAndByte expectedPointerFloatAndByte = PointerFloatAndByte.Get();
		PointerFloatAndByte nativePointerFloatAndByte = EchoPointerFloatAndByte(expectedPointerFloatAndByte);
		PointerFloatAndByte managedPointerFloatAndByte = EchoPointerFloatAndByteManaged(expectedPointerFloatAndByte);
		
		if (!expectedPointerFloatAndByte.Equals(nativePointerFloatAndByte))
		{
			Console.WriteLine("Native call for EchoPointerFloatAndByte failed");
			ok = false;
		}

		if (!expectedPointerFloatAndByte.Equals(managedPointerFloatAndByte))
		{
			Console.WriteLine("Managed call for EchoPointerFloatAndByte failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoShortIntFloatIntPtrWrapper()
	{
		bool ok = true;
		ShortIntFloatIntPtr expectedShortIntFloatIntPtr = ShortIntFloatIntPtr.Get();
		ShortIntFloatIntPtr nativeShortIntFloatIntPtr = EchoShortIntFloatIntPtr(expectedShortIntFloatIntPtr);
		ShortIntFloatIntPtr managedShortIntFloatIntPtr = EchoShortIntFloatIntPtrManaged(expectedShortIntFloatIntPtr);
		
		if (!expectedShortIntFloatIntPtr.Equals(nativeShortIntFloatIntPtr))
		{
			Console.WriteLine("Native call for EchoShortIntFloatIntPtr failed");
			ok = false;
		}

		if (!expectedShortIntFloatIntPtr.Equals(managedShortIntFloatIntPtr))
		{
			Console.WriteLine("Managed call for EchoShortIntFloatIntPtr failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoTwoLongsWrapper()
	{
		bool ok = true;
		TwoLongs expectedTwoLongs = TwoLongs.Get();
		TwoLongs nativeTwoLongs = EchoTwoLongs(expectedTwoLongs);
		TwoLongs managedTwoLongs = EchoTwoLongsManaged(expectedTwoLongs);
		
		if (!expectedTwoLongs.Equals(nativeTwoLongs))
		{
			Console.WriteLine("Native call for EchoTwoLongs failed");
			ok = false;
		}

		if (!expectedTwoLongs.Equals(managedTwoLongs))
		{
			Console.WriteLine("Managed call for EchoTwoLongs failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoTwoFloatsWrapper()
	{
		bool ok = true;
		TwoFloats expectedTwoFloats = TwoFloats.Get();
		TwoFloats nativeTwoFloats = EchoTwoFloats(expectedTwoFloats);
		TwoFloats managedTwoFloats = EchoTwoFloatsManaged(expectedTwoFloats);
		
		if (!expectedTwoFloats.Equals(nativeTwoFloats))
		{
			Console.WriteLine("Native call for EchoTwoFloats failed");
			ok = false;
		}

		if (!expectedTwoFloats.Equals(managedTwoFloats))
		{
			Console.WriteLine("Managed call for EchoTwoFloats failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoTwoDoublesWrapper()
	{
		bool ok = true;
		TwoDoubles expectedTwoDoubles = TwoDoubles.Get();
		TwoDoubles nativeTwoDoubles = EchoTwoDoubles(expectedTwoDoubles);
		TwoDoubles managedTwoDoubles = EchoTwoDoublesManaged(expectedTwoDoubles);
		
		if (!expectedTwoDoubles.Equals(nativeTwoDoubles))
		{
			Console.WriteLine("Native call for EchoTwoDoubles failed");
			ok = false;
		}

		if (!expectedTwoDoubles.Equals(managedTwoDoubles))
		{
			Console.WriteLine("Managed call for EchoTwoDoubles failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoFourLongsWrapper()
	{
		bool ok = true;
		FourLongs expectedFourLongs = FourLongs.Get();
		FourLongs nativeFourLongs = EchoFourLongs(expectedFourLongs);
		FourLongs managedFourLongs = EchoFourLongsManaged(expectedFourLongs);
		
		if (!expectedFourLongs.Equals(nativeFourLongs))
		{
			Console.WriteLine("Native call for EchoFourLongs failed");
			ok = false;
		}

		if (!expectedFourLongs.Equals(managedFourLongs))
		{
			Console.WriteLine("Managed call for EchoFourLongs failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoFourDoublesWrapper()
	{
		bool ok = true;
		FourDoubles expectedFourDoubles = FourDoubles.Get();
		FourDoubles nativeFourDoubles = EchoFourDoubles(expectedFourDoubles);
		FourDoubles managedFourDoubles = EchoFourDoublesManaged(expectedFourDoubles);
		
		if (!expectedFourDoubles.Equals(nativeFourDoubles))
		{
			Console.WriteLine("Native call for EchoFourDoubles failed");
			ok = false;
		}

		if (!expectedFourDoubles.Equals(managedFourDoubles))
		{
			Console.WriteLine("Managed call for EchoFourDoubles failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoInlineArray1Wrapper()
	{
		bool ok = true;
		InlineArray1 expectedInlineArray1 = InlineArray1.Get();
		InlineArray1 nativeInlineArray1 = EchoInlineArray1(expectedInlineArray1);
		InlineArray1 managedInlineArray1 = EchoInlineArray1Managed(expectedInlineArray1);
		
		if (!expectedInlineArray1.Equals(nativeInlineArray1))
		{
			Console.WriteLine("Native call for EchoInlineArray1 failed");
			ok = false;
		}

		if (!expectedInlineArray1.Equals(managedInlineArray1))
		{
			Console.WriteLine("Managed call for EchoInlineArray1 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoInlineArray2Wrapper()
	{
		bool ok = true;
		InlineArray2 expectedInlineArray2 = InlineArray2.Get();
		InlineArray2 nativeInlineArray2 = EchoInlineArray2(expectedInlineArray2);
		InlineArray2 managedInlineArray2 = EchoInlineArray2Managed(expectedInlineArray2);
		
		if (!expectedInlineArray2.Equals(nativeInlineArray2))
		{
			Console.WriteLine("Native call for EchoInlineArray2 failed");
			ok = false;
		}

		if (!expectedInlineArray2.Equals(managedInlineArray2))
		{
			Console.WriteLine("Managed call for EchoInlineArray2 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoInlineArray3Wrapper()
	{
		bool ok = true;
		InlineArray3 expectedInlineArray3 = InlineArray3.Get();
		InlineArray3 nativeInlineArray3 = EchoInlineArray3(expectedInlineArray3);
		InlineArray3 managedInlineArray3 = EchoInlineArray3Managed(expectedInlineArray3);
		
		if (!expectedInlineArray3.Equals(nativeInlineArray3))
		{
			Console.WriteLine("Native call for EchoInlineArray3 failed");
			ok = false;
		}

		if (!expectedInlineArray3.Equals(managedInlineArray3))
		{
			Console.WriteLine("Managed call for EchoInlineArray3 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoInlineArray4Wrapper()
	{
		bool ok = true;
		InlineArray4 expectedInlineArray4 = InlineArray4.Get();
		InlineArray4 nativeInlineArray4 = EchoInlineArray4(expectedInlineArray4);
		InlineArray4 managedInlineArray4 = EchoInlineArray4Managed(expectedInlineArray4);
		
		if (!expectedInlineArray4.Equals(nativeInlineArray4))
		{
			Console.WriteLine("Native call for EchoInlineArray4 failed");
			ok = false;
		}

		if (!expectedInlineArray4.Equals(managedInlineArray4))
		{
			Console.WriteLine("Managed call for EchoInlineArray4 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoInlineArray5Wrapper()
	{
		bool ok = true;
		InlineArray5 expectedInlineArray5 = InlineArray5.Get();
		InlineArray5 nativeInlineArray5 = EchoInlineArray5(expectedInlineArray5);
		InlineArray5 managedInlineArray5 = EchoInlineArray5Managed(expectedInlineArray5);
		
		if (!expectedInlineArray5.Equals(nativeInlineArray5))
		{
			Console.WriteLine("Native call for EchoInlineArray5 failed");
			ok = false;
		}

		if (!expectedInlineArray5.Equals(managedInlineArray5))
		{
			Console.WriteLine("Managed call for EchoInlineArray5 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoInlineArray6Wrapper()
	{
		bool ok = true;
		InlineArray6 expectedInlineArray6 = InlineArray6.Get();
		InlineArray6 nativeInlineArray6 = EchoInlineArray6(expectedInlineArray6);
		InlineArray6 managedInlineArray6 = EchoInlineArray6Managed(expectedInlineArray6);
		
		if (!expectedInlineArray6.Equals(nativeInlineArray6))
		{
			Console.WriteLine("Native call for EchoInlineArray6 failed");
			ok = false;
		}

		if (!expectedInlineArray6.Equals(managedInlineArray6))
		{
			Console.WriteLine("Managed call for EchoInlineArray6 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoNested1Wrapper()
	{
		bool ok = true;
		Nested1 expectedNested1 = Nested1.Get();
		Nested1 nativeNested1 = EchoNested1(expectedNested1);
		Nested1 managedNested1 = EchoNested1Managed(expectedNested1);
		
		if (!expectedNested1.Equals(nativeNested1))
		{
			Console.WriteLine("Native call for EchoNested1 failed");
			ok = false;
		}

		if (!expectedNested1.Equals(managedNested1))
		{
			Console.WriteLine("Managed call for EchoNested1 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoNested2Wrapper()
	{
		bool ok = true;
		Nested2 expectedNested2 = Nested2.Get();
		Nested2 nativeNested2 = EchoNested2(expectedNested2);
		Nested2 managedNested2 = EchoNested2Managed(expectedNested2);
		
		if (!expectedNested2.Equals(nativeNested2))
		{
			Console.WriteLine("Native call for EchoNested2 failed");
			ok = false;
		}

		if (!expectedNested2.Equals(managedNested2))
		{
			Console.WriteLine("Managed call for EchoNested2 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoNested3Wrapper()
	{
		bool ok = true;
		Nested3 expectedNested3 = Nested3.Get();
		Nested3 nativeNested3 = EchoNested3(expectedNested3);
		Nested3 managedNested3 = EchoNested3Managed(expectedNested3);
		
		if (!expectedNested3.Equals(nativeNested3))
		{
			Console.WriteLine("Native call for EchoNested3 failed");
			ok = false;
		}

		if (!expectedNested3.Equals(managedNested3))
		{
			Console.WriteLine("Managed call for EchoNested3 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoNested4Wrapper()
	{
		bool ok = true;
		Nested4 expectedNested4 = Nested4.Get();
		Nested4 nativeNested4 = EchoNested4(expectedNested4);
		Nested4 managedNested4 = EchoNested4Managed(expectedNested4);
		
		if (!expectedNested4.Equals(nativeNested4))
		{
			Console.WriteLine("Native call for EchoNested4 failed");
			ok = false;
		}

		if (!expectedNested4.Equals(managedNested4))
		{
			Console.WriteLine("Managed call for EchoNested4 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoNested5Wrapper()
	{
		bool ok = true;
		Nested5 expectedNested5 = Nested5.Get();
		Nested5 nativeNested5 = EchoNested5(expectedNested5);
		Nested5 managedNested5 = EchoNested5Managed(expectedNested5);
		
		if (!expectedNested5.Equals(nativeNested5))
		{
			Console.WriteLine("Native call for EchoNested5 failed");
			ok = false;
		}

		if (!expectedNested5.Equals(managedNested5))
		{
			Console.WriteLine("Managed call for EchoNested5 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoNested6Wrapper()
	{
		bool ok = true;
		Nested6 expectedNested6 = Nested6.Get();
		Nested6 nativeNested6 = EchoNested6(expectedNested6);
		Nested6 managedNested6 = EchoNested6Managed(expectedNested6);
		
		if (!expectedNested6.Equals(nativeNested6))
		{
			Console.WriteLine("Native call for EchoNested6 failed");
			ok = false;
		}

		if (!expectedNested6.Equals(managedNested6))
		{
			Console.WriteLine("Managed call for EchoNested6 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoNested7Wrapper()
	{
		bool ok = true;
		Nested7 expectedNested7 = Nested7.Get();
		Nested7 nativeNested7 = EchoNested7(expectedNested7);
		Nested7 managedNested7 = EchoNested7Managed(expectedNested7);
		
		if (!expectedNested7.Equals(nativeNested7))
		{
			Console.WriteLine("Native call for EchoNested7 failed");
			ok = false;
		}

		if (!expectedNested7.Equals(managedNested7))
		{
			Console.WriteLine("Managed call for EchoNested7 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoNested8Wrapper()
	{
		bool ok = true;
		Nested8 expectedNested8 = Nested8.Get();
		Nested8 nativeNested8 = EchoNested8(expectedNested8);
		Nested8 managedNested8 = EchoNested8Managed(expectedNested8);
		
		if (!expectedNested8.Equals(nativeNested8))
		{
			Console.WriteLine("Native call for EchoNested8 failed");
			ok = false;
		}

		if (!expectedNested8.Equals(managedNested8))
		{
			Console.WriteLine("Managed call for EchoNested8 failed");
			ok = false;
		}

		return ok;
	}

	static bool EchoNested9Wrapper()
	{
		bool ok = true;
		Nested9 expectedNested9 = Nested9.Get();
		Nested9 nativeNested9 = EchoNested9(expectedNested9);
		Nested9 managedNested9 = EchoNested9Managed(expectedNested9);
		
		if (!expectedNested9.Equals(nativeNested9))
		{
			Console.WriteLine("Native call for EchoNested9 failed");
			ok = false;
		}

		if (!expectedNested9.Equals(managedNested9))
		{
			Console.WriteLine("Managed call for EchoNested9 failed");
			ok = false;
		}

		return ok;
	}

	static bool NotEnoughRegistersSysV1Wrapper()
	{
		bool ok = true;

		TwoLongs expectedNotEnoughRegistersSysV1 = TwoLongs.Get();
		TwoLongs nativeNotEnoughRegistersSysV1 = NotEnoughRegistersSysV1(1, 2, 3, 4, 5, 6, expectedNotEnoughRegistersSysV1);
		TwoLongs managedNotEnoughRegistersSysV1 = NotEnoughRegistersSysV1Managed(1, 2, 3, 4, 5, 6, expectedNotEnoughRegistersSysV1);

		if (!expectedNotEnoughRegistersSysV1.Equals(nativeNotEnoughRegistersSysV1))
		{
			Console.WriteLine("Native NotEnoughRegistersSysV1 failed");
			ok = false;
		}

		if (!expectedNotEnoughRegistersSysV1.Equals(managedNotEnoughRegistersSysV1))
		{
			Console.WriteLine("Managed NotEnoughRegistersSysV1 failed");
			ok = false;
		}

		return ok;
	}

	static bool NotEnoughRegistersSysV2Wrapper()
	{
		bool ok = true;

		TwoLongs expectedNotEnoughRegistersSysV2 = TwoLongs.Get();
		TwoLongs nativeNotEnoughRegistersSysV2 = NotEnoughRegistersSysV2(1, 2, 3, 4, 5, expectedNotEnoughRegistersSysV2);
		TwoLongs managedNotEnoughRegistersSysV2 = NotEnoughRegistersSysV2Managed(1, 2, 3, 4, 5, expectedNotEnoughRegistersSysV2);

		if (!expectedNotEnoughRegistersSysV2.Equals(nativeNotEnoughRegistersSysV2))
		{
			Console.WriteLine("Native NotEnoughRegistersSysV2 failed");
			ok = false;
		}

		if (!expectedNotEnoughRegistersSysV2.Equals(managedNotEnoughRegistersSysV2))
		{
			Console.WriteLine("Managed NotEnoughRegistersSysV2 failed");
			ok = false;
		}

		return ok;
	}

	static bool NotEnoughRegistersSysV3Wrapper()
	{
		bool ok = true;
		
		DoubleAndByte expectedNotEnoughRegistersSysV3 = DoubleAndByte.Get();
		DoubleAndByte nativeNotEnoughRegistersSysV3 = NotEnoughRegistersSysV3(1, 2, 3, 4, 5, 6, expectedNotEnoughRegistersSysV3);
		DoubleAndByte managedNotEnoughRegistersSysV3 = NotEnoughRegistersSysV3Managed(1, 2, 3, 4, 5, 6, expectedNotEnoughRegistersSysV3);

		if (!expectedNotEnoughRegistersSysV3.Equals(nativeNotEnoughRegistersSysV3))
		{
			Console.WriteLine("Native NotEnoughRegistersSysV3 failed");
			ok = false;
		}

		if (!expectedNotEnoughRegistersSysV3.Equals(managedNotEnoughRegistersSysV3))
		{
			Console.WriteLine("Managed NotEnoughRegistersSysV3 failed");
			ok = false;
		}

		return ok;
	}

	static bool NotEnoughRegistersSysV4Wrapper()
	{
		bool ok = true;

		TwoDoubles expectedNotEnoughRegistersSysV4 = TwoDoubles.Get();
		TwoDoubles nativeNotEnoughRegistersSysV4 = NotEnoughRegistersSysV4(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, 8.0d, expectedNotEnoughRegistersSysV4);
		TwoDoubles managedNotEnoughRegistersSysV4 = NotEnoughRegistersSysV4Managed(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, 8.0d, expectedNotEnoughRegistersSysV4);

		if (!expectedNotEnoughRegistersSysV4.Equals(nativeNotEnoughRegistersSysV4))
		{
			Console.WriteLine("Native NotEnoughRegistersSysV4 failed");
			ok = false;
		}

		if (!expectedNotEnoughRegistersSysV4.Equals(managedNotEnoughRegistersSysV4))
		{
			Console.WriteLine("Managed NotEnoughRegistersSysV4 failed");
			ok = false;
		}

		return ok;
	}

	static bool NotEnoughRegistersSysV5Wrapper()
	{
		bool ok = true;

		TwoDoubles expectedNotEnoughRegistersSysV5 = TwoDoubles.Get();
		TwoDoubles nativeNotEnoughRegistersSysV5 = NotEnoughRegistersSysV5(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, expectedNotEnoughRegistersSysV5);
		TwoDoubles managedNotEnoughRegistersSysV5 = NotEnoughRegistersSysV5Managed(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, expectedNotEnoughRegistersSysV5);

		if (!expectedNotEnoughRegistersSysV5.Equals(nativeNotEnoughRegistersSysV5))
		{
			Console.WriteLine("Native NotEnoughRegistersSysV5 failed");
			ok = false;
		}

		if (!expectedNotEnoughRegistersSysV5.Equals(managedNotEnoughRegistersSysV5))
		{
			Console.WriteLine("Managed NotEnoughRegistersSysV5 failed");
			ok = false;
		}

		return ok;
	}

	static bool NotEnoughRegistersSysV6Wrapper()
	{
		bool ok = true;
		
		DoubleAndByte expectedNotEnoughRegistersSysV6 = DoubleAndByte.Get();
		DoubleAndByte nativeNotEnoughRegistersSysV6 = NotEnoughRegistersSysV6(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, 8.0d, expectedNotEnoughRegistersSysV6);
		DoubleAndByte managedNotEnoughRegistersSysV6 = NotEnoughRegistersSysV6Managed(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, 8.0d, expectedNotEnoughRegistersSysV6);

		if (!expectedNotEnoughRegistersSysV6.Equals(nativeNotEnoughRegistersSysV6))
		{
			Console.WriteLine("Native NotEnoughRegistersSysV6 failed");
			ok = false;
		}

		if (!expectedNotEnoughRegistersSysV6.Equals(managedNotEnoughRegistersSysV6))
		{
			Console.WriteLine("Managed NotEnoughRegistersSysV6 failed");
			ok = false;
		}

		return ok;
	}

	static bool EnoughRegistersSysV1Wrapper()
	{
		bool ok = true;

		TwoDoubles expectedEnoughRegistersSysV1 = TwoDoubles.Get();
		TwoDoubles nativeEnoughRegistersSysV1 = EnoughRegistersSysV1(1, 2, 3, 4, 5, 6, expectedEnoughRegistersSysV1);
		TwoDoubles managedEnoughRegistersSysV1 = EnoughRegistersSysV1Managed(1, 2, 3, 4, 5, 6, expectedEnoughRegistersSysV1);

		if (!expectedEnoughRegistersSysV1.Equals(nativeEnoughRegistersSysV1))
		{
			Console.WriteLine("Native EnoughRegistersSysV1 failed");
			ok = false;
		}

		if (!expectedEnoughRegistersSysV1.Equals(managedEnoughRegistersSysV1))
		{
			Console.WriteLine("Managed EnoughRegistersSysV1 failed");
			ok = false;
		}

		return ok;
	}

	static bool EnoughRegistersSysV2Wrapper()
	{
		bool ok = true;

		DoubleAndByte expectedEnoughRegistersSysV2 = DoubleAndByte.Get();
		DoubleAndByte nativeEnoughRegistersSysV2 = EnoughRegistersSysV2(1, 2, 3, 4, 5, expectedEnoughRegistersSysV2);
		DoubleAndByte managedEnoughRegistersSysV2 = EnoughRegistersSysV2Managed(1, 2, 3, 4, 5, expectedEnoughRegistersSysV2);

		if (!expectedEnoughRegistersSysV2.Equals(nativeEnoughRegistersSysV2))
		{
			Console.WriteLine("Native EnoughRegistersSysV2 failed");
			ok = false;
		}

		if (!expectedEnoughRegistersSysV2.Equals(managedEnoughRegistersSysV2))
		{
			Console.WriteLine("Managed EnoughRegistersSysV2 failed");
			ok = false;
		}

		return ok;
	}

	static bool EnoughRegistersSysV3Wrapper()
	{
		bool ok = true;

		TwoLongs expectedEnoughRegistersSysV3 = TwoLongs.Get();
		TwoLongs nativeEnoughRegistersSysV3 = EnoughRegistersSysV3(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, 8.0d, expectedEnoughRegistersSysV3);
		TwoLongs managedEnoughRegistersSysV3 = EnoughRegistersSysV3Managed(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, 8.0d, expectedEnoughRegistersSysV3);

		if (!expectedEnoughRegistersSysV3.Equals(nativeEnoughRegistersSysV3))
		{
			Console.WriteLine("Native EnoughRegistersSysV3 failed");
			ok = false;
		}

		if (!expectedEnoughRegistersSysV3.Equals(managedEnoughRegistersSysV3))
		{
			Console.WriteLine("Managed EnoughRegistersSysV3 failed");
			ok = false;
		}

		return ok;
	}

	static bool EnoughRegistersSysV4Wrapper()
	{
		bool ok = true;

		DoubleAndByte expectedEnoughRegistersSysV4 = DoubleAndByte.Get();
		DoubleAndByte nativeEnoughRegistersSysV4 = EnoughRegistersSysV4(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, expectedEnoughRegistersSysV4);
		DoubleAndByte managedEnoughRegistersSysV4 = EnoughRegistersSysV4Managed(1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d, 7.0d, expectedEnoughRegistersSysV4);

		if (!expectedEnoughRegistersSysV4.Equals(nativeEnoughRegistersSysV4))
		{
			Console.WriteLine("Native EnoughRegistersSysV4 failed");
			ok = false;
		}

		if (!expectedEnoughRegistersSysV4.Equals(managedEnoughRegistersSysV4))
		{
			Console.WriteLine("Managed EnoughRegistersSysV4 failed");
			ok = false;
		}

		return ok;
	}

	[Fact]
	public static int TestEntryPoint()
	{
		var ok = true;
	
		if (!EchoSingleByteWrapper()) ok = false;
		if (!EchoSingleLongWrapper()) ok = false;
		if (!EchoSingleFloatWrapper()) ok = false;
		if (!EchoSingleDoubleWrapper()) ok = false;
		if (!EchoByteAndFloatWrapper()) ok = false;
		if (!EchoLongAndFloatWrapper()) ok = false;
		if (!EchoByteAndDoubleWrapper()) ok = false;
		if (!EchoDoubleAndByteWrapper()) ok = false;
		if (!EchoPointerAndByteWrapper()) ok = false;
		if (!EchoByteAndPointerWrapper()) ok = false;
		if (!EchoByteFloatAndPointerWrapper()) ok = false;
		if (!EchoPointerFloatAndByteWrapper()) ok = false;
		if (!EchoShortIntFloatIntPtrWrapper()) ok = false;
		if (!EchoTwoLongsWrapper()) ok = false;
		if (!EchoTwoFloatsWrapper()) ok = false;
		if (!EchoTwoDoublesWrapper()) ok = false;
		if (!EchoFourLongsWrapper()) ok = false;
		if (!EchoFourDoublesWrapper()) ok = false;
		if (!EchoInlineArray1Wrapper()) ok = false;
		if (!EchoInlineArray2Wrapper()) ok = false;
		if (!EchoInlineArray3Wrapper()) ok = false;
		if (!EchoInlineArray4Wrapper()) ok = false;
		if (!EchoInlineArray5Wrapper()) ok = false;
		if (!EchoInlineArray6Wrapper()) ok = false;
		if (!EchoNested1Wrapper()) ok = false;
		if (!EchoNested2Wrapper()) ok = false;
		if (!EchoNested3Wrapper()) ok = false;
		if (!EchoNested4Wrapper()) ok = false;
		if (!EchoNested5Wrapper()) ok = false;
		if (!EchoNested6Wrapper()) ok = false;
		if (!EchoNested7Wrapper()) ok = false;
		if (!EchoNested8Wrapper()) ok = false;
		if (!EchoNested9Wrapper()) ok = false;
		if (!NotEnoughRegistersSysV1Wrapper()) ok = false;
		if (!NotEnoughRegistersSysV2Wrapper()) ok = false;
		if (!NotEnoughRegistersSysV3Wrapper()) ok = false;
		if (!NotEnoughRegistersSysV4Wrapper()) ok = false;
		if (!NotEnoughRegistersSysV5Wrapper()) ok = false;
		if (!NotEnoughRegistersSysV6Wrapper()) ok = false;
		if (!EnoughRegistersSysV1Wrapper()) ok = false;
		if (!EnoughRegistersSysV2Wrapper()) ok = false;
		if (!EnoughRegistersSysV3Wrapper()) ok = false;
		if (!EnoughRegistersSysV4Wrapper()) ok = false;
		
		return ok ? 100 : -1;
	}
}
