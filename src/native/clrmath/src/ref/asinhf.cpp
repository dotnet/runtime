/*
 * Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software without
 *    specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
 * OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 *
 */

#include "clrmath.h"
#include "libm/libm_util.h"
#include "libm/libm_special.h"

#undef _FUNCNAME
#define _FUNCNAME "asinhf"
float FN_PROTOTYPE_REF(asinhf)(float x)
{

    double dx;
    unsigned int ux, ax, xneg;
    double absx, r, rarg, t, poly;

    static const unsigned int
        rteps = 0x39800000,    /* sqrt(eps) = 2.44140625000000000000e-04 */
        recrteps = 0x46000000; /* 1/rteps = 4.09600000000000000000e+03 */

    static const double
        log2 = 6.93147180559945286227e-01;  /* 0x3fe62e42fefa39ef */

    GET_BITS_SP32(x, ux);
    ax = ux & ~SIGNBIT_SP32;
    xneg = ux & SIGNBIT_SP32;

    if ((ux & EXPBITS_SP32) == EXPBITS_SP32)
    {
        /* x is either NaN or infinity */
        if (ux & MANTBITS_SP32)
        {
            /* x is NaN */
#ifdef WINDOWS
            return __amd_handle_errorf(_FUNCNAME, __amd_asinh, ux | 0x00400000, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0F, 1);
#else
            if (ux & QNAN_MASK_32)
                return __amd_handle_errorf(_FUNCNAME, __amd_asinh, ux | 0x00400000, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0F, 1);
            else
                return __amd_handle_errorf(_FUNCNAME, __amd_asinh, ux | 0x00400000, _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0F, 1);
#endif
        }
        else
        {
            /* x is infinity. Return the same infinity. */
            return x;
        }
    }
    else if (ax < rteps) /* abs(x) < sqrt(epsilon) */
    {
        if (ax == 0x00000000)
        {
            /* x is +/-zero. Return the same zero. */
            return x;
        }
        else
        {
            /* Tiny arguments approximated by asinhf(x) = x
               - avoid slow operations on denormalized numbers */
#ifdef WINDOWS
            return x; //return valf_with_flags(x,AMD_F_INEXACT);
#else
            return __amd_handle_errorf(_FUNCNAME, __amd_asinh, ux, _UNDERFLOW, AMD_F_UNDERFLOW | AMD_F_INEXACT, ERANGE, x, 0.0F, 1);
#endif
        }
    }

    dx = x;
    if (xneg)
        absx = -dx;
    else
        absx = dx;

    if (ax <= 0x40800000) /* abs(x) <= 4.0 */
    {
        /* Arguments less than 4.0 in magnitude are
           approximated by [4,4] minimax polynomials
        */
        t = dx * dx;
        if (ax <= 0x40000000) /* abs(x) <= 2 */
            poly =
            (-0.1152965835871758072e-1 +
                (-0.1480204186473758321e-1 +
                    (-0.5063201055468483248e-2 +
                        (-0.4162727710583425360e-3 -
                            0.1177198915954942694e-5 * t) * t) * t) * t) /
            (0.6917795026025976739e-1 +
                (0.1199423176003939087e+0 +
                    (0.6582362487198468066e-1 +
                        (0.1260024978680227945e-1 +
                            0.6284381367285534560e-3 * t) * t) * t) * t);
        else
            poly =
            (-0.185462290695578589e-2 +
                (-0.113672533502734019e-2 +
                    (-0.142208387300570402e-3 +
                        (-0.339546014993079977e-5 -
                            0.151054665394480990e-8 * t) * t) * t) * t) /
            (0.111486158580024771e-1 +
                (0.117782437980439561e-1 +
                    (0.325903773532674833e-2 +
                        (0.255902049924065424e-3 +
                            0.434150786948890837e-5 * t) * t) * t) * t);
        return (float)(dx + dx * t * poly);
    }
    else
    {
        /* abs(x) > 4.0 */
        if (ax > recrteps)
        {
            /* Arguments greater than 1/sqrt(epsilon) in magnitude are
               approximated by asinhf(x) = ln(2) + ln(abs(x)), with sign of x */
            r = FN_PROTOTYPE(log)(absx) + log2;
        }
        else
        {
            rarg = absx * absx + 1.0;
            /* Arguments such that 4.0 <= abs(x) <= 1/sqrt(epsilon) are
               approximated by
                 asinhf(x) = ln(abs(x) + sqrt(x*x+1))
               with the sign of x (see Abramowitz and Stegun 4.6.20) */
               /* Use assembly instruction to compute r = sqrt(rarg); */
            ASMSQRT(rarg, r);
            r += absx;
            r = FN_PROTOTYPE(log)(r);
        }
        if (xneg)
            return (float)(-r);
        else
            return (float)r;
    }
}
