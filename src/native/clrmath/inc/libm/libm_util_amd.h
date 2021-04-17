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

#ifndef LIBM_UTIL_AMD_H_INCLUDED
#define LIBM_UTIL_AMD_H_INCLUDED 1

typedef float F32;
typedef unsigned int U32;
typedef int S32;

typedef double F64;
typedef unsigned long long  U64;
typedef long long S64;

union UT32_
{
    F32 f32;
    U32 u32;
};

union UT64_
{
    F64 f64;
    U64 u64;
};

typedef union UT32_ UT32;
typedef union UT64_ UT64;




#define QNAN_MASK_32        0x00400000
#define QNAN_MASK_64        0x0008000000000000ULL

#define MULTIPLIER_SP 24
#define MULTIPLIER_DP 53

/*Special numbers Float */
#define POS_ONE_F32 0x3F800000
#define NEG_ONE_F32 0xbf800000
#define POS_ZERO_F32 0x00000000
#define NEG_ZERO_F32 0x80000000
#define POS_INF_F32 0x7F800000
#define NEG_INF_F32 0xFF800000
#define POS_SNAN_F32 0x7fb00000
#define NEG_SNAN_F32 0xffb00000
#define POS_QNAN_F32 0x7ff00000
#define NEG_QNAN_F32 0xfff00000
#define POS_PI_F32 0x40490fd8
#define NEG_PI_F32 0xc0490fd8

/*Special numbers Double */
#define POS_ONE_F64 0x3FF0000000000000
#define NEG_ONE_F64 0xBFF0000000000000
#define POS_ZERO_F64 0x0000000000000000
#define NEG_ZERO_F64 0x8000000000000000
#define POS_INF_F64 0x7ff0000000000000
#define NEG_INF_F64 0xfff0000000000000
#define POS_SNAN_F64 0x7FF4001000000000
#define NEG_SNAN_F64 0xfff2000000000000
#define POS_QNAN_F64 0x7ff87ff7fdedffff
#define NEG_QNAN_F64 0xfff2000000000000
#define POS_PI_F64 0x40091EB851EB851F
#define NEG_PI_F64 0xc00921fb54442d18

static const double VAL_2PMULTIPLIER_DP = 9007199254740992.0;
static const double VAL_2PMMULTIPLIER_DP = 1.1102230246251565404236316680908e-16;
static const float VAL_2PMULTIPLIER_SP = 16777216.0F;
static const float VAL_2PMMULTIPLIER_SP = 5.9604645e-8F;

/* Definitions for double functions on 64 bit machines */
#define SIGNBIT_DP64      0x8000000000000000
#define EXPBITS_DP64      0x7ff0000000000000ULL
#define MANTBITS_DP64     0x000fffffffffffff
#define ONEEXPBITS_DP64   0x3ff0000000000000
#define TWOEXPBITS_DP64   0x4000000000000000
#define HALFEXPBITS_DP64  0x3fe0000000000000
#define IMPBIT_DP64       0x0010000000000000
#define QNANBITPATT_DP64  0x7ff8000000000000ULL
#define INDEFBITPATT_DP64 0xfff8000000000000
#define PINFBITPATT_DP64  0x7ff0000000000000
#define NINFBITPATT_DP64  0xfff0000000000000
#define EXPBIAS_DP64      1023
#define EXPSHIFTBITS_DP64 52
#define BIASEDEMIN_DP64   1
#define EMIN_DP64         -1022
#define BIASEDEMAX_DP64   2046
#define EMAX_DP64         1023
#define LAMBDA_DP64       1.0e300
#define MANTLENGTH_DP64   53
#define BASEDIGITS_DP64   15
#define EXP_MIN           0xc0874910d52d3052
#define EXP_MAX_DOUBLE    709.7822265625

/* These definitions, used by float functions,
   are for both 32 and 64 bit machines */
#define SIGNBIT_SP32      0x80000000
#define EXPBITS_SP32      0x7f800000
#define MANTBITS_SP32     0x007fffff
#define ONEEXPBITS_SP32   0x3f800000
#define TWOEXPBITS_SP32   0x40000000
#define HALFEXPBITS_SP32  0x3f000000
#define IMPBIT_SP32       0x00800000
#define QNANBITPATT_SP32  0x7fc00000
#define INDEFBITPATT_SP32 0xffc00000
#define PINFBITPATT_SP32  0x7f800000
#define NINFBITPATT_SP32  0xff800000
#define EXPBIAS_SP32      127
#define EXPSHIFTBITS_SP32 23
#define BIASEDEMIN_SP32   1
#define EMIN_SP32         -126
#define BIASEDEMAX_SP32   254
#define EMAX_SP32         127
#define LAMBDA_SP32       1.0e30
#define MANTLENGTH_SP32   24
#define BASEDIGITS_SP32   7

#define CLASS_SIGNALLING_NAN 1
#define CLASS_QUIET_NAN 2
#define CLASS_NEGATIVE_INFINITY 3
#define CLASS_NEGATIVE_NORMAL_NONZERO 4
#define CLASS_NEGATIVE_DENORMAL 5
#define CLASS_NEGATIVE_ZERO 6
#define CLASS_POSITIVE_ZERO 7
#define CLASS_POSITIVE_DENORMAL 8
#define CLASS_POSITIVE_NORMAL_NONZERO 9
#define CLASS_POSITIVE_INFINITY 10

#define OLD_BITS_SP32(x) (*((unsigned int *)&x))
#define OLD_BITS_DP64(x) (*((unsigned long long *)&x))


   // exception status set
#define MXCSR_ES_INEXACT       0x00000020
#define MXCSR_ES_UNDERFLOW     0x00000010
#define MXCSR_ES_OVERFLOW      0x00000008
#define MXCSR_ES_DIVBYZERO     0x00000004
#define MXCSR_ES_INVALID       0x00000001

#if defined(WINDOWS)
#define	AMD_F_NONE		  0x0
#define AMD_F_OVERFLOW    0x00000001
#define AMD_F_UNDERFLOW   0x00000002
#define AMD_F_DIVBYZERO   0x00000004
#define AMD_F_INVALID     0x00000008
#define AMD_F_INEXACT     0x00000010

#else

/* Processor-dependent floating-point status flags */
#define	AMD_F_NONE		  0x0
#define AMD_F_OVERFLOW 0x00000008
#define AMD_F_UNDERFLOW 0x00000010
#define AMD_F_DIVBYZERO 0x00000004
#define AMD_F_INVALID 0x00000001
#define AMD_F_INEXACT 0x00000020
#endif
/* Processor-dependent floating-point precision-control flags */
#define AMD_F_EXTENDED 0x00000300
#define AMD_F_DOUBLE   0x00000200
#define AMD_F_SINGLE   0x00000000

/* Processor-dependent floating-point rounding-control flags */
#define AMD_F_RC_NEAREST 0x00000000
#define AMD_F_RC_DOWN    0x00002000
#define AMD_F_RC_UP      0x00004000
#define AMD_F_RC_ZERO    0x00006000

#define INT_MIN     (-2147483647 - 1) /* minimum (signed) int value */
#define INT_MAX       2147483647    /* maximum (signed) int value */



/* Alternatives to the above functions which don't have
   problems when using high optimization levels on gcc */
#define GET_BITS_SP32(x, ux) \
  { \
    volatile union {float f; unsigned int i;} _bitsy; \
    _bitsy.f = (x); \
    ux = _bitsy.i; \
  }
#define PUT_BITS_SP32(ux, x) \
  { \
    volatile union {float f; unsigned int i;} _bitsy; \
    _bitsy.i = (ux); \
     x = _bitsy.f; \
  }

#define GET_BITS_DP64(x, ux) \
  { \
    volatile union {double d; unsigned long long i;} _bitsy; \
    _bitsy.d = (x); \
    ux = _bitsy.i; \
  }
#define PUT_BITS_DP64(ux, x) \
  { \
    volatile union {double d; unsigned long long i;} _bitsy; \
    _bitsy.i = (ux); \
    x = _bitsy.d; \
  }

#endif /* LIBM_UTIL_AMD_H_INCLUDED */
