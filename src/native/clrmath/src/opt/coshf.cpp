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
  *   float coshf(float x)
  *
  * Spec:
  *   coshf(|x| > 89.415985107421875) = Infinity
  *   coshf(Infinity)  = infinity
  *   coshf(-Infinity) = infinity
  *
  ******************************************
  * Implementation Notes
  * ---------------------
  *
  * cosh(x) = (exp(x) + exp(-x))/2
  * cosh(-x) = +cosh(x)
  *
  * checks for special cases
  * if ( asint(x) > infinity) return x with overflow exception and
  * return x.
  * if x is NaN then raise invalid FP operation exception and return x.
  *
  * if x < 0x1p-11
  *  coshf(x) = 1+1/2*x*x
  * if 0x1p-11 < x < 0x1.62e43p-2
  *
  *  coshf = C0 + Y*Y*(C1 + Y*Y*(C2 + Y*Y*C3))
  * if 0x1.62e43p-2 < x < 8.5
  *  coshf = 0.5 * (exp(x) + 1/exp(x))
  *
  * if 8.5 < x < 0x1.62e42ep6
  *  coshf = 0.5 * exp(x)
  *
  * if 0x1.62e42ep6 < x
  *  coshf = v/2 * exp(x - log(v)) where v = 0x1.0000e8p-1
  *
  */

#include "clrmath.h"
#include "libm/libm_util.h"
#include "libm/libm_inlines.h"
#include "libm/libm_special.h"
#include "libm/libm_types.h"
#include "libm/libm_typehelper.h"
#include "libm/libm_poly.h"

struct coshf_data_t {
    uint32_t arg_max, infinity;
    float logV, invV2, halfV, halfVm1;
    float theeps, xc, ybar, wmax;
    float poly_coshf[4];
    float half, t1;
};

static const coshf_data_t coshf_data = {
    /* .arg_max = */    0x42B2D4FC,
    /* .infinity = */   0x7F800000,
    /* .logV =  */      0x1.62e6p-1,
    /* .invV2 =  */     0x1.fffc6p-3,
    /* .halfV = */      0x1.0000e8p0,
    /* .halfVm1 = */    0x1.d0112ep-17,
    /* .theeps = */     0x1p-11,
    /* .xc = */         0x1.62e43p-2,
    /* .ybar = */       0x1.62e42ep6,
    /* .wmax = */       0x1.62e0f2p6,
    /* .poly_coshf = */ {
        0x1p+0,
        0x1p-1,
        0x1.555466p-5,
        0x1.6da5e2p-10,
    },
    /* .half = */       0x1p-1,
    /* .t1 = */         0x1p-1,
};

#define C0 coshf_data.poly_coshf[0]
#define C1 coshf_data.poly_coshf[1]
#define C2 coshf_data.poly_coshf[2]
#define C3 coshf_data.poly_coshf[3]

#define LOGV    coshf_data.logV
#define INVV2   coshf_data.invV2
#define HALFVM1 coshf_data.halfVm1
#define HALFV   coshf_data.halfV
#define THEEPS  coshf_data.theeps
#define XC      coshf_data.xc
#define YBAR    coshf_data.ybar
#define WMAX    coshf_data.wmax
#define HALF    coshf_data.half
#define T1      coshf_data.t1
#define INF     coshf_date.infinity
#define ARG_MAX coshf_data.arg_max

float ALM_PROTO_OPT(coshf)(float x)
{

    float y, w, z, r, result;

    uint32_t ux = asuint32(x) & 0x7FFFFFFF;

    y = asfloat(ux);

    if (unlikely(ux > ARG_MAX)) {

        if (ux > PINFBITPATT_SP32) /* |x| is a NaN? */ {

            return x + x;

        }
        else {
            /* x is infinity */
            return FN_PROTOTYPE(copysignf)(INFINITY, x);

        }

    }

    if (ux <= asuint32(THEEPS)) {

        return (float)(1.0 + T1 * y * y);

    }

    if (ux > asuint32(8.5)) {

        if (y > YBAR) {

            w = y - LOGV;

            z = ALM_PROTO(expf)(w);

            return HALFV * z;
        }

        z = ALM_PROTO(expf)(y);

        return (HALF * z);
    }
    else {
        /*if(y > THEEPS)*/
        if (y > XC) {

            z = ALM_PROTO(expf)(y);

            return (float)(HALF * (z + (1.0 / z)));
        }

        /* coshf(x) = C0 + y*y*(C1 + y*y*(C2 + y*y*C3))  */
        r = y * y;

        result = C0 + r * (C1 + r * (C2 + r * C3));

        return (result);

    }

}
