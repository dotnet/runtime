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
#define _FUNCNAME "atanhf"
float FN_PROTOTYPE_REF(atanhf)(float x)
{

    double dx;
    unsigned int ux, ax;
    double r, t, poly;

    GET_BITS_SP32(x, ux);
    ax = ux & ~SIGNBIT_SP32;

    if ((ux & EXPBITS_SP32) == EXPBITS_SP32)
    {
        /* x is either NaN or infinity */
        if (ux & MANTBITS_SP32)
        {
            /* x is NaN */
#ifdef WINDOWS
            return __amd_handle_errorf(_FUNCNAME, __amd_tanh, ux | 0x00400000, _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0F, 1);
#else
            if (ux & QNAN_MASK_32)
                return __amd_handle_errorf(_FUNCNAME, __amd_tanh, ux | 0x00400000, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0F, 1);
            else
                return __amd_handle_errorf(_FUNCNAME, __amd_tanh, ux | 0x00400000, _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0F, 1);
#endif
        }
        else
        {
            /* x is infinity; return a NaN */
            return __amd_handle_errorf(_FUNCNAME, __amd_tanh, INDEFBITPATT_SP32, _DOMAIN,
                AMD_F_INVALID, EDOM, x, 0.0F, 1);

        }
    }
    else if (ax >= 0x3f800000)
    {
        if (ax > 0x3f800000)
        {
            /* abs(x) > 1.0; return NaN */
            return __amd_handle_errorf(_FUNCNAME, __amd_tanh, INDEFBITPATT_SP32, _DOMAIN,
                AMD_F_INVALID, EDOM, x, 0.0F, 1);
        }
        else if (ux == 0x3f800000)
        {
            /* x = +1.0; return infinity with the same sign as x
               and set the divbyzero status flag */
            return __amd_handle_errorf(_FUNCNAME, __amd_tanh, PINFBITPATT_SP32, _DOMAIN,
                AMD_F_DIVBYZERO, EDOM, x, 0.0F, 1);
        }
        else
        {
            /* x = -1.0; return infinity with the same sign as x */
            return __amd_handle_errorf(_FUNCNAME, __amd_tanh, NINFBITPATT_SP32, _DOMAIN,
                AMD_F_DIVBYZERO, EDOM, x, 0.0F, 1);
        }
    }

    if (ax < 0x39000000)
    {
        if (ax == 0x00000000)
        {
            /* x is +/-zero. Return the same zero. */
            return x;
        }
        else
        {
            /* Arguments smaller than 2^(-13) in magnitude are
               approximated by atanhf(x) = x, raising inexact flag. */
#ifdef WINDOWS
            return x; // return valf_with_flags(x, AMD_F_INEXACT);
#else
            return __amd_handle_errorf(_FUNCNAME, __amd_tanh, ux, _UNDERFLOW, AMD_F_UNDERFLOW | AMD_F_INEXACT, ERANGE, x, 0.0F, 1);
#endif
        }
    }
    else
    {
        dx = x;
        if (ax < 0x3f000000)
        {
            /* Arguments up to 0.5 in magnitude are
               approximated by a [2,2] minimax polynomial */
            t = dx * dx;
            poly =
                (0.39453629046e0 +
                    (-0.28120347286e0 +
                        0.92834212715e-2 * t) * t) /
                (0.11836088638e1 +
                    (-0.15537744551e1 +
                        0.45281890445e0 * t) * t);
            return (float)(dx + dx * t * poly);
        }
        else
        {
            /* abs(x) >= 0.5 */
            /* Note that
                 atanhf(x) = 0.5 * ln((1+x)/(1-x))
               (see Abramowitz and Stegun 4.6.22).
               For greater accuracy we use the variant formula
               atanhf(x) = log(1 + 2x/(1-x)) = log1p(2x/(1-x)).
            */
            if (ux & SIGNBIT_SP32)
            {
                /* Argument x is negative */
                r = (-2.0 * dx) / (1.0 + dx);
                r = 0.5 * FN_PROTOTYPE(log1p)(r);
                return (float)-r;
            }
            else
            {
                r = (2.0 * dx) / (1.0 - dx);
                r = 0.5 * FN_PROTOTYPE(log1p)(r);
                return (float)r;
            }
        }
    }
}
