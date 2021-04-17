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

double FN_PROTOTYPE_REF(ceil)(double x)
{
    double r;
    long long rexp, xneg;
    unsigned long long ux, ax, ur, mask;

    GET_BITS_DP64(x, ux);
    /*ax is |x|*/
    ax = ux & (~SIGNBIT_DP64);
    /*xneg stores the sign of the input x*/
    xneg = (ux != ax);
    /*The range is divided into
      > 2^53. ceil will either the number itself or Nan
              always returns a QNan. Raises exception if input is a SNan
      < 1.0   If 0.0 then return with the appropriate sign
              If input is less than -0.0 and greater than -1.0 then return -0.0
              If input is greater than 0.0 and less than 1.0 then return 1.0
      1.0 < |x| < 2^53
              appropriately check the exponent and set the return Value by shifting
              */
    if (ax >= 0x4340000000000000) /* abs(x) > 2^53*/
    {
        /* abs(x) is either NaN, infinity, or >= 2^53 */
        if (ax > 0x7ff0000000000000)
            /* x is NaN */
#ifdef WINDOWS
            return __amd_handle_error("ceil", __amd_ceil, ux | 0x0008000000000000, _DOMAIN, 0, EDOM, x, 0.0, 1);
#else
        {
            if (!(ax & 0x0008000000000000))// x is snan
                return __amd_handle_error("ceil", __amd_ceil, ux | 0x0008000000000000, _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0, 1);
            else
                return x;
        }
#endif

        else
            return x;
    }
    else if (ax < 0x3ff0000000000000) /* abs(x) < 1.0 */
    {
        if (ax == 0x0000000000000000)
            /* x is +zero or -zero; return the same zero */
            return x;
        else if (xneg) /* x < 0.0; return -0.0 */
        {
            PUT_BITS_DP64(0x8000000000000000, r);
            return r;
        }
        else
            return 1.0;
    }
    else
    {
        /*Get the exponent for the floating point number. Should be between 0 and 53*/
        rexp = ((ux & EXPBITS_DP64) >> EXPSHIFTBITS_DP64) - EXPBIAS_DP64;
        /* Mask out the bits of r that we don't want */
        mask = 1;
        mask = (mask << (EXPSHIFTBITS_DP64 - rexp)) - 1;
        /*Keeps the exponent part and the required mantissa.*/
        ur = (ux & ~mask);
        PUT_BITS_DP64(ur, r);
        if (xneg || (ur == ux))
            return r;
        else
            /* We threw some bits away and x was positive */
            return r + 1.0;
    }

}
