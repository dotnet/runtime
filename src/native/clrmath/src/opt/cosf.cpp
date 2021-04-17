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
  *   float cosf(float x)
  *
  * Spec:
  *   cos(0)    = 1
  *   cos(-0)   = -1
  *   cos(inf)  = NaN
  *   cos(-inf) = NaN
  *
  *
  ******************************************
  * Implementation Notes
  * ---------------------
  *
  * Checks for special cases
  * if ( ux = infinity) raise overflow exception and return x
  * if x is NaN then raise invalid FP operation exception and return x.
  *
  * 1. Argument reduction
  * if |x| > 5e5 then
  *      __amd_remainder_piby2d2f((uint64_t)x, &r, &region)
  * else
  *      Argument reduction
  *      Let z = |x| * 2/pi
  *      z = dn + r, where dn = round(z)
  *      rhead =  dn * pi/2_head
  *      rtail = dn * pi/2_tail
  *      r = z - dn = |x| - rhead - rtail
  *      expdiff = exp(dn) - exp(r)
  *      if(expdiff) > 15)
  *      rtail = |x| - dn*pi/2_tail2
  *      r = |x| -  dn*pi/2_head -  dn*pi/2_tail1
  *          -  dn*pi/2_tail2  - (((rhead + rtail) - rhead )-rtail)
  *
  * 2. Polynomial approximation
  * if(dn is even)
  *       x4 = x2 * x2;
  *       s = 0.5 * x2;
  *       t =  1.0 - s;
  *       poly = x4 * (C1 + x2 * (C2 + x2 * (C3 + x2 * C4 )))
  *       r = t + poly
  * else
  *       x3 = x2 * r
  *       poly = x3 * (S1 + x2 * (S2 + x2 * (S3 + x2 * S4)))
  *       r = r + poly
  * if((sign + 1) & 2)
  *       return r
  * else
  *       return -r;
  *
  * if |x| < pi/4 && |x| > 2.0^(-13)
  *   r = 0.5 * x2;
  *   t = 1 - r;
  *   cos(x) = t + ((1.0 - t) - r) + (x*x * (x*x * C1 + C2*x*x + C3*x*x
  *             + C4*x*x +x*x*C5 + x*x*C6)))
  *
  * if |x| < 2.0^(-13) && |x| > 2.0^(-27)
  *   cos(x) = 1.0 - x*x*0.5;;
  *
  * else return 1.0
  ******************************************
  */

#include "clrmath.h"
#include "libm/libm_util.h"
#include "libm/libm_special.h"
#include "libm/libm_types.h"
#include "libm/libm_typehelper.h"
#include "libm/libm_poly.h"

struct cosf_data_t {
    const double piby2_1, piby2_1tail;
    const double piby2_2, piby2_2tail;
    const double twobypi, alm_shift;
    double poly_sin[4];
    double poly_cos[4];
};

static const cosf_data_t cosf_data = {
    /* .piby2_1 = */     0x1.921fb54400000p0,
    /* .piby2_1tail = */ 0x1.0b4611a626331p-34,
    /* .piby2_2 = */     0x1.0b4611a600000p-34,
    /* .piby2_2tail = */ 0x1.3198a2e037073p-69,
    /* .twobypi = */     0x1.45f306dc9c883p-1,
    /* .alm_shift = */   0x1.8p+52,

    /*
     * Polynomial coefficients
     */

     /* .poly_sin = */    {
         -0x1.5555555555555p-3,
         0x1.1111111110bb3p-7,
         -0x1.a01a019e83e5cp-13,
         0x1.71de3796cde01p-19,
     },
     /* .poly_cos = */    {
         0x1.5555555555555p-5,
         -0x1.6c16c16c16967p-10,
         0x1.A01A019F4EC91p-16,
         -0x1.27E4FA17F667Bp-22,
     },
};

void __amd_remainder_piby2d2f(uint64_t x, double* r, int* region);

#define COSF_PIBY2_1     cosf_data.piby2_1
#define COSF_PIBY2_1TAIL cosf_data.piby2_1tail
#define COSF_PIBY2_2     cosf_data.piby2_2
#define COSF_PIBY2_2TAIL cosf_data.piby2_2tail
#define COSF_TWO_BY_PI   cosf_data.twobypi
#define COSF_ALM_SHIFT   cosf_data.alm_shift

#define COSF_PIBY4       0x3F490FDB
#define COSF_FIVE_E6     0x4A989680

#define S1  cosf_data.poly_sin[0]
#define S2  cosf_data.poly_sin[1]
#define S3  cosf_data.poly_sin[2]
#define S4  cosf_data.poly_sin[3]

#define C1  cosf_data.poly_cos[0]
#define C2  cosf_data.poly_cos[1]
#define C3  cosf_data.poly_cos[2]
#define C4  cosf_data.poly_cos[3]

#define SIGN_MASK32  0x7FFFFFFF
#define COSF_SMALL   0x3C000000 /* 2.0^(-13) */
#define COSF_SMALLER 0x39000000 /* 2.0^(-27) */


float _cosf_special(float x);

float
ALM_PROTO_OPT(cosf)(float x)
{

    double r, rhead, rtail;
    double xd, x2, x3, x4;
    double poly, t, s;
    uint64_t uy;
    int32_t region;

    /* cos(inf) = cos(-inf) = cos(NaN) = NaN */

    /* Get absolute value of input x */
    uint32_t ux = asuint32(x);
    ux = ux & SIGN_MASK32;

    if (unlikely(ux >= PINFBITPATT_SP32)) {
        /* infinity or NaN */
        return _cosf_special(x);
    }

    /* ux > pi/4 */
    if (ux > COSF_PIBY4) {

        float ax = asfloat(ux);

        /* Convert input to double precision */
        xd = (double)ax;

        if (ux < COSF_FIVE_E6) {
            /* reduce  the argument to be in a range from -pi/4 to +pi/4
                by subtracting multiples of pi/2 */

                /* |x| * 2/pi */
            r = COSF_TWO_BY_PI * xd;

            /* Get the exponent part */
            int32_t xexp = ux >> 23;

            /* dn = int(|x| * 2/pi) */
            double npi2d = r + COSF_ALM_SHIFT;
            int64_t npi2 = asuint64(npi2d);
            npi2d -= COSF_ALM_SHIFT;

            /* rhead = x - dn * pi/2_head */
            rhead = xd - npi2d * COSF_PIBY2_1;

            /* rtail = dn * pi/2_tail */
            rtail = npi2d * COSF_PIBY2_1TAIL;

            /* r = |x| * 2/pi - dn */
            r = rhead - rtail;

            uy = asuint64(r);

            /* expdiff = exponent(dn) - exponent(r) */
            int64_t expdiff = xexp - ((uy << 1) >> 53);

            region = (int32_t)npi2;

            if (expdiff > 15) {

                t = rhead;

                /* rtail = |x| - dn*pi/2_tail2 */
                rtail = npi2d * COSF_PIBY2_2;

                /* r = |x| -  dn*pi/2_head -  dn*pi/2_tail1
                 *     -  dn*pi/2_tail2  - (((rhead + rtail)
                 *     - rhead )-rtail)
                 */
                rhead = t - rtail;
                rtail = npi2d * COSF_PIBY2_2TAIL - ((t - rhead) - rtail);
                r = rhead - rtail;
            }

        }
        else {

            /* Reduce x into range [-pi/4,pi/4] */
            __amd_remainder_piby2d2f(asuint64(xd), &r, &region);
        }

        x2 = r * r;

        if (region & 1) {

            /*if region 1 or 3 then sin region */
            x3 = x2 * r;

            /* poly = x3 * (S1 + x2 * (S2 + x2 * (S3 + x2 * S4))) */
            r += x3 * POLY_EVAL_3(x2, S1, S2, S3, S4);

        }
        else {

            /* region 0 or 2 do a cos calculation */
            x4 = x2 * x2;
            s = 0.5 * x2;
            t = 1.0 - s;

            /* poly = x4 * (C1 + x2 * (C2 + x2 * (C3 + x2 * C4))) */
            poly = x4 * POLY_EVAL_3(x2, C1, C2, C3, C4);
            r = t + poly;

        }

        region += 1;

        if (region & 2) {

            /* If region is 2 or 3, sign is -ve */
            return (float)-r;

        }

        return (float)(r);
    }
    /* if |x| < 2.0^(-13) && |x| > 2.0^(-27) */
    else if (ux >= COSF_SMALLER) {

        /* if |x| < pi/4 && |x| > 2.0^(-13) */
        if (ux >= COSF_SMALL) {

            /* r = 0.5 * x2 */
            x2 = x * x;
            r = 0.5 * x2;

            t = 1 - r;

            /* cos(x) = t + ((1.0 - t) - r) + (x2 * (x2 * C1 + C2 * x2 + C3 * x2
             *          + C4 * x2 ))
             */
            s = t + ((1.0f - t) - r);
            return (float)(s + (x2 * (x2 * POLY_EVAL_4(x2, C1, C2, C3, C4))));

        }

        /* cos(x) = 1.0 - x * x* 0.5 */
        return (float)(1.0f - (x * x * 0.5));
    }

    return 1.0f;
}
