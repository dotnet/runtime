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

 /*
  * ISO-IEC-10967-2: Elementary Numerical Functions
  * Signature:
  *   float tanhf(float x)
  *
  * Spec:
  *  tanhf(0)    =  0
  *  tanhf(-0)   =  0
  *  tanhf(inf)  =  1
  *  tanhf(-inf) = -1
  *
  *
  ******************************************
  * Implementation Notes
  * ---------------------
  * To compute tanhf(float x)
  * Let y = |x|
  * The input argument is then reduced to one of these three intervals:
  *
  * 1. 0 <= y < 1
  *    In this case, tanhf(y) is calculated as,
  *    tanhf(y) = y + C1 * y^3 + C2 * y^5 + C3 * y^7 + C4 * y^9 +
  *               C5 * y^11 + C6 * y^13 + C7 * y^15
  *
  *    The polynomial coefficients are derived using fpminmax algorithm.
  *
  * 2. 1 <= y < 0x1.154246p3
  *    In this case, tanhf(y) is calculated as,
  *    tanhf(y) = 1 - 2/(z + 1),     where z = e^(-2 * y)
  *
  *    This can be approximated using the polynomial,
  *    tanhf(y) = 1.0 + 2 * (z^8 - z^7 + z^6 - z^5 + z^4 - z^3 + z^2 - z)
  *
  * 3. 0x1.154246p3 <= y < +inf
  *    In this case, tanhf(y) = 1
  *
  * If x < 0, then we use the identity
  *       tanhf(-x) = -tanhf(x)
  *
  * Max ULP of current implementation: 1
  */

#include "clrmath.h"
#include "libm/libm_util.h"
#include "libm/libm_special.h"
#include "libm/libm_types.h"
#include "libm/libm_typehelper.h"
#include "libm/libm_poly.h"

struct tanhf_data_t {
    float poly_tanhf[7];
    float max_arg;
};

static const tanhf_data_t tanhf_data = {
    /* .poly_tanhf = */ {
        -0x1.55553p-2,
        0x1.110aeap-3,
        -0x1.b94f7ep-5,
        0x1.5fadfcp-6,
        -0x1.037056p-7,
        0x1.2b22eap-9,
        -0x1.728278p-12,
    },
    /* .max_arg = */    0x1.154246p3,
};

#define C1 tanhf_data.poly_tanhf[0]
#define C2 tanhf_data.poly_tanhf[1]
#define C3 tanhf_data.poly_tanhf[2]
#define C4 tanhf_data.poly_tanhf[3]
#define C5 tanhf_data.poly_tanhf[4]
#define C6 tanhf_data.poly_tanhf[5]
#define C7 tanhf_data.poly_tanhf[6]

#define TANHF_MAX_ARG   tanhf_data.max_arg

#define TANHF_SMALL_ARG   0x39000000
#define TANHF_SIGN_MASK32 ~(1U<<31)

float
ALM_PROTO_OPT(tanhf)(float x)
{

    float y, z, result;
    float poly;
    uint32_t sign, ux;

    /* Get sign of input argument */
    ux = asuint32(x);
    sign = ux & (~TANHF_SIGN_MASK32);

    /* Get absolute value of input argument */
    y = asfloat(ux & TANHF_SIGN_MASK32);

    /* Check for Special cases */
    ux = asuint32(y);

    /* |x| is small enough that tanhf(x) = x */
    if (ux < TANHF_SMALL_ARG) {

        if (ux == POS_ZERO_F32)
            /* For +/- 0 */
            return x;
        else
            /* For underflow */
            return _tanhf_special(x);

    }
    else if (ux > PINFBITPATT_SP32)
        /* For +/-inf */
        return x + x;

    if (y > TANHF_MAX_ARG)
        /* For x > max_arg */
        return asfloat(asuint32(1.0f) ^ sign);

    if (y < 1.0) {

        /* Compute tanhf using the polynomial
           y + C1 * y^3 + C2 * y^5 + C3 * y^7 + C4 * y^9 +
           C5 * y^11 + C6 * y^13 + C7 * y^15
        */
        result = POLY_EVAL_ODD_15F(y, C1, C2, C3, C4, C5, C6, C7);

    }
    else {

        // z = e^(-2 * y)
        z = ALM_PROTO(expf)(-2.0f * y);
        float z2 = z * z;
        float z4 = z2 * z2;

        float a0 = 1.0f - z;
        float a1 = 1 + z2;

        float b0 = a0 * a1;
        float b1 = z4 * a0 * a1;

        /* tanhf can be approximated using the polynomial
           1.0 + 2 * (z^8 - z^7 + z^6 - z^5 + z^4 - z^3 + z^2 - z)
        */
        poly = b0 + b1;

        result = 1.0f - (2.0f * z * poly);
    }

    /* Result is -ve if input argument is -ve */
    return asfloat(asuint32(result) ^ sign);

}
