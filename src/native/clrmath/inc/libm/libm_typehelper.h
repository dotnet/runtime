/*
 * Copyright (C) 2018-2020, Advanced Micro Devices, Inc. All rights reserved.
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

#ifndef __LIBM_TYPEHELPER_H__
#define __LIBM_TYPEHELPER_H__

#include "libm/libm_types.h"

static inline uint32_t
asuint32(float f)
{
    flt32u_t fl;
    fl.f = f;
    return fl.i;
}

static inline float
asfloat(uint32_t i)
{
    flt32u_t fl;
    fl.i = i;
    return fl.f;
}

static inline double
asdouble(uint64_t i)
{
    flt64_t dbl;
    dbl.i = i;
    return dbl.d;
}

static inline uint64_t
asuint64(double f)
{
    flt64u_t fl;
    fl.d = f;
    return fl.i;
}

static inline double
eval_as_double(double d)
{
    return d;
}

static inline float
eval_as_float(float f)
{
    return f;
}

static inline int32_t
cast_float_to_i32(float x)
{
    return (int32_t)x;
}

static inline   int64_t
cast_double_to_i64(double x)
{
    return (int64_t)x;
}

static inline float
cast_i32_to_float(int32_t x)
{
    return (float)x;
}

static inline double
cast_i64_to_double(int64_t x)
{
    return (double)x;
}

#endif	/* __LIBM_TYPEHELPER_H__ */
