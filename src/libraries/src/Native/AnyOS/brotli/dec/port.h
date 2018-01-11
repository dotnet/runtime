/* Copyright 2015 Google Inc. All Rights Reserved.

   Distributed under MIT license.
   See file LICENSE for detail or copy at https://opensource.org/licenses/MIT
*/

/* Macros for compiler / platform specific features and build options.

   Build options are:
    * BROTLI_BUILD_32_BIT disables 64-bit optimizations
    * BROTLI_BUILD_64_BIT forces to use 64-bit optimizations
    * BROTLI_BUILD_BIG_ENDIAN forces to use big-endian optimizations
    * BROTLI_BUILD_ENDIAN_NEUTRAL disables endian-aware optimizations
    * BROTLI_BUILD_LITTLE_ENDIAN forces to use little-endian optimizations
    * BROTLI_BUILD_MODERN_COMPILER forces to use modern compilers built-ins,
      features and attributes
    * BROTLI_BUILD_PORTABLE disables dangerous optimizations, like unaligned
      read and overlapping memcpy; this reduces decompression speed by 5%
    * BROTLI_BUILD_NO_RBIT disables "rbit" optimization for ARM CPUs
    * BROTLI_DEBUG dumps file name and line number when decoder detects stream
      or memory error
    * BROTLI_ENABLE_LOG enables asserts and dumps various state information
 */

#ifndef BROTLI_DEC_PORT_H_
#define BROTLI_DEC_PORT_H_

#if defined(BROTLI_ENABLE_LOG) || defined(BROTLI_DEBUG)
#include <assert.h>
#include <stdio.h>
#endif

#include <brotli/port.h>

#if defined(__arm__) || defined(__thumb__) || \
    defined(_M_ARM) || defined(_M_ARMT) || defined(__ARM64_ARCH_8__)
#define BROTLI_TARGET_ARM
#if (defined(__ARM_ARCH) && (__ARM_ARCH == 7)) || \
    (defined(M_ARM) && (M_ARM == 7))
#define BROTLI_TARGET_ARMV7
#endif  /* ARMv7 */
#if defined(__aarch64__) || defined(__ARM64_ARCH_8__)
#define BROTLI_TARGET_ARMV8
#endif  /* ARMv8 */
#endif  /* ARM */

#if defined(__i386) || defined(_M_IX86)
#define BROTLI_TARGET_X86
#endif

#if defined(__x86_64__) || defined(_M_X64)
#define BROTLI_TARGET_X64
#endif

#if defined(__PPC64__)
#define BROTLI_TARGET_POWERPC64
#endif

#ifdef BROTLI_BUILD_PORTABLE
#define BROTLI_ALIGNED_READ (!!1)
#elif defined(BROTLI_TARGET_X86) || defined(BROTLI_TARGET_X64) || \
     defined(BROTLI_TARGET_ARMV7) || defined(BROTLI_TARGET_ARMV8)
/* Allow unaligned read only for white-listed CPUs. */
#define BROTLI_ALIGNED_READ (!!0)
#else
#define BROTLI_ALIGNED_READ (!!1)
#endif

/* IS_CONSTANT macros returns true for compile-time constant expressions. */
#if BROTLI_MODERN_COMPILER || __has_builtin(__builtin_constant_p)
#define IS_CONSTANT(x) (!!__builtin_constant_p(x))
#else
#define IS_CONSTANT(x) (!!0)
#endif

#ifdef BROTLI_ENABLE_LOG
#define BROTLI_DCHECK(x) assert(x)
#define BROTLI_LOG(x) printf x
#else
#define BROTLI_DCHECK(x)
#define BROTLI_LOG(x)
#endif

#if defined(BROTLI_DEBUG) || defined(BROTLI_ENABLE_LOG)
static BROTLI_INLINE void BrotliDump(const char* f, int l, const char* fn) {
  fprintf(stderr, "%s:%d (%s)\n", f, l, fn);
  fflush(stderr);
}
#define BROTLI_DUMP() BrotliDump(__FILE__, __LINE__, __FUNCTION__)
#else
#define BROTLI_DUMP() (void)(0)
#endif

#if defined(BROTLI_BUILD_64_BIT)
#define BROTLI_64_BITS 1
#elif defined(BROTLI_BUILD_32_BIT)
#define BROTLI_64_BITS 0
#elif defined(BROTLI_TARGET_X64) || defined(BROTLI_TARGET_ARMV8) || \
    defined(BROTLI_TARGET_POWERPC64)
#define BROTLI_64_BITS 1
#else
#define BROTLI_64_BITS 0
#endif

#if (BROTLI_64_BITS)
#define reg_t uint64_t
#else
#define reg_t uint32_t
#endif

#if defined(BROTLI_BUILD_BIG_ENDIAN)
#define BROTLI_LITTLE_ENDIAN 0
#define BROTLI_BIG_ENDIAN 1
#elif defined(BROTLI_BUILD_LITTLE_ENDIAN)
#define BROTLI_LITTLE_ENDIAN 1
#define BROTLI_BIG_ENDIAN 0
#elif defined(BROTLI_BUILD_ENDIAN_NEUTRAL)
#define BROTLI_LITTLE_ENDIAN 0
#define BROTLI_BIG_ENDIAN 0
#elif defined(__BYTE_ORDER__) && (__BYTE_ORDER__ == __ORDER_LITTLE_ENDIAN__)
#define BROTLI_LITTLE_ENDIAN 1
#define BROTLI_BIG_ENDIAN 0
#elif defined(_WIN32)
/* Win32 can currently always be assumed to be little endian */
#define BROTLI_LITTLE_ENDIAN 1
#define BROTLI_BIG_ENDIAN 0
#else
#if (defined(__BYTE_ORDER__) && (__BYTE_ORDER__ == __ORDER_BIG_ENDIAN__))
#define BROTLI_BIG_ENDIAN 1
#else
#define BROTLI_BIG_ENDIAN 0
#endif
#define BROTLI_LITTLE_ENDIAN 0
#endif

#define BROTLI_REPEAT(N, X) {     \
  if ((N & 1) != 0) {X;}          \
  if ((N & 2) != 0) {X; X;}       \
  if ((N & 4) != 0) {X; X; X; X;} \
}

#if (BROTLI_MODERN_COMPILER || defined(__llvm__)) && \
    !defined(BROTLI_BUILD_NO_RBIT)
#if defined(BROTLI_TARGET_ARMV7) || defined(BROTLI_TARGET_ARMV8)
/* TODO: detect ARMv6T2 and enable this code for it. */
static BROTLI_INLINE reg_t BrotliRBit(reg_t input) {
  reg_t output;
  __asm__("rbit %0, %1\n" : "=r"(output) : "r"(input));
  return output;
}
#define BROTLI_RBIT(x) BrotliRBit(x)
#endif  /* armv7 */
#endif  /* gcc || clang */

#if defined(BROTLI_TARGET_ARM)
#define BROTLI_HAS_UBFX (!!1)
#else
#define BROTLI_HAS_UBFX (!!0)
#endif

#define BROTLI_ALLOC(S, L) S->alloc_func(S->memory_manager_opaque, L)

#define BROTLI_FREE(S, X) {                  \
  S->free_func(S->memory_manager_opaque, X); \
  X = NULL;                                  \
}

#endif  /* BROTLI_DEC_PORT_H_ */
