// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _IMMINTRIN_H
#define _IMMINTRIN_H

/*===---- avxintrin.h - AVX intrinsics -------------------------------------===
 *
 * Part of the LLVM Project, under the Apache License v2.0 with LLVM Exceptions.
 * See https://llvm.org/LICENSE.txt for license information.
 * SPDX-License-Identifier: Apache-2.0 WITH LLVM-exception
 *
 *===-----------------------------------------------------------------------===
 */

typedef double __v4df __attribute__ ((__vector_size__ (32)));
typedef long long __v4di __attribute__ ((__vector_size__ (32)));
typedef float __v8sf __attribute__ ((__vector_size__ (32)));
typedef unsigned char __v16qu __attribute__((__vector_size__(16)));
typedef unsigned long long __v4du __attribute__ ((__vector_size__ (32)));

typedef float __m256 __attribute__ ((__vector_size__ (32), __aligned__(32)));
typedef double __m256d __attribute__((__vector_size__(32), __aligned__(32)));
typedef float __m256_u __attribute__ ((__vector_size__ (32), __aligned__(1)));
typedef long long __m256i __attribute__((__vector_size__(32), __aligned__(32)));
typedef double __m256d_u __attribute__((__vector_size__(32), __aligned__(1)));
typedef long long __m256i_u __attribute__((__vector_size__(32), __aligned__(1)));
typedef int __v8si __attribute__ ((__vector_size__ (32)));

/* Define the default attributes for the functions in this file. */
#define __DEFAULT_FN_ATTRS __attribute__((__always_inline__, __nodebug__, __target__("avx"), __min_vector_width__(256)))
#define __DEFAULT_FN_ATTRS128 __attribute__((__always_inline__, __nodebug__, __target__("avx"), __min_vector_width__(128)))

#define _CMP_GT_OS    0x0e /* Greater-than (ordered, signaling)  */

//// Stores integer values from a 256-bit integer vector to an unaligned
///    memory location pointed to by \a __p.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VMOVDQU </c> instruction.
///
/// \param __p
///    A pointer to a memory location that will receive the integer values.
/// \param __a
///    A 256-bit integer vector containing the values to be moved.
static __inline void __DEFAULT_FN_ATTRS
_mm256_storeu_si256(__m256i_u *__p, __m256i __a)
{
  struct __storeu_si256 {
    __m256i_u __v;
  } __attribute__((__packed__, __may_alias__));
  ((struct __storeu_si256*)__p)->__v = __a;
}

/// Stores single-precision floating point values from a 256-bit vector
///    of [8 x float] to an unaligned memory location pointed to by \a __p.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VMOVUPS </c> instruction.
///
/// \param __p
///    A pointer to a memory location that will receive the float values.
/// \param __a
///    A 256-bit vector of [8 x float] containing the values to be moved.
static __inline void __DEFAULT_FN_ATTRS
_mm256_storeu_ps(float *__p, __m256 __a)
{
  struct __storeu_ps {
    __m256_u __v;
  } __attribute__((__packed__, __may_alias__));
  ((struct __storeu_ps*)__p)->__v = __a;
}

/// Loads 256 bits of integer data from an unaligned memory location
///    pointed to by \a __p into a 256-bit integer vector. This intrinsic may
///    perform better than \c _mm256_loadu_si256 when the data crosses a cache
///    line boundary.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VLDDQU </c> instruction.
///
/// \param __p
///    A pointer to a 256-bit integer vector containing integer values.
/// \returns A 256-bit integer vector containing the moved values.
static __inline __m256i __DEFAULT_FN_ATTRS
_mm256_lddqu_si256(__m256i const *__p)
{
  return (__m256i)__builtin_ia32_lddqu256((char const *)__p);
}

/// Loads 4 double-precision floating point values from an unaligned
///    memory location pointed to by \a __p into a vector of [4 x double].
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VMOVUPD </c> instruction.
///
/// \param __p
///    A pointer to a memory location containing double-precision floating
///    point values.
/// \returns A 256-bit vector of [4 x double] containing the moved values.
static __inline __m256d __DEFAULT_FN_ATTRS
_mm256_loadu_pd(double const *__p)
{
  struct __loadu_pd {
    __m256d_u __v;
  } __attribute__((__packed__, __may_alias__));
  return ((const struct __loadu_pd*)__p)->__v;
}

/// Casts a 256-bit integer vector into a 256-bit floating-point vector
///    of [4 x double].
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic has no corresponding instruction.
///
/// \param __a
///    A 256-bit integer vector.
/// \returns A 256-bit floating-point vector of [4 x double] containing the same
///    bitwise pattern as the parameter.
static __inline __m256d __DEFAULT_FN_ATTRS
_mm256_castsi256_pd(__m256i __a)
{
  return (__m256d)__a;
}

/// Casts a 256-bit floating-point vector of [8 x float] into a 256-bit
///    integer vector.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic has no corresponding instruction.
///
/// \param __a
///    A 256-bit floating-point vector of [8 x float].
/// \returns A 256-bit integer vector containing the same bitwise pattern as the
///    parameter.
static __inline __m256i __DEFAULT_FN_ATTRS
_mm256_castps_si256(__m256 __a)
{
  return (__m256i)__a;
}

/// Casts a 256-bit floating-point vector of [8 x float] into a 256-bit
///    floating-point vector of [4 x double].
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic has no corresponding instruction.
///
/// \param __a
///    A 256-bit floating-point vector of [8 x float].
/// \returns A 256-bit floating-point vector of [4 x double] containing the same
///    bitwise pattern as the parameter.
static __inline __m256d __DEFAULT_FN_ATTRS
_mm256_castps_pd(__m256 __a)
{
  return (__m256d)__a;
}

/// Merges 64-bit double-precision data values stored in either of the
///    two 256-bit vectors of [4 x double], as specified by the 256-bit vector
///    operand.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VBLENDVPD </c> instruction.
///
/// \param __a
///    A 256-bit vector of [4 x double].
/// \param __b
///    A 256-bit vector of [4 x double].
/// \param __c
///    A 256-bit vector operand, with mask bits 255, 191, 127, and 63 specifying
///    how the values are to be copied. The position of the mask bit corresponds
///    to the most significant bit of a copied value. When a mask bit is 0, the
///    corresponding 64-bit element in operand \a __a is copied to the same
///    position in the destination. When a mask bit is 1, the corresponding
///    64-bit element in operand \a __b is copied to the same position in the
///    destination.
/// \returns A 256-bit vector of [4 x double] containing the copied values.
static __inline __m256d __DEFAULT_FN_ATTRS
_mm256_blendv_pd(__m256d __a, __m256d __b, __m256d __c)
{
  return (__m256d)__builtin_ia32_blendvpd256(
    (__v4df)__a, (__v4df)__b, (__v4df)__c);
}

/// Casts a 256-bit floating-point vector of [4 x double] into a 256-bit
///    integer vector.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic has no corresponding instruction.
///
/// \param __a
///    A 256-bit floating-point vector of [4 x double].
/// \returns A 256-bit integer vector containing the same bitwise pattern as the
///    parameter.
static __inline __m256i __DEFAULT_FN_ATTRS
_mm256_castpd_si256(__m256d __a)
{
  return (__m256i)__a;
}

/// Constructs a 256-bit floating-point vector of [4 x double]
///    initialized with the specified double-precision floating-point values.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VUNPCKLPD+VINSERTF128 </c>
///   instruction.
///
/// \param __a
///    A double-precision floating-point value used to initialize bits [255:192]
///    of the result.
/// \param __b
///    A double-precision floating-point value used to initialize bits [191:128]
///    of the result.
/// \param __c
///    A double-precision floating-point value used to initialize bits [127:64]
///    of the result.
/// \param __d
///    A double-precision floating-point value used to initialize bits [63:0]
///    of the result.
/// \returns An initialized 256-bit floating-point vector of [4 x double].
static __inline __m256d __DEFAULT_FN_ATTRS
_mm256_set_pd(double __a, double __b, double __c, double __d)
{
  return __extension__ (__m256d){ __d, __c, __b, __a };
}

/* Create vectors with repeated elements */
/// Constructs a 256-bit floating-point vector of [4 x double], with each
///    of the four double-precision floating-point vector elements set to the
///    specified double-precision floating-point value.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VMOVDDUP+VINSERTF128 </c> instruction.
///
/// \param __w
///    A double-precision floating-point value used to initialize each vector
///    element of the result.
/// \returns An initialized 256-bit floating-point vector of [4 x double].
static __inline __m256d __DEFAULT_FN_ATTRS
_mm256_set1_pd(double __w)
{
  return _mm256_set_pd(__w, __w, __w, __w);
}

/// This intrinsic corresponds to the <c> VSHUFPD </c> instruction.
///
/// \param a
///    A 256-bit vector of [4 x double].
/// \param b
///    A 256-bit vector of [4 x double].
/// \param mask
///    An immediate value containing 8-bit values specifying which elements to
///    copy from \a a and \a b: \n
///    Bit [0]=0: Bits [63:0] are copied from \a a to bits [63:0] of the
///    destination. \n
///    Bit [0]=1: Bits [127:64] are copied from \a a to bits [63:0] of the
///    destination. \n
///    Bit [1]=0: Bits [63:0] are copied from \a b to bits [127:64] of the
///    destination. \n
///    Bit [1]=1: Bits [127:64] are copied from \a b to bits [127:64] of the
///    destination. \n
///    Bit [2]=0: Bits [191:128] are copied from \a a to bits [191:128] of the
///    destination. \n
///    Bit [2]=1: Bits [255:192] are copied from \a a to bits [191:128] of the
///    destination. \n
///    Bit [3]=0: Bits [191:128] are copied from \a b to bits [255:192] of the
///    destination. \n
///    Bit [3]=1: Bits [255:192] are copied from \a b to bits [255:192] of the
///    destination.
/// \returns A 256-bit vector of [4 x double] containing the shuffled values.
#define _mm256_shuffle_pd(a, b, mask) \
  (__m256d)__builtin_ia32_shufpd256((__v4df)(__m256d)(a), \
                                    (__v4df)(__m256d)(b), (int)(mask))

/// Loads 8 single-precision floating point values from an unaligned
///    memory location pointed to by \a __p into a vector of [8 x float].
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VMOVUPS </c> instruction.
///
/// \param __p
///    A pointer to a memory location containing single-precision floating
///    point values.
/// \returns A 256-bit vector of [8 x float] containing the moved values.
static __inline __m256 __DEFAULT_FN_ATTRS
_mm256_loadu_ps(float const *__p)
{
  struct __loadu_ps {
    __m256_u __v;
  } __attribute__((__packed__, __may_alias__));
  return ((const struct __loadu_ps*)__p)->__v;
}

/// Casts a 256-bit integer vector into a 256-bit floating-point vector
///    of [8 x float].
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic has no corresponding instruction.
///
/// \param __a
///    A 256-bit integer vector.
/// \returns A 256-bit floating-point vector of [8 x float] containing the same
///    bitwise pattern as the parameter.
static __inline __m256 __DEFAULT_FN_ATTRS
_mm256_castsi256_ps(__m256i __a)
{
  return (__m256)__a;
}

/// Constructs a 256-bit floating-point vector of [8 x float] initialized
///    with the specified single-precision floating-point values.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic is a utility function and does not correspond to a specific
///   instruction.
///
/// \param __a
///    A single-precision floating-point value used to initialize bits [255:224]
///    of the result.
/// \param __b
///    A single-precision floating-point value used to initialize bits [223:192]
///    of the result.
/// \param __c
///    A single-precision floating-point value used to initialize bits [191:160]
///    of the result.
/// \param __d
///    A single-precision floating-point value used to initialize bits [159:128]
///    of the result.
/// \param __e
///    A single-precision floating-point value used to initialize bits [127:96]
///    of the result.
/// \param __f
///    A single-precision floating-point value used to initialize bits [95:64]
///    of the result.
/// \param __g
///    A single-precision floating-point value used to initialize bits [63:32]
///    of the result.
/// \param __h
///    A single-precision floating-point value used to initialize bits [31:0]
///    of the result.
/// \returns An initialized 256-bit floating-point vector of [8 x float].
static __inline __m256 __DEFAULT_FN_ATTRS
_mm256_set_ps(float __a, float __b, float __c, float __d,
              float __e, float __f, float __g, float __h)
{
  return __extension__ (__m256){ __h, __g, __f, __e, __d, __c, __b, __a };
}

/// Constructs a 256-bit floating-point vector of [8 x float], with each
///    of the eight single-precision floating-point vector elements set to the
///    specified single-precision floating-point value.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VPERMILPS+VINSERTF128 </c>
///   instruction.
///
/// \param __w
///    A single-precision floating-point value used to initialize each vector
///    element of the result.
/// \returns An initialized 256-bit floating-point vector of [8 x float].
static __inline __m256 __DEFAULT_FN_ATTRS
_mm256_set1_ps(float __w)
{
  return _mm256_set_ps(__w, __w, __w, __w, __w, __w, __w, __w);
}

/// Constructs a 256-bit integer vector initialized with the specified
///    32-bit integral values.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic is a utility function and does not correspond to a specific
///   instruction.
///
/// \param __i0
///    A 32-bit integral value used to initialize bits [255:224] of the result.
/// \param __i1
///    A 32-bit integral value used to initialize bits [223:192] of the result.
/// \param __i2
///    A 32-bit integral value used to initialize bits [191:160] of the result.
/// \param __i3
///    A 32-bit integral value used to initialize bits [159:128] of the result.
/// \param __i4
///    A 32-bit integral value used to initialize bits [127:96] of the result.
/// \param __i5
///    A 32-bit integral value used to initialize bits [95:64] of the result.
/// \param __i6
///    A 32-bit integral value used to initialize bits [63:32] of the result.
/// \param __i7
///    A 32-bit integral value used to initialize bits [31:0] of the result.
/// \returns An initialized 256-bit integer vector.
static __inline __m256i __DEFAULT_FN_ATTRS
_mm256_set_epi32(int __i0, int __i1, int __i2, int __i3,
                 int __i4, int __i5, int __i6, int __i7)
{
  return __extension__ (__m256i)(__v8si){ __i7, __i6, __i5, __i4, __i3, __i2, __i1, __i0 };
}

/// Constructs a 256-bit integer vector of [8 x i32], with each of the
///    32-bit integral vector elements set to the specified 32-bit integral
///    value.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VPERMILPS+VINSERTF128 </c>
///   instruction.
///
/// \param __i
///    A 32-bit integral value used to initialize each vector element of the
///    result.
/// \returns An initialized 256-bit integer vector of [8 x i32].
static __inline __m256i __DEFAULT_FN_ATTRS
_mm256_set1_epi32(int __i)
{
  return _mm256_set_epi32(__i, __i, __i, __i, __i, __i, __i, __i);
}

/// Constructs a 256-bit integer vector initialized with the specified
///    64-bit integral values.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VPUNPCKLQDQ+VINSERTF128 </c>
///   instruction.
///
/// \param __a
///    A 64-bit integral value used to initialize bits [255:192] of the result.
/// \param __b
///    A 64-bit integral value used to initialize bits [191:128] of the result.
/// \param __c
///    A 64-bit integral value used to initialize bits [127:64] of the result.
/// \param __d
///    A 64-bit integral value used to initialize bits [63:0] of the result.
/// \returns An initialized 256-bit integer vector.
static __inline __m256i __DEFAULT_FN_ATTRS
_mm256_set_epi64x(long long __a, long long __b, long long __c, long long __d)
{
  return __extension__ (__m256i)(__v4di){ __d, __c, __b, __a };
}

/// Constructs a 256-bit integer vector of [4 x i64], with each of the
///    64-bit integral vector elements set to the specified 64-bit integral
///    value.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VMOVDDUP+VINSERTF128 </c> instruction.
///
/// \param __q
///    A 64-bit integral value used to initialize each vector element of the
///    result.
/// \returns An initialized 256-bit integer vector of [4 x i64].
static __inline __m256i __DEFAULT_FN_ATTRS
_mm256_set1_epi64x(long long __q)
{
  return _mm256_set_epi64x(__q, __q, __q, __q);
}

/// Extracts the sign bits of single-precision floating point elements
///    in a 256-bit vector of [8 x float] and writes them to the lower order
///    bits of the return value.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VMOVMSKPS </c> instruction.
///
/// \param __a
///    A 256-bit vector of [8 x float] containing the single-precision floating
///    point values with sign bits to be extracted.
/// \returns The sign bits from the operand, written to bits [7:0].
static __inline int __DEFAULT_FN_ATTRS
_mm256_movemask_ps(__m256 __a)
{
  return __builtin_ia32_movmskps256((__v8sf)__a);
}

/* Vector extract sign mask */
/// Extracts the sign bits of double-precision floating point elements
///    in a 256-bit vector of [4 x double] and writes them to the lower order
///    bits of the return value.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VMOVMSKPD </c> instruction.
///
/// \param __a
///    A 256-bit vector of [4 x double] containing the double-precision
///    floating point values with sign bits to be extracted.
/// \returns The sign bits from the operand, written to bits [3:0].
static __inline int __DEFAULT_FN_ATTRS
_mm256_movemask_pd(__m256d __a)
{
  return __builtin_ia32_movmskpd256((__v4df)__a);
}

/* Vector Blend */
/// Merges 64-bit double-precision data values stored in either of the
///    two 256-bit vectors of [4 x double], as specified by the immediate
///    integer operand.
///
/// \headerfile <x86intrin.h>
///
/// \code
/// __m256d _mm256_blend_pd(__m256d V1, __m256d V2, const int M);
/// \endcode
///
/// This intrinsic corresponds to the <c> VBLENDPD </c> instruction.
///
/// \param V1
///    A 256-bit vector of [4 x double].
/// \param V2
///    A 256-bit vector of [4 x double].
/// \param M
///    An immediate integer operand, with mask bits [3:0] specifying how the
///    values are to be copied. The position of the mask bit corresponds to the
///    index of a copied value. When a mask bit is 0, the corresponding 64-bit
///    element in operand \a V1 is copied to the same position in the
///    destination. When a mask bit is 1, the corresponding 64-bit element in
///    operand \a V2 is copied to the same position in the destination.
/// \returns A 256-bit vector of [4 x double] containing the copied values.
#define _mm256_blend_pd(V1, V2, M) \
  (__m256d)__builtin_ia32_blendpd256((__v4df)(__m256d)(V1), \
                                     (__v4df)(__m256d)(V2), (int)(M))


/// Compares each of the corresponding double-precision values of two
///    256-bit vectors of [4 x double], using the operation specified by the
///    immediate integer operand.
///
///    Returns a [4 x double] vector consisting of four doubles corresponding to
///    the four comparison results: zero if the comparison is false, and all 1's
///    if the comparison is true.
///
/// \headerfile <x86intrin.h>
///
/// \code
/// __m256d _mm256_cmp_pd(__m256d a, __m256d b, const int c);
/// \endcode
///
/// This intrinsic corresponds to the <c> VCMPPD </c> instruction.
///
/// \param a
///    A 256-bit vector of [4 x double].
/// \param b
///    A 256-bit vector of [4 x double].
/// \param c
///    An immediate integer operand, with bits [4:0] specifying which comparison
///    operation to use: \n
///    0x00: Equal (ordered, non-signaling) \n
///    0x01: Less-than (ordered, signaling) \n
///    0x02: Less-than-or-equal (ordered, signaling) \n
///    0x03: Unordered (non-signaling) \n
///    0x04: Not-equal (unordered, non-signaling) \n
///    0x05: Not-less-than (unordered, signaling) \n
///    0x06: Not-less-than-or-equal (unordered, signaling) \n
///    0x07: Ordered (non-signaling) \n
///    0x08: Equal (unordered, non-signaling) \n
///    0x09: Not-greater-than-or-equal (unordered, signaling) \n
///    0x0A: Not-greater-than (unordered, signaling) \n
///    0x0B: False (ordered, non-signaling) \n
///    0x0C: Not-equal (ordered, non-signaling) \n
///    0x0D: Greater-than-or-equal (ordered, signaling) \n
///    0x0E: Greater-than (ordered, signaling) \n
///    0x0F: True (unordered, non-signaling) \n
///    0x10: Equal (ordered, signaling) \n
///    0x11: Less-than (ordered, non-signaling) \n
///    0x12: Less-than-or-equal (ordered, non-signaling) \n
///    0x13: Unordered (signaling) \n
///    0x14: Not-equal (unordered, signaling) \n
///    0x15: Not-less-than (unordered, non-signaling) \n
///    0x16: Not-less-than-or-equal (unordered, non-signaling) \n
///    0x17: Ordered (signaling) \n
///    0x18: Equal (unordered, signaling) \n
///    0x19: Not-greater-than-or-equal (unordered, non-signaling) \n
///    0x1A: Not-greater-than (unordered, non-signaling) \n
///    0x1B: False (ordered, signaling) \n
///    0x1C: Not-equal (ordered, signaling) \n
///    0x1D: Greater-than-or-equal (ordered, non-signaling) \n
///    0x1E: Greater-than (ordered, non-signaling) \n
///    0x1F: True (unordered, signaling)
/// \returns A 256-bit vector of [4 x double] containing the comparison results.
#define _mm256_cmp_pd(a, b, c) \
  (__m256d)__builtin_ia32_cmppd256((__v4df)(__m256d)(a), \
                                   (__v4df)(__m256d)(b), (c))

/// Compares each of the corresponding values of two 256-bit vectors of
///    [8 x float], using the operation specified by the immediate integer
///    operand.
///
///    Returns a [8 x float] vector consisting of eight floats corresponding to
///    the eight comparison results: zero if the comparison is false, and all
///    1's if the comparison is true.
///
/// \headerfile <x86intrin.h>
///
/// \code
/// __m256 _mm256_cmp_ps(__m256 a, __m256 b, const int c);
/// \endcode
///
/// This intrinsic corresponds to the <c> VCMPPS </c> instruction.
///
/// \param a
///    A 256-bit vector of [8 x float].
/// \param b
///    A 256-bit vector of [8 x float].
/// \param c
///    An immediate integer operand, with bits [4:0] specifying which comparison
///    operation to use: \n
///    0x00: Equal (ordered, non-signaling) \n
///    0x01: Less-than (ordered, signaling) \n
///    0x02: Less-than-or-equal (ordered, signaling) \n
///    0x03: Unordered (non-signaling) \n
///    0x04: Not-equal (unordered, non-signaling) \n
///    0x05: Not-less-than (unordered, signaling) \n
///    0x06: Not-less-than-or-equal (unordered, signaling) \n
///    0x07: Ordered (non-signaling) \n
///    0x08: Equal (unordered, non-signaling) \n
///    0x09: Not-greater-than-or-equal (unordered, signaling) \n
///    0x0A: Not-greater-than (unordered, signaling) \n
///    0x0B: False (ordered, non-signaling) \n
///    0x0C: Not-equal (ordered, non-signaling) \n
///    0x0D: Greater-than-or-equal (ordered, signaling) \n
///    0x0E: Greater-than (ordered, signaling) \n
///    0x0F: True (unordered, non-signaling) \n
///    0x10: Equal (ordered, signaling) \n
///    0x11: Less-than (ordered, non-signaling) \n
///    0x12: Less-than-or-equal (ordered, non-signaling) \n
///    0x13: Unordered (signaling) \n
///    0x14: Not-equal (unordered, signaling) \n
///    0x15: Not-less-than (unordered, non-signaling) \n
///    0x16: Not-less-than-or-equal (unordered, non-signaling) \n
///    0x17: Ordered (signaling) \n
///    0x18: Equal (unordered, signaling) \n
///    0x19: Not-greater-than-or-equal (unordered, non-signaling) \n
///    0x1A: Not-greater-than (unordered, non-signaling) \n
///    0x1B: False (ordered, signaling) \n
///    0x1C: Not-equal (ordered, signaling) \n
///    0x1D: Greater-than-or-equal (ordered, non-signaling) \n
///    0x1E: Greater-than (ordered, non-signaling) \n
///    0x1F: True (unordered, signaling)
/// \returns A 256-bit vector of [8 x float] containing the comparison results.
#define _mm256_cmp_ps(a, b, c) \
  (__m256)__builtin_ia32_cmpps256((__v8sf)(__m256)(a), \
                                  (__v8sf)(__m256)(b), (c))

/* Cast between vector types */
/// Casts a 256-bit floating-point vector of [4 x double] into a 256-bit
///    floating-point vector of [8 x float].
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic has no corresponding instruction.
///
/// \param __a
///    A 256-bit floating-point vector of [4 x double].
/// \returns A 256-bit floating-point vector of [8 x float] containing the same
///    bitwise pattern as the parameter.
static __inline __m256 __DEFAULT_FN_ATTRS
_mm256_castpd_ps(__m256d __a)
{
  return (__m256)__a;
}

/// Stores double-precision floating point values from a 256-bit vector
///    of [4 x double] to an unaligned memory location pointed to by \a __p.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> VMOVUPD </c> instruction.
///
/// \param __p
///    A pointer to a memory location that will receive the double-precision
///    floating point values.
/// \param __a
///    A 256-bit vector of [4 x double] containing the values to be moved.
static __inline void __DEFAULT_FN_ATTRS
_mm256_storeu_pd(double *__p, __m256d __a)
{
  struct __storeu_pd {
    __m256d_u __v;
  } __attribute__((__packed__, __may_alias__));
  ((struct __storeu_pd*)__p)->__v = __a;
}

#undef __DEFAULT_FN_ATTRS128

/*===---- avx2intrin.h - AVX2 intrinsics -----------------------------------===
 *
 * Part of the LLVM Project, under the Apache License v2.0 with LLVM Exceptions.
 * See https://llvm.org/LICENSE.txt for license information.
 * SPDX-License-Identifier: Apache-2.0 WITH LLVM-exception
 *
 *===-----------------------------------------------------------------------===
 */

/* Define the default attributes for the functions in this file. */
#define __DEFAULT_FN_ATTRS256 __attribute__((__always_inline__, __nodebug__, __target__("avx2"), __min_vector_width__(256)))
#define __DEFAULT_FN_ATTRS128 __attribute__((__always_inline__, __nodebug__, __target__("avx2"), __min_vector_width__(128)))

static __inline__ __m256i __DEFAULT_FN_ATTRS256
_mm256_min_epu32(__m256i __a, __m256i __b)
{
  return (__m256i)__builtin_ia32_pminud256((__v8si)__a, (__v8si)__b);
}

static __inline__ __m256i __DEFAULT_FN_ATTRS256
_mm256_max_epu32(__m256i __a, __m256i __b)
{
  return (__m256i)__builtin_ia32_pmaxud256((__v8si)__a, (__v8si)__b);
}

#define _mm256_blend_epi32(V1, V2, M) \
  (__m256i)__builtin_ia32_pblendd256((__v8si)(__m256i)(V1), \
                                     (__v8si)(__m256i)(V2), (int)(M))


#define _mm256_shuffle_epi32(a, imm) \
  (__m256i)__builtin_ia32_pshufd256((__v8si)(__m256i)(a), (int)(imm))

static __inline__ __m256i __DEFAULT_FN_ATTRS256
_mm256_cmpgt_epi64(__m256i __a, __m256i __b)
{
  return (__m256i)((__v4di)__a > (__v4di)__b);
}

static __inline__ __m256i __DEFAULT_FN_ATTRS256
_mm256_cvtepu8_epi32(__m128i __V)
{
  return (__m256i)__builtin_convertvector(__builtin_shufflevector((__v16qu)__V, (__v16qu)__V, 0, 1, 2, 3, 4, 5, 6, 7), __v8si);
}

#define _mm256_permute4x64_pd(V, M) \
  (__m256d)__builtin_ia32_permdf256((__v4df)(__m256d)(V), (int)(M))


static __inline__ __m256i __DEFAULT_FN_ATTRS256
_mm256_xor_si256(__m256i __a, __m256i __b)
{
  return (__m256i)((__v4du)__a ^ (__v4du)__b);
}

static __inline__ __m256i __DEFAULT_FN_ATTRS256
_mm256_cmpgt_epi32(__m256i __a, __m256i __b)
{
  return (__m256i)((__v8si)__a > (__v8si)__b);
}

static __inline__ __m256 __DEFAULT_FN_ATTRS256
_mm256_permutevar8x32_ps(__m256 __a, __m256i __b)
{
  return (__m256)__builtin_ia32_permvarsf256((__v8sf)__a, (__v8si)__b);
}

#undef __DEFAULT_FN_ATTRS
#undef __DEFAULT_FN_ATTRS128

/*===---- avx512fintrin.h - AVX512F intrinsics -----------------------------===
 *
 * Part of the LLVM Project, under the Apache License v2.0 with LLVM Exceptions.
 * See https://llvm.org/LICENSE.txt for license information.
 * SPDX-License-Identifier: Apache-2.0 WITH LLVM-exception
 *
 *===-----------------------------------------------------------------------===
 */

typedef float __m512 __attribute__((__vector_size__(64), __aligned__(64)));
typedef long long __m512i __attribute__((__vector_size__(64), __aligned__(64)));
typedef double __m512d __attribute__((__vector_size__(64), __aligned__(64)));
typedef double __m512d_u __attribute__((__vector_size__(64), __aligned__(1)));
typedef long long __m512i __attribute__((__vector_size__(64), __aligned__(64)));
typedef long long __m512i_u __attribute__((__vector_size__(64), __aligned__(1)));
typedef float __m512_u __attribute__((__vector_size__(64), __aligned__(1)));

typedef long long __v8di __attribute__((__vector_size__(64)));
typedef double __v8df __attribute__((__vector_size__(64)));
typedef int __v16si __attribute__((__vector_size__(64)));
typedef float __v16sf __attribute__((__vector_size__(64)));

typedef unsigned char __mmask8;
typedef unsigned short __mmask16;

#define _MM_FROUND_CUR_DIRECTION    0x04

/* Define the default attributes for the functions in this file. */
#define __DEFAULT_FN_ATTRS512 __attribute__((__always_inline__, __nodebug__, __target__("avx512f"), __min_vector_width__(512)))
#define __DEFAULT_FN_ATTRS128 __attribute__((__always_inline__, __nodebug__, __target__("avx512f"), __min_vector_width__(128)))
#define __DEFAULT_FN_ATTRS __attribute__((__always_inline__, __nodebug__, __target__("avx512f")))

/* Constants for integer comparison predicates */
typedef enum {
    _MM_CMPINT_EQ,      /* Equal */
    _MM_CMPINT_LT,      /* Less than */
    _MM_CMPINT_LE,      /* Less than or Equal */
    _MM_CMPINT_UNUSED,
    _MM_CMPINT_NE,      /* Not Equal */
    _MM_CMPINT_NLT,     /* Not Less than */
#define _MM_CMPINT_GE   _MM_CMPINT_NLT  /* Greater than or Equal */
    _MM_CMPINT_NLE      /* Not Less than or Equal */
#define _MM_CMPINT_GT   _MM_CMPINT_NLE  /* Greater than */
} _MM_CMPINT_ENUM;

typedef enum
{
  _MM_PERM_AAAA = 0x00, _MM_PERM_AAAB = 0x01, _MM_PERM_AAAC = 0x02,
  _MM_PERM_AAAD = 0x03, _MM_PERM_AABA = 0x04, _MM_PERM_AABB = 0x05,
  _MM_PERM_AABC = 0x06, _MM_PERM_AABD = 0x07, _MM_PERM_AACA = 0x08,
  _MM_PERM_AACB = 0x09, _MM_PERM_AACC = 0x0A, _MM_PERM_AACD = 0x0B,
  _MM_PERM_AADA = 0x0C, _MM_PERM_AADB = 0x0D, _MM_PERM_AADC = 0x0E,
  _MM_PERM_AADD = 0x0F, _MM_PERM_ABAA = 0x10, _MM_PERM_ABAB = 0x11,
  _MM_PERM_ABAC = 0x12, _MM_PERM_ABAD = 0x13, _MM_PERM_ABBA = 0x14,
  _MM_PERM_ABBB = 0x15, _MM_PERM_ABBC = 0x16, _MM_PERM_ABBD = 0x17,
  _MM_PERM_ABCA = 0x18, _MM_PERM_ABCB = 0x19, _MM_PERM_ABCC = 0x1A,
  _MM_PERM_ABCD = 0x1B, _MM_PERM_ABDA = 0x1C, _MM_PERM_ABDB = 0x1D,
  _MM_PERM_ABDC = 0x1E, _MM_PERM_ABDD = 0x1F, _MM_PERM_ACAA = 0x20,
  _MM_PERM_ACAB = 0x21, _MM_PERM_ACAC = 0x22, _MM_PERM_ACAD = 0x23,
  _MM_PERM_ACBA = 0x24, _MM_PERM_ACBB = 0x25, _MM_PERM_ACBC = 0x26,
  _MM_PERM_ACBD = 0x27, _MM_PERM_ACCA = 0x28, _MM_PERM_ACCB = 0x29,
  _MM_PERM_ACCC = 0x2A, _MM_PERM_ACCD = 0x2B, _MM_PERM_ACDA = 0x2C,
  _MM_PERM_ACDB = 0x2D, _MM_PERM_ACDC = 0x2E, _MM_PERM_ACDD = 0x2F,
  _MM_PERM_ADAA = 0x30, _MM_PERM_ADAB = 0x31, _MM_PERM_ADAC = 0x32,
  _MM_PERM_ADAD = 0x33, _MM_PERM_ADBA = 0x34, _MM_PERM_ADBB = 0x35,
  _MM_PERM_ADBC = 0x36, _MM_PERM_ADBD = 0x37, _MM_PERM_ADCA = 0x38,
  _MM_PERM_ADCB = 0x39, _MM_PERM_ADCC = 0x3A, _MM_PERM_ADCD = 0x3B,
  _MM_PERM_ADDA = 0x3C, _MM_PERM_ADDB = 0x3D, _MM_PERM_ADDC = 0x3E,
  _MM_PERM_ADDD = 0x3F, _MM_PERM_BAAA = 0x40, _MM_PERM_BAAB = 0x41,
  _MM_PERM_BAAC = 0x42, _MM_PERM_BAAD = 0x43, _MM_PERM_BABA = 0x44,
  _MM_PERM_BABB = 0x45, _MM_PERM_BABC = 0x46, _MM_PERM_BABD = 0x47,
  _MM_PERM_BACA = 0x48, _MM_PERM_BACB = 0x49, _MM_PERM_BACC = 0x4A,
  _MM_PERM_BACD = 0x4B, _MM_PERM_BADA = 0x4C, _MM_PERM_BADB = 0x4D,
  _MM_PERM_BADC = 0x4E, _MM_PERM_BADD = 0x4F, _MM_PERM_BBAA = 0x50,
  _MM_PERM_BBAB = 0x51, _MM_PERM_BBAC = 0x52, _MM_PERM_BBAD = 0x53,
  _MM_PERM_BBBA = 0x54, _MM_PERM_BBBB = 0x55, _MM_PERM_BBBC = 0x56,
  _MM_PERM_BBBD = 0x57, _MM_PERM_BBCA = 0x58, _MM_PERM_BBCB = 0x59,
  _MM_PERM_BBCC = 0x5A, _MM_PERM_BBCD = 0x5B, _MM_PERM_BBDA = 0x5C,
  _MM_PERM_BBDB = 0x5D, _MM_PERM_BBDC = 0x5E, _MM_PERM_BBDD = 0x5F,
  _MM_PERM_BCAA = 0x60, _MM_PERM_BCAB = 0x61, _MM_PERM_BCAC = 0x62,
  _MM_PERM_BCAD = 0x63, _MM_PERM_BCBA = 0x64, _MM_PERM_BCBB = 0x65,
  _MM_PERM_BCBC = 0x66, _MM_PERM_BCBD = 0x67, _MM_PERM_BCCA = 0x68,
  _MM_PERM_BCCB = 0x69, _MM_PERM_BCCC = 0x6A, _MM_PERM_BCCD = 0x6B,
  _MM_PERM_BCDA = 0x6C, _MM_PERM_BCDB = 0x6D, _MM_PERM_BCDC = 0x6E,
  _MM_PERM_BCDD = 0x6F, _MM_PERM_BDAA = 0x70, _MM_PERM_BDAB = 0x71,
  _MM_PERM_BDAC = 0x72, _MM_PERM_BDAD = 0x73, _MM_PERM_BDBA = 0x74,
  _MM_PERM_BDBB = 0x75, _MM_PERM_BDBC = 0x76, _MM_PERM_BDBD = 0x77,
  _MM_PERM_BDCA = 0x78, _MM_PERM_BDCB = 0x79, _MM_PERM_BDCC = 0x7A,
  _MM_PERM_BDCD = 0x7B, _MM_PERM_BDDA = 0x7C, _MM_PERM_BDDB = 0x7D,
  _MM_PERM_BDDC = 0x7E, _MM_PERM_BDDD = 0x7F, _MM_PERM_CAAA = 0x80,
  _MM_PERM_CAAB = 0x81, _MM_PERM_CAAC = 0x82, _MM_PERM_CAAD = 0x83,
  _MM_PERM_CABA = 0x84, _MM_PERM_CABB = 0x85, _MM_PERM_CABC = 0x86,
  _MM_PERM_CABD = 0x87, _MM_PERM_CACA = 0x88, _MM_PERM_CACB = 0x89,
  _MM_PERM_CACC = 0x8A, _MM_PERM_CACD = 0x8B, _MM_PERM_CADA = 0x8C,
  _MM_PERM_CADB = 0x8D, _MM_PERM_CADC = 0x8E, _MM_PERM_CADD = 0x8F,
  _MM_PERM_CBAA = 0x90, _MM_PERM_CBAB = 0x91, _MM_PERM_CBAC = 0x92,
  _MM_PERM_CBAD = 0x93, _MM_PERM_CBBA = 0x94, _MM_PERM_CBBB = 0x95,
  _MM_PERM_CBBC = 0x96, _MM_PERM_CBBD = 0x97, _MM_PERM_CBCA = 0x98,
  _MM_PERM_CBCB = 0x99, _MM_PERM_CBCC = 0x9A, _MM_PERM_CBCD = 0x9B,
  _MM_PERM_CBDA = 0x9C, _MM_PERM_CBDB = 0x9D, _MM_PERM_CBDC = 0x9E,
  _MM_PERM_CBDD = 0x9F, _MM_PERM_CCAA = 0xA0, _MM_PERM_CCAB = 0xA1,
  _MM_PERM_CCAC = 0xA2, _MM_PERM_CCAD = 0xA3, _MM_PERM_CCBA = 0xA4,
  _MM_PERM_CCBB = 0xA5, _MM_PERM_CCBC = 0xA6, _MM_PERM_CCBD = 0xA7,
  _MM_PERM_CCCA = 0xA8, _MM_PERM_CCCB = 0xA9, _MM_PERM_CCCC = 0xAA,
  _MM_PERM_CCCD = 0xAB, _MM_PERM_CCDA = 0xAC, _MM_PERM_CCDB = 0xAD,
  _MM_PERM_CCDC = 0xAE, _MM_PERM_CCDD = 0xAF, _MM_PERM_CDAA = 0xB0,
  _MM_PERM_CDAB = 0xB1, _MM_PERM_CDAC = 0xB2, _MM_PERM_CDAD = 0xB3,
  _MM_PERM_CDBA = 0xB4, _MM_PERM_CDBB = 0xB5, _MM_PERM_CDBC = 0xB6,
  _MM_PERM_CDBD = 0xB7, _MM_PERM_CDCA = 0xB8, _MM_PERM_CDCB = 0xB9,
  _MM_PERM_CDCC = 0xBA, _MM_PERM_CDCD = 0xBB, _MM_PERM_CDDA = 0xBC,
  _MM_PERM_CDDB = 0xBD, _MM_PERM_CDDC = 0xBE, _MM_PERM_CDDD = 0xBF,
  _MM_PERM_DAAA = 0xC0, _MM_PERM_DAAB = 0xC1, _MM_PERM_DAAC = 0xC2,
  _MM_PERM_DAAD = 0xC3, _MM_PERM_DABA = 0xC4, _MM_PERM_DABB = 0xC5,
  _MM_PERM_DABC = 0xC6, _MM_PERM_DABD = 0xC7, _MM_PERM_DACA = 0xC8,
  _MM_PERM_DACB = 0xC9, _MM_PERM_DACC = 0xCA, _MM_PERM_DACD = 0xCB,
  _MM_PERM_DADA = 0xCC, _MM_PERM_DADB = 0xCD, _MM_PERM_DADC = 0xCE,
  _MM_PERM_DADD = 0xCF, _MM_PERM_DBAA = 0xD0, _MM_PERM_DBAB = 0xD1,
  _MM_PERM_DBAC = 0xD2, _MM_PERM_DBAD = 0xD3, _MM_PERM_DBBA = 0xD4,
  _MM_PERM_DBBB = 0xD5, _MM_PERM_DBBC = 0xD6, _MM_PERM_DBBD = 0xD7,
  _MM_PERM_DBCA = 0xD8, _MM_PERM_DBCB = 0xD9, _MM_PERM_DBCC = 0xDA,
  _MM_PERM_DBCD = 0xDB, _MM_PERM_DBDA = 0xDC, _MM_PERM_DBDB = 0xDD,
  _MM_PERM_DBDC = 0xDE, _MM_PERM_DBDD = 0xDF, _MM_PERM_DCAA = 0xE0,
  _MM_PERM_DCAB = 0xE1, _MM_PERM_DCAC = 0xE2, _MM_PERM_DCAD = 0xE3,
  _MM_PERM_DCBA = 0xE4, _MM_PERM_DCBB = 0xE5, _MM_PERM_DCBC = 0xE6,
  _MM_PERM_DCBD = 0xE7, _MM_PERM_DCCA = 0xE8, _MM_PERM_DCCB = 0xE9,
  _MM_PERM_DCCC = 0xEA, _MM_PERM_DCCD = 0xEB, _MM_PERM_DCDA = 0xEC,
  _MM_PERM_DCDB = 0xED, _MM_PERM_DCDC = 0xEE, _MM_PERM_DCDD = 0xEF,
  _MM_PERM_DDAA = 0xF0, _MM_PERM_DDAB = 0xF1, _MM_PERM_DDAC = 0xF2,
  _MM_PERM_DDAD = 0xF3, _MM_PERM_DDBA = 0xF4, _MM_PERM_DDBB = 0xF5,
  _MM_PERM_DDBC = 0xF6, _MM_PERM_DDBD = 0xF7, _MM_PERM_DDCA = 0xF8,
  _MM_PERM_DDCB = 0xF9, _MM_PERM_DDCC = 0xFA, _MM_PERM_DDCD = 0xFB,
  _MM_PERM_DDDA = 0xFC, _MM_PERM_DDDB = 0xFD, _MM_PERM_DDDC = 0xFE,
  _MM_PERM_DDDD = 0xFF
} _MM_PERM_ENUM;

/* SIMD load ops */

static __inline __m512i __DEFAULT_FN_ATTRS512
_mm512_loadu_si512 (void const *__P)
{
  struct __loadu_si512 {
    __m512i_u __v;
  } __attribute__((__packed__, __may_alias__));
  return ((const struct __loadu_si512*)__P)->__v;
}

static __inline __m512d __DEFAULT_FN_ATTRS512
_mm512_loadu_pd(void const *__p)
{
  struct __loadu_pd {
    __m512d_u __v;
  } __attribute__((__packed__, __may_alias__));
  return ((const struct __loadu_pd*)__p)->__v;
}

static __inline __m512 __DEFAULT_FN_ATTRS512
_mm512_loadu_ps(void const *__p)
{
  struct __loadu_ps {
    __m512_u __v;
  } __attribute__((__packed__, __may_alias__));
  return ((const struct __loadu_ps*)__p)->__v;
}

static __inline void __DEFAULT_FN_ATTRS512
_mm512_storeu_pd(void *__P, __m512d __A)
{
  struct __storeu_pd {
    __m512d_u __v;
  } __attribute__((__packed__, __may_alias__));
  ((struct __storeu_pd*)__P)->__v = __A;
}

static __inline void __DEFAULT_FN_ATTRS512
_mm512_storeu_ps(void *__P, __m512 __A)
{
  struct __storeu_ps {
    __m512_u __v;
  } __attribute__((__packed__, __may_alias__));
  ((struct __storeu_ps*)__P)->__v = __A;
}

static __inline void __DEFAULT_FN_ATTRS512
_mm512_storeu_si512 (void *__P, __m512i __A)
{
  struct __storeu_si512 {
    __m512i_u __v;
  } __attribute__((__packed__, __may_alias__));
  ((struct __storeu_si512*)__P)->__v = __A;
}

static __inline __m512i __DEFAULT_FN_ATTRS512
_mm512_min_epi64(__m512i __A, __m512i __B)
{
  return (__m512i)__builtin_ia32_pminsq512((__v8di)__A, (__v8di)__B);
}

static __inline __m512i __DEFAULT_FN_ATTRS512
_mm512_max_epi64(__m512i __A, __m512i __B)
{
  return (__m512i)__builtin_ia32_pmaxsq512((__v8di)__A, (__v8di)__B);
}

static __inline __m512d __DEFAULT_FN_ATTRS512
_mm512_castsi512_pd (__m512i __A)
{
  return (__m512d) (__A);
}

static __inline __m512i __DEFAULT_FN_ATTRS512
_mm512_min_epu32(__m512i __A, __m512i __B)
{
  return (__m512i)__builtin_ia32_pminud512((__v16si)__A, (__v16si)__B);
}

static __inline __m512i __DEFAULT_FN_ATTRS512
_mm512_max_epu32(__m512i __A, __m512i __B)
{
  return (__m512i)__builtin_ia32_pmaxud512((__v16si)__A, (__v16si)__B);
}

static __inline__ __m512i __DEFAULT_FN_ATTRS512
_mm512_mask_max_epi64 (__m512i __W, __mmask8 __M, __m512i __A, __m512i __B)
{
  return (__m512i)__builtin_ia32_selectq_512((__mmask8)__M,
                                             (__v8di)_mm512_max_epi64(__A, __B),
                                             (__v8di)__W);
}

static __inline__ __m512i __DEFAULT_FN_ATTRS512
_mm512_mask_max_epu32 (__m512i __W, __mmask16 __M, __m512i __A, __m512i __B)
{
  return (__m512i)__builtin_ia32_selectd_512((__mmask16)__M,
                                            (__v16si)_mm512_max_epu32(__A, __B),
                                            (__v16si)__W);
}


#define _mm512_shuffle_epi32(A, I) \
  (__m512i)__builtin_ia32_pshufd512((__v16si)(__m512i)(A), (int)(I))

#define _mm512_permutex_pd(X, C) \
  (__m512d)__builtin_ia32_permdf512((__v8df)(__m512d)(X), (int)(C))

#define _mm512_permutex_epi64(X, C) \
  (__m512i)__builtin_ia32_permdi512((__v8di)(__m512i)(X), (int)(C))

static __inline__ void __DEFAULT_FN_ATTRS512
_mm512_mask_compressstoreu_pd (void *__P, __mmask8 __U, __m512d __A)
{
  __builtin_ia32_compressstoredf512_mask ((__v8df *) __P, (__v8df) __A,
            (__mmask8) __U);
}

static __inline__ void __DEFAULT_FN_ATTRS512
_mm512_mask_compressstoreu_ps (void *__P, __mmask16 __U, __m512 __A)
{
  __builtin_ia32_compressstoresf512_mask ((__v16sf *) __P, (__v16sf) __A,
            (__mmask16) __U);
}

static __inline __m512d __DEFAULT_FN_ATTRS512
_mm512_set1_pd(double __w)
{
  return __extension__ (__m512d){ __w, __w, __w, __w, __w, __w, __w, __w };
}

static __inline __m512 __DEFAULT_FN_ATTRS512
_mm512_set1_ps(float __w)
{
  return __extension__ (__m512){ __w, __w, __w, __w, __w, __w, __w, __w,
                                 __w, __w, __w, __w, __w, __w, __w, __w  };
}

static __inline __m512i __DEFAULT_FN_ATTRS512
_mm512_set1_epi32(int __s)
{
  return __extension__ (__m512i)(__v16si){
    __s, __s, __s, __s, __s, __s, __s, __s,
    __s, __s, __s, __s, __s, __s, __s, __s };
}

static __inline __m512i __DEFAULT_FN_ATTRS512
_mm512_set1_epi64(long long __d)
{
  return __extension__(__m512i)(__v8di){ __d, __d, __d, __d, __d, __d, __d, __d };
}

static __inline__ void __DEFAULT_FN_ATTRS512
_mm512_mask_compressstoreu_epi32 (void *__P, __mmask16 __U, __m512i __A)
{
  __builtin_ia32_compressstoresi512_mask ((__v16si *) __P, (__v16si) __A,
            (__mmask16) __U);
}

static __inline__ void __DEFAULT_FN_ATTRS512
_mm512_mask_compressstoreu_epi64 (void *__P, __mmask8 __U, __m512i __A)
{
  __builtin_ia32_compressstoredi512_mask ((__v8di *) __P, (__v8di) __A,
            (__mmask8) __U);
}

#define _mm512_permute_pd(X, C) \
  (__m512d)__builtin_ia32_vpermilpd512((__v8df)(__m512d)(X), (int)(C))

static __inline __m512i __DEFAULT_FN_ATTRS512
_mm512_castpd_si512 (__m512d __A)
{
  return (__m512i) (__A);
}

#define _mm512_shuffle_f64x2(A, B, imm) \
  (__m512d)__builtin_ia32_shuf_f64x2((__v8df)(__m512d)(A), \
                                     (__v8df)(__m512d)(B), (int)(imm))


#define _mm512_shuffle_i64x2(A, B, imm) \
  (__m512i)__builtin_ia32_shuf_i64x2((__v8di)(__m512i)(A), \
                                     (__v8di)(__m512i)(B), (int)(imm))

#define _mm256_cmp_ps_mask(a, b, p)  \
  (__mmask8)__builtin_ia32_cmpps256_mask((__v8sf)(__m256)(a), \
                                         (__v8sf)(__m256)(b), (int)(p), \
                                         (__mmask8)-1)

#define _mm512_cmp_ps_mask(A, B, P) \
  _mm512_cmp_round_ps_mask((A), (B), (P), _MM_FROUND_CUR_DIRECTION)


/* Integer compare */

#define _mm512_cmpeq_epi32_mask(A, B) \
    _mm512_cmp_epi32_mask((A), (B), _MM_CMPINT_EQ)
#define _mm512_mask_cmpeq_epi32_mask(k, A, B) \
    _mm512_mask_cmp_epi32_mask((k), (A), (B), _MM_CMPINT_EQ)
#define _mm512_cmpge_epi32_mask(A, B) \
    _mm512_cmp_epi32_mask((A), (B), _MM_CMPINT_GE)
#define _mm512_mask_cmpge_epi32_mask(k, A, B) \
    _mm512_mask_cmp_epi32_mask((k), (A), (B), _MM_CMPINT_GE)
#define _mm512_cmpgt_epi32_mask(A, B) \
    _mm512_cmp_epi32_mask((A), (B), _MM_CMPINT_GT)
#define _mm512_mask_cmpgt_epi32_mask(k, A, B) \
    _mm512_mask_cmp_epi32_mask((k), (A), (B), _MM_CMPINT_GT)
#define _mm512_cmple_epi32_mask(A, B) \
    _mm512_cmp_epi32_mask((A), (B), _MM_CMPINT_LE)
#define _mm512_mask_cmple_epi32_mask(k, A, B) \
    _mm512_mask_cmp_epi32_mask((k), (A), (B), _MM_CMPINT_LE)
#define _mm512_cmplt_epi32_mask(A, B) \
    _mm512_cmp_epi32_mask((A), (B), _MM_CMPINT_LT)
#define _mm512_mask_cmplt_epi32_mask(k, A, B) \
    _mm512_mask_cmp_epi32_mask((k), (A), (B), _MM_CMPINT_LT)
#define _mm512_cmpneq_epi32_mask(A, B) \
    _mm512_cmp_epi32_mask((A), (B), _MM_CMPINT_NE)
#define _mm512_mask_cmpneq_epi32_mask(k, A, B) \
    _mm512_mask_cmp_epi32_mask((k), (A), (B), _MM_CMPINT_NE)

#define _mm512_cmpeq_epu32_mask(A, B) \
    _mm512_cmp_epu32_mask((A), (B), _MM_CMPINT_EQ)
#define _mm512_mask_cmpeq_epu32_mask(k, A, B) \
    _mm512_mask_cmp_epu32_mask((k), (A), (B), _MM_CMPINT_EQ)
#define _mm512_cmpge_epu32_mask(A, B) \
    _mm512_cmp_epu32_mask((A), (B), _MM_CMPINT_GE)
#define _mm512_mask_cmpge_epu32_mask(k, A, B) \
    _mm512_mask_cmp_epu32_mask((k), (A), (B), _MM_CMPINT_GE)
#define _mm512_cmpgt_epu32_mask(A, B) \
    _mm512_cmp_epu32_mask((A), (B), _MM_CMPINT_GT)
#define _mm512_mask_cmpgt_epu32_mask(k, A, B) \
    _mm512_mask_cmp_epu32_mask((k), (A), (B), _MM_CMPINT_GT)
#define _mm512_cmple_epu32_mask(A, B) \
    _mm512_cmp_epu32_mask((A), (B), _MM_CMPINT_LE)
#define _mm512_mask_cmple_epu32_mask(k, A, B) \
    _mm512_mask_cmp_epu32_mask((k), (A), (B), _MM_CMPINT_LE)
#define _mm512_cmplt_epu32_mask(A, B) \
    _mm512_cmp_epu32_mask((A), (B), _MM_CMPINT_LT)
#define _mm512_mask_cmplt_epu32_mask(k, A, B) \
    _mm512_mask_cmp_epu32_mask((k), (A), (B), _MM_CMPINT_LT)
#define _mm512_cmpneq_epu32_mask(A, B) \
    _mm512_cmp_epu32_mask((A), (B), _MM_CMPINT_NE)
#define _mm512_mask_cmpneq_epu32_mask(k, A, B) \
    _mm512_mask_cmp_epu32_mask((k), (A), (B), _MM_CMPINT_NE)

#define _mm512_cmpeq_epi64_mask(A, B) \
    _mm512_cmp_epi64_mask((A), (B), _MM_CMPINT_EQ)
#define _mm512_mask_cmpeq_epi64_mask(k, A, B) \
    _mm512_mask_cmp_epi64_mask((k), (A), (B), _MM_CMPINT_EQ)
#define _mm512_cmpge_epi64_mask(A, B) \
    _mm512_cmp_epi64_mask((A), (B), _MM_CMPINT_GE)
#define _mm512_mask_cmpge_epi64_mask(k, A, B) \
    _mm512_mask_cmp_epi64_mask((k), (A), (B), _MM_CMPINT_GE)
#define _mm512_cmpgt_epi64_mask(A, B) \
    _mm512_cmp_epi64_mask((A), (B), _MM_CMPINT_GT)
#define _mm512_mask_cmpgt_epi64_mask(k, A, B) \
    _mm512_mask_cmp_epi64_mask((k), (A), (B), _MM_CMPINT_GT)
#define _mm512_cmple_epi64_mask(A, B) \
    _mm512_cmp_epi64_mask((A), (B), _MM_CMPINT_LE)
#define _mm512_mask_cmple_epi64_mask(k, A, B) \
    _mm512_mask_cmp_epi64_mask((k), (A), (B), _MM_CMPINT_LE)
#define _mm512_cmplt_epi64_mask(A, B) \
    _mm512_cmp_epi64_mask((A), (B), _MM_CMPINT_LT)
#define _mm512_mask_cmplt_epi64_mask(k, A, B) \
    _mm512_mask_cmp_epi64_mask((k), (A), (B), _MM_CMPINT_LT)
#define _mm512_cmpneq_epi64_mask(A, B) \
    _mm512_cmp_epi64_mask((A), (B), _MM_CMPINT_NE)
#define _mm512_mask_cmpneq_epi64_mask(k, A, B) \
    _mm512_mask_cmp_epi64_mask((k), (A), (B), _MM_CMPINT_NE)

#define _mm512_cmpeq_epu64_mask(A, B) \
    _mm512_cmp_epu64_mask((A), (B), _MM_CMPINT_EQ)
#define _mm512_mask_cmpeq_epu64_mask(k, A, B) \
    _mm512_mask_cmp_epu64_mask((k), (A), (B), _MM_CMPINT_EQ)
#define _mm512_cmpge_epu64_mask(A, B) \
    _mm512_cmp_epu64_mask((A), (B), _MM_CMPINT_GE)
#define _mm512_mask_cmpge_epu64_mask(k, A, B) \
    _mm512_mask_cmp_epu64_mask((k), (A), (B), _MM_CMPINT_GE)
#define _mm512_cmpgt_epu64_mask(A, B) \
    _mm512_cmp_epu64_mask((A), (B), _MM_CMPINT_GT)
#define _mm512_mask_cmpgt_epu64_mask(k, A, B) \
    _mm512_mask_cmp_epu64_mask((k), (A), (B), _MM_CMPINT_GT)
#define _mm512_cmple_epu64_mask(A, B) \
    _mm512_cmp_epu64_mask((A), (B), _MM_CMPINT_LE)
#define _mm512_mask_cmple_epu64_mask(k, A, B) \
    _mm512_mask_cmp_epu64_mask((k), (A), (B), _MM_CMPINT_LE)
#define _mm512_cmplt_epu64_mask(A, B) \
    _mm512_cmp_epu64_mask((A), (B), _MM_CMPINT_LT)
#define _mm512_mask_cmplt_epu64_mask(k, A, B) \
    _mm512_mask_cmp_epu64_mask((k), (A), (B), _MM_CMPINT_LT)
#define _mm512_cmpneq_epu64_mask(A, B) \
    _mm512_cmp_epu64_mask((A), (B), _MM_CMPINT_NE)
#define _mm512_mask_cmpneq_epu64_mask(k, A, B) \
    _mm512_mask_cmp_epu64_mask((k), (A), (B), _MM_CMPINT_NE)

#define _mm512_cmp_pd_mask(A, B, P) \
  _mm512_cmp_round_pd_mask((A), (B), (P), _MM_FROUND_CUR_DIRECTION)

#define _mm512_cmp_epu64_mask(a, b, p) \
  (__mmask8)__builtin_ia32_ucmpq512_mask((__v8di)(__m512i)(a), \
                                         (__v8di)(__m512i)(b), (int)(p), \
                                         (__mmask8)-1)

#define _mm512_cmp_epi32_mask(a, b, p) \
  (__mmask16)__builtin_ia32_cmpd512_mask((__v16si)(__m512i)(a), \
                                         (__v16si)(__m512i)(b), (int)(p), \
                                         (__mmask16)-1)

#define _mm512_cmp_epi64_mask(a, b, p) \
  (__mmask8)__builtin_ia32_cmpq512_mask((__v8di)(__m512i)(a), \
                                        (__v8di)(__m512i)(b), (int)(p), \
                                        (__mmask8)-1)

#define _mm512_cmp_epu32_mask(a, b, p) \
  (__mmask16)__builtin_ia32_ucmpd512_mask((__v16si)(__m512i)(a), \
                                          (__v16si)(__m512i)(b), (int)(p), \
                                          (__mmask16)-1)

#define _mm512_cmp_round_ps_mask(A, B, P, R) \
  (__mmask16)__builtin_ia32_cmpps512_mask((__v16sf)(__m512)(A), \
                                          (__v16sf)(__m512)(B), (int)(P), \
                                          (__mmask16)-1, (int)(R))

#define _mm512_mask_cmp_round_ps_mask(U, A, B, P, R) \
  (__mmask16)__builtin_ia32_cmpps512_mask((__v16sf)(__m512)(A), \
                                          (__v16sf)(__m512)(B), (int)(P), \
                                          (__mmask16)(U), (int)(R))

#define _mm512_cmp_round_pd_mask(A, B, P, R) \
  (__mmask8)__builtin_ia32_cmppd512_mask((__v8df)(__m512d)(A), \
                                         (__v8df)(__m512d)(B), (int)(P), \
                                         (__mmask8)-1, (int)(R))


#undef __DEFAULT_FN_ATTRS

/*===---- popcntintrin.h - POPCNT intrinsics -------------------------------===
 *
 * Part of the LLVM Project, under the Apache License v2.0 with LLVM Exceptions.
 * See https://llvm.org/LICENSE.txt for license information.
 * SPDX-License-Identifier: Apache-2.0 WITH LLVM-exception
 *
 *===-----------------------------------------------------------------------===
 */

/* Define the default attributes for the functions in this file. */
#define __DEFAULT_FN_ATTRS __attribute__((__always_inline__, __nodebug__, __target__("popcnt")))

/// Counts the number of bits in the source operand having a value of 1.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> POPCNT </c> instruction.
///
/// \param __A
///    An unsigned 32-bit integer operand.
/// \returns A 32-bit integer containing the number of bits with value 1 in the
///    source operand.
static __inline__ int __DEFAULT_FN_ATTRS
_mm_popcnt_u32(unsigned int __A)
{
  return __builtin_popcount(__A);
}

#ifdef __x86_64__
/// Counts the number of bits in the source operand having a value of 1.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the <c> POPCNT </c> instruction.
///
/// \param __A
///    An unsigned 64-bit integer operand.
/// \returns A 64-bit integer containing the number of bits with value 1 in the
///    source operand.
static __inline__ long long __DEFAULT_FN_ATTRS
_mm_popcnt_u64(unsigned long long __A)
{
  return __builtin_popcountll(__A);
}
#endif /* __x86_64__ */

#endif //_IMMINTRIN_H