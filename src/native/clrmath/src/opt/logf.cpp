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
#include "libm/libm_types.h"
#include "libm/libm_typehelper.h"

#define MASK_MANT_ALL7 0x007f8000
#define MASK_MANT8 0x00008000
#define MASK_SIGN 0x7FFFFFFF
#define LOGF_N  8
#define LOGF_POLY_DEGREE  2

#define FLAG_X_ZERO 0x1
#define FLAG_X_NEG  0x2
#define FLAG_X_NAN  0x3

#include "logf_data.h"

extern float_t _logf_special(float_t x, float_t y, uint32_t code);

extern "C" logf_table logf_lookup[];

struct logf_data_t {
    float_t log2_head, log2_tail;
    float_t poly[LOGF_POLY_DEGREE];
    //const float_t *tab;
    float_t poly1[LOGF_POLY_DEGREE];
};

static const logf_data_t logf_data = {
    /* .log2_head = */ 0x1.62e3p-1,
    /* .log2_tail = */ 0x1.2fefa2p-17,

    /* Polynomial constants */

    /* .poly = */ {
        0.5f, /* C1= 1/2 */
        3.333333432674407958984375E-1f, /* C2 = 1/3 */
    },

    // .tab = logf_lookup,

    /* Polynomial constants for cases near to 1 */
    /* .poly1 = */ {
        8.33333333333317923934e-02f,    /* A1 */
        1.25000000037717509602e-02f,    /* A2 */
    },
};

#define LOG2_HEAD logf_data.log2_head
#define LOG2_TAIL logf_data.log2_tail

static inline float_t
inline_log1pf(float_t x)
{
    float_t r, r2, w;

    r = x - 1.0f;

    w = r / (2.0f + r);

    float_t correction = w * r;

    w += w;

    float_t w2 = w * w;

#define A1 logf_data.poly1[0]
#define A2 logf_data.poly1[1]

    r2 = (w * w2 * (A1 + A2 * w2)) - correction;

    float_t f = r2 + r;

    return f;
}


/*
 * ISO-IEC-10967-2: Elementary Numerical Functions
 * Signature:
 *   float logf(float x)
 *
 * Spec:
 *   logf(x)
 *          = logf(x)           if x ∈ F and x > 0
 *          = x                 if x = qNaN
 *          = 0                 if x = 1
 *          = -inf              if x = (-0, 0}
 *          = NaN               otherwise
 *
 * Implementation Notes:
 *       x  =   2^m * (1 + A)     where 1 <= A < 2
 *
 *       x  =   2^m * (1 + (G + g)) where 1 <= 1+G < 2 and g < 2^(-8)
 *
 *    Let F = (1 + G) and f = g
 *       x  = 2^m*(F + f), so 1 <= F < 2, f < 2^(-8)
 *
 *       x  = (2^m) * 2 * (F/2+f/2)
 *               So, 0.5 <= F/2 < 1,   f/2 < 2^-9
 *
 *    logf(x) = logf(2^m) + logf(2) + logf(F/2 + f/2)
 *            => A        +   B     +       C
 *
 *       A =  logf(2^m) = m*logf(2)    .. (1)
 *       B =  logf(2)                  .. (2) => Constant can be precalculated
 *       C =  logf(F/2*(1 + f/F))      .. (3)
 *         =>  logf(F/2*(1 + f/F))
 *         =>  logf(F/2) + logf(1 + f/F)
 *
 *     logf(x)  = logf(2^m) + logf(2) + logf(F/2) + logf(1 + f/F)
 *
 *               (from log(a) + log(b) <=> log(a*b))
 *
 *             => m*logf(2) + logf(F) + logf(1 + r) where r = 1+f/F
 */

float
ALM_PROTO_OPT(logf)(float x)
{
    uint32_t ux = asuint32(x);

    if (unlikely(ux - 0x00800000 >= 0x7f800000 - 0x00800000))
    {
        /* x < 0x1p-126 or inf or nan. */
        if (ux * 2 == 0)                /* log(0) = -inf */
            return -INFINITY;
        if (ux == 0x7f800000)           /* log(inf) = inf */
            return x;
        if ((ux & 0x80000000) || ux * 2 >= 0xff000000)
            return (float)FN_PROTOTYPE(sqrt)(x);             /* Return NaN */

        /*
         * 'x' has to be denormal, Normalize it
         * there is a possibility that only the last (23'rd) bit is set
         * Hence multiply by 2^23 to bring to 1.xxxxx format.
         */
        ux = asuint32(x * 0x1p23f);
        ux -= 23 << 23;
    }

    int32_t expo = (ux >> EXPSHIFTBITS_SP32) - EMAX_SP32;
    float_t f_expo = (float_t)expo;

#define NEAR_ONE_LO asuint32(1 - 0x1.0p-4)
#define NEAR_ONE_HI asuint32(1 + 0x1.1p-4)

    /* Values very close to 1, e^(-1/16) <= x <= e^(1/16)*/
    if (unlikely(ux - NEAR_ONE_LO < NEAR_ONE_HI - NEAR_ONE_LO)) {
        return inline_log1pf(x);
    }

    /*
     * Here onwards, 'x' is neither -ve, nor close to 1
     */
    uint32_t mant, mant1, idx;
    float_t  y, f, finv, r, r2, q, w;

    mant = ux & MANTBITS_SP32;
    mant1 = ux & MASK_MANT_ALL7;
    /*This step is needed for more accuracy */
/*    mant1 += ((ux & MASK_MANT8) << 1); */

    idx = mant1 >> (EXPSHIFTBITS_SP32 - LOGF_N);

    y = asfloat(mant |= HALFEXPBITS_SP32);
    f = asfloat(mant1 |= HALFEXPBITS_SP32);

    logf_table* tbl = &logf_lookup[idx];

    finv = tbl->f_inv;

    r = (f - y) * finv;

#define C1 logf_data.poly[0]
#define C2 logf_data.poly[1]

    r2 = r * r;                 /* r^2 */
    q = r + (r2 * (C1 + r * C2));

    q = (f_expo * LOG2_TAIL) - q;

    w = (LOG2_HEAD * f_expo) + tbl->f_128_head;

    q = (tbl->f_128_tail + q) + w;

    return q;
}
