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
	int32_t FieldI0;
	Empty FieldE0;
};

extern "C" DLLEXPORT IntEmpty EchoIntEmptySysV(int i0, IntEmpty val)
{
	return val;
}


struct IntEmptyPair
{
	IntEmpty FieldIE0;
	IntEmpty FieldIE1;
};

extern "C" DLLEXPORT IntEmptyPair EchoIntEmptyPairSysV(int i0, IntEmptyPair val)
{
	return val;
}


struct EmptyFloatIntInt
{
	Empty FieldE0;
	float FieldF0;
	int32_t FieldI0;
	int32_t FieldI1;
};

extern "C" DLLEXPORT EmptyFloatIntInt EchoEmptyFloatIntIntSysV(int i0, float f0, EmptyFloatIntInt val)
{
	return val;
}


struct FloatFloatEmptyFloat
{
	float FieldF0;
	float FieldF1;
	Empty FieldE0;
	float FieldF2;
};

extern "C" DLLEXPORT FloatFloatEmptyFloat EchoFloatFloatEmptyFloatSysV(float f0, FloatFloatEmptyFloat val)
{
	return val;
}

