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

struct Empty8Float
{
	Empty e0, e1, e2, e3, e4, e5, e6, e7;
	float FieldF;
};

struct EmptyFloatEmpty5Byte
{
	Empty e;
	float FieldF;
	Empty e0, e1, e2, e3, e4;
	int8_t FieldB;
};

struct EmptyFloatEmpty5UByte
{
	Empty e;
	float FieldF;
	Empty e0, e1, e2, e3, e4;
	uint8_t FieldB;
};

struct LongEmptyDouble
{
	int64_t FieldL;
	Empty FieldE;
	double FieldD;
};

struct NestedEmpty
{
	struct InnerEmpty
	{
		Empty e;
	} e;
};
static_assert(sizeof(NestedEmpty) == 1, "Nested empty struct must be sized like in .NET");

struct NestedEmptyFloatDouble
{
	NestedEmpty FieldNE;
	float FieldF;
	double FieldD;
};

struct EmptyIntAndFloat
{	
	struct EmptyInt
	{
		Empty FieldE;
		int32_t FieldI;
	};
	EmptyInt FieldEI;
	float FieldF;
};

struct LongEmptyAndFloat
{
	struct LongEmpty
	{
		int64_t FieldL;
		Empty FieldE;
	};
	LongEmpty FieldLE;
	float FieldF;
};

struct ArrayOfEmpties
{
	Empty e[1];
};

struct ArrayOfEmptiesFloatDouble
{
	ArrayOfEmpties FieldAoE;
	float FieldF;
	double FieldD;
};

template<typename T>
struct Eight
{
	T e1, e2, e3, e4, e5, e6, e7, e8;
};

struct FloatEmpty32kInt
{
	float FieldF;
	Eight<Eight<Eight<Eight<Eight<Empty>>>>> FieldEmpty32k;
	int32_t FieldI;
};

#pragma pack(push, 1)
struct PackedEmptyFloatLong
{
	Empty FieldE;
	float FieldF;
	int64_t FieldL;
};
#pragma pack(pop)

struct ExplicitFloatLong
{
	PackedEmptyFloatLong s;
};
static_assert(offsetof(ExplicitFloatLong, s.FieldE) == 0, "");
static_assert(offsetof(ExplicitFloatLong, s.FieldF) == 1, "");
static_assert(offsetof(ExplicitFloatLong, s.FieldL) == 5, "");

extern "C"
{

DLLEXPORT Empty8Float EchoEmpty8FloatRiscV(int a0, float fa0, Empty8Float fa1)
{
	return fa1;
}

DLLEXPORT Empty8Float EchoEmpty8FloatInIntegerRegsRiscV(
	int a0,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, Empty8Float a1_a2)
{
	return a1_a2;
}

DLLEXPORT Empty8Float EchoEmpty8FloatSplitRiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, Empty8Float a7_stack0)
{
	return a7_stack0;
}

DLLEXPORT Empty8Float EchoEmpty8FloatOnStackRiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, Empty8Float stack0_stack1)
{
	return stack0_stack1;
}

DLLEXPORT EmptyFloatEmpty5Byte EchoEmptyFloatEmpty5ByteRiscV(int a0, float fa0, EmptyFloatEmpty5Byte fa1_a1)
{
	return fa1_a1;
}

DLLEXPORT EmptyFloatEmpty5UByte EchoEmptyFloatEmpty5UByteRiscV(int a0, float fa0, EmptyFloatEmpty5UByte fa1_a1)
{
	return fa1_a1;
}

DLLEXPORT EmptyFloatEmpty5Byte EchoEmptyFloatEmpty5ByteInIntegerRegsRiscV(
	int a0,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, EmptyFloatEmpty5Byte a1_a2)
{
	return a1_a2;
}

DLLEXPORT EmptyFloatEmpty5Byte EchoEmptyFloatEmpty5ByteSplitRiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, EmptyFloatEmpty5Byte a7_stack0)
{
	return a7_stack0;
}

DLLEXPORT EmptyFloatEmpty5Byte EchoEmptyFloatEmpty5ByteOnStackRiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, EmptyFloatEmpty5Byte stack0_stack1)
{
	return stack0_stack1;
}

DLLEXPORT LongEmptyDouble EchoLongEmptyDoubleRiscV(int a0, float fa0, LongEmptyDouble a1_fa1)
{
	return a1_fa1;
}

DLLEXPORT LongEmptyDouble EchoLongEmptyDoubleByImplicitRefRiscV(
	int a0, float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, LongEmptyDouble a1)
{
	return a1;
}

DLLEXPORT NestedEmptyFloatDouble EchoNestedEmptyFloatDoubleRiscV(int a0, float fa0, NestedEmptyFloatDouble fa1_fa2)
{
	return fa1_fa2;
}

DLLEXPORT NestedEmptyFloatDouble EchoNestedEmptyFloatDoubleInIntegerRegsRiscV(
	int a0, float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, NestedEmptyFloatDouble a1_a2)
{
	return a1_a2;
}

DLLEXPORT EmptyIntAndFloat EchoEmptyIntAndFloatRiscV(int a0, float fa0, EmptyIntAndFloat a1_fa1)
{
	return a1_fa1;
}

DLLEXPORT LongEmptyAndFloat EchoLongEmptyAndFloatRiscV(int a0, float fa0, LongEmptyAndFloat a1_fa1)
{
	return a1_fa1;
}

DLLEXPORT ArrayOfEmptiesFloatDouble EchoArrayOfEmptiesFloatDoubleRiscV(int a0, float fa0, ArrayOfEmptiesFloatDouble a1_a2)
{
	return a1_a2;
}

DLLEXPORT FloatEmpty32kInt EchoFloatEmpty32kIntRiscV(int a0, float fa0, FloatEmpty32kInt fa1_a1)
{
	return fa1_a1;
}

DLLEXPORT PackedEmptyFloatLong EchoPackedEmptyFloatLongRiscV(int a0, float fa0, PackedEmptyFloatLong fa1_a1)
{
	return fa1_a1;
}

DLLEXPORT PackedEmptyFloatLong EchoPackedEmptyFloatLongInIntegerRegsRiscV(
	int a0,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, PackedEmptyFloatLong a1_a2)
{
	return a1_a2;
}

DLLEXPORT PackedEmptyFloatLong EchoPackedEmptyFloatLongSplitRiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, PackedEmptyFloatLong a7_stack0)
{
	return a7_stack0;
}

DLLEXPORT PackedEmptyFloatLong EchoPackedEmptyFloatLongOnStackRiscV(
	int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7,
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, PackedEmptyFloatLong stack0_stack1)
{
	return stack0_stack1;
}

DLLEXPORT ExplicitFloatLong EchoExplicitFloatLongRiscV(int a0, float fa0, ExplicitFloatLong fa1_a1)
{
	return fa1_a1;
}

} // extern "C"
