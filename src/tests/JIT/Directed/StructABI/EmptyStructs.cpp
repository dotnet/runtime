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

extern "C" DLLEXPORT IntEmpty EchoIntEmptySysV(int i0, IntEmpty val)
{
	return val;
}


struct IntEmptyPair
{
	IntEmpty IntEmpty0;
	IntEmpty IntEmpty1;
};

extern "C" DLLEXPORT IntEmptyPair EchoIntEmptyPairSysV(int i0, IntEmptyPair val)
{
	return val;
}


struct EmptyFloatIntInt
{
	Empty Empty0;
	float Float0;
	int32_t Int0;
	int32_t Int1;
};

extern "C" DLLEXPORT EmptyFloatIntInt EchoEmptyFloatIntIntSysV(int i0, float f0, EmptyFloatIntInt val)
{
	return val;
}


struct FloatFloatEmptyFloat
{
	float Float0;
	float Float1;
	Empty Empty0;
	float Float2;
};

extern "C" DLLEXPORT FloatFloatEmptyFloat EchoFloatFloatEmptyFloatSysV(float f0, FloatFloatEmptyFloat val)
{
	return val;
}

