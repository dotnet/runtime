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
 *
 * ISO-IEC-10967-2: Elementary Numerical Functions
 * Signature:
 *   double tan(double x)
 *
 * Spec:
 *   tan(0)    = 0
 *   tan(-0)   = 0
 *   tan(inf)  = NaN
 *   tan(NaN) = NaN
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
 * if 2.0^(-13) < |x| < pi/4 then
 *    tan(pi/4-x) = (1-tan(x))/(1+tan(x)) for x close to pi/4
 *    tan(x-pi/4) = (tan(x)-1)/(tan(x)+1) close to -pi/4
 *    tan(x) is approximated by Core Remez [2,3] approximation to tan(x+xx) on the
 *    interval [0,0.68].
 * if 2.0^(-27) < |x| < 2.0^(-13) then tan(x) = x + (x * x * x * 1/3)
 * if |x| < 2.0^(-27) then underflow
 *
 * if x < 5e5 then
 *  Reduce x into range [-pi/4,pi/4] and then compute tan(pi/4-x)
 *
 */

#include "clrmath.h"
#include "libm/libm_util.h"
#include "libm/libm_special.h"
#include "libm/libm_types.h"
#include "libm/libm_typehelper.h"
#include "libm/libm_poly.h"

struct tan_data_t {
    const uint64_t pi_by_4, small_x, min, zero;
    const uint64_t five_e5, seven_pi_by_4;
    const double one_by_three, twobypi, piby2_1, piby2_1tail, invpi;
    const double piby2_2, piby2_2tail, ALM_SHIFT;
    const double piby2_3, piby2_3tail, one_by_six;
    const double piby4_lead, piby4_tail;
    const uint64_t five_pi_by_4, three_pi_by_4, nine_pi_by_4;
    double poly_tan[7];
};

static const tan_data_t tan_data = {
    /* .pi_by_4 = */       0x3fe921fb54442d18,
    /* .small_x = */       0x3f20000000000000,
    /* .min = */           0x3e40000000000000,
    /* .zero = */          0x0,
    /* .five_e5 = */       0x411E848000000000,
    /* .seven_pi_by_4 = */ 0x4015fdbbe9bba775,
    /* .one_by_three = */  0.333333333333333333,
    /* .twobypi = */       6.36619772367581382433e-01, /* 0x3fe45f306dc9c883 */
    /* .piby2_1 = */       1.57079632673412561417e+00, /* 0x3ff921fb54400000 */
    /* .piby2_1tail = */   6.07710050650619224932e-11, /* 0x3dd0b4611a626331 */
    /* .invpi = */         0x1.45f306dc9c883p-2,
    /* .piby2_2 = */       6.07710050630396597660e-11, /* 0x3dd0b4611a600000 */
    /* .piby2_2tail = */   0x1.3198a2e037073p-69,
    /* .ALM_SHIFT = */     0x1.8p+52,
    /* .piby2_3 = */       2.02226624871116645580e-21, /* 0x3ba3198a2e000000 */
    /* .piby2_3tail = */   8.47842766036889956997e-32, /* 0x397b839a252049c1 */
    /* .one_by_six = */    0.1666666666666666666,
    /* .piby4_lead = */    7.85398163397448278999e-01, /* 0x3fe921fb54442d18 */
    /* .piby4_tail = */    3.06161699786838240164e-17, /* 0x3c81a62633145c06 */
    /* .five_pi_by_4 = */  0x400f6a7a2955385e,
    /* .three_pi_by_4 = */ 0x4002d97c7f3321d2,
    /* .nine_pi_by_4 = */  0x401c463abeccb2bb,

    /*
     * Polynomial coefficients
     */

     /* .poly_tan = */ {
         0.372379159759792203640806338901e0,
         -0.229345080057565662883358588111e-1,
         0.224044448537022097264602535574e-3,
         0.111713747927937668539901657944e1,
         -0.515658515729031149329237816945e0,
         0.260656620398645407524064091208e-1,
         -0.232371494088563558304549252913e-3
     },
};

#define PI_BY_4       tan_data.pi_by_4
#define SMALL_X       tan_data.small_x
#define SMALLER_X     tan_data.min
#define ZERO_         tan_data.zero
#define ONE_BY_THREE  tan_data.one_by_three
#define FIVE_e5       tan_data.five_e5
#define TWO_BY_PI     tan_data.twobypi
#define PI_BY_2_1     tan_data.piby2_1
#define PI_BY_2_1TAIL tan_data.piby2_1tail
#define PI_BY_2_2     tan_data.piby2_2
#define PI_BY_2_2TAIL tan_data.piby2_2tail
#define PI_BY_2_3     tan_data.piby2_3
#define PI_BY_2_3TAIL tan_data.piby2_3tail
#define FIVE_PI_BY_4  tan_data.five_pi_by_4
#define THREE_PI_BY_4 tan_data.three_pi_by_4
#define NINE_PI_BY_4  tan_data.nine_pi_by_4
#define SEVEN_PI_BY_4 tan_data.seven_pi_by_4
#define ALM_SHIFT     tan_data.ALM_SHIFT
#define PI_BY_4_HEAD  tan_data.piby4_lead
#define PI_BY_4_TAIL  tan_data.piby4_tail

#define T1  tan_data.poly_tan[0]
#define T2  tan_data.poly_tan[1]
#define T3  tan_data.poly_tan[2]
#define T4  tan_data.poly_tan[3]
#define T5  tan_data.poly_tan[4]
#define T6  tan_data.poly_tan[5]
#define T7  tan_data.poly_tan[6]

#define MASK_LOWER32 0xffffffff00000000
extern void __amd_remainder_piby2(double x, double* r, double* rr, int32_t* region);


/* tan(x + xx) approximation valid on the interval [-pi/4,pi/4].
   If recip is true return -1/tan(x + xx) instead. */
static inline double tan_piby4(double x, double xx, int32_t recip)
{
    double r, r1, r2, t1, t2, xl;
    int32_t transform = 0;

    /* In order to maintain relative precision transform using the identity:
       tan(pi/4-x) = (1-tan(x))/(1+tan(x)) for arguments close to pi/4.
       Similarly use tan(x-pi/4) = (tan(x)-1)/(tan(x)+1) close to -pi/4. */

    if (x > 0.68) {
        transform = 1;

        x = PI_BY_4_HEAD - x;

        xl = PI_BY_4_TAIL - xx;

        x += xl;

        xx = 0.0;
    }
    else if (x < -0.68) {
        transform = -1;

        x = PI_BY_4_HEAD + x;

        xl = PI_BY_4_TAIL + xx;

        x += xl;

        xx = 0.0;
    }

    /* Core Remez [2,3] approximation to tan(x+xx) on the
     interval [0,0.68]. */

    r = x * x + 2.0 * x * xx;

    t1 = x;

    r1 = xx + x * r * (T1 + r * (T2 + r * T3));

    /* r2 = T4 + r*T5 + r^2*T6 + r^3*T7 */
    r2 = POLY_EVAL_3(r, T4, T5, T6, T7);

    t2 = r1 / r2;

    /* Reconstruct tan(x) in the transformed case. */

    if (transform) {
        double t;

        t = t1 + t2;

        if (recip) {

            return transform * (2 * t / (t - 1) - 1.0);

        }
        else {

            return transform * (1.0 - 2 * t / (1 + t));

        }
    }

    if (recip) {
        /* Compute -1.0/(t1 + t2) accurately */
        double trec, trec_top, z1, z2, t;
        uint64_t u;

        t = t1 + t2;

        u = asuint64(t);

        u &= MASK_LOWER32;

        z1 = asdouble(u);

        z2 = t2 - (z1 - t1);

        trec = -1.0 / t;

        u = asuint64(trec);

        u &= MASK_LOWER32;

        trec_top = asdouble(u);

        return trec_top + trec * ((1.0 + trec_top * z1) + trec_top * z2);
    }
    else {

        return t1 + t2;

    }
}


double ALM_PROTO_OPT(tan)(double x)
{
    double r, rr, t, rhead, rtail, npi2d;
    int32_t npi2, region, xneg;
    uint64_t ux, uy, ax, xexp, expdiff;

    ux = asuint64(x);

    ax = (ux & ~SIGNBIT_DP64);

    if (ax <= PI_BY_4) { /* abs(x) <= pi/4 */

        if (ax < SMALL_X) { /* abs(x) < 2.0^(-13) */

            if (ax < SMALLER_X) {/* abs(x) < 2.0^(-27) */

                if (ax == ZERO_) {

                    return x;

                }
                else {

                    return  __amd_handle_error("tan", __amd_tan, ux, _UNDERFLOW,
                        AMD_F_UNDERFLOW | AMD_F_INEXACT,
                        ERANGE, x, 0.0, 1);

                }
            }
            else {

                return x + (x * x * x * ONE_BY_THREE);

            }
        }
        else {

            return tan_piby4(x, 0.0, 0);

        }
    }
    else if (unlikely(((ux & EXPBITS_DP64) == EXPBITS_DP64))) {

        /* x is either NaN or infinity */
        if (ux & MANTBITS_DP64) {

            /* x is NaN */
            if (ux & QNAN_MASK_64) {

                return  __amd_handle_error("tan", __amd_tan, ux | QNAN_MASK_64,
                    _DOMAIN, AMD_F_NONE, EDOM, x, 0.0, 1);
            }
            else {

                return  __amd_handle_error("tan", __amd_tan, ux | QNAN_MASK_64,
                    _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0, 1);
            }

        }
        else {
            /* x is infinity. Return a NaN */
            return  __amd_handle_error("tan", __amd_tan, INDEFBITPATT_DP64, _DOMAIN,
                AMD_F_INVALID, EDOM, x, 0.0, 1);
        }
    }

    xneg = ux >> 63;

    x = asdouble(ax);

    if (ax < FIVE_e5) { /* x < 5e5 */
        /* For these size arguments we can just carefully subtract the
         appropriate multiple of pi/2, using extra precision where
         x is close to an exact multiple of pi/2 */

        xexp = ax >> EXPSHIFTBITS_DP64;
        /* How many pi/2 is x a multiple of? */
        if (ax <= FIVE_PI_BY_4) { /* 5pi/4 */

            if (ax <= THREE_PI_BY_4) /* 3pi/4 */
                npi2 = 1;
            else
                npi2 = 2;

            npi2d = (double)npi2;
        }
        else if (ax <= NINE_PI_BY_4) {/* 9pi/4 */

            if (ax <= SEVEN_PI_BY_4) /* 7pi/4 */
                npi2 = 3;
            else
                npi2 = 4;

            npi2d = (double)npi2;
        }
        else {

            npi2d = x * TWO_BY_PI + ALM_SHIFT;

            npi2 = (int32_t)asuint64(npi2d);

            npi2d -= ALM_SHIFT;

        }

        /* Subtract the multiple from x to get an extra-precision remainder */
        rhead = x - npi2d * PI_BY_2_1;

        rtail = npi2d * PI_BY_2_1TAIL;

        uy = asuint64(rhead);

        expdiff = xexp - ((uy & EXPBITS_DP64) >> EXPSHIFTBITS_DP64);

        if (expdiff > 15) {
            /* The remainder is pretty small compared with x, which
               implies that x is a near multiple of pi/2
               (x matches the multiple to at least 15 bits) */
            t = rhead;

            rtail = npi2d * PI_BY_2_2;

            rhead = t - rtail;

            rtail = npi2d * PI_BY_2_2TAIL - ((t - rhead) - rtail);

            if (expdiff > 48) {
                /* x matches a pi/2 multiple to at least 48 bits */
                t = rhead;

                rtail = npi2d * PI_BY_2_3;

                rhead = t - rtail;

                rtail = npi2d * PI_BY_2_3TAIL - ((t - rhead) - rtail);
            }
        }

        r = rhead - rtail;

        rr = (rhead - r) - rtail;

        region = npi2 & 3;
    }
    else {
        /* Reduce x into range [-pi/4,pi/4] */
        __amd_remainder_piby2(x, &r, &rr, &region);

    }

    if (xneg) {

        return -tan_piby4(r, rr, region & 1);

    }
    else {

        return tan_piby4(r, rr, region & 1);

    }
}
