// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
class VarInt
{
public:
    static uint32_t ReadUnsigned(PTR_uint8_t & pbEncoding)
    {
        uintptr_t lengthBits = *pbEncoding & 0x0F;
        size_t  negLength = s_negLengthTab[lengthBits];
        uintptr_t shift = s_shiftTab[lengthBits];
        uint32_t result = *(PTR_uint32_t)(pbEncoding - negLength - 4);

        result >>= shift;
        pbEncoding -= negLength;

        return result;
    }

private:
    static int8_t s_negLengthTab[16];
    static uint8_t s_shiftTab[16];
};

#ifndef __GNUC__
__declspec(selectany)
#endif
int8_t
#ifdef __GNUC__
__attribute__((weak))
#endif
VarInt::s_negLengthTab[16] =
{
    -1,    // 0
    -2,    // 1
    -1,    // 2
    -3,    // 3

    -1,    // 4
    -2,    // 5
    -1,    // 6
    -4,    // 7

    -1,    // 8
    -2,    // 9
    -1,    // 10
    -3,    // 11

    -1,    // 12
    -2,    // 13
    -1,    // 14
    -5,    // 15
};

#ifndef __GNUC__
__declspec(selectany)
#endif
uint8_t
#ifdef __GNUC__
__attribute__((weak))
#endif
VarInt::s_shiftTab[16] =
{
    32-7*1,    // 0
    32-7*2,    // 1
    32-7*1,    // 2
    32-7*3,    // 3

    32-7*1,    // 4
    32-7*2,    // 5
    32-7*1,    // 6
    32-7*4,    // 7

    32-7*1,    // 8
    32-7*2,    // 9
    32-7*1,    // 10
    32-7*3,    // 11

    32-7*1,    // 12
    32-7*2,    // 13
    32-7*1,    // 14
    0,         // 15
};
