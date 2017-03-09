// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// From llvm-3.9/clang-3.9.1 xmmintrin.h:

/*===---- xmmintrin.h - SSE intrinsics -------------------------------------===
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

#ifdef __clang__

typedef float __m128 __attribute__((__vector_size__(16)));

/* Define the default attributes for the functions in this file. */
#define __DEFAULT_FN_ATTRS __attribute__((__always_inline__, __nodebug__))

/// \brief Loads a 128-bit floating-point vector of [4 x float] from an aligned
///    memory location.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the \c VMOVAPS / MOVAPS instruction.
///
/// \param __p
///    A pointer to a 128-bit memory location. The address of the memory
///    location has to be 128-bit aligned.
/// \returns A 128-bit vector of [4 x float] containing the loaded valus.
static __inline__ __m128 __DEFAULT_FN_ATTRS
_mm_load_ps(const float *__p)
{
    return *(__m128*)__p;
}

/// \brief Loads a 128-bit floating-point vector of [4 x float] from an
///    unaligned memory location.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the \c VMOVUPS / MOVUPS instruction.
///
/// \param __p
///    A pointer to a 128-bit memory location. The address of the memory
///    location does not have to be aligned.
/// \returns A 128-bit vector of [4 x float] containing the loaded values.
static __inline__ __m128 __DEFAULT_FN_ATTRS
_mm_loadu_ps(const float *__p)
{
    struct __loadu_ps
    {
        __m128 __v;
    } __attribute__((__packed__, __may_alias__));
    return ((struct __loadu_ps*)__p)->__v;
}

/// \brief Stores float values from a 128-bit vector of [4 x float] to an
///    unaligned memory location.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to the \c VMOVUPS / MOVUPS instruction.
///
/// \param __p
///    A pointer to a 128-bit memory location. The address of the memory
///    location does not have to be aligned.
/// \param __a
///    A 128-bit vector of [4 x float] containing the values to be stored.
static __inline__ void __DEFAULT_FN_ATTRS
_mm_storeu_ps(float *__p, __m128 __a)
{
    struct __storeu_ps
    {
        __m128 __v;
    } __attribute__((__packed__, __may_alias__));
    ((struct __storeu_ps*)__p)->__v = __a;
}

/// \brief Stores the lower 32 bits of a 128-bit vector of [4 x float] into
///    four contiguous elements in an aligned memory location.
///
/// \headerfile <x86intrin.h>
///
/// This intrinsic corresponds to \c VMOVAPS / MOVAPS + \c shuffling
///    instruction.
///
/// \param __p
///    A pointer to a 128-bit memory location.
/// \param __a
///    A 128-bit vector of [4 x float] whose lower 32 bits are stored to each
///    of the four contiguous elements pointed by __p.
static __inline__ void __DEFAULT_FN_ATTRS
_mm_store_ps(float *__p, __m128 __a)
{
    *(__m128*)__p = __a;
}

#endif // __clang__
