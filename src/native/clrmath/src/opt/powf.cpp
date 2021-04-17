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
  * Contains implementation of powf()
  * Prototype :
  * float powf(float x, float y)
  *
  * Algorithm
  * x^y = e^(y*ln(x))
  *
  * Look in exp, log for the respective algorithms
  *

 */

#include "clrmath.h"
#include "libm/libm_util.h"
#include "libm/libm_special.h"
#include "libm/libm_typehelper.h"

#define N 8
#define TABLE_SIZE (1ULL << N)
#define MAX_POLYDEGREE  8

extern "C" uint64_t log_256[];

#if N == 8
#define POLY_DEGREE 4
extern "C" const uint64_t log_table_256[];
extern "C" const uint64_t log_f_inv_256[];
#define TAB_F_INV log_f_inv_256
#define TAB_LOG   log_table_256
#define MANT_MASK_N  (0x000FF00000000000ULL)
#define MANT_MASK_N1 (0x0000080000000000ULL)
#elif N == 9
#define POLY_DEGREE 5
extern "C" double log_table_512[];
extern "C" double log_f_inv_512[];

#elif N == 10
#define POLY_DEGREE 4
extern "C" double log_table_1024[];
extern "C" double log_f_inv_1024[];
#define TAB_F_INV log_f_inv_1024
#define TAB_LOG   log_table_1024
#define MANT_MASK_N  (0x000FFC0000000000ULL)
#define MANT_MASK_N1 (0x0000020000000000ULL)
#endif

#define MANT_BITS_MASK (TABLE_SIZE - 1)
#define MANT1_BITS_MASK (1ULL << (N + 1))

#define EXPF_N 6
#define EXPF_POLY_DEGREE 4
#if EXPF_N == 5
#define EXPF_POLY_DEGREE 3
#elif EXPF_N == 4
#define EXPF_POLY_DEGREE 3
#endif

#define EXPF_TABLE_SIZE (1 << EXPF_N)
#define EXPF_MAX_POLY_DEGREE 4

struct log_data_t {
    double ALIGN(16) poly[MAX_POLYDEGREE];
    double_t ln2_lead, ln2_tail;
};

#if N == 8
#endif

static const log_data_t log_data = {
    /*
     * Polynomial constants, 1/x! (reciprocal x)
     * To make better use of cache line,
     * we dont store 0! and 1!
     */
    /* .poly = */     { /* skip for 0/1 and 1/1 */
                0x1.0000000000000p-1,    /* 1/2 */
                0x1.5555555555555p-2,    /* 1/3 */
                0x1.0000000000000p-2,    /* 1/4 */
                0x1.999999999999ap-3,    /* 1/5 */
                0x1.5555555555555p-3,    /* 1/6 */
                0x1.2492492492492p-3,    /* 1/7 */
                0x1.0000000000000p-3,    /* 1/8 */
                0x1.c71c71c71c71cp-4,    /* 1/9 */
    },

    // .ln2  = 0x1.62e42fefa39efp-1; /* ln(2) */

    /* .ln2_lead = */ 0x1.62e42e0000000p-1,
    /* .ln2_tail = */ 0x1.efa39ef35793cp-25,
};

#define C2 log_data.poly[0]
#define C3 log_data.poly[1]
#define C4 log_data.poly[2]
#define C5 log_data.poly[3]
#define C6 log_data.poly[4]
#define C7 log_data.poly[5]
#define C8 log_data.poly[6]
#define LN2_LEAD log_data.ln2_lead
#define LN2_TAIL log_data.ln2_tail
#define SIGN_BIAS 0x8000000000000000

#include "expf_data.h"

static const expf_data expf_v2_data = {
    /* .tblsz_byln2 = */ 0x1.71547652b82fep+6,
    /* .Huge = */        0x1.8000000000000p+52,
    /* .ln2by_tblsz = */ 0x1.62e42fefa39efp-7,
    /* .poly = */        {
        1.0,    /* 1/1! = 1 */
        0x1.0000000000000p-1,   /* 1/2! = 1/2    */
        0x1.5555555555555p-3,   /* 1/3! = 1/6    */
        0x1.cacccaa4ba57cp-5,   /* 1/4! = 1/24   */
    },
#if EXPF_N == 6
    /* .table_v3 = */    &__two_to_jby64[0],
#elif EXPF_N == 5
    /* .table_v3 = */    &__two_to_jby32[0],
#endif
};

#define D1 expf_v2_data.poly[0]
#define D2 expf_v2_data.poly[1]
#define D3 expf_v2_data.poly[2]
#define D4 expf_v2_data.poly[3]

#define EXPF_LN2_BY_TBLSZ expf_v2_data.ln2by_tblsz
#define EXPF_TBLSZ_BY_LN2 expf_v2_data.tblsz_byln2
#define EXPF_HUGE expf_v2_data.Huge
#define EXPF_TABLE expf_v2_data.table

#define EXPF_FARG_MIN -0x1.9fe368p6f    /* log(0x1p-150) ~= -103.97 */
#define EXPF_FARG_MAX 0x1.62e42ep6f    /* log(0x1p128)  ~=   88.72  */
#define Ln2 0x1.62e42fefa39efp-1
#define EXP_Y_INF 3
#define EXP_Y_ZERO 2

struct log_table {
    double lead, tail;
};

double _log_special(double x, double y, uint32_t code);
float _expf_special(float x, float y, uint32_t code);

/* Returns 0 if not int, 1 if odd int, 2 if even int.  The argument is
   the bit representation of a non-zero finite floating-point value.  */
static inline int
checkint(uint32_t iy)
{
    int e = iy >> 23 & 0xff;
    if (e < 0x7f)
        return 0;
    if (e > 0x7f + 23)
        return 2;
    if (iy & ((1 << (0x7f + 23 - e)) - 1))
        return 0;
    if (iy & (1 << (0x7f + 23 - e)))
        return 1;
    return 2;
}

static inline int
isSignalingNaN(float x)
{
    uint32_t ix = asuint32(x);
    return 2 * (ix ^ 0x00400000) > 2u * 0x7fc00000;;
}

static inline uint64_t top12(double x)
{
    /* 12 are the exponent bits */
    return asuint64(x) >> (64 - 12);
}

static inline int
zeroinfnan(uint32_t ix)
{
    return 2 * ix - 1 >= 2u * 0x7f800000 - 1;
}


static inline double_t
calculate_log(double_t x)
{
    double_t q, r;
    double_t dexpo, j_times_half;

    uint64_t ux = asuint64(x);

    int32_t expo = (ux >> 52) - 1023;

    flt64_t mant;
    mant.i = ux & 0x000fffffffffffffULL;

    dexpo = (double)expo;
    uint64_t mant_n = ux & MANT_MASK_N;

    /*
     * Step needed for better accuracy
    uint64_t mant_n1 = ux & MANT_MASK_N1;
    uint64_t j = (mant_n) + (mant_n1 << 1);
    */

    uint64_t j = (mant_n);

    mant.i |= 0x3fe0000000000000ULL;               /* F */
    j_times_half = asdouble(0x3fe0000000000000ULL | j); /* Y */

    j >>= (52 - N);

    /* f = F - Y */
    double_t f = j_times_half - mant.d;

    r = f * asdouble(TAB_F_INV[j]);

    double_t r2 = r * r;                /* r^2 */

    double_t temp = C2 + r * C3;

    q = r + r2 * temp;

    /* m*log(2) + log(G) - poly */

    temp = (dexpo * Ln2) + asdouble(log_256[j]);

    temp -= q;

    return temp;

}

static inline float calculate_exp(double_t x, uint64_t sign_bias)
{
    double_t q, dn, r, z;
    uint64_t n, j;

    if (unlikely(top12(x) > top12(88.0))) {

        if ((float)x > EXPF_FARG_MAX) {
            return _expf_special((float)x, asfloat((sign_bias >> 32) | PINFBITPATT_SP32), EXP_Y_INF);
        }

        if (((float)x) < EXPF_FARG_MIN) {
            return _expf_special((float)x, asfloat(sign_bias >> 32), EXP_Y_ZERO);
        }

    }

    z = x * EXPF_TBLSZ_BY_LN2;

    /*
     * n  = (int) scale(x)
     * dn = (double) n
     */
#undef FAST_INTEGER_CONVERSION
#define FAST_INTEGER_CONVERSION 1
#if FAST_INTEGER_CONVERSION
    dn = z + EXPF_HUGE;

    n = asuint64(dn);

    dn -= EXPF_HUGE;
#else
    n = z;
    dn = cast_i32_to_float(n);

#endif

    r = x - dn * EXPF_LN2_BY_TBLSZ;

    j = n % EXPF_TABLE_SIZE;

    double_t qtmp = D2 + (D3 * r);

    double_t r2 = r * r;

    double_t tbl = asdouble(sign_bias | (asuint64(__two_to_jby64[j]) + (n << (52 - EXPF_N))));

    q = r + (r2 * qtmp);

    double_t result = tbl + tbl * q;

    return (float_t)(result);

}

float ALM_PROTO_OPT(powf)(float x, float y)
{
    double_t logx, ylogx, result, q, r;
    double_t dx;
    uint32_t ux, uy;

    ux = asuint32(x);
    uy = asuint32(y);
    uint64_t sign_bias = 0;

    if (unlikely(ux - 0x3F880000 >= 0x7f800000 - 0x3F880000 || zeroinfnan(uy)))
    {

        /*  All x less than 1.0625, infinity, NaN and y = zero, infinity or NAN caught here
         *  x < 0x1p-126 or inf or nan.
         *  Either (x < 0x1p-126 or inf or nan) or (y is 0 or inf or nan).
         *
         */
        if (unlikely(zeroinfnan(uy)))
        {
            if (2 * uy == 0)
                return isSignalingNaN(x) ? x + y : 1.0f;

            if (ux == 0x3f800000)
                return isSignalingNaN(y) ? x + y : 1.0f;

            if (2 * ux > 2u * 0x7f800000 || 2 * uy > 2u * 0x7f800000)
                return x + y;

            if (2 * ux == 2 * 0x3f800000)
                return 1.0f;

            if ((2 * ux < 2 * 0x3f800000) == !(uy & 0x80000000))
                return 0.0f; /* |x|<1 && y==inf or |x|>1 && y==-inf.  */

            return y * y;
        }

        if (unlikely(zeroinfnan(ux)))
        {
            float_t x2 = x * x;

            if (ux & 0x80000000 && checkint(uy) == 1) /* x is -0 and y is odd */
            {
                x2 = -x2;
                sign_bias = SIGN_BIAS;
            }

            if (2 * ux == 0 && uy & 0x80000000)
            {
                x = INFINITY;
                ux = asuint32(x);
                return asfloat((sign_bias >> 32) | ux);
            }

            return uy & 0x80000000 ? (1 / x2) : x2; /* if y is negative, return 1/x else return x */
        }

        /* x and y are non-zero finite  */
        if (ux & 0x80000000) /* x is negative */
        {
            /* Finite x < 0 */
            int yint = checkint(uy);
            if (yint == 0)
                return (float)FN_PROTOTYPE(sqrt)(x);
            if (yint == 1)
                sign_bias = SIGN_BIAS;

            ux &= 0x7fffffff; /* x is negative, y is integer */
            x = asfloat(ux);
        }

        /* if 0.9375 < x < 1.0625 */
        if ((0x3F880000 - ux) < (0x3F880000 - 0x3F700000))
        {
            dx = (double_t)x;

            double_t  u, u2, u3, u7;
            double_t  A1, A2, B1, B2, R1, R2;
            static const double ca[5] = {
                      0x1.55555555554e6p-4, /* 1/2^2 * 3 */
                      0x1.9999999bac6d4p-7, /* 1/2^4 * 5 */
                      0x1.2492307f1519fp-9, /* 1/2^6 * 7 */
                      0x1.c8034c85dfff0p-12 /* 1/2^8 * 9 */
            };

            /*
             * Less than threshold, no table lookup
             *
             */

            flt64_t one_minus_mant;
            one_minus_mant.d = dx - 1.0;

            r = one_minus_mant.d;

            double_t u_by_2 = r / (2.0 + r);

            q = u_by_2 * r;  /* correction */

            u = u_by_2 + u_by_2;

#define CA1 ca[0]
#define CA2 ca[1]
#define CA3 ca[2]
#define CA4 ca[3]

            u2 = u * u;

            A1 = CA2 * u2 + CA1;
            A2 = CA4 * u2 + CA3;

            u3 = u2 * u;
            B1 = u3 * A1;

            u7 = u * (u3 * u3);
            B2 = u7 * A2;

            R1 = B1 + B2;
            R2 = R1 - q;

            logx = r + R2;

            ylogx = y * logx;

            result = calculate_exp(ylogx, sign_bias);

            return (float)result;

        }
    }

    dx = (double_t)x;

    logx = calculate_log(dx);

    ylogx = y * logx;

    result = calculate_exp(ylogx, sign_bias);

    return (float)result;
}
