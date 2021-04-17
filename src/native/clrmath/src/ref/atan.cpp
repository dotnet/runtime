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

double FN_PROTOTYPE_REF(atan)(double x)
{

    /* Some constants and split constants. */

    static double piby2 = 1.5707963267948966e+00; /* 0x3ff921fb54442d18 */
    double chi, clo, v, s, q, z;

    /* Find properties of argument x. */

    unsigned long long ux, aux, xneg;
    GET_BITS_DP64(x, ux);
    aux = ux & ~SIGNBIT_DP64;
    xneg = (ux != aux);

    if (xneg) v = -x;
    else v = x;

    /* Argument reduction to range [-7/16,7/16] */

    if (aux < 0x3e50000000000000) /* v < 2.0^(-26) */
    {
        /* x is a good approximation to atan(x) and avoids working on
           intermediate denormal numbers */
        if (aux == 0x0000000000000000)
            return x;
        else
#ifdef WINDOWS
            return x; //val_with_flags(x, AMD_F_INEXACT);
#else
            return  __amd_handle_error("atan", __amd_atan, ux, _UNDERFLOW, AMD_F_UNDERFLOW | AMD_F_INEXACT, ERANGE, x, 0.0, 1);
#endif
    }
    else if (aux > 0x4003800000000000) /* v > 39./16. */
    {

        if (aux > PINFBITPATT_DP64)
        {
            /* x is NaN */
#ifdef WINDOWS
            return  __amd_handle_error("atan", __amd_atan, ux | 0x0008000000000000, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0, 1);
#else
            return x + x; /* Raise invalid if it's a signalling NaN */
#endif
        }
        else if (aux > 0x4370000000000000)
        { /* abs(x) > 2^56 => arctan(1/x) is
             insignificant compared to piby2 */
            if (xneg)
                return -piby2;//val_with_flags(-piby2, AMD_F_INEXACT);
            else
                return piby2;//val_with_flags(piby2, AMD_F_INEXACT);
        }

        x = -1.0 / v;
        /* (chi + clo) = arctan(infinity) */
        chi = 1.57079632679489655800e+00; /* 0x3ff921fb54442d18 */
        clo = 6.12323399573676480327e-17; /* 0x3c91a62633145c06 */
    }
    else if (aux > 0x3ff3000000000000) /* 39./16. > v > 19./16. */
    {
        x = (v - 1.5) / (1.0 + 1.5 * v);
        /* (chi + clo) = arctan(1.5) */
        chi = 9.82793723247329054082e-01; /* 0x3fef730bd281f69b */
        clo = 1.39033110312309953701e-17; /* 0x3c7007887af0cbbc */
    }
    else if (aux > 0x3fe6000000000000) /* 19./16. > v > 11./16. */
    {
        x = (v - 1.0) / (1.0 + v);
        /* (chi + clo) = arctan(1.) */
        chi = 7.85398163397448278999e-01; /* 0x3fe921fb54442d18 */
        clo = 3.06161699786838240164e-17; /* 0x3c81a62633145c06 */
    }
    else if (aux > 0x3fdc000000000000) /* 11./16. > v > 7./16. */
    {
        x = (2.0 * v - 1.0) / (2.0 + v);
        /* (chi + clo) = arctan(0.5) */
        chi = 4.63647609000806093515e-01; /* 0x3fddac670561bb4f */
        clo = 2.26987774529616809294e-17; /* 0x3c7a2b7f222f65e0 */
    }
    else  /* v < 7./16. */
    {
        x = v;
        chi = 0.0;
        clo = 0.0;
    }

    /* Core approximation: Remez(4,4) on [-7/16,7/16] */

    s = x * x;
    q = x * s *
        (0.268297920532545909e0 +
            (0.447677206805497472e0 +
                (0.220638780716667420e0 +
                    (0.304455919504853031e-1 +
                        0.142316903342317766e-3 * s) * s) * s) * s) /
        (0.804893761597637733e0 +
            (0.182596787737507063e1 +
                (0.141254259931958921e1 +
                    (0.424602594203847109e0 +
                        0.389525873944742195e-1 * s) * s) * s) * s);

    z = chi - ((q - clo) - x);

    if (xneg) z = -z;
    return z;
}
