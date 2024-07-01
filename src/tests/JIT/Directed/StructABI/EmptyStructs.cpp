// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdint.h>
#include <stddef.h>

#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif // _MSC_VER

struct Empty
{
};
static_assert(sizeof(Empty) == 1, "Empty struct must be sized like in .NET");


struct IntEmpty
{
	int32_t Int0;
	Empty Empty0;
};

extern "C" DLLEXPORT IntEmpty Echo_IntEmpty_SysV(int i0, float f0, IntEmpty val, int i1, float f1)
{
	val.Int0 += i1 + (int)f1;
	return val;
}


struct IntEmptyPair
{
	IntEmpty IntEmpty0;
	IntEmpty IntEmpty1;
};

extern "C" DLLEXPORT IntEmptyPair Echo_IntEmptyPair_SysV(int i0, float f0, IntEmptyPair val, int i1, float f1)
{
	val.IntEmpty0.Int0 += i1 + (int)f1;
	return val;
}


struct EmptyFloatIntInt
{
	Empty Empty0;
	float Float0;
	int32_t Int0;
	int32_t Int1;
};

extern "C" DLLEXPORT EmptyFloatIntInt Echo_EmptyFloatIntInt_SysV(
	int i0, float f0, EmptyFloatIntInt val, int i1, float f1)
{
	val.Float0 += (float)i1 + f1;
	return val;
}


struct FloatFloatEmptyFloat
{
	float Float0;
	float Float1;
	Empty Empty0;
	float Float2;
};

extern "C" DLLEXPORT FloatFloatEmptyFloat Echo_FloatFloatEmptyFloat_SysV(
	int i0, float f0, FloatFloatEmptyFloat val, int i1, float f1)
{
	val.Float2 += (float)i1 + f1;
	return val;
}


template<typename T>
struct Eight
{
	T E0, E1, E2, E3, E4, E5, E6, E7;
};

struct Empty8Float
{
	Eight<Empty> EightEmpty0;
	float Float0;
};

extern "C" DLLEXPORT Empty8Float Echo_Empty8Float_RiscV(
	int a0, float fa0, Empty8Float fa1, int a1, float fa2)
{
	fa1.Float0 += (float)a1 + fa2;
	return fa1;
}

extern "C" DLLEXPORT Empty8Float Echo_Empty8Float_InIntegerRegs_RiscV(
	int a0,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	Empty8Float a1_a2, int a3, float a4)
{
	a1_a2.Float0 += (float)a3 + a4;
	return a1_a2;
}

extern "C" DLLEXPORT Empty8Float Echo_Empty8Float_Split_RiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	Empty8Float a7_stack0, int stack1, float stack2)
{
	a7_stack0.Float0 += (float)stack1 + stack2;
	return a7_stack0;
}

extern "C" DLLEXPORT Empty8Float Echo_Empty8Float_OnStack_RiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	Empty8Float stack0_stack1, int stack2, float stack3)
{
	stack0_stack1.Float0 += (float)stack2 + stack3;
	return stack0_stack1;
}


struct FloatEmpty8Float
{
	float Float0;
	Eight<Empty> EightEmpty0;
	float Float1;
};

extern "C" DLLEXPORT FloatEmpty8Float Echo_FloatEmpty8Float_RiscV(
	int a0, float fa0, FloatEmpty8Float fa1_fa2, int a1, float fa2)
{
	fa1_fa2.Float1 += (float)a1 + fa2;
	return fa1_fa2;
}

extern "C" DLLEXPORT FloatEmpty8Float Echo_FloatEmpty8Float_InIntegerRegs_RiscV(
	int a0,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	FloatEmpty8Float a1_a2, int a3, float a4)
{
	a1_a2.Float0 += (float)a3 + a4;
	return a1_a2;
}

extern "C" DLLEXPORT FloatEmpty8Float Echo_FloatEmpty8Float_Split_RiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	FloatEmpty8Float a7_stack0, int stack1, float stack2)
{
	a7_stack0.Float0 += (float)stack1 + stack2;
	return a7_stack0;
}

extern "C" DLLEXPORT FloatEmpty8Float Echo_FloatEmpty8Float_OnStack_RiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	FloatEmpty8Float stack0_stack1, int stack2, float stack3)
{
	stack0_stack1.Float0 += (float)stack2 + stack3;
	return stack0_stack1;
}


struct FloatEmptyShort
{
	float Float0;
	Empty Empty0;
	short Short0;
};

extern "C" DLLEXPORT FloatEmptyShort Echo_FloatEmptyShort_RiscV(
	int a0, float fa0, FloatEmptyShort fa1_a1, int a1, float fa2)
{
	fa1_a1.Short0 += (short)(a1 + (int)fa2);
	return fa1_a1;
}

extern "C" DLLEXPORT FloatEmptyShort Echo_FloatEmptyShort_InIntegerRegs_RiscV(
	int a0,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	FloatEmptyShort a1_a2, int a3, float a4)
{
	a1_a2.Short0 += (short)(a3 + (int)a4);
	return a1_a2;
}

extern "C" DLLEXPORT FloatEmptyShort Echo_FloatEmptyShort_OnStack_RiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	FloatEmptyShort stack0, int stack1, float stack2)
{
	stack0.Short0 += (short)(stack1 + (int)stack2);
	return stack0;
}

struct EmptyFloatEmpty5Sbyte
{
	Empty Empty0;
	float Float0;
	Empty Empty1, Empty2, Empty3, Empty4, Empty5;
	int8_t Sbyte0;
};

extern "C" DLLEXPORT EmptyFloatEmpty5Sbyte Echo_EmptyFloatEmpty5Sbyte_RiscV(int a0, float fa0,
	EmptyFloatEmpty5Sbyte fa1_a1, int a2, float fa2)
{
	fa1_a1.Float0 += (float)a2 + fa2;
	return fa1_a1;
}


struct EmptyFloatEmpty5Byte
{
	Empty Empty0;
	float Float0;
	Empty Empty1, Empty2, Empty3, Empty4, Empty5;
	int8_t Byte0;
};

extern "C" DLLEXPORT EmptyFloatEmpty5Byte Echo_EmptyFloatEmpty5Byte_RiscV(int a0, float fa0,
	EmptyFloatEmpty5Byte fa1_a1, int a2, float fa2)
{
	fa1_a1.Float0 += (float)a2 + fa2;
	return fa1_a1;
}

extern "C" DLLEXPORT EmptyFloatEmpty5Byte Echo_EmptyFloatEmpty5Byte_InIntegerRegs_RiscV(
	int a0,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	EmptyFloatEmpty5Byte a1_a2, int a3, float a4)
{
	a1_a2.Float0 += (float)a3 + a4;
	return a1_a2;
}

extern "C" DLLEXPORT EmptyFloatEmpty5Byte Echo_EmptyFloatEmpty5Byte_Split_RiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	EmptyFloatEmpty5Byte a7_stack0, int stack1, float stack2)
{
	a7_stack0.Float0 += (float)stack1 + stack2;
	return a7_stack0;
}

extern "C" DLLEXPORT EmptyFloatEmpty5Byte Echo_EmptyFloatEmpty5Byte_OnStack_RiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	EmptyFloatEmpty5Byte stack0_stack1, int stack2, float stack3)
{
	stack0_stack1.Float0 += (float)stack2 + stack3;
	return stack0_stack1;
}


struct NestedEmpty
{
	struct InnerEmpty
	{
		Empty Empty0;
	} InnerEmpty0;
};
static_assert(sizeof(NestedEmpty) == 1, "Nested empty struct must be sized like in .NET");

struct DoubleFloatNestedEmpty
{
	double Double0;
	float Float0;
	NestedEmpty NestedEmpty0;
};

extern "C" DLLEXPORT DoubleFloatNestedEmpty Echo_DoubleFloatNestedEmpty_RiscV(int a0, float fa0,
	DoubleFloatNestedEmpty fa1_fa2, int a1, float fa3)
{
	fa1_fa2.Float0 += (float)a1 + fa3;
	return fa1_fa2;
}

extern "C" DLLEXPORT DoubleFloatNestedEmpty Echo_DoubleFloatNestedEmpty_InIntegerRegs_RiscV(
	int a0,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6,
	DoubleFloatNestedEmpty a1_a2, int a3, float fa7)
{
	return a1_a2;
	a1_a2.Float0 += (float)a3 + fa7;
}


struct EmptyUshortAndDouble
{
	struct EmptyUshort
	{
		Empty Empty0;
		uint16_t Ushort0;
	};
	EmptyUshort EmptyUshort0;
	double Double0;
};

extern "C" DLLEXPORT EmptyUshortAndDouble Echo_EmptyUshortAndDouble_RiscV(int a0, float fa0,
	EmptyUshortAndDouble a1_fa1, int a2, double fa2)
{
	a1_fa1.Double0 += (double)a2 + fa2;
	return a1_fa1;
}


#pragma pack(push, 1)
struct PackedEmptyFloatLong
{
	Empty Empty0;
	float Float0;
	int64_t Long0;
};
#pragma pack(pop)
static_assert(sizeof(PackedEmptyFloatLong) == 13, "");

extern "C" DLLEXPORT PackedEmptyFloatLong Echo_PackedEmptyFloatLong_RiscV(int a0, float fa0,
	PackedEmptyFloatLong fa1_a1, int a2, float fa2)
{
	fa1_a1.Float0 += (float)a2 + fa2;
	return fa1_a1;
}

extern "C" DLLEXPORT PackedEmptyFloatLong Echo_PackedEmptyFloatLong_InIntegerRegs_RiscV(
	int a0,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	PackedEmptyFloatLong a1_a2, int a3, float a4)
{
	a1_a2.Float0 += (float)a3 + a4;
	return a1_a2;
}

extern "C" DLLEXPORT PackedEmptyFloatLong Echo_PackedEmptyFloatLong_Split_RiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	PackedEmptyFloatLong a7_stack0, int stack1, float stack2)
{
	a7_stack0.Float0 += (float)stack1 + stack2;
	return a7_stack0;
}

extern "C" DLLEXPORT PackedEmptyFloatLong Echo_PackedEmptyFloatLong_OnStack_RiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7,
	PackedEmptyFloatLong stack0_stack1, int stack2, float stack3)
{
	stack0_stack1.Float0 += (float)stack2 + stack3;
	return stack0_stack1;
}
