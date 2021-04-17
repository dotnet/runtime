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
  *   float tan(float x)
  *
  * Spec:
  *   tanf(0)    = 0
  *   tanf(-0)   = 0
  *   tanf(inf)  = NaN
  *   tanf(NaN) = NaN
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

struct tanf_data_t {
    const uint64_t pi_by_4;
    const uint64_t five_e5, seven_pi_by_4;
    const uint64_t five_pi_by_4, three_pi_by_4, nine_pi_by_4;
    const double one_by_three, twobypi, piby2_1, piby2_1tail, invpi;
    const double piby2_2, piby2_2tail, shift;
    const double piby2_3, piby2_3tail, one_by_six;
    const double piby4_lead, piby4_tail;

    double poly_tanf[5];
};

static const tanf_data_t tanf_data = {
    /* .pi_by_4 = */       0x3fe921fb54442d18,
    /* .five_e5 = */       0x411E848000000000,
    /* .seven_pi_by_4 = */ 0x4015fdbbe9bba775,
    /* .five_pi_by_4 = */  0x400f6a7a2955385e,
    /* .three_pi_by_4 = */ 0x4002d97c7f3321d2,
    /* .nine_pi_by_4 = */  0x401c463abeccb2bb,
    /* .one_by_three = */  0.333333333333333333,
    /* .twobypi = */       6.36619772367581382433e-01, /* 0x3fe45f306dc9c883 */
    /* .piby2_1 = */       1.57079632673412561417e+00, /* 0x3ff921fb54400000 */
    /* .piby2_1tail = */   6.07710050650619224932e-11, /* 0x3dd0b4611a626331 */
    /* .invpi = */         0x1.45f306dc9c883p-2,
    /* .piby2_2 = */       6.07710050630396597660e-11, /* 0x3dd0b4611a600000 */
    /* .piby2_2tail = */   2.02226624879595063154e-21,
    /* .shift = */         0x1.8p+52,
    /* .piby2_3 = */       2.02226624871116645580e-21, /* 0x3ba3198a2e000000 */
    /* .piby2_3tail = */   8.47842766036889956997e-32, /* 0x397b839a252049c1 */
    /* .one_by_six = */    0.1666666666666666666,
    /* .piby4_lead = */    7.85398163397448278999e-01, /* 0x3fe921fb54442d18 */
    /* .piby4_tail = */    3.06161699786838240164e-17, /* 0x3c81a62633145c06 */

    /* Polynomial coefficients */

    /* .poly_tanf = */     {
        0.385296071263995406715129e0,
        0.172032480471481694693109e-1,
        0.115588821434688393452299e+1,
        -0.51396505478854532132342e0,
        0.1844239256901656082986661e-1,
    },
};

#define PI_BY_4       tanf_data.pi_by_4
#define ALM_TANF_ZERO          (0x0L)
#define ONE_BY_THREE  tanf_data.one_by_three
#define FIVE_e5       tanf_data.five_e5
#define TWO_BY_PI     tanf_data.twobypi
#define PI_BY_2_1     tanf_data.piby2_1
#define PI_BY_2_1TAIL tanf_data.piby2_1tail
#define PI_BY_2_2     tanf_data.piby2_2
#define PI_BY_2_2TAIL tanf_data.piby2_2tail
#define PI_BY_2_3     tanf_data.piby2_3
#define PI_BY_2_3TAIL tanf_data.piby2_3tail
#define FIVE_PI_BY_4  tanf_data.five_pi_by_4
#define THREE_PI_BY_4 tanf_data.three_pi_by_4
#define NINE_PI_BY_4  tanf_data.nine_pi_by_4
#define SEVEN_PI_BY_4 tanf_data.seven_pi_by_4
#define ALM_SHIFT     tanf_data.shift
#define PI_BY_4_HEAD  tanf_data.piby4_lead
#define PI_BY_4_TAIL  tanf_data.piby4_tail

#define T1  tanf_data.poly_tanf[0]
#define T2  tanf_data.poly_tanf[1]
#define T3  tanf_data.poly_tanf[2]
#define T4  tanf_data.poly_tanf[3]
#define T5  tanf_data.poly_tanf[4]

#define MASK_LOWER32 0xffffffff00000000UL

void __amd_remainder_piby2d2f(uint64_t ux, double* r, int* region);

/*
 * tan(x + xx) approximation valid on the interval
 *     [-pi/4,pi/4].
 * If recip is true return -1/tan(x + xx) instead.
 */
static inline float
tan_piby4(double x, int32_t recip) {

    double r, t, r1, r2;

    /* Core Remez [1,2] approximation to tan(x) on the^M
     interval [0,pi/4]. */
    r = x * x;

    r1 = (T1 - T2 * r);

    r2 = (T3 + r * (T4 + r * T5));

    t = x + x * r * r1 / r2;

    if (recip)
        return (float)(-1.0 / t);
    else
        return (float)t;

}

static inline float
tan_piby4i_zero(double x) {

    double r, t, r1, r2;

    /* Core Remez [1,2] approximation to tan(x) on the^M
     interval [0,pi/4]. */
    r = x * x;

    r1 = (T1 - T2 * r);

    r2 = (T3 + r * (T4 + r * T5));

    t = x + x * r * r1 / r2;

    return (float)t;

}

static float
__tanf_special_inline(float x)
{
    uint32_t uxf;

    uxf = asuint32(x);

    /* x is either NaN or infinity */
    if (uxf & MANTBITS_SP32) {
        /* x is NaN */
        if (uxf & QNAN_MASK_32)
            return  __amd_handle_errorf("tanf", __amd_tan, uxf | QNAN_MASK_32,
                _DOMAIN, AMD_F_NONE, EDOM, x, 0.0f, 1);

        return  __amd_handle_errorf("tanf", __amd_tan, uxf | QNAN_MASK_32,
            _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0f, 1);
    }

    /* x is infinity. Return a NaN */
    return  __amd_handle_errorf("tanf", __amd_tan, INDEFBITPATT_SP32, _DOMAIN,
        AMD_F_INVALID, EDOM, x, 0.0f, 1);
}

#define ALM_TANF_SMALL_X     0x3C000000 
#define ALM_TANF_ARG_MIN     0x39000000 

static float
__tanf_very_small_x(float x)
{
    uint32_t ax = asuint32(x) & ~SIGNBIT_SP32;

    if (ax == ALM_TANF_ZERO)
        return x;

    if (ax < ALM_TANF_SMALL_X) { /* abs(x) < 2.0^(-13) */
        if (ax < ALM_TANF_ARG_MIN) /* abs(x) < 2.0^(-27) */
            return  __amd_handle_errorf("tanf", __amd_tan, asuint32(x), _UNDERFLOW,
                AMD_F_UNDERFLOW | AMD_F_INEXACT,
                ERANGE, x, 0.0, 1);

        /*
         *  2^-13 < abs(x) < 2^-27
         *  tan(x) = x + x^3 * 0.333333333
         */
        return (float)(x + (x * x * x * ONE_BY_THREE));
    }

    return tan_piby4i_zero(x);
}

float ALM_PROTO_OPT(tanf)(float x)
{
    double    dx, r;
    int32_t   region, xneg;
    uint32_t  uxf;

    uxf = asuint32(x);

    xneg = uxf & SIGNBIT_SP32;

    if (unlikely(((uxf & PINFBITPATT_SP32) == PINFBITPATT_SP32))) {
        return __tanf_special_inline(x);
    }

    /* uxf = abs(uxf) */
    uxf &= ~SIGNBIT_SP32;

    dx = (double)asfloat(uxf);

    uint64_t ax = asuint64(dx);

    if (unlikely(ax >= FIVE_e5)) {
        /* Reduce x into range [-pi/4,pi/4] */
        __amd_remainder_piby2d2f(ax, &r, &region);
    }
    else {

        double    rhead, rtail, npi2d;
        uint32_t  npi2;

        if (ax <= PI_BY_4) { /* abs(x) <= pi/4 */
            return __tanf_very_small_x(x);
        }

        /* Here on , pi/4 < ax < 5e5
         * For these size arguments we can just carefully subtract the
         * appropriate multiple of pi/2, using extra precision where
         * x is close to an exact multiple of pi/2
         */

        npi2d = dx * TWO_BY_PI + ALM_SHIFT;

        npi2 = (uint32_t)asuint64(npi2d);

        npi2d -= ALM_SHIFT;

        /* Subtract the multiple from x to get an extra-precision remainder */
        rhead = dx - npi2d * PI_BY_2_1;

        rtail = npi2d * PI_BY_2_1TAIL;

        r = rhead - rtail;

        region = npi2;
    }

    float res = tan_piby4(r, region & 1);

    /* tan(x) = -tan(x) if x is negative */
    res = asfloat(xneg ^ asuint32(res));

    return res;

}
