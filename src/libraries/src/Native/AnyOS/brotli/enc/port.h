/* Copyright 2013 Google Inc. All Rights Reserved.

   Distributed under MIT license.
   See file LICENSE for detail or copy at https://opensource.org/licenses/MIT
*/

/* Macros for endianness, branch prediction and unaligned loads and stores. */

#ifndef BROTLI_ENC_PORT_H_
#define BROTLI_ENC_PORT_H_

#include <assert.h>
#include <string.h>  /* memcpy */

#include <brotli/port.h>
#include <brotli/types.h>

#if defined OS_LINUX || defined OS_CYGWIN
#include <endian.h>
#elif defined OS_FREEBSD
#include <machine/endian.h>
#elif defined OS_MACOSX
#include <machine/endian.h>
/* Let's try and follow the Linux convention */
#define __BYTE_ORDER  BYTE_ORDER
#define __LITTLE_ENDIAN LITTLE_ENDIAN
#endif

/* define the macro BROTLI_LITTLE_ENDIAN
   using the above endian definitions from endian.h if
   endian.h was included */
#ifdef __BYTE_ORDER
#if __BYTE_ORDER == __LITTLE_ENDIAN
#define BROTLI_LITTLE_ENDIAN
#endif

#else

#if defined(__LITTLE_ENDIAN__)
#define BROTLI_LITTLE_ENDIAN
#endif
#endif  /* __BYTE_ORDER */

#if defined(__BYTE_ORDER__) && (__BYTE_ORDER__ == __ORDER_LITTLE_ENDIAN__)
#define BROTLI_LITTLE_ENDIAN
#endif

/* Enable little-endian optimization for x64 architecture on Windows. */
#if (defined(_WIN32) || defined(_WIN64)) && defined(_M_X64)
#define BROTLI_LITTLE_ENDIAN
#endif

/* Portable handling of unaligned loads, stores, and copies.
   On some platforms, like ARM, the copy functions can be more efficient
   then a load and a store. */

#if defined(BROTLI_LITTLE_ENDIAN) && (\
    defined(ARCH_PIII) || defined(ARCH_ATHLON) || \
    defined(ARCH_K8) || defined(_ARCH_PPC))

/* x86 and x86-64 can perform unaligned loads/stores directly;
   modern PowerPC hardware can also do unaligned integer loads and stores;
   but note: the FPU still sends unaligned loads and stores to a trap handler!
*/

#define BROTLI_UNALIGNED_LOAD32(_p) (*(const uint32_t *)(_p))
#define BROTLI_UNALIGNED_LOAD64LE(_p) (*(const uint64_t *)(_p))

#define BROTLI_UNALIGNED_STORE64LE(_p, _val) \
  (*(uint64_t *)(_p) = (_val))

#elif defined(BROTLI_LITTLE_ENDIAN) && defined(__arm__) && \
  !defined(__ARM_ARCH_5__) && \
  !defined(__ARM_ARCH_5T__) && \
  !defined(__ARM_ARCH_5TE__) && \
  !defined(__ARM_ARCH_5TEJ__) && \
  !defined(__ARM_ARCH_6__) && \
  !defined(__ARM_ARCH_6J__) && \
  !defined(__ARM_ARCH_6K__) && \
  !defined(__ARM_ARCH_6Z__) && \
  !defined(__ARM_ARCH_6ZK__) && \
  !defined(__ARM_ARCH_6T2__)

/* ARMv7 and newer support native unaligned accesses, but only of 16-bit
   and 32-bit values (not 64-bit); older versions either raise a fatal signal,
   do an unaligned read and rotate the words around a bit, or do the reads very
   slowly (trip through kernel mode). */

#define BROTLI_UNALIGNED_LOAD32(_p) (*(const uint32_t *)(_p))

static BROTLI_INLINE uint64_t BROTLI_UNALIGNED_LOAD64LE(const void *p) {
  uint64_t t;
  memcpy(&t, p, sizeof t);
  return t;
}

static BROTLI_INLINE void BROTLI_UNALIGNED_STORE64LE(void *p, uint64_t v) {
  memcpy(p, &v, sizeof v);
}

#else

/* These functions are provided for architectures that don't support */
/* unaligned loads and stores. */

static BROTLI_INLINE uint32_t BROTLI_UNALIGNED_LOAD32(const void *p) {
  uint32_t t;
  memcpy(&t, p, sizeof t);
  return t;
}

#if defined(BROTLI_LITTLE_ENDIAN)

static BROTLI_INLINE uint64_t BROTLI_UNALIGNED_LOAD64LE(const void *p) {
  uint64_t t;
  memcpy(&t, p, sizeof t);
  return t;
}

static BROTLI_INLINE void BROTLI_UNALIGNED_STORE64LE(void *p, uint64_t v) {
  memcpy(p, &v, sizeof v);
}

#else  /* BROTLI_LITTLE_ENDIAN */

static BROTLI_INLINE uint64_t BROTLI_UNALIGNED_LOAD64LE(const void *p) {
  const uint8_t* in = (const uint8_t*)p;
  uint64_t value = (uint64_t)(in[0]);
  value |= (uint64_t)(in[1]) << 8;
  value |= (uint64_t)(in[2]) << 16;
  value |= (uint64_t)(in[3]) << 24;
  value |= (uint64_t)(in[4]) << 32;
  value |= (uint64_t)(in[5]) << 40;
  value |= (uint64_t)(in[6]) << 48;
  value |= (uint64_t)(in[7]) << 56;
  return value;
}

static BROTLI_INLINE void BROTLI_UNALIGNED_STORE64LE(void *p, uint64_t v) {
  uint8_t* out = (uint8_t*)p;
  out[0] = (uint8_t)v;
  out[1] = (uint8_t)(v >> 8);
  out[2] = (uint8_t)(v >> 16);
  out[3] = (uint8_t)(v >> 24);
  out[4] = (uint8_t)(v >> 32);
  out[5] = (uint8_t)(v >> 40);
  out[6] = (uint8_t)(v >> 48);
  out[7] = (uint8_t)(v >> 56);
}

#endif  /* BROTLI_LITTLE_ENDIAN */

#endif

#define TEMPLATE_(T)                                                           \
  static BROTLI_INLINE T brotli_min_ ## T (T a, T b) { return a < b ? a : b; } \
  static BROTLI_INLINE T brotli_max_ ## T (T a, T b) { return a > b ? a : b; }
TEMPLATE_(double) TEMPLATE_(float) TEMPLATE_(int)
TEMPLATE_(size_t) TEMPLATE_(uint32_t) TEMPLATE_(uint8_t)
#undef TEMPLATE_
#define BROTLI_MIN(T, A, B) (brotli_min_ ## T((A), (B)))
#define BROTLI_MAX(T, A, B) (brotli_max_ ## T((A), (B)))

#define BROTLI_SWAP(T, A, I, J) { \
  T __brotli_swap_tmp = (A)[(I)]; \
  (A)[(I)] = (A)[(J)];            \
  (A)[(J)] = __brotli_swap_tmp;   \
}

#define BROTLI_ENSURE_CAPACITY(M, T, A, C, R) {  \
  if (C < (R)) {                                 \
    size_t _new_size = (C == 0) ? (R) : C;       \
    T* new_array;                                \
    while (_new_size < (R)) _new_size *= 2;      \
    new_array = BROTLI_ALLOC((M), T, _new_size); \
    if (!BROTLI_IS_OOM(m) && C != 0)             \
      memcpy(new_array, A, C * sizeof(T));       \
    BROTLI_FREE((M), A);                         \
    A = new_array;                               \
    C = _new_size;                               \
  }                                              \
}

#endif  /* BROTLI_ENC_PORT_H_ */
