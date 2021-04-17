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
  * Literal translation of ASM routine + optimizations
  *
  * ISO-IEC-10967-2: Elementary Numerical Functions
  * Signature:
  *   double exp(double x)
  *
  * Spec:
  *   exp(1) = e
  *   exp(x) = 1           if x ∈ F and exp(x) ≠ eˣ
  *   exp(x) = 1           if x = -0
  *   exp(x) = +inf        if x = +inf
  *   exp(x) = 0           if x = -inf
  *   exp(x) = eˣ
  *
  *   exp(x) overflows     if (approximately) x > ln(DBL_MAX). (709.782712893384)
  */

  /*
   * Implementation Notes
   * ----------------------
   * 1. Argument Reduction:
   *      e^x = 2^(x/ln2) = 2^(x*(64/ln(2))/64)     --- (1)
   *
   *      Choose 'n' and 'f', such that
   *      x * 64/ln2 = n + f                        --- (2) | n is integer
   *							  | |f| <= 0.5
   *     Choose 'm' and 'j' such that,
   *      n = (64 * m) + j                          --- (3)
   *
   *     From (1), (2) and (3),
   *      e^x = 2^((64*m + j + f)/64)
   *          = (2^m) * (2^(j/64)) * 2^(f/64)
   *          = (2^m) * (2^(j/64)) * e^(f*(ln(2)/64))
   *
   * 2. Table Lookup
   *      Values of (2^(j/64)) are precomputed, j = 0, 1, 2, 3 ... 63
   *
   * 3. Polynomial Evaluation
   *   From (2),
   *     f = x*(64/ln(2)) - n
   *   Let,
   *     r  = f*(ln(2)/64) = x - n*(ln(2)/64)
   *
   * 4. Reconstruction
   *      Thus,
   *        e^x = (2^m) * (2^(j/64)) * e^r
   *
   */
#include "clrmath.h"
#include "libm/libm_util.h"
#include "libm/libm_special.h"
#include "libm/libm_types.h"
#include "libm/libm_typehelper.h"

   /*
    * N defines the precision with which we deal with 'x'
    * I.O.W (1 << N) is the size  of the look up table
    */

#define EXP_N 6

#include "exp_data.h"

double _exp_special(double x, double y, uint32_t code);

static inline uint32_t top12(double x)
{
    return asuint64(x) >> 52;
}

#define FMAX_X			 0x1.62e42fefa39efp+9
#define FMIN_X			-0x1.74910d52d3051p+9
#define DENORMAL_LOW    -0x1.74046dfefd9d0p+9
#define DENORMAL_MIN     0x0000000000000001

double
ALM_PROTO_OPT(exp)(double x)
{
    double_t r, q, dn;
    int64_t  n, m;
    int32_t  j;
    flt64_t  q1;
    q1.i = 0;

#define EXP_X_NAN       1
#define EXP_Y_ZERO      2
#define EXP_Y_INF       3

    /*
     * Top 11 bits, ignoring sign bit
     * this is with BIAS
     */
    uint32_t exponent = top12(x) & 0x7ff;
    /*
     * 11-bit 'exponent' is compared with, 12-bit unsigned value
     * one comparison for multiple decisions
      */
    if (unlikely(exponent - top12(0x1p-54) >= top12(512.0) - top12(0x1p-54))) {
        if (exponent - top12(0x1p-54) >= 0x80000000)
            return 1.0;

        if (x > FMAX_X) {
            if (_isnan(x))
                return  _exp_special(x, asdouble(QNANBITPATT_DP64), EXP_X_NAN);

            if (x == INFINITY)
                return x; /* No exception to be raised */

            return  _exp_special(x, asdouble(PINFBITPATT_DP64), EXP_Y_INF);
        }

        if (x <= FMIN_X) {
            if (asuint64(x) == NINFBITPATT_DP64)
                return  0.0; /* No exception to be raised */

            return _exp_special(x, 0.0, EXP_Y_ZERO);
        }

        if (x <= DENORMAL_LOW)
            return _exp_special(x, asdouble(DENORMAL_MIN), EXP_Y_ZERO);


        exponent = 0xfff;
    }

    double_t a = x * EXP_TBLSZ_BY_LN2;

#define FAST_INTEGER_CONVERSION 1
#if FAST_INTEGER_CONVERSION
    q1.d = a + EXP_HUGE;
    n = q1.i;
    dn = q1.d - EXP_HUGE;
#else
    n = cast_double_to_i64(a);
    dn = cast_i64_to_double(n);
#endif

    double_t r1 = x + dn * EXP_LN2_BY_TBLSZ_HEAD;
    double_t r2 = dn * EXP_LN2_BY_TBLSZ_TAIL;
    r = r1 + r2;

    /* table index, for lookup, truncated */
    j = n % EXP_TABLE_SIZE;

    /*
     * n-j/TABLE_SIZE, TABLE_SIZE = 1<<N
     * combining
     *    (n - j) >> N   and
     *    m <<= 52
     */
    m = (n - j) << (52 - EXP_N);

    /* redefining for easy reading */
#define C2 EXP_C2
#define C3 EXP_C3
#define C4 EXP_C4
#define C5 EXP_C5
#define C6 EXP_C6
#define C7 EXP_C7

#define ESTRIN_SCHEME  0xee
#define HORNER_SCHEME  0xef

#define POLY_EVAL_METHOD ESTRIN_SCHEME
#define EXP_POLY_DEGREE 6
#if POLY_EVAL_METHOD == HORNER_SCHEME
#if !defined(EXP_POLY_DEGREE)
#define EXP_POLY_DEGREE 6
#endif
#if EXP_POLY_DEGREE == 7
    q = r * (1 + r * (C2 + r * (C3 + r * (C4 + r * (C5 + r * (C6 + r * C7))))));
#elif   EXP_POLY_DEGREE == 6
    q = r * (1 + r * (C2 + r * (C3 + r * (C4 + r * (C5 + r * C6)))));
#elif EXP_POLY_DEGREE == 5
    q = r * (1 + r * (C2 + r * (C3 + r * (C4 + r * C5))));
#elif EXP_POLY_DEGREE == 4
    q = r * (1 + r * (C2 + r * (C3 + r * C4)));
#elif EXP_POLY_DEGREE == 3
    q = r * (1 + r * (C2 + r * C3));
#else  /* Poly order <=2 */
    q = r * (1 + r * C2);
#endif	/* Order <=2 && Order == 3 */

#elif POLY_EVAL_METHOD == ESTRIN_SCHEME
    /* Estrin's */
    // r + ((r*r)*(1/2 + (r*1/6))) +
    // ((r*r) * (r*r)) * (1/24 + (r * (1/120 + (r*1/720))))

    r2 = r * r; 			/* r^2 */
    q = r + (r2 * (C2 + r * C3));

#if EXP_POLY_DEGREE == 4
    q += (r2 * r2) * C4; /* r^4 * C4 */
#elif EXP_POLY_DEGREE == 5
    q += (r2 * r2) * (C4 + r * C5);
#elif EXP_POLY_DEGREE == 6
    q += (r2 * r2) * (C4 + r * (C5 + r * C6));
#endif
#else
    #warning "POLY_EVAL_METHOD is not defined"
#endif  /* if HORNER_SCHEME || ESTRIN_SCHEME */

        /* f(j)*q + f1 + f2 */
        struct exp_table* tbl = &((struct exp_table*)EXP_TABLE_DATA)[j];
    q = q * tbl->main + tbl->head + tbl->tail;

    /*
     * Processing denormals
     */
    if (unlikely(exponent == 0xfff)) {
        /* re-calculate m */
        int32_t m2 = (int32_t)((n - j) >> EXP_N);
        if (m2 <= -1022) {
            if (m2 < -1022 || q < 1.0) {
                /* Process true de-normals */
                m2 += 1074;
                flt64u_t tmp;
                tmp.i = (1ULL << m2);
                return q * tmp.d;
            }
        }
    }

    q1.d = asdouble(m + asuint64(q));

    return q1.d;

#if defined(__ENABLE_IEEE_EXCEPTIONS)
#endif
}
