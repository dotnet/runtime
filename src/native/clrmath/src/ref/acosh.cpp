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
#define _FUNCNAME "acosh"
double FN_PROTOTYPE_REF(acosh)(double x)
{

    unsigned long long ux;
    double r, rarg, r1, r2;
    int xexp;

    static const unsigned long long
        recrteps = 0x4196a09e667f3bcd; /* 1/sqrt(eps) = 9.49062656242515593767e+07 */
      /* log2_lead and log2_tail sum to an extra-precise version
         of log(2) */

    static const double
        log2_lead = 6.93147122859954833984e-01,  /* 0x3fe62e42e0000000 */
        log2_tail = 5.76999904754328540596e-08;  /* 0x3e6efa39ef35793c */


    GET_BITS_DP64(x, ux);

    if ((ux & EXPBITS_DP64) == EXPBITS_DP64)
    {
        /* x is either NaN or infinity */
        if (ux & MANTBITS_DP64)
        {
            /* x is NaN */
#ifdef WINDOWS
            return __amd_handle_error(_FUNCNAME, __amd_acosh, ux | 0x0008000000000000, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0, 1);
#else
            if (ux & QNAN_MASK_64)
                return __amd_handle_error(_FUNCNAME, __amd_acosh, ux | 0x0008000000000000, _DOMAIN, AMD_F_NONE, EDOM, x, 0.0, 1);
            else
                return __amd_handle_error(_FUNCNAME, __amd_acosh, ux | 0x0008000000000000, _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0, 1);
#endif
        }
        else
        {
            /* x is infinity */
            if (ux & SIGNBIT_DP64) // negative infinity return nan raise error
                return __amd_handle_error(_FUNCNAME, __amd_acosh, INDEFBITPATT_DP64, _DOMAIN, AMD_F_INVALID, EDOM, x, 0.0, 1);
            else
                return x;
        }
    }
    else if ((ux & SIGNBIT_DP64) || (ux <= 0x3ff0000000000000))
    {
        /* x <= 1.0 */
        if (ux == 0x3ff0000000000000)
        {
            /* x = 1.0; return zero. */
            return 0.0;
        }
        else
        {
            /* x is less than 1.0. Return a NaN. */
            return __amd_handle_error(_FUNCNAME, __amd_acosh, INDEFBITPATT_DP64, _DOMAIN,
                AMD_F_INVALID, EDOM, x, 0.0, 1);
        }
    }


    if (ux > recrteps)
    {
        /* Arguments greater than 1/sqrt(epsilon) in magnitude are
           approximated by acosh(x) = ln(2) + ln(x) */
           /* log_kernel_amd(x) returns xexp, r1, r2 such that
              log(x) = xexp*log(2) + r1 + r2 */
        log_kernel_amd64(x, ux, &xexp, &r1, &r2);
        /* Add (xexp+1) * log(2) to z1,z2 to get the result acosh(x).
           The computed r1 is not subject to rounding error because
           (xexp+1) has at most 10 significant bits, log(2) has 24 significant
           bits, and r1 has up to 24 bits; and the exponents of r1
           and r2 differ by at most 6. */
        r1 = ((xexp + 1) * log2_lead + r1);
        r2 = ((xexp + 1) * log2_tail + r2);
        return r1 + r2;
    }
    else if (ux >= 0x4060000000000000)
    {
        /* 128.0 <= x <= 1/sqrt(epsilon) */
        /* acosh for these arguments is approximated by
           acosh(x) = ln(x + sqrt(x*x-1)) */
        rarg = x * x - 1.0;
        /* Use assembly instruction to compute r = sqrt(rarg); */
        ASMSQRT(rarg, r);
        r += x;
        GET_BITS_DP64(r, ux);
        log_kernel_amd64(r, ux, &xexp, &r1, &r2);
        r1 = (xexp * log2_lead + r1);
        r2 = (xexp * log2_tail + r2);
        return r1 + r2;
    }
    else
    {
        /* 1.0 < x <= 128.0 */
        double u1, u2, v1, v2, w1, w2, hx, tx, t, r, s, p1, p2, a1, a2, c1, c2,
            poly;
        if (ux >= 0x3ff8000000000000)
        {
            /* 1.5 <= x <= 128.0 */
            /* We use minimax polynomials,
               based on Abramowitz and Stegun 4.6.32 series
               expansion for acosh(x), with the log(2x) and 1/(2.2.x^2)
               terms removed. We compensate for these two terms later.
            */
            t = x * x;
            if (ux >= 0x4040000000000000)
            {
                /* [3,2] for 32.0 <= x <= 128.0 */
                poly =
                    (0.45995704464157438175e-9 +
                        (-0.89080839823528631030e-9 +
                            (-0.10370522395596168095e-27 +
                                0.35255386405811106347e-32 * t) * t) * t) /
                    (0.21941191335882074014e-8 +
                        (-0.10185073058358334569e-7 +
                            0.95019562478430648685e-8 * t) * t);
            }
            else if (ux >= 0x4020000000000000)
            {
                /* [3,3] for 8.0 <= x <= 32.0 */
                poly =
                    (-0.54903656589072526589e-10 +
                        (0.27646792387218569776e-9 +
                            (-0.26912957240626571979e-9 -
                                0.86712268396736384286e-29 * t) * t) * t) /
                    (-0.24327683788655520643e-9 +
                        (0.20633757212593175571e-8 +
                            (-0.45438330985257552677e-8 +
                                0.28707154390001678580e-8 * t) * t) * t);
            }
            else if (ux >= 0x4010000000000000)
            {
                /* [4,3] for 4.0 <= x <= 8.0 */
                poly =
                    (-0.20827370596738166108e-6 +
                        (0.10232136919220422622e-5 +
                            (-0.98094503424623656701e-6 +
                                (-0.11615338819596146799e-18 +
                                    0.44511847799282297160e-21 * t) * t) * t) * t) /
                    (-0.92579451630913718588e-6 +
                        (0.76997374707496606639e-5 +
                            (-0.16727286999128481170e-4 +
                                0.10463413698762590251e-4 * t) * t) * t);
            }
            else if (ux >= 0x4000000000000000)
            {
                /* [5,5] for 2.0 <= x <= 4.0 */
                poly =
                    (-0.122195030526902362060e-7 +
                        (0.157894522814328933143e-6 +
                            (-0.579951798420930466109e-6 +
                                (0.803568881125803647331e-6 +
                                    (-0.373906657221148667374e-6 -
                                        0.317856399083678204443e-21 * t) * t) * t) * t) * t) /
                    (-0.516260096352477148831e-7 +
                        (0.894662592315345689981e-6 +
                            (-0.475662774453078218581e-5 +
                                (0.107249291567405130310e-4 +
                                    (-0.107871445525891289759e-4 +
                                        0.398833767702587224253e-5 * t) * t) * t) * t) * t);
            }
            else if (ux >= 0x3ffc000000000000)
            {
                /* [5,4] for 1.75 <= x <= 2.0 */
                poly =
                    (0.1437926821253825186e-3 +
                        (-0.1034078230246627213e-2 +
                            (0.2015310005461823437e-2 +
                                (-0.1159685218876828075e-2 +
                                    (-0.9267353551307245327e-11 +
                                        0.2880267770324388034e-12 * t) * t) * t) * t) * t) /
                    (0.6305521447028109891e-3 +
                        (-0.6816525887775002944e-2 +
                            (0.2228081831550003651e-1 +
                                (-0.2836886105406603318e-1 +
                                    0.1236997707206036752e-1 * t) * t) * t) * t);
            }
            else
            {
                /* [5,4] for 1.5 <= x <= 1.75 */
                poly =
                    (0.7471936607751750826e-3 +
                        (-0.4849405284371905506e-2 +
                            (0.8823068059778393019e-2 +
                                (-0.4825395461288629075e-2 +
                                    (-0.1001984320956564344e-8 +
                                        0.4299919281586749374e-10 * t) * t) * t) * t) * t) /
                    (0.3322359141239411478e-2 +
                        (-0.3293525930397077675e-1 +
                            (0.1011351440424239210e0 +
                                (-0.1227083591622587079e0 +
                                    0.5147099404383426080e-1 * t) * t) * t) * t);
            }
            GET_BITS_DP64(x, ux);
            log_kernel_amd64(x, ux, &xexp, &r1, &r2);
            r1 = ((xexp + 1) * log2_lead + r1);
            r2 = ((xexp + 1) * log2_tail + r2);
            /* Now (r1,r2) sum to log(2x). Subtract the term
               1/(2.2.x^2) = 0.25/t, and add poly/t, carefully
               to maintain precision. (Note that we add poly/t
               rather than poly because of the *x factor used
               when generating the minimax polynomial) */
            v2 = (poly - 0.25) / t;
            r = v2 + r1;
            s = ((r1 - r) + v2) + r2;
            v1 = r + s;
            return v1 + ((r - v1) + s);
        }

        /* Here 1.0 <= x <= 1.5. It is hard to maintain accuracy here so
           we have to go to great lengths to do so. */

           /* We compute the value
                t = x - 1.0 + sqrt(2.0*(x - 1.0) + (x - 1.0)*(x - 1.0))
              using simulated quad precision. */
        t = x - 1.0;
        u1 = t * 2.0;

        /* dekker_mul12(t,t,&v1,&v2); */
        GET_BITS_DP64(t, ux);
        ux &= 0xfffffffff8000000;
        PUT_BITS_DP64(ux, hx);
        tx = t - hx;
        v1 = t * t;
        v2 = (((hx * hx - v1) + hx * tx) + tx * hx) + tx * tx;

        /* dekker_add2(u1,0.0,v1,v2,&w1,&w2); */
        r = u1 + v1;
        s = (((u1 - r) + v1) + v2);
        w1 = r + s;
        w2 = (r - w1) + s;

        /* dekker_sqrt2(w1,w2,&u1,&u2); */
        ASMSQRT(w1, p1);
        GET_BITS_DP64(p1, ux);
        ux &= 0xfffffffff8000000;
        PUT_BITS_DP64(ux, c1);
        c2 = p1 - c1;
        a1 = p1 * p1;
        a2 = (((c1 * c1 - a1) + c1 * c2) + c2 * c1) + c2 * c2;
        p2 = (((w1 - a1) - a2) + w2) * 0.5 / p1;
        u1 = p1 + p2;
        u2 = (p1 - u1) + p2;

        /* dekker_add2(u1,u2,t,0.0,&v1,&v2); */
        r = u1 + t;
        s = (((u1 - r) + t)) + u2;
        r1 = r + s;
        r2 = (r - r1) + s;
        t = r1 + r2;

        /* Check for x close to 1.0. */
        if (x < 1.13)
        {
            /* Here 1.0 <= x < 1.13 implies r <= 0.656. In this region
               we need to take extra care to maintain precision.
               We have t = r1 + r2 = (x - 1.0 + sqrt(x*x-1.0))
               to more than basic precision. We use the Taylor series
               for log(1+x), with terms after the O(x*x) term
               approximated by a [6,6] minimax polynomial. */
            double b1, b2, c1, c2, e1, e2, q1, q2, c, cc, hr1, tr1, hpoly, tpoly, hq1, tq1, hr2, tr2;
            poly =
                (0.30893760556597282162e-21 +
                    (0.10513858797132174471e0 +
                        (0.27834538302122012381e0 +
                            (0.27223638654807468186e0 +
                                (0.12038958198848174570e0 +
                                    (0.23357202004546870613e-1 +
                                        (0.15208417992520237648e-2 +
                                            0.72741030690878441996e-7 * t) * t) * t) * t) * t) * t) * t) /
                (0.31541576391396523486e0 +
                    (0.10715979719991342022e1 +
                        (0.14311581802952004012e1 +
                            (0.94928647994421895988e0 +
                                (0.32396235926176348977e0 +
                                    (0.52566134756985833588e-1 +
                                        0.30477895574211444963e-2 * t) * t) * t) * t) * t) * t);

            /* Now we can compute the result r = acosh(x) = log1p(t)
               using the formula t - 0.5*t*t + poly*t*t. Since t is
               represented as r1+r2, the formula becomes
               r = r1+r2 - 0.5*(r1+r2)*(r1+r2) + poly*(r1+r2)*(r1+r2).
               Expanding out, we get
                 r = r1 + r2 - (0.5 + poly)*(r1*r1 + 2*r1*r2 + r2*r2)
               and ignoring negligible quantities we get
                 r = r1 + r2 - 0.5*r1*r1 + r1*r2 + poly*t*t
            */
            if (x < 1.06)
            {
                double b, c, e;
                b = r1 * r2;
                c = 0.5 * r1 * r1;
                e = poly * t * t;
                /* N.B. the order of additions and subtractions is important */
                r = (((r2 - b) + e) - c) + r1;
                return r;
            }
            else
            {
                /* For 1.06 <= x <= 1.13 we must evaluate in extended precision
                   to reach about 1 ulp accuracy (in this range the simple code
                   above only manages about 1.5 ulp accuracy) */

                   /* Split poly, r1 and r2 into head and tail sections */
                GET_BITS_DP64(poly, ux);
                ux &= 0xfffffffff8000000;
                PUT_BITS_DP64(ux, hpoly);
                tpoly = poly - hpoly;
                GET_BITS_DP64(r1, ux);
                ux &= 0xfffffffff8000000;
                PUT_BITS_DP64(ux, hr1);
                tr1 = r1 - hr1;
                GET_BITS_DP64(r2, ux);
                ux &= 0xfffffffff8000000;
                PUT_BITS_DP64(ux, hr2);
                tr2 = r2 - hr2;

                /* e = poly*t*t */
                c = poly * r1;
                cc = (((hpoly * hr1 - c) + hpoly * tr1) + tpoly * hr1) + tpoly * tr1;
                cc = poly * r2 + cc;
                q1 = c + cc;
                q2 = (c - q1) + cc;
                GET_BITS_DP64(q1, ux);
                ux &= 0xfffffffff8000000;
                PUT_BITS_DP64(ux, hq1);
                tq1 = q1 - hq1;
                c = q1 * r1;
                cc = (((hq1 * hr1 - c) + hq1 * tr1) + tq1 * hr1) + tq1 * tr1;
                cc = q1 * r2 + q2 * r1 + cc;
                e1 = c + cc;
                e2 = (c - e1) + cc;

                /* b = r1*r2 */
                b1 = r1 * r2;
                b2 = (((hr1 * hr2 - b1) + hr1 * tr2) + tr1 * hr2) + tr1 * tr2;

                /* c = 0.5*r1*r1 */
                c1 = (0.5 * r1) * r1;
                c2 = (((0.5 * hr1 * hr1 - c1) + 0.5 * hr1 * tr1) + 0.5 * tr1 * hr1) + 0.5 * tr1 * tr1;

                /* v = a + d - b */
                r = r1 - b1;
                s = (((r1 - r) - b1) - b2) + r2;
                v1 = r + s;
                v2 = (r - v1) + s;

                /* w = (a + d - b) - c */
                r = v1 - c1;
                s = (((v1 - r) - c1) - c2) + v2;
                w1 = r + s;
                w2 = (r - w1) + s;

                /* u = ((a + d - b) - c) + e */
                r = w1 + e1;
                s = (((w1 - r) + e1) + e2) + w2;
                u1 = r + s;
                u2 = (r - u1) + s;

                /* The result r = acosh(x) */
                r = u1 + u2;

                return r;
            }
        }
        else
        {
            /* For arguments 1.13 <= x <= 1.5 the log1p function
               is good enough */
            return FN_PROTOTYPE(log1p)(t);
        }
    }
}
