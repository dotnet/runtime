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

float FN_PROTOTYPE_REF(ceilf)(float x)
{
    float r;
    int rexp, xneg;
    unsigned int ux, ax, ur, mask;

    GET_BITS_SP32(x, ux);
    /*ax is |x|*/
    ax = ux & (~SIGNBIT_SP32);
    /*xneg stores the sign of the input x*/
    xneg = (ux != ax);
    /*The range is divided into
      > 2^24. ceil will either the number itself or Nan
              always returns a QNan. Raises exception if input is a SNan
      < 1.0   If 0.0 then return with the appropriate sign
              If input is less than -0.0 and greater than -1.0 then return -0.0
              If input is greater than 0.0 and less than 1.0 then return 1.0
      1.0 < |x| < 2^24
              appropriately check the exponent and set the return Value by shifting
              */
    if (ax >= 0x4b800000) /* abs(x) > 2^24*/
    {
        /* abs(x) is either NaN, infinity, or >= 2^24 */
        if (ax > 0x7f800000)
            /* x is NaN */
#ifdef WINDOWS
            return __amd_handle_errorf("ceilf", __amd_ceil, ux | 0x00400000, _DOMAIN, 0, EDOM, x, 0.0, 1);
#else
            if (!(ax & 0x00400000)) //x is snan
                return __amd_handle_errorf("ceilf", __amd_ceil, ux | 0x00400000, _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0, 1);
            else
                return x;
#endif

        else
            return x;
    }
    else if (ax < 0x3f800000) /* abs(x) < 1.0 */
    {
        if (ax == 0x00000000)
            /* x is +zero or -zero; return the same zero */
            return x;
        else if (xneg) /* x < 0.0 */
            return -0.0F;
        else
            return 1.0F;
    }
    else
    {
        rexp = ((ux & EXPBITS_SP32) >> EXPSHIFTBITS_SP32) - EXPBIAS_SP32;
        /* Mask out the bits of r that we don't want */
        mask = (1 << (EXPSHIFTBITS_SP32 - rexp)) - 1;
        /*Keeps the exponent part and the required mantissa.*/
        ur = (ux & ~mask);
        PUT_BITS_SP32(ur, r);

        if (xneg || (ux == ur)) return r;
        else
            /* We threw some bits away and x was positive */
            return r + 1.0F;
    }
}
