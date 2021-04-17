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

 /* Contains implementation of double pow(double x, double y)
  * x^y = 2^(y*log2(x))
  */

#include "clrmath.h"
#include "libm/libm_util.h"
#include "libm/libm_special.h"
#include "libm/libm_typehelper.h"

#define L__exp_bias 0x00000000000003ff /* 1023 */
#define L__mant_mask 0x000fffffffffffff
#define N 10
#define TABLE_SIZE (1ULL << N)
#define ABSOLUTE_VALUE 0x7FFFFFFFFFFFFFFF
#define TOP12_EXP_MAX 0x40862E42
#define MANTISSA_10_BITS 0x000FFC0000000000
#define MANTISSA_11_BIT 0x0000020000000000

#define EXP_X_NAN 1
#define EXP_Y_ZERO 2
#define EXP_Y_INF 3

double _exp_special(double x, double y, uint32_t code);

struct log_data {
    uint64_t head;
    uint64_t tail;
};

struct exp_data {
    uint64_t head;
    uint64_t tail;
};

typedef union {
    uint64_t value;
    uint32_t split[2];
} doubleword;

extern "C" log_data log_Finv[];
extern "C" log_data log_f_256[];
extern "C" exp_data exp_lookup[];

struct pow_data_t {
    double ALIGN(16) poly_log[8];
    double ALIGN(16) poly_exp[8];
    double_t L__real_log2_head,
        L__real_log2_tail,
        log2_by_N_head,
        log2_by_N_tail,
        N_by_log2;
};

static const pow_data_t  pow_data = {
    /*
     * Polynomial constants, 1/x! (reciprocal x)
     * To make better use of cache line,
     * we dont store 0! and 1!
     */

    /* .poly_log =      */    {       /* skip for 0/1 and 1/1 */
       0x1.0000000000000p-1,   /* 1/2 */
       0x1.5555555555555p-2,   /* 1/3 */
       0x1.0000000000000p-2,   /* 1/4 */
       0x1.999999999999ap-3,   /* 1/5 */
       0x1.5555555555555p-3,   /* 1/6 */
       0x1.2492492492492p-3,   /* 1/7 */
       0x1.0000000000000p-3,   /* 1/8 */
       0x1.c71c71c71c71cp-4,   /* 1/9 */
   },
   /* .poly_exp = */          {
       0x1.5555555555555p-3,   /* 1/3! * 3 */
       0x1.5555555555555p-5,   /* 1/4! * 5 */
       0x1.1111111111111p-7,   /* 1/5! * 7 */
       0x1.6c16c16c16c17p-10   /* 1/6! * 9 */
   },

    /* .L__real_log2_head = */ 0x1.62e42e0000000p-1,
    /* .L__real_log2_tail = */ 0x1.efa39ef35793cp-25,
    /* .log2_by_N_head = */    0x1.62e42f0000000p-11,
    /* .log2_by_N_tail = */    0x1.DF473DE6AF279p-36,
    /* .N_by_log2 = */         0x1.71547652b82fep10,
};

#define C1 pow_data.poly_log[0]
#define C2 pow_data.poly_log[1]
#define C3 pow_data.poly_log[2]
#define C4 pow_data.poly_log[3]

#define LOG2_HEAD pow_data.L__real_log2_head
#define LOG2_TAIL pow_data.L__real_log2_tail
#define LOG2_BY_N_HEAD pow_data.log2_by_N_head
#define LOG2_BY_N_TAIL pow_data.log2_by_N_tail
#define N_BY_LOG2 pow_data.N_by_log2

#define a0 0.5
#define a1 pow_data.poly_exp[0]
#define a2 pow_data.poly_exp[1]
#define a3 pow_data.poly_exp[2]

#define POW_X_ONE_Y_SNAN 1
#define POW_X_ZERO_Z_INF 2
#define POW_X_NAN 3
#define POW_Y_NAN 4
#define POW_X_NAN_Y_NAN 5
#define POW_X_NEG_Y_NOTINT 6
#define POW_Z_ZERO 7
#define POW_Z_DENORMAL 8
#define POW_Z_INF 9

double _pow_special(double x, double y, double z, uint32_t code);

static inline double_t
compute_log(uint64_t ux, double_t* log_lo, int32_t expadjust)
{
    /*
     * Calculate log(x) in higher precision and store as log_hi and log_lo
     *
     * x very close to 1.0 is handled differently, for x everywhere else
     * a brief explanation is given below
     *
     * x = (2 ^ m) * A
     * x = (2 ^ m) * (G + g) with (1 <= G < 2) and (g <= 2 ^ (-9))
     * x = (2 ^ m) * 2 * (G/2 + g/2)
     * x = (2 ^ m)* 2* (F + f) with (0.5 <= F < 1) and (f <= 2 ^ (-10))
     *
     * Y = (2 ^ (-1)) * (2 ^ (-m)) * (2 ^ m) * A
     * Now, range of Y is: 0.5 <= Y < 1
     * F = 0x100 + (first 8 mantissa bits) + (9th mantissa bit)
     * Now, range of F is: 256 <= F <= 512
     * F = F / 512
     * Now, range of F is: 0.5 <= F <= 1
     * f = -(Y - F), with (f <= 2^(-10))
     * log(x) = m * log(2) + log(2) + log(F - f)
     * log(x) = m * log(2) + log(2) + log(F) + log(1 -(f / F))
     * log(x) = m * log(2) + log(2 * F) + log(1 - r)
     * r = (f / F), with (r <= 2^(-9))
     * r = f * (1 / F) with (1 / F) precomputed to avoid division
     * log(x) = m * log(2) + log(G) - poly
     * log(G) is precomputed
     * poly = (r + (r ^ 2) / 2 + (r ^ 3) / 3 + (r ^ 4) / 4) + (r ^ 5) / 5) + (r ^ 6) / 6))
     * log(2) and log(G) need to be maintained in extra precision
     * to avoid losing precision in the calculations
     * Store the exponent of x in xexp and put
     * f into the range [0.5, 1)
    */
    int32_t xexp;
    double_t r, r1, w, z, w1, resT_t, resT, resH;
    double_t u, f, z1, q, f1, f2, poly;
    xexp = ((ux & EXPBITS_DP64) >> EXPSHIFTBITS_DP64) - EXPBIAS_DP64 - expadjust;
    double_t exponent_x = (double_t)xexp;
    f1 = asdouble((ux & MANTBITS_DP64) | HALFEXPBITS_DP64);
    uint64_t index = (ux & MANTISSA_10_BITS);
    index += ((ux & MANTISSA_11_BIT) << 1);
    f = asdouble(index | HALFEXPBITS_DP64);
    index = index >> 42;
    f2 = f - f1;
    /*
    * At this point, x = 2**xexp * ( f1  +  f2 ) where
    * f1 = j/128, j = 64, 65, ..., 128 and |f2| <= 1/256.
    * Compute 'u' from Taylor series of log2(1+f1/f2)
    */
    z1 = asdouble(log_Finv[index].tail);
    q = asdouble(log_Finv[index].head);
    w = asdouble(log_f_256[index].head);
    w1 = asdouble(log_f_256[index].tail);
    r = f2 * z1;
    r1 = f2 * q;
    u = r + r1;
    z = r1 - u;

    /*
     * Polynomial evaluation
     * For N=10,
     * poly = u * (u * (C1 + u * (C2 + u * (C3 + u * (C4)))))
    */

    double_t A1, B0, usquare;

    /* Estrin Scheme */
    A1 = C1 + u * C2;
    double_t A2 = C3 + u * C4;
    usquare = u * u;
    B0 = usquare * A1; /* u^2 * C1 + u^3 * C2 */
    double ufour = usquare * usquare;
    poly = B0 + ufour * A2;
    poly = (z + r) + poly;
    resT_t = (LOG2_TAIL * exponent_x - poly) + w1;
    resT = resT_t - u;
    resH = LOG2_HEAD * exponent_x + w;
    double log_hi = resT + resH;
    *log_lo = resH - log_hi + resT;
    return log_hi;
}

static inline double_t
compute_exp(double_t v, double_t vt, uint64_t result_sign)
{
    double_t A1, A2, z, r, usquare, w, w1, B0, poly;
    const double_t EXP_HUGE = 0x1.8000000000000p52;
    int64_t n;
    int64_t i;
    doubleword xword;
    double temp = v;
    v = v * N_BY_LOG2;
    xword.value = asuint64(temp);
    int32_t abs_ylogx = (xword.value & ABSOLUTE_VALUE) >> 32;

    /* check if x > 1024 * ln(2) */
    if (unlikely(abs_ylogx >= TOP12_EXP_MAX))
    {
        /* if abs(y * log(x)) > 709.7822265625 */
        if (xword.value >= EXP_MIN)
        {
            /* if y * log(x) < -745.133219101941222106688655913 */
            v = asdouble(0x0 | result_sign);
            return _exp_special((double)xword.value, v, EXP_Y_ZERO);

        }
        if (temp > EXP_MAX_DOUBLE)
        {
            /* if y * log(x) > 709.7822265625 */
            v = asdouble(EXPBITS_DP64 | result_sign);
            return  _exp_special((double)xword.value, v, EXP_Y_INF);

        }
        abs_ylogx = 0xfff;
    }
    double_t fastconvert = v + EXP_HUGE;
    n = asuint64(fastconvert);
    double_t dn = fastconvert - EXP_HUGE;

    /* Table size = 1024. Here N = 10 */
    int32_t index = n % (1 << N);
    r = temp - (dn * LOG2_BY_N_HEAD);
    int64_t m = ((n - index) << (EXPSHIFTBITS_DP64 - N)) + 0x3ff0000000000000;
    r = (r - (LOG2_BY_N_TAIL * dn)) + vt;
    /*
     * Taylor's series to evaluate exp(r)
     * For N = 11
     * polynomial = r * (1.0 + r * (0.5 + r * (1/3! + r * (1/4!)))
     * for N = 10
     * polynomial =r * (1.0 + r * (0.5 + r * (1/3! + r * (1/4! + r * (1/5!))))
    */
    /*Estrin's approach used here for polynomial evaluation
     *
     * N = 10
     *
     */
    A1 = a0 + r * a1; /* A1 = 0.5 + r ^ (1/3!) */
    usquare = r * r;
    A2 = a2 + r * a3;
    B0 = r + usquare * A1; /* r + 0.5 * r ^ 2 + r ^ 3 * (1 / 3!) */
    poly = B0 + usquare * usquare * A2;
    w = asdouble(exp_lookup[index].head);
    w1 = asdouble(exp_lookup[index].tail);
    temp = w1 + poly * w1;
    z = poly * w;
    double_t result = w + (z + temp);

    /* Process denormals */
    if (unlikely(abs_ylogx == 0xfff))
    {
        int32_t m2 = (int32_t)((n - index) >> N);
        if (result < 1.0 || m2 < EMIN_DP64)
        {
            m2 = m2 + 1074;
            i = 1ULL << m2;
            dn = asdouble(i);
            n = asuint64(result * dn);
            result = asdouble(n | result_sign);
            return result;
        }
    }

    z = asdouble(m | result_sign);
    return result * z;
}

static inline uint32_t checkint(uint64_t u)
{
    int32_t u_exp = ((u & ABSOLUTE_VALUE) >> EXPSHIFTBITS_DP64);
    /*
     * See whether u is an integer.
     * status = 0 means not an integer.
     * status = 1 means odd integer.
     * status = 2 means even integer.
    */
    if (u_exp < 0x3ff)
        return 0;
    if (u_exp > 0x3ff + EXPSHIFTBITS_DP64)
        return 2;
    if (u & ((1ULL << (0x3ff + EXPSHIFTBITS_DP64 - u_exp)) - 1))
        return 0;
    if (u & (1ULL << (0x3ff + EXPSHIFTBITS_DP64 - u_exp)))
        return 1; /* odd integer */
    return 2;
}

/* Returns 1 if input is the bit representation of 0, infinity or nan. */
static inline int checkzeroinfnan(uint64_t i)
{
    return 2 * i - 1 >= 2 * EXPBITS_DP64 - 1;
}

static inline int issignaling_inline(double x)
{
    uint64_t ix;
    ix = asuint64(x);
    return 2 * (ix ^ QNAN_MASK_64) > 2 * QNANBITPATT_DP64;
}

static inline double _pow_inexact(double x)
{
    double_t a = 0x1.0p+0; /* a = 1.0 */
    double_t b = 0x1.4000000000000p+3; /* b = 10.0 */

    // Managed code doesn't support FP exceptions
    //__asm __volatile("divsd %1, %0" :  "+x" (a) : "x" (b));

    return x;
}

double
ALM_PROTO_OPT(pow)(double x, double y)
{
    double_t log_lo;
    double_t f;
    int32_t expadjust = 0;
    uint64_t ux, uy, result_sign;
    uint64_t infinity = EXPBITS_DP64;
    uint64_t one = ONEEXPBITS_DP64;
    ux = asuint64(x);
    uy = asuint64(y);
    uint32_t xhigh = ux >> EXPSHIFTBITS_DP64; /* Top 12 bits of x */
    uint32_t yhigh = uy >> EXPSHIFTBITS_DP64; /* Top 12 bits of y */
    result_sign = 0; /* Hold the sign of the result */

    if (unlikely(xhigh - 0x001 >= 0x7ff - 0x001
        || (yhigh & 0x7ff) - 0x3be >= 0x43e - 0x3be))
    {
        if (unlikely(checkzeroinfnan(uy)))
        {
            if (2 * uy == 0)
                return issignaling_inline(x) ? x + y : 1.0;
            if (ux == one)
                return issignaling_inline(y) ? x + y : 1.0;
            if (2 * ux > 2 * infinity || 2 * uy > 2 * infinity)
                return x + y;
            if (2 * ux == 2 * one)
                return 1.0;
            if ((2 * ux < 2 * one) == !(uy >> 63))
                return 0.0; /* |x| < 1 && y = inf or |x| > 1 && y = -inf */
            return y * y;
        }
        if (unlikely(checkzeroinfnan(ux)))
        {

            double x2 = x * x;
            /* x is negative , y is odd*/
            if (ux >> 63 && checkint(uy) == 1)
            {
                result_sign = SIGNBIT_DP64;
            }
            if (2 * ux == 0 && uy >> 63)
            {
                x2 = INFINITY;
                x2 = asdouble(ux | result_sign);
                return x2;
            }
            x2 = asdouble(asuint64(x2) | result_sign);
            return uy >> 63 ? (1 / x2) : x2;
        }
        /* Here x and y are non-zero finite. */
        if (ux >> 63)
        {
            /* Finite x < 0 */
            uint32_t yint = checkint(uy);
            if (yint == 0)
                return sqrt(x);
            if (yint == 1)
                result_sign = SIGNBIT_DP64;
            ux &= ABSOLUTE_VALUE;
            xhigh &= 0x7ff;
        }
        if ((yhigh & 0x7ff) - 0x3be >= 0x43e - 0x3be) {
            /* Note: sign_bias = 0 here because y is not odd. */
            if (ux == one)
            {
                return _pow_inexact(1.0);
            }
            if ((yhigh & 0x7ff) < 0x3be)
            {
                /* |y| < 2 ^ -65, x ^ y ~= 1 + y * log(x) */
                return ux > one ? 1.0 + y : 1.0 - y;
            }
            return (ux > one) == (yhigh < 0x800) ?
                (DBL_MAX * DBL_MAX) :
                _pow_special(x, y, 0.0, POW_Z_ZERO);
        }

        if (xhigh == 0)
        {
            /* subnormal x */
            uint64_t mant = ux & MANTBITS_DP64;
            f = (double)asuint64((double)(mant | ONEEXPBITS_DP64));
            double_t temp = f - 1.0;
            ux = asuint64(temp);
            expadjust = 1022;
        }
    }

    double_t log_hi = compute_log(ux, &log_lo, expadjust);

    /* Multiplication of log_hi and log_lo with y */
    double_t v = log_hi * y;
    double_t vt = y * log_lo + fma(y, log_hi, -v);

    double_t result = compute_exp(v, vt, result_sign);
    return result;
}
