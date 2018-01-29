// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: fp.h
//

//

#ifndef _FP_H
#define _FP_H

#include <clrtypes.h>

struct FPSINGLE {
    #if BIGENDIAN
        unsigned int sign: 1;
        unsigned int exp: 8;
        unsigned int mant: 23;
    #else
        unsigned int mant: 23;
        unsigned int exp: 8;
        unsigned int sign: 1;
    #endif
};
    
struct FPDOUBLE {
    #if BIGENDIAN
        unsigned int sign: 1;
        unsigned int exp: 11;
        unsigned int mantHi: 20;
        unsigned int mantLo;
    #else
        unsigned int mantLo;
        unsigned int mantHi: 20;
        unsigned int exp: 11;
        unsigned int sign: 1;
    #endif
};

static void ExtractFractionAndBiasedExponent(double value, UINT64* f, int* e)
{
    if (((FPDOUBLE*)&value)->exp != 0)
    {
        // For normalized value, according to https://en.wikipedia.org/wiki/Double-precision_floating-point_format
        // value = 1.fraction * 2^(exp - 1023)
        //       = (1 + mantissa / 2^52) * 2^(exp - 1023)
        //       = (2^52 + mantissa) * 2^(exp - 1023 - 52)
        //
        // So f = (2^52 + mantissa), e = exp - 1075;
        *f = ((UINT64)(((FPDOUBLE*)&value)->mantHi) << 32) | ((FPDOUBLE*)&value)->mantLo + ((UINT64)1 << 52);
        *e = ((FPDOUBLE*)&value)->exp - 1075;
    }
    else
    {
        // For denormalized value, according to https://en.wikipedia.org/wiki/Double-precision_floating-point_format
        // value = 0.fraction * 2^(1 - 1023)
        //       = (mantissa / 2^52) * 2^(-1022)
        //       = mantissa * 2^(-1022 - 52)
        //       = mantissa * 2^(-1074)
        // So f = mantissa, e = -1074
        *f = ((UINT64)(((FPDOUBLE*)&value)->mantHi) << 32) | ((FPDOUBLE*)&value)->mantLo;
        *e = -1074;
    }
}

#endif // _FP_H