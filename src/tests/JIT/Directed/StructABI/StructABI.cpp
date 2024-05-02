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
struct Sixteen
{
	T e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15, e16;
};

struct FloatEmptyMegabyteInt
{
	float FieldF;
	Sixteen<Sixteen<Sixteen<Sixteen<Sixteen<Empty>>>>> FieldEmptyMegabyte;
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

DLLEXPORT LongEmptyDouble EchoLongEmptyDoubleRiscV(LongEmptyDouble a0_fa0)
{
	return a0_fa0;
}

DLLEXPORT LongEmptyDouble EchoLongEmptyDoubleByImplicitRefRiscV(
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, float fa7, LongEmptyDouble byRef)
{
	return byRef;
}

DLLEXPORT NestedEmptyFloatDouble EchoNestedEmptyFloatDoubleRiscV(NestedEmptyFloatDouble fa0_fa1)
{
	return fa0_fa1;
}

DLLEXPORT NestedEmptyFloatDouble EchoNestedEmptyFloatDoubleInIntegerRegsRiscV(
	float fa0, float fa1, float fa2, float fa3, float fa4, float fa5, float fa6, NestedEmptyFloatDouble a0_a1)
{
	return a0_a1;
}

DLLEXPORT EmptyIntAndFloat EchoEmptyIntAndFloatRiscV(EmptyIntAndFloat a0_fa0)
{
	return a0_fa0;
}

DLLEXPORT LongEmptyAndFloat EchoLongEmptyAndFloatRiscV(LongEmptyAndFloat a0_fa0)
{
	return a0_fa0;
}

DLLEXPORT ArrayOfEmptiesFloatDouble EchoArrayOfEmptiesFloatDoubleInIntegerRegsRiscV(ArrayOfEmptiesFloatDouble a0_a1)
{
	return a0_a1;
}

DLLEXPORT FloatEmptyMegabyteInt EchoFloatEmptyMegabyteIntRiscV(FloatEmptyMegabyteInt fa0_a0)
{
	return fa0_a0;
}

DLLEXPORT PackedEmptyFloatLong EchoPackedEmptyFloatLongRiscV(PackedEmptyFloatLong fa0_a0)
{
	return fa0_a0;
}

DLLEXPORT ExplicitFloatLong EchoExplicitFloatLongRiscV(ExplicitFloatLong fa0_a0)
{
	return fa0_a0;
}

} // extern "C"
