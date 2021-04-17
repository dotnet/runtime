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

float FN_PROTOTYPE_REF(modff)(float x, float* iptr)
{
    /* modff splits the argument x into integer and fraction parts,
       each with the same sign as x. */

    unsigned int ux, mask;
    int xexp;

    GET_BITS_SP32(x, ux);
    xexp = ((ux & (~SIGNBIT_SP32)) >> EXPSHIFTBITS_SP32) - EXPBIAS_SP32;

    if (xexp < 0)
    {
        /* abs(x) < 1.0. Set iptr to zero with the sign of x
           and return x. */
        PUT_BITS_SP32(ux & SIGNBIT_SP32, *iptr);
        return x;
    }
    else if (xexp < EXPSHIFTBITS_SP32)
    {
        float r;
        unsigned int ur;
        /* x lies between 1.0 and 2**(24) */
        /* Mask out the bits of x that we don't want */
        mask = (1 << (EXPSHIFTBITS_SP32 - xexp)) - 1;
        PUT_BITS_SP32(ux & ~mask, *iptr);
        r = x - *iptr;
        GET_BITS_SP32(r, ur);
        PUT_BITS_SP32(((ux & SIGNBIT_SP32) | ur), r);
        return r;
    }
    else if ((ux & (~SIGNBIT_SP32)) > 0x7f800000)
    {
        /* x is NaN */
        *iptr = x;
#ifdef WINDOWS
        return __amd_handle_errorf("modff", __amd_logb, ux | QNAN_MASK_32, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0, 1); /* Raise invalid if it is a signalling NaN */
#else
        return x + x;
#endif
    }
    else
    {
        /* x is infinity or large. Set iptr to x and return zero
           with the sign of x. */
        *iptr = x;
        PUT_BITS_SP32(ux & SIGNBIT_SP32, x);
        return x;
    }
}
