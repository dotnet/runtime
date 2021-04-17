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

double FN_PROTOTYPE_REF(asin)(double x)
{
    /* Computes arcsin(x).
       The argument is first reduced by noting that arcsin(x)
       is invalid for abs(x) > 1 and arcsin(-x) = -arcsin(x).
       For denormal and small arguments arcsin(x) = x to machine
       accuracy. Remaining argument ranges are handled as follows.
       For abs(x) <= 0.5 use
       arcsin(x) = x + x^3*R(x^2)
       where R(x^2) is a rational minimax approximation to
       (arcsin(x) - x)/x^3.
       For abs(x) > 0.5 exploit the identity:
        arcsin(x) = pi/2 - 2*arcsin(sqrt(1-x)/2)
       together with the above rational approximation, and
       reconstruct the terms carefully.
      */

      /* Some constants and split constants. */

    static const double
        piby2_tail = 6.1232339957367660e-17, /* 0x3c91a62633145c07 */
        hpiby2_head = 7.8539816339744831e-01, /* 0x3fe921fb54442d18 */
        piby2 = 1.5707963267948965e+00; /* 0x3ff921fb54442d18 */
    double u, v, y, s = 0.0, r;
    int xexp, xnan, transform = 0;

    unsigned long long ux, aux, xneg;
    GET_BITS_DP64(x, ux);
    aux = ux & ~SIGNBIT_DP64;
    xneg = (ux & SIGNBIT_DP64);
    xnan = (aux > PINFBITPATT_DP64);
    xexp = (int)((ux & EXPBITS_DP64) >> EXPSHIFTBITS_DP64) - EXPBIAS_DP64;

    /* Special cases */

    if (xnan)
    {
#ifdef WINDOWS
        return  __amd_handle_error("asin", __amd_asin, ux | 0x0008000000000000, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0, 1);
#else
        //return x + x; /* With invalid if it's a signalling NaN */
        if (ux & QNAN_MASK_64)
            return  __amd_handle_error("asin", __amd_asin, ux | 0x0008000000000000, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0, 1);
        else
            return  __amd_handle_error("asin", __amd_asin, ux | 0x0008000000000000, _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0, 1);
#endif
    }
    else if (xexp < -28)
    { /* y small enough that arcsin(x) = x */
#ifdef WINDOWS
        return x; //val_with_flags(x, AMD_F_INEXACT);
#else
        if ((ux == SIGNBIT_DP64) || (ux == 0x0))
            return x;
        else
            return  __amd_handle_error("asin", __amd_asin, ux, _UNDERFLOW, AMD_F_UNDERFLOW | AMD_F_INEXACT, ERANGE, x, 0.0, 1);
#endif
    }
    else if (xexp >= 0)
    { /* abs(x) >= 1.0 */
        if (x == 1.0)
            return piby2; //val_with_flags(piby2, AMD_F_INEXACT);
        else if (x == -1.0)
            return -piby2;//val_with_flags(-piby2, AMD_F_INEXACT);
        else
#ifdef WINDOWS
            return  __amd_handle_error("asin", __amd_asin, INDEFBITPATT_DP64, _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0, 1);
#else
            //return retval_errno_edom(x);
            return  __amd_handle_error("asin", __amd_asin, INDEFBITPATT_DP64, _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0, 1);
#endif
    }

    if (xneg) y = -x;
    else y = x;

    transform = (xexp >= -1); /* abs(x) >= 0.5 */

    if (transform)
    { /* Transform y into the range [0,0.5) */
        r = 0.5 * (1.0 - y);
        ASMSQRT(r, s);
        y = s;
    }
    else
        r = y * y;

    /* Use a rational approximation for [0.0, 0.5] */

    u = r * (0.227485835556935010735943483075 +
        (-0.445017216867635649900123110649 +
            (0.275558175256937652532686256258 +
                (-0.0549989809235685841612020091328 +
                    (0.00109242697235074662306043804220 +
                        0.0000482901920344786991880522822991 * r) * r) * r) * r) * r) /
        (1.36491501334161032038194214209 +
            (-3.28431505720958658909889444194 +
                (2.76568859157270989520376345954 +
                    (-0.943639137032492685763471240072 +
                        0.105869422087204370341222318533 * r) * r) * r) * r);

    if (transform)
    { /* Reconstruct asin carefully in transformed region */
        {
            double c, s1, p, q;
            unsigned long long us;
            GET_BITS_DP64(s, us);
            PUT_BITS_DP64(0xffffffff00000000 & us, s1);
            c = (r - s1 * s1) / (s + s1);
            p = 2.0 * s * u - (piby2_tail - 2.0 * c);
            q = hpiby2_head - 2.0 * s1;
            v = hpiby2_head - (p - q);
        }
    }
    else
    {
#ifdef WINDOWS
        /* Use a temporary variable to prevent VC++ rearranging
              y + y*u
           into
              y * (1 + u)
           and getting an incorrectly rounded result */
        double tmp;
        tmp = y * u;
        v = y + tmp;
#else
        v = y + y * u;
#endif
    }

    if (xneg) return -v;
    else return v;
}
