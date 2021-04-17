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
  *   double cos(double x)
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
  * checks for special cases
  * if ( ux = infinity) raise overflow exception and return x
  * if x is NaN then raise invalid FP operation exception and return x.
  *
  * 1. Argument reduction
  * if |x| > 5e5 then
  *      __amd_remainder_piby2(x, &r, &rr, &region)
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
  *      r = |x| -  dn*pi/2_head -  dn*pi/2_tail1 -  dn*pi/2_tail2  - (((rhead + rtail) - rhead )-rtail)
  * rr = (|x| - rhead) - r + rtail
  *
  * 2. Polynomial approximation
  * if(dn is even)
  *       rr = rr * r;
  *       x4 = x2 * x2;
  *       s = 0.5 * x2;
  *       t =  s - 1.0;
  *       poly = x4 * (C1 + x2 * (C2 + x2 * (C3 + x2 * (C4 + x2 * (C5 + x2 * x6)))))
  *       r = (((1.0 + t) - s) - rr) + poly - t
  * else
  *       x3 = x2 * r
  *       poly = S2 + (r2 * (S3 + (r2 * (S4 + (r2 * (S5 + S6 * r2))))))
  *       r = r - ((x2 * (0.5*rr - x3 * poly)) - rr) - S1 * x3
  * if((sign + 1) & 2)
  *       return r
  * else
  *       return -r;

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

struct cos_data_t {
    const double twobypi, piby2_1, piby2_1tail, invpi, pi, pi1, pi2;
    const double piby2_2, piby2_2tail, ALM_SHIFT;
    const double one_by_six;
    double poly_sin[7];
    double poly_cos[6];
};

static const cos_data_t cos_data = {
    /* .twobypi = */     0x1.45f306dc9c883p-1,
    /* .piby2_1 = */     0x1.921fb54400000p0,
    /* .piby2_1tail = */ 0x1.0b4611a626331p-34,
    /* .invpi = */       0x1.45f306dc9c883p-2,
    /* .pi = */          0x1.921fb54442d18p1,
    /* .pi1 = */         0x1.921fb50000000p1,
    /* .pi2 = */         0x1.110b4611a6263p-25,
    /* .piby2_2 = */     0x1.0b4611a600000p-34,
    /* .piby2_2tail = */ 0x1.3198a2e037073p-69,
    /* .ALM_SHIFT = */   0x1.8p+52,
    /* .one_by_six = */  0.1666666666666666666,

    /*
     * Polynomial coefficients
     */

     /* .poly_sin = */    {
         -0x1.5555555555555p-3,
         0x1.1111111110bb3p-7,
         -0x1.a01a019e83e5cp-13,
         0x1.71de3796cde01p-19,
         -0x1.ae600b42fdfa7p-26,
         0x1.5e0b2f9a43bb8p-33
     },
    /* .poly_cos = */    {
        0x1.5555555555555p-5,   /* 0.0416667 */
        -0x1.6c16c16c16967p-10, /* -0.00138889 */
        0x1.A01A019F4EC91p-16,  /* 2.48016e-005 */
        -0x1.27E4FA17F667Bp-22, /* -2.75573e-007 */
        0x1.1EEB690382EECp-29,  /* 2.08761e-009 */
        -0x1.907DB47258AA7p-37  /* -1.13826e-011 */
    },
};

void __amd_remainder_piby2(double x, double* r, double* rr, int* region);

#define pi          cos_data.pi
#define pi1         cos_data.pi1
#define pi2         cos_data.pi2
#define invpi       cos_data.invpi
#define TwobyPI     cos_data.twobypi
#define PIby2_1     cos_data.piby2_1
#define PIby2_1tail cos_data.piby2_1tail
#define PIby2_2     cos_data.piby2_2
#define PIby2_2tail cos_data.piby2_2tail
#define PIby4       0x3fe921fb54442d18
#define FiveE6      0x415312d000000000
#define ONE_BY_SIX  cos_data.one_by_six
#define ALM_SHIFT   cos_data.ALM_SHIFT

#define S1  cos_data.poly_sin[0]
#define S2  cos_data.poly_sin[1]
#define S3  cos_data.poly_sin[2]
#define S4  cos_data.poly_sin[3]
#define S5  cos_data.poly_sin[4]
#define S6  cos_data.poly_sin[5]

#define C1  cos_data.poly_cos[0]
#define C2  cos_data.poly_cos[1]
#define C3  cos_data.poly_cos[2]
#define C4  cos_data.poly_cos[3]
#define C5  cos_data.poly_cos[4]
#define C6  cos_data.poly_cos[5]

#define SIGN_MASK   0x7FFFFFFFFFFFFFFF /* Infinity */
#define INF         0x7ff0000000000000
#define SIGN_MASK32 0x7FFFFFFF
#define COS_SMALL   0x3f20000000000000  /* 2.0^(-13) */
#define COS_SMALLER 0X3e40000000000000  /* 2.0^(-27) */


double _cos_special(double x);

double
ALM_PROTO_OPT(cos)(double x)
{

    double r, rr, poly, x2, t, s;
    double rhead, rtail, x3, x4;
    uint64_t uy;
    int32_t region;

    /* cos(inf) = cos(-inf) = cos(NaN) = NaN */

    uint64_t ux = asuint64(x);

    ux = ux & SIGN_MASK;

    if (unlikely((ux & SIGN_MASK) >= INF)) {
        /* infinity or NaN */
        return _cos_special(x);
    }

    if (ux > PIby4) {

        x = asdouble(ux);
        /* ux > pi/4 */
        if (ux < FiveE6) {
            /* reduce  the argument to be in a range from -pi/4 to +pi/4
                by subtracting multiples of pi/2 */

            r = TwobyPI * x; /* x * two_by_pi*/

            int32_t xexp = ux >> 52;

            double npi2d = r + ALM_SHIFT;

            int64_t npi2 = asuint64(npi2d);

            npi2d -= ALM_SHIFT;

            rhead = x - npi2d * PIby2_1;

            rtail = npi2d * PIby2_1tail;

            r = rhead - rtail;

            uy = asuint64(r);

            int64_t expdiff = xexp - ((uy << 1) >> 53);

            region = (int32_t)npi2;

            if (expdiff > 15) {

                t = rhead;

                rtail = npi2d * PIby2_2;

                rhead = t - rtail;

                rtail = npi2d * PIby2_2tail - ((t - rhead) - rtail);

                r = rhead - rtail;
            }

            rr = (rhead - r) - rtail;
        }
        else {
            // Reduce x into range [-pi/4,pi/4]
            __amd_remainder_piby2(x, &r, &rr, &region);
        }

        x2 = r * r;

        if (region & 1) {

            /*if region 1 or 3 then sin region */

            x3 = x2 * r;

            /* poly = S2 + (r2 * (S3 + (r2 * (S4 + (r2 * (S5 + S6 * r2)))))) */
            poly = POLY_EVAL_5(x2, S2, S3, S4, S5, S6);

            s = 0.5 * rr;

            poly = ((x2 * (s - x3 * poly)) - rr) - S1 * x3;

            r -= poly; /* r - ((r2 * (0.5 * rr - x3 * poly) - rr) - S1*r3 */

        }
        else {
            /* region 0 or 2 do a cos calculation */
            rr = rr * r;

            x4 = x2 * x2;

            s = 0.5 * x2;

            t = s - 1.0;

            /* poly = x4 * (C1 + x2 * (C2 + x2 * (C3 + x2 * (C4 + x2 * (C5 + x2 * x6))))) */
            poly = x4 * POLY_EVAL_6(x2, C1, C2, C3, C4, C5, C6);

            r = (((1.0 + t) - s) - rr) + poly;

            r -= t;

        }

        region += 1;

        if (region & 2) {

            return -r;

        }

        return r;
    }
    else if (ux >= COS_SMALL) {
        /* pi/4 > |x| > 2.0^(-13) */
        x2 = x * x;

        r = 0.5 * x2;

        t = 1 - r;

        s = t + ((1.0 - t) - r);

        return s + (x2 * (x2 * POLY_EVAL_6(x2, C1, C2, C3, C4, C5, C6)));

    }
    else if (ux >= COS_SMALLER) {
        /* if 2.0^(-13) > |x| > 2.0^(-27) */
        return 1.0 - (x * x * 0.5);

    }

    return 1.0;
}
