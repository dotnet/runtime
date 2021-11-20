/* functable.c -- Choose relevant optimized functions at runtime
 * Copyright (C) 2017 Hans Kristian Rosbach
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include "zbuild.h"
#include "zendian.h"
#include "deflate.h"
#include "deflate_p.h"

#include "functable.h"

#ifdef X86_FEATURES
#  include "fallback_builtins.h"
#endif

/* insert_string */
extern void insert_string_c(deflate_state *const s, const uint32_t str, uint32_t count);
#ifdef X86_SSE42_CRC_HASH
extern void insert_string_sse4(deflate_state *const s, const uint32_t str, uint32_t count);
#elif defined(ARM_ACLE_CRC_HASH)
extern void insert_string_acle(deflate_state *const s, const uint32_t str, uint32_t count);
#endif

/* quick_insert_string */
extern Pos quick_insert_string_c(deflate_state *const s, const uint32_t str);
#ifdef X86_SSE42_CRC_HASH
extern Pos quick_insert_string_sse4(deflate_state *const s, const uint32_t str);
#elif defined(ARM_ACLE_CRC_HASH)
extern Pos quick_insert_string_acle(deflate_state *const s, const uint32_t str);
#endif

/* slide_hash */
#ifdef X86_SSE2
void slide_hash_sse2(deflate_state *s);
#elif defined(ARM_NEON_SLIDEHASH)
void slide_hash_neon(deflate_state *s);
#elif defined(POWER8_VSX_SLIDEHASH)
void slide_hash_power8(deflate_state *s);
#endif
#ifdef X86_AVX2
void slide_hash_avx2(deflate_state *s);
#endif

/* adler32 */
extern uint32_t adler32_c(uint32_t adler, const unsigned char *buf, size_t len);
#ifdef ARM_NEON_ADLER32
extern uint32_t adler32_neon(uint32_t adler, const unsigned char *buf, size_t len);
#endif
#ifdef X86_SSSE3_ADLER32
extern uint32_t adler32_ssse3(uint32_t adler, const unsigned char *buf, size_t len);
#endif
#ifdef X86_AVX2_ADLER32
extern uint32_t adler32_avx2(uint32_t adler, const unsigned char *buf, size_t len);
#endif
#ifdef POWER8_VSX_ADLER32
extern uint32_t adler32_power8(uint32_t adler, const unsigned char* buf, size_t len);
#endif

/* memory chunking */
extern uint32_t chunksize_c(void);
extern uint8_t* chunkcopy_c(uint8_t *out, uint8_t const *from, unsigned len);
extern uint8_t* chunkcopy_safe_c(uint8_t *out, uint8_t const *from, unsigned len, uint8_t *safe);
extern uint8_t* chunkunroll_c(uint8_t *out, unsigned *dist, unsigned *len);
extern uint8_t* chunkmemset_c(uint8_t *out, unsigned dist, unsigned len);
extern uint8_t* chunkmemset_safe_c(uint8_t *out, unsigned dist, unsigned len, unsigned left);
#ifdef X86_SSE2_CHUNKSET
extern uint32_t chunksize_sse2(void);
extern uint8_t* chunkcopy_sse2(uint8_t *out, uint8_t const *from, unsigned len);
extern uint8_t* chunkcopy_safe_sse2(uint8_t *out, uint8_t const *from, unsigned len, uint8_t *safe);
extern uint8_t* chunkunroll_sse2(uint8_t *out, unsigned *dist, unsigned *len);
extern uint8_t* chunkmemset_sse2(uint8_t *out, unsigned dist, unsigned len);
extern uint8_t* chunkmemset_safe_sse2(uint8_t *out, unsigned dist, unsigned len, unsigned left);
#endif
#ifdef X86_AVX_CHUNKSET
extern uint32_t chunksize_avx(void);
extern uint8_t* chunkcopy_avx(uint8_t *out, uint8_t const *from, unsigned len);
extern uint8_t* chunkcopy_safe_avx(uint8_t *out, uint8_t const *from, unsigned len, uint8_t *safe);
extern uint8_t* chunkunroll_avx(uint8_t *out, unsigned *dist, unsigned *len);
extern uint8_t* chunkmemset_avx(uint8_t *out, unsigned dist, unsigned len);
extern uint8_t* chunkmemset_safe_avx(uint8_t *out, unsigned dist, unsigned len, unsigned left);
#endif
#ifdef ARM_NEON_CHUNKSET
extern uint32_t chunksize_neon(void);
extern uint8_t* chunkcopy_neon(uint8_t *out, uint8_t const *from, unsigned len);
extern uint8_t* chunkcopy_safe_neon(uint8_t *out, uint8_t const *from, unsigned len, uint8_t *safe);
extern uint8_t* chunkunroll_neon(uint8_t *out, unsigned *dist, unsigned *len);
extern uint8_t* chunkmemset_neon(uint8_t *out, unsigned dist, unsigned len);
extern uint8_t* chunkmemset_safe_neon(uint8_t *out, unsigned dist, unsigned len, unsigned left);
#endif

/* CRC32 */
Z_INTERNAL uint32_t crc32_generic(uint32_t, const unsigned char *, uint64_t);

#ifdef ARM_ACLE_CRC_HASH
extern uint32_t crc32_acle(uint32_t, const unsigned char *, uint64_t);
#endif

#if BYTE_ORDER == LITTLE_ENDIAN
extern uint32_t crc32_little(uint32_t, const unsigned char *, uint64_t);
#elif BYTE_ORDER == BIG_ENDIAN
extern uint32_t crc32_big(uint32_t, const unsigned char *, uint64_t);
#endif

/* compare258 */
extern uint32_t compare258_c(const unsigned char *src0, const unsigned char *src1);
#ifdef UNALIGNED_OK
extern uint32_t compare258_unaligned_16(const unsigned char *src0, const unsigned char *src1);
extern uint32_t compare258_unaligned_32(const unsigned char *src0, const unsigned char *src1);
#ifdef UNALIGNED64_OK
extern uint32_t compare258_unaligned_64(const unsigned char *src0, const unsigned char *src1);
#endif
#ifdef X86_SSE42_CMP_STR
extern uint32_t compare258_unaligned_sse4(const unsigned char *src0, const unsigned char *src1);
#endif
#if defined(X86_AVX2) && defined(HAVE_BUILTIN_CTZ)
extern uint32_t compare258_unaligned_avx2(const unsigned char *src0, const unsigned char *src1);
#endif
#endif

/* longest_match */
extern uint32_t longest_match_c(deflate_state *const s, Pos cur_match);
#ifdef UNALIGNED_OK
extern uint32_t longest_match_unaligned_16(deflate_state *const s, Pos cur_match);
extern uint32_t longest_match_unaligned_32(deflate_state *const s, Pos cur_match);
#ifdef UNALIGNED64_OK
extern uint32_t longest_match_unaligned_64(deflate_state *const s, Pos cur_match);
#endif
#ifdef X86_SSE42_CMP_STR
extern uint32_t longest_match_unaligned_sse4(deflate_state *const s, Pos cur_match);
#endif
#if defined(X86_AVX2) && defined(HAVE_BUILTIN_CTZ)
extern uint32_t longest_match_unaligned_avx2(deflate_state *const s, Pos cur_match);
#endif
#endif

Z_INTERNAL Z_TLS struct functable_s functable;

Z_INTERNAL void cpu_check_features(void)
{
    static int features_checked = 0;
    if (features_checked)
        return;
#if defined(X86_FEATURES)
    x86_check_features();
#elif defined(ARM_FEATURES)
    arm_check_features();
#elif defined(POWER_FEATURES)
    power_check_features();
#endif
    features_checked = 1;
}

/* stub functions */
Z_INTERNAL void insert_string_stub(deflate_state *const s, const uint32_t str, uint32_t count) {
    // Initialize default

    functable.insert_string = &insert_string_c;
    cpu_check_features();

#ifdef X86_SSE42_CRC_HASH
    if (x86_cpu_has_sse42)
        functable.insert_string = &insert_string_sse4;
#elif defined(ARM_ACLE_CRC_HASH)
    if (arm_cpu_has_crc32)
        functable.insert_string = &insert_string_acle;
#endif

    functable.insert_string(s, str, count);
}

Z_INTERNAL Pos quick_insert_string_stub(deflate_state *const s, const uint32_t str) {
    functable.quick_insert_string = &quick_insert_string_c;

#ifdef X86_SSE42_CRC_HASH
    if (x86_cpu_has_sse42)
        functable.quick_insert_string = &quick_insert_string_sse4;
#elif defined(ARM_ACLE_CRC_HASH)
    if (arm_cpu_has_crc32)
        functable.quick_insert_string = &quick_insert_string_acle;
#endif

    return functable.quick_insert_string(s, str);
}

Z_INTERNAL void slide_hash_stub(deflate_state *s) {

    functable.slide_hash = &slide_hash_c;
    cpu_check_features();

#ifdef X86_SSE2
#  if !defined(__x86_64__) && !defined(_M_X64) && !defined(X86_NOCHECK_SSE2)
    if (x86_cpu_has_sse2)
#  endif
        functable.slide_hash = &slide_hash_sse2;
#elif defined(ARM_NEON_SLIDEHASH)
#  ifndef ARM_NOCHECK_NEON
    if (arm_cpu_has_neon)
#  endif
        functable.slide_hash = &slide_hash_neon;
#endif
#ifdef X86_AVX2
    if (x86_cpu_has_avx2)
        functable.slide_hash = &slide_hash_avx2;
#endif
#ifdef POWER8_VSX_SLIDEHASH
    if (power_cpu_has_arch_2_07)
        functable.slide_hash = &slide_hash_power8;
#endif

    functable.slide_hash(s);
}

Z_INTERNAL uint32_t adler32_stub(uint32_t adler, const unsigned char *buf, size_t len) {
    // Initialize default
    functable.adler32 = &adler32_c;
    cpu_check_features();

#ifdef ARM_NEON_ADLER32
#  ifndef ARM_NOCHECK_NEON
    if (arm_cpu_has_neon)
#  endif
        functable.adler32 = &adler32_neon;
#endif
#ifdef X86_SSSE3_ADLER32
    if (x86_cpu_has_ssse3)
        functable.adler32 = &adler32_ssse3;
#endif
#ifdef X86_AVX2_ADLER32
    if (x86_cpu_has_avx2)
        functable.adler32 = &adler32_avx2;
#endif
#ifdef POWER8_VSX_ADLER32
    if (power_cpu_has_arch_2_07)
        functable.adler32 = &adler32_power8;
#endif

    return functable.adler32(adler, buf, len);
}

Z_INTERNAL uint32_t chunksize_stub(void) {
    // Initialize default
    functable.chunksize = &chunksize_c;
    cpu_check_features();

#ifdef X86_SSE2_CHUNKSET
# if !defined(__x86_64__) && !defined(_M_X64) && !defined(X86_NOCHECK_SSE2)
    if (x86_cpu_has_sse2)
# endif
        functable.chunksize = &chunksize_sse2;
#endif
#ifdef X86_AVX_CHUNKSET
    if (x86_cpu_has_avx2)
        functable.chunksize = &chunksize_avx;
#endif
#ifdef ARM_NEON_CHUNKSET
    if (arm_cpu_has_neon)
        functable.chunksize = &chunksize_neon;
#endif

    return functable.chunksize();
}

Z_INTERNAL uint8_t* chunkcopy_stub(uint8_t *out, uint8_t const *from, unsigned len) {
    // Initialize default
    functable.chunkcopy = &chunkcopy_c;

#ifdef X86_SSE2_CHUNKSET
# if !defined(__x86_64__) && !defined(_M_X64) && !defined(X86_NOCHECK_SSE2)
    if (x86_cpu_has_sse2)
# endif
        functable.chunkcopy = &chunkcopy_sse2;
#endif
#ifdef X86_AVX_CHUNKSET
    if (x86_cpu_has_avx2)
        functable.chunkcopy = &chunkcopy_avx;
#endif
#ifdef ARM_NEON_CHUNKSET
    if (arm_cpu_has_neon)
        functable.chunkcopy = &chunkcopy_neon;
#endif

    return functable.chunkcopy(out, from, len);
}

Z_INTERNAL uint8_t* chunkcopy_safe_stub(uint8_t *out, uint8_t const *from, unsigned len, uint8_t *safe) {
    // Initialize default
    functable.chunkcopy_safe = &chunkcopy_safe_c;

#ifdef X86_SSE2_CHUNKSET
# if !defined(__x86_64__) && !defined(_M_X64) && !defined(X86_NOCHECK_SSE2)
    if (x86_cpu_has_sse2)
# endif
        functable.chunkcopy_safe = &chunkcopy_safe_sse2;
#endif
#ifdef X86_AVX_CHUNKSET
    if (x86_cpu_has_avx2)
        functable.chunkcopy_safe = &chunkcopy_safe_avx;
#endif
#ifdef ARM_NEON_CHUNKSET
    if (arm_cpu_has_neon)
        functable.chunkcopy_safe = &chunkcopy_safe_neon;
#endif

    return functable.chunkcopy_safe(out, from, len, safe);
}

Z_INTERNAL uint8_t* chunkunroll_stub(uint8_t *out, unsigned *dist, unsigned *len) {
    // Initialize default
    functable.chunkunroll = &chunkunroll_c;

#ifdef X86_SSE2_CHUNKSET
# if !defined(__x86_64__) && !defined(_M_X64) && !defined(X86_NOCHECK_SSE2)
    if (x86_cpu_has_sse2)
# endif
        functable.chunkunroll = &chunkunroll_sse2;
#endif
#ifdef X86_AVX_CHUNKSET
    if (x86_cpu_has_avx2)
        functable.chunkunroll = &chunkunroll_avx;
#endif
#ifdef ARM_NEON_CHUNKSET
    if (arm_cpu_has_neon)
        functable.chunkunroll = &chunkunroll_neon;
#endif

    return functable.chunkunroll(out, dist, len);
}

Z_INTERNAL uint8_t* chunkmemset_stub(uint8_t *out, unsigned dist, unsigned len) {
    // Initialize default
    functable.chunkmemset = &chunkmemset_c;

#ifdef X86_SSE2_CHUNKSET
# if !defined(__x86_64__) && !defined(_M_X64) && !defined(X86_NOCHECK_SSE2)
    if (x86_cpu_has_sse2)
# endif
        functable.chunkmemset = &chunkmemset_sse2;
#endif
#ifdef X86_AVX_CHUNKSET
    if (x86_cpu_has_avx2)
        functable.chunkmemset = &chunkmemset_avx;
#endif
#ifdef ARM_NEON_CHUNKSET
    if (arm_cpu_has_neon)
        functable.chunkmemset = &chunkmemset_neon;
#endif

    return functable.chunkmemset(out, dist, len);
}

Z_INTERNAL uint8_t* chunkmemset_safe_stub(uint8_t *out, unsigned dist, unsigned len, unsigned left) {
    // Initialize default
    functable.chunkmemset_safe = &chunkmemset_safe_c;

#ifdef X86_SSE2_CHUNKSET
# if !defined(__x86_64__) && !defined(_M_X64) && !defined(X86_NOCHECK_SSE2)
    if (x86_cpu_has_sse2)
# endif
        functable.chunkmemset_safe = &chunkmemset_safe_sse2;
#endif
#ifdef X86_AVX_CHUNKSET
    if (x86_cpu_has_avx2)
        functable.chunkmemset_safe = &chunkmemset_safe_avx;
#endif
#ifdef ARM_NEON_CHUNKSET
    if (arm_cpu_has_neon)
        functable.chunkmemset_safe = &chunkmemset_safe_neon;
#endif

    return functable.chunkmemset_safe(out, dist, len, left);
}

Z_INTERNAL uint32_t crc32_stub(uint32_t crc, const unsigned char *buf, uint64_t len) {
    int32_t use_byfour = sizeof(void *) == sizeof(ptrdiff_t);

    Assert(sizeof(uint64_t) >= sizeof(size_t),
           "crc32_z takes size_t but internally we have a uint64_t len");
    /* return a function pointer for optimized arches here after a capability test */

    cpu_check_features();

    if (use_byfour) {
#if BYTE_ORDER == LITTLE_ENDIAN
        functable.crc32 = crc32_little;
#  if defined(ARM_ACLE_CRC_HASH)
        if (arm_cpu_has_crc32)
            functable.crc32 = crc32_acle;
#  endif
#elif BYTE_ORDER == BIG_ENDIAN
        functable.crc32 = crc32_big;
#else
#  error No endian defined
#endif
    } else {
        functable.crc32 = crc32_generic;
    }

    return functable.crc32(crc, buf, len);
}

Z_INTERNAL uint32_t compare258_stub(const unsigned char *src0, const unsigned char *src1) {

    functable.compare258 = &compare258_c;

#ifdef UNALIGNED_OK
#  if defined(UNALIGNED64_OK) && defined(HAVE_BUILTIN_CTZLL)
    functable.compare258 = &compare258_unaligned_64;
#  elif defined(HAVE_BUILTIN_CTZ)
    functable.compare258 = &compare258_unaligned_32;
#  else
    functable.compare258 = &compare258_unaligned_16;
#  endif
#  ifdef X86_SSE42_CMP_STR
    if (x86_cpu_has_sse42)
        functable.compare258 = &compare258_unaligned_sse4;
#  endif
#  if defined(X86_AVX2) && defined(HAVE_BUILTIN_CTZ)
    if (x86_cpu_has_avx2)
        functable.compare258 = &compare258_unaligned_avx2;
#  endif
#endif

    return functable.compare258(src0, src1);
}

Z_INTERNAL uint32_t longest_match_stub(deflate_state *const s, Pos cur_match) {

    functable.longest_match = &longest_match_c;

#ifdef UNALIGNED_OK
#  if defined(UNALIGNED64_OK) && defined(HAVE_BUILTIN_CTZLL)
    functable.longest_match = &longest_match_unaligned_64;
#  elif defined(HAVE_BUILTIN_CTZ)
    functable.longest_match = &longest_match_unaligned_32;
#  else
    functable.longest_match = &longest_match_unaligned_16;
#  endif
#  ifdef X86_SSE42_CMP_STR
    if (x86_cpu_has_sse42)
        functable.longest_match = &longest_match_unaligned_sse4;
#  endif
#  if defined(X86_AVX2) && defined(HAVE_BUILTIN_CTZ)
    if (x86_cpu_has_avx2)
        functable.longest_match = &longest_match_unaligned_avx2;
#  endif
#endif

    return functable.longest_match(s, cur_match);
}

/* functable init */
Z_INTERNAL Z_TLS struct functable_s functable = {
    insert_string_stub,
    quick_insert_string_stub,
    adler32_stub,
    crc32_stub,
    slide_hash_stub,
    compare258_stub,
    longest_match_stub,
    chunksize_stub,
    chunkcopy_stub,
    chunkcopy_safe_stub,
    chunkunroll_stub,
    chunkmemset_stub,
    chunkmemset_safe_stub
};
