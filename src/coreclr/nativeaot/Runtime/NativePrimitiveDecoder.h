// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C/C++ clone of NativePrimitiveDecoder.cs subset

class NativePrimitiveDecoder
{
public:
    static uint32_t ReadUnsigned(uint8_t* & p)
    {
        uint32_t value = 0;

        uint32_t val = *p;
        if ((val & 1) == 0)
        {
            value = (val >> 1);
            p += 1;
        }
        else if ((val & 2) == 0)
        {
            value = (val >> 2) |
                    (*(p + 1) << 6);
            p += 2;
        }
        else if ((val & 4) == 0)
        {
            value = (val >> 3) |
                    (*(p + 1) << 5) |
                    (*(p + 2) << 13);
            p += 3;
        }
        else if ((val & 8) == 0)
        {
            value = (val >> 4) |
                    (*(p + 1) << 4) |
                    (*(p + 2) << 12) |
                    (*(p + 3) << 20);
            p += 4;
        }
        else
        {
            value = *(p+1) | (*(p+2) << 8) | (*(p+3) << 16) | (*(p+4) << 24);
            p += 5;
        }

        return value;
    }

    static int32_t ReadInt32(uint8_t* & p)
    {
        int32_t value = *p | (*(p+1) << 8) | (*(p+2) << 16) | (*(p+3) << 24);
        p += 4;
        return value;
    }

    static uint32_t ReadUInt32(uint8_t* & p)
    {
        uint32_t value = *p | (*(p+1) << 8) | (*(p+2) << 16) | (*(p+3) << 24);
        p += 4;
        return value;
    }
};
