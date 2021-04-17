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
#include "libm/libm_inlines.h"

#undef _FUNCNAME
#define _FUNCNAME "asinh"
double FN_PROTOTYPE_REF(asinh)(double x)
{

    unsigned long long ux, ax, xneg;
    double absx, r, rarg, t, r1, r2, poly, s, v1, v2;
    int xexp;

    static const unsigned long long
        rteps = 0x3e46a09e667f3bcd,    /* sqrt(eps) = 1.05367121277235086670e-08 */
        recrteps = 0x4196a09e667f3bcd; /* 1/rteps = 9.49062656242515593767e+07 */

      /* log2_lead and log2_tail sum to an extra-precise version
         of log(2) */
    static const double
        log2_lead = 6.93147122859954833984e-01,  /* 0x3fe62e42e0000000 */
        log2_tail = 5.76999904754328540596e-08;  /* 0x3e6efa39ef35793c */


    GET_BITS_DP64(x, ux);
    ax = ux & ~SIGNBIT_DP64;
    xneg = ux & SIGNBIT_DP64;
    PUT_BITS_DP64(ax, absx);

    if ((ux & EXPBITS_DP64) == EXPBITS_DP64)
    {
        /* x is either NaN or infinity */
        if (ux & MANTBITS_DP64)
        {
#ifdef WINDOWS
            /* x is NaN */
            return __amd_handle_error("asinh", __amd_asinh, ux | 0x0008000000000000, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0, 1);
#else
            if (ux & QNAN_MASK_64)
                return __amd_handle_error("asinh", __amd_asinh, ux | 0x0008000000000000, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0, 1);
            else
                return __amd_handle_error("asinh", __amd_asinh, ux | 0x0008000000000000, _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0, 1);
#endif
        }
        else
        {
            /* x is infinity. Return the same infinity. */
            return x;
        }
    }
    else if (ax < rteps) /* abs(x) < sqrt(epsilon) */
    {
        if (ax == 0x0000000000000000)
        {
            /* x is +/-zero. Return the same zero. */
            return x;
        }
        else
        {
            /* Tiny arguments approximated by asinh(x) = x
               - avoid slow operations on denormalized numbers */
#ifdef WINDOWS
            return x; //return val_with_flags(x,AMD_F_INEXACT);
#else
            return __amd_handle_error("asinh", __amd_asinh, ux, _UNDERFLOW, AMD_F_UNDERFLOW | AMD_F_INEXACT, ERANGE, x, 0.0, 1);
#endif
        }
    }


    if (ax <= 0x3ff0000000000000) /* abs(x) <= 1.0 */
    {
        /* Arguments less than 1.0 in magnitude are
           approximated by [4,4] or [5,4] minimax polynomials
           fitted to asinh series 4.6.31 (x < 1) from Abramowitz and Stegun
        */
        t = x * x;
        if (ax < 0x3fd0000000000000)
        {
            /* [4,4] for 0 < abs(x) < 0.25 */
            poly =
                (-0.12845379283524906084997e0 +
                    (-0.21060688498409799700819e0 +
                        (-0.10188951822578188309186e0 +
                            (-0.13891765817243625541799e-1 -
                                0.10324604871728082428024e-3 * t) * t) * t) * t) /
                (0.77072275701149440164511e0 +
                    (0.16104665505597338100747e1 +
                        (0.11296034614816689554875e1 +
                            (0.30079351943799465092429e0 +
                                0.235224464765951442265117e-1 * t) * t) * t) * t);
        }
        else if (ax < 0x3fe0000000000000)
        {
            /* [4,4] for 0.25 <= abs(x) < 0.5 */
            poly =
                (-0.12186605129448852495563e0 +
                    (-0.19777978436593069928318e0 +
                        (-0.94379072395062374824320e-1 +
                            (-0.12620141363821680162036e-1 -
                                0.903396794842691998748349e-4 * t) * t) * t) * t) /
                (0.73119630776696495279434e0 +
                    (0.15157170446881616648338e1 +
                        (0.10524909506981282725413e1 +
                            (0.27663713103600182193817e0 +
                                0.21263492900663656707646e-1 * t) * t) * t) * t);
        }
        else if (ax < 0x3fe8000000000000)
        {
            /* [4,4] for 0.5 <= abs(x) < 0.75 */
            poly =
                (-0.81210026327726247622500e-1 +
                    (-0.12327355080668808750232e0 +
                        (-0.53704925162784720405664e-1 +
                            (-0.63106739048128554465450e-2 -
                                0.35326896180771371053534e-4 * t) * t) * t) * t) /
                (0.48726015805581794231182e0 +
                    (0.95890837357081041150936e0 +
                        (0.62322223426940387752480e0 +
                            (0.15028684818508081155141e0 +
                                0.10302171620320141529445e-1 * t) * t) * t) * t);
        }
        else
        {
            /* [5,4] for 0.75 <= abs(x) <= 1.0 */
            poly =
                (-0.4638179204422665073e-1 +
                    (-0.7162729496035415183e-1 +
                        (-0.3247795155696775148e-1 +
                            (-0.4225785421291932164e-2 +
                                (-0.3808984717603160127e-4 +
                                    0.8023464184964125826e-6 * t) * t) * t) * t) * t) /
                (0.2782907534642231184e0 +
                    (0.5549945896829343308e0 +
                        (0.3700732511330698879e0 +
                            (0.9395783438240780722e-1 +
                                0.7200057974217143034e-2 * t) * t) * t) * t);
        }
        return x + x * t * poly;
    }
    else if (ax < 0x4040000000000000)
    {
        /* 1.0 <= abs(x) <= 32.0 */
        /* Arguments in this region are approximated by various
           minimax polynomials fitted to asinh series 4.6.31
           in Abramowitz and Stegun.
        */
        t = x * x;
        if (ax >= 0x4020000000000000)
        {
            /* [3,3] for 8.0 <= abs(x) <= 32.0 */
            poly =
                (-0.538003743384069117e-10 +
                    (-0.273698654196756169e-9 +
                        (-0.268129826956403568e-9 -
                            0.804163374628432850e-29 * t) * t) * t) /
                (0.238083376363471960e-9 +
                    (0.203579344621125934e-8 +
                        (0.450836980450693209e-8 +
                            0.286005148753497156e-8 * t) * t) * t);
        }
        else if (ax >= 0x4010000000000000)
        {
            /* [4,3] for 4.0 <= abs(x) <= 8.0 */
            poly =
                (-0.178284193496441400e-6 +
                    (-0.928734186616614974e-6 +
                        (-0.923318925566302615e-6 +
                            (-0.776417026702577552e-19 +
                                0.290845644810826014e-21 * t) * t) * t) * t) /
                (0.786694697277890964e-6 +
                    (0.685435665630965488e-5 +
                        (0.153780175436788329e-4 +
                            0.984873520613417917e-5 * t) * t) * t);

        }
        else if (ax >= 0x4000000000000000)
        {
            /* [5,4] for 2.0 <= abs(x) <= 4.0 */
            poly =
                (-0.209689451648100728e-6 +
                    (-0.219252358028695992e-5 +
                        (-0.551641756327550939e-5 +
                            (-0.382300259826830258e-5 +
                                (-0.421182121910667329e-17 +
                                    0.492236019998237684e-19 * t) * t) * t) * t) * t) /
                (0.889178444424237735e-6 +
                    (0.131152171690011152e-4 +
                        (0.537955850185616847e-4 +
                            (0.814966175170941864e-4 +
                                0.407786943832260752e-4 * t) * t) * t) * t);
        }
        else if (ax >= 0x3ff8000000000000)
        {
            /* [5,4] for 1.5 <= abs(x) <= 2.0 */
            poly =
                (-0.195436610112717345e-4 +
                    (-0.233315515113382977e-3 +
                        (-0.645380957611087587e-3 +
                            (-0.478948863920281252e-3 +
                                (-0.805234112224091742e-12 +
                                    0.246428598194879283e-13 * t) * t) * t) * t) * t) /
                (0.822166621698664729e-4 +
                    (0.135346265620413852e-2 +
                        (0.602739242861830658e-2 +
                            (0.972227795510722956e-2 +
                                0.510878800983771167e-2 * t) * t) * t) * t);
        }
        else
        {
            /* [5,5] for 1.0 <= abs(x) <= 1.5 */
            poly =
                (-0.121224194072430701e-4 +
                    (-0.273145455834305218e-3 +
                        (-0.152866982560895737e-2 +
                            (-0.292231744584913045e-2 +
                                (-0.174670900236060220e-2 -
                                    0.891754209521081538e-12 * t) * t) * t) * t) * t) /
                (0.499426632161317606e-4 +
                    (0.139591210395547054e-2 +
                        (0.107665231109108629e-1 +
                            (0.325809818749873406e-1 +
                                (0.415222526655158363e-1 +
                                    0.186315628774716763e-1 * t) * t) * t) * t) * t);
        }
        log_kernel_amd64(absx, ax, &xexp, &r1, &r2);
        r1 = ((xexp + 1) * log2_lead + r1);
        r2 = ((xexp + 1) * log2_tail + r2);
        /* Now (r1,r2) sum to log(2x). Add the term
           1/(2.2.x^2) = 0.25/t, and add poly/t, carefully
           to maintain precision. (Note that we add poly/t
           rather than poly because of the *x factor used
           when generating the minimax polynomial) */
        v2 = (poly + 0.25) / t;
        r = v2 + r1;
        s = ((r1 - r) + v2) + r2;
        v1 = r + s;
        v2 = (r - v1) + s;
        r = v1 + v2;
        if (xneg)
            return -r;
        else
            return r;
    }
    else
    {
        /* abs(x) > 32.0 */
        if (ax > recrteps)
        {
            /* Arguments greater than 1/sqrt(epsilon) in magnitude are
               approximated by asinh(x) = ln(2) + ln(abs(x)), with sign of x */
               /* log_kernel_amd(x) returns xexp, r1, r2 such that
                  log(x) = xexp*log(2) + r1 + r2 */
            log_kernel_amd64(absx, ax, &xexp, &r1, &r2);
            /* Add (xexp+1) * log(2) to z1,z2 to get the result asinh(x).
               The computed r1 is not subject to rounding error because
               (xexp+1) has at most 10 significant bits, log(2) has 24 significant
               bits, and r1 has up to 24 bits; and the exponents of r1
               and r2 differ by at most 6. */
            r1 = ((xexp + 1) * log2_lead + r1);
            r2 = ((xexp + 1) * log2_tail + r2);
            if (xneg)
                return -(r1 + r2);
            else
                return r1 + r2;
        }
        else
        {
            rarg = absx * absx + 1.0;
            /* Arguments such that 32.0 <= abs(x) <= 1/sqrt(epsilon) are
               approximated by
                 asinh(x) = ln(abs(x) + sqrt(x*x+1))
               with the sign of x (see Abramowitz and Stegun 4.6.20) */
               /* Use assembly instruction to compute r = sqrt(rarg); */
            ASMSQRT(rarg, r);
            r += absx;
            GET_BITS_DP64(r, ax);
            log_kernel_amd64(r, ax, &xexp, &r1, &r2);
            r1 = (xexp * log2_lead + r1);
            r2 = (xexp * log2_tail + r2);
            if (xneg)
                return -(r1 + r2);
            else
                return r1 + r2;
        }
    }
}
