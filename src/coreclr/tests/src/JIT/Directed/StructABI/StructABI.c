// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdint.h>

#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#ifdef BIT64
#define __int64     long
#else // BIT64
#define __int64     long long
#endif // BIT64

#define __int32     int
#define __int16     short int
#define __int8      char        // assumes char is signed

#endif // _MSC_VER

struct SingleByte
{
	uint8_t Byte;
};

struct SingleLong
{
	uint64_t Long;
};

struct SingleFloat
{
	float Float;
};

struct SingleDouble
{
	double Double;
};

struct ByteAndFloat
{
	uint8_t Byte;
	float Float;
};

struct FloatAndByte
{
	float Float;
	uint8_t Byte;
};

struct LongAndFloat
{
	uint64_t Long;
	float Float;
};

struct ByteAndDouble
{
	uint8_t Byte;
	double Double;
};

struct DoubleAndByte
{
	double Double;
	uint8_t Byte;
};

struct PointerAndByte
{
	void* Pointer;
	uint8_t Byte;
};

struct ByteAndPointer
{
	uint8_t Byte;
	void* Pointer;
};

struct ByteFloatAndPointer
{
	uint8_t Byte;
	float Float;
	void* Pointer;
};

struct PointerFloatAndByte
{
	void* Pointer;
	float Float;
	uint8_t Byte;
};

struct ShortIntFloatIntPtr
{
	__int16 Short;
	__int32 Int;
	float Float;
	__int32* Pointer;
};

struct TwoLongs
{
    uint64_t Long1;
    uint64_t Long2;
};

struct TwoFloats
{
	float Float1;
	float Float2;
};

struct TwoDoubles
{
	double Double1;
	double Double2;
};

struct FourLongs
{
    uint64_t Long1;
    uint64_t Long2;
    uint64_t Long3;
    uint64_t Long4;
};

struct FourDoubles
{
    double Double1;
    double Double2;
    double Double3;
    double Double4;
};

struct InlineArray1
{
	uint8_t Array[16];
};

struct InlineArray2
{
	float Array[4];
};

struct InlineArray3
{
	float Array[3];
};

struct InlineArray4
{
	uint16_t Array[5];
};

struct InlineArray5
{
	uint8_t Array[9];
};

struct InlineArray6
{
	double Array[1];
};

struct Nested1
{
	struct LongAndFloat Field1;
	struct LongAndFloat Field2;
};

struct Nested2
{
	struct ByteAndFloat Field1;
	struct FloatAndByte Field2;
};

struct Nested3
{
	void* Field1;
	struct FloatAndByte Field2;
};

struct Nested4
{
	struct InlineArray5 Field1;
	uint16_t Field2;
};

struct Nested5
{
	uint16_t Field1;
	struct InlineArray5 Field2;
};

struct Nested6
{
	struct InlineArray4 Field1;
	uint32_t Field2;
};

struct Nested7
{
	uint32_t Field1;
	struct InlineArray4 Field2;
};

struct Nested8
{
	struct InlineArray4 Field1;
	uint16_t Field2;
};

struct Nested9
{
	uint16_t Field1;
	struct InlineArray4 Field2;
};

DLLEXPORT struct SingleByte EchoSingleByte(struct SingleByte value)
{
	return value;
}

DLLEXPORT struct SingleLong EchoSingleLong(struct SingleLong value)
{
	return value;
}

DLLEXPORT struct SingleFloat EchoSingleFloat(struct SingleFloat value)
{
	return value;
}

DLLEXPORT struct SingleDouble EchoSingleDouble(struct SingleDouble value)
{
	return value;
}

DLLEXPORT struct ByteAndFloat EchoByteAndFloat(struct ByteAndFloat value)
{
	return value;
}

DLLEXPORT struct LongAndFloat EchoLongAndFloat(struct LongAndFloat value)
{
	return value;
}

DLLEXPORT struct ByteAndDouble EchoByteAndDouble(struct ByteAndDouble value)
{
	return value;
}

DLLEXPORT struct DoubleAndByte EchoDoubleAndByte(struct DoubleAndByte value)
{
	return value;
}

DLLEXPORT struct PointerAndByte EchoPointerAndByte(struct PointerAndByte value)
{
	return value;
}

DLLEXPORT struct ByteAndPointer EchoByteAndPointer(struct ByteAndPointer value)
{
	return value;
}

DLLEXPORT struct ByteFloatAndPointer EchoByteFloatAndPointer(struct ByteFloatAndPointer value)
{
	return value;
}

DLLEXPORT struct PointerFloatAndByte EchoPointerFloatAndByte(struct PointerFloatAndByte value)
{
	return value;
}

DLLEXPORT struct ShortIntFloatIntPtr EchoShortIntFloatIntPtr(struct ShortIntFloatIntPtr value)
{
	return value;
}

DLLEXPORT struct TwoLongs EchoTwoLongs(struct TwoLongs value)
{
	return value;
}

DLLEXPORT struct TwoFloats EchoTwoFloats(struct TwoFloats value)
{
	return value;
}

DLLEXPORT struct TwoDoubles EchoTwoDoubles(struct TwoDoubles value)
{
	return value;
}

DLLEXPORT struct FourLongs EchoFourLongs(struct FourLongs value)
{
	return value;
}

DLLEXPORT struct FourDoubles EchoFourDoubles(struct FourDoubles value)
{
	return value;
}

DLLEXPORT struct InlineArray1 EchoInlineArray1(struct InlineArray1 value)
{
	return value;
}

DLLEXPORT struct InlineArray2 EchoInlineArray2(struct InlineArray2 value)
{
	return value;
}

DLLEXPORT struct InlineArray3 EchoInlineArray3(struct InlineArray3 value)
{
	return value;
}

DLLEXPORT struct InlineArray4 EchoInlineArray4(struct InlineArray4 value)
{
	return value;
}

DLLEXPORT struct InlineArray5 EchoInlineArray5(struct InlineArray5 value)
{
	return value;
}

DLLEXPORT struct InlineArray6 EchoInlineArray6(struct InlineArray6 value)
{
	return value;
}

DLLEXPORT struct Nested1 EchoNested1(struct Nested1 value)
{
	return value;
}

DLLEXPORT struct Nested2 EchoNested2(struct Nested2 value)
{
	return value;
}

DLLEXPORT struct Nested3 EchoNested3(struct Nested3 value)
{
	return value;
}

DLLEXPORT struct Nested4 EchoNested4(struct Nested4 value)
{
	return value;
}

DLLEXPORT struct Nested5 EchoNested5(struct Nested5 value)
{
	return value;
}

DLLEXPORT struct Nested6 EchoNested6(struct Nested6 value)
{
	return value;
}

DLLEXPORT struct Nested7 EchoNested7(struct Nested7 value)
{
	return value;
}

DLLEXPORT struct Nested8 EchoNested8(struct Nested8 value)
{
	return value;
}

DLLEXPORT struct Nested9 EchoNested9(struct Nested9 value)
{
	return value;
}

DLLEXPORT struct TwoLongs NotEnoughRegistersSysV1(uint64_t a, uint64_t b, uint64_t c, uint64_t d, uint64_t e, uint64_t f, struct TwoLongs value)
{
    return value;
}

DLLEXPORT struct TwoLongs NotEnoughRegistersSysV2(uint64_t a, uint64_t b, uint64_t c, uint64_t d, uint64_t e, struct TwoLongs value)
{
    return value;
}

DLLEXPORT struct DoubleAndByte NotEnoughRegistersSysV3(uint64_t a, uint64_t b, uint64_t c, uint64_t d, uint64_t e, uint64_t f, struct DoubleAndByte value)
{
    return value;
}

DLLEXPORT struct TwoDoubles NotEnoughRegistersSysV4(double a, double b, double c, double d, double e, double f, double g, double h, struct TwoDoubles value)
{
    return value;
}

DLLEXPORT struct TwoDoubles NotEnoughRegistersSysV5(double a, double b, double c, double d, double e, double f, double g, struct TwoDoubles value)
{
    return value;
}

DLLEXPORT struct DoubleAndByte NotEnoughRegistersSysV6(double a, double b, double c, double d, double e, double f, double g, double h, struct DoubleAndByte value)
{
    return value;
}

DLLEXPORT struct TwoDoubles EnoughRegistersSysV1(uint64_t a, uint64_t b, uint64_t c, uint64_t d, uint64_t e, uint64_t f, struct TwoDoubles value)
{
    return value;
}

DLLEXPORT struct DoubleAndByte EnoughRegistersSysV2(uint64_t a, uint64_t b, uint64_t c, uint64_t d, uint64_t e, struct DoubleAndByte value)
{
    return value;
}

DLLEXPORT struct TwoLongs EnoughRegistersSysV3(double a, double b, double c, double d, double e, double f, double g, double h, struct TwoLongs value)
{
    return value;
}

DLLEXPORT struct DoubleAndByte EnoughRegistersSysV4(double a, double b, double c, double d, double e, double f, double g, struct DoubleAndByte value)
{
    return value;
}
