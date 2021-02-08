// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// From llvm-3.9/clang-3.9.1 emmintrin.h:

/*===---- emmintrin.h - SSE2 intrinsics ------------------------------------===
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 *===-----------------------------------------------------------------------===
 */

#include "palrt.h"
#ifdef __GNUC__
#ifndef __EMMINTRIN_H
#define __IMMINTRIN_H

typedef long long __m128i __attribute__((__vector_size__(16)));

typedef unsigned long long __v2du __attribute__ ((__vector_size__ (16)));
typedef short __v8hi __attribute__((__vector_size__(16)));
typedef char __v16qi __attribute__((__vector_size__(16)));


/* Define the default attribute for the functions in this file. */
#define __DEFAULT_FN_ATTRS __attribute__((__always_inline__, NODEBUG_ATTRIBUTE))

/// \brief Performs a bitwise OR of two 128-bit integer vectors.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the \c VPOR / POR instruction.
///
/// \param __a
///    A 128-bit integer vector containing one of the source operands.
/// \param __b
///    A 128-bit integer vector containing one of the source operands.
/// \returns A 128-bit integer vector containing the bitwise OR of the values
///    in both operands.
static __inline__ __m128i __DEFAULT_FN_ATTRS
_mm_or_si128(__m128i __a, __m128i __b)
{
  return (__m128i)((__v2du)__a | (__v2du)__b);
}

/// \brief Compares each of the corresponding 16-bit values of the 128-bit
///    integer vectors for equality. Each comparison yields 0h for false, FFFFh
///    for true.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the \c VPCMPEQW / PCMPEQW instruction.
///
/// \param __a
///    A 128-bit integer vector.
/// \param __b
///    A 128-bit integer vector.
/// \returns A 128-bit integer vector containing the comparison results.
static __inline__ __m128i __DEFAULT_FN_ATTRS
_mm_cmpeq_epi16(__m128i __a, __m128i __b)
{
  return (__m128i)((__v8hi)__a == (__v8hi)__b);
}

/// \brief Moves packed integer values from an unaligned 128-bit memory location
///    to elements in a 128-bit integer vector.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the \c VMOVDQU / MOVDQU instruction.
///
/// \param __p
///    A pointer to a memory location containing integer values.
/// \returns A 128-bit integer vector containing the moved values.
static __inline__ __m128i __DEFAULT_FN_ATTRS
_mm_loadu_si128(__m128i const *__p)
{
  struct __loadu_si128 {
    __m128i __v;
  } __attribute__((__packed__, __may_alias__));
  return ((struct __loadu_si128*)__p)->__v;
}

/// \brief Initializes all values in a 128-bit vector of [8 x i16] with the
///    specified 16-bit value.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic is a utility function and does not correspond to a specific
///    instruction.
///
/// \param __w
///    A 16-bit value used to initialize the elements of the destination integer
///    vector.
/// \returns An initialized 128-bit vector of [8 x i16] with all elements
///    containing the value provided in the operand.
static __inline__ __m128i __DEFAULT_FN_ATTRS
_mm_set1_epi16(short __w)
{
  return (__m128i)(__v8hi){ __w, __w, __w, __w, __w, __w, __w, __w };
}

static __inline__ int __DEFAULT_FN_ATTRS
_mm_movemask_epi8(__m128i __a)
{
  return __builtin_ia32_pmovmskb128((__v16qi)__a);
}

#undef __DEFAULT_FN_ATTRS

#endif /* __EMMINTRIN_H */
#endif // __GNUC__
