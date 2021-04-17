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
#define _FUNCNAME "acoshf"
float FN_PROTOTYPE_REF(acoshf)(float x)
{

    unsigned int ux;
    double dx, r, rarg, t;

    static const unsigned int
        recrteps = 0x46000000; /* 1/sqrt(eps) = 4.09600000000000000000e+03 */

    static const double
        log2 = 6.93147180559945286227e-01;  /* 0x3fe62e42fefa39ef */

    GET_BITS_SP32(x, ux);

    if ((ux & EXPBITS_SP32) == EXPBITS_SP32)
    {
        /* x is either NaN or infinity */
        if (ux & MANTBITS_SP32)
        {
            /* x is NaN */
#ifdef WINDOWS
            return __amd_handle_errorf(_FUNCNAME, __amd_acosh, ux | 0x00400000, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0F, 1);
#else
            if (ux & QNAN_MASK_32)
                return __amd_handle_errorf(_FUNCNAME, __amd_acosh, ux | 0x00400000, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0F, 1);
            else
                return __amd_handle_errorf(_FUNCNAME, __amd_acosh, ux | 0x00400000, _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0F, 1);
#endif
        }
        else
        {
            /* x is infinity */
            if (ux & SIGNBIT_SP32)
                /* x is negative infinity. Return a NaN. */
                return __amd_handle_errorf(_FUNCNAME, __amd_acosh, INDEFBITPATT_SP32, _DOMAIN,
                    AMD_F_INVALID, EDOM, x, 0.0F, 1);
            else
                /* Return positive infinity with no signal */
                return x;
        }
    }
    else if ((ux & SIGNBIT_SP32) || (ux < 0x3f800000))
    {
        /* x is less than 1.0. Return a NaN. */
        return __amd_handle_errorf(_FUNCNAME, __amd_acosh, INDEFBITPATT_SP32, _DOMAIN,
            AMD_F_INVALID, EDOM, x, 0.0F, 1);
    }

    dx = x;

    if (ux > recrteps)
    {
        /* Arguments greater than 1/sqrt(epsilon) in magnitude are
           approximated by acoshf(x) = ln(2) + ln(x) */
        r = FN_PROTOTYPE(log)(dx) + log2;
    }
    else if (ux > 0x40000000)
    {
        /* 2.0 <= x <= 1/sqrt(epsilon) */
        /* acoshf for these arguments is approximated by
           acoshf(x) = ln(x + sqrt(x*x-1)) */
        rarg = dx * dx - 1.0;
        /* Use assembly instruction to compute r = sqrt(rarg); */
        ASMSQRT(rarg, r);
        rarg = r + dx;
        r = FN_PROTOTYPE(log)(rarg);
    }
    else
    {
        /* sqrt(epsilon) <= x <= 2.0 */
        t = dx - 1.0;
        rarg = 2.0 * t + t * t;
        ASMSQRT(rarg, r);  /* r = sqrt(rarg) */
        rarg = t + r;
        r = FN_PROTOTYPE(log1p)(rarg);
    }
    return (float)(r);
}
