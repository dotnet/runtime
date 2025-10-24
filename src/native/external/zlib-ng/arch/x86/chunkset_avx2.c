/* chunkset_avx2.c -- AVX2 inline functions to copy small data chunks.
 * For conditions of distribution and use, see copyright notice in zlib.h
 */
#include "zbuild.h"
#include "zmemory.h"

#ifdef X86_AVX2
#include "avx2_tables.h"
#include <immintrin.h>
#include "x86_intrins.h"

typedef __m256i chunk_t;
typedef __m128i halfchunk_t;

#define HAVE_CHUNKMEMSET_2
#define HAVE_CHUNKMEMSET_4
#define HAVE_CHUNKMEMSET_8
#define HAVE_CHUNKMEMSET_16
#define HAVE_CHUNK_MAG
#define HAVE_HALF_CHUNK

static inline void chunkmemset_2(uint8_t *from, chunk_t *chunk) {
    *chunk = _mm256_set1_epi16(zng_memread_2(from));
}

static inline void chunkmemset_4(uint8_t *from, chunk_t *chunk) {
    *chunk = _mm256_set1_epi32(zng_memread_4(from));
}

static inline void chunkmemset_8(uint8_t *from, chunk_t *chunk) {
    *chunk = _mm256_set1_epi64x(zng_memread_8(from));
}

static inline void chunkmemset_16(uint8_t *from, chunk_t *chunk) {
    /* See explanation in chunkset_avx512.c */
#if defined(_MSC_VER) && _MSC_VER <= 1900
    halfchunk_t half = _mm_loadu_si128((__m128i*)from);
    *chunk = _mm256_inserti128_si256(_mm256_castsi128_si256(half), half, 1);
#else
    *chunk = _mm256_broadcastsi128_si256(_mm_loadu_si128((__m128i*)from));
#endif
}

static inline void loadchunk(uint8_t const *s, chunk_t *chunk) {
    *chunk = _mm256_loadu_si256((__m256i *)s);
}

static inline void storechunk(uint8_t *out, chunk_t *chunk) {
    _mm256_storeu_si256((__m256i *)out, *chunk);
}

static inline chunk_t GET_CHUNK_MAG(uint8_t *buf, uint32_t *chunk_rem, uint32_t dist) {
    lut_rem_pair lut_rem = perm_idx_lut[dist - 3];
    __m256i ret_vec;
    /* While technically we only need to read 4 or 8 bytes into this vector register for a lot of cases, GCC is
     * compiling this to a shared load for all branches, preferring the simpler code.  Given that the buf value isn't in
     * GPRs to begin with the 256 bit load is _probably_ just as inexpensive */
    *chunk_rem = lut_rem.remval;

    /* See note in chunkset_ssse3.c for why this is ok */
    __msan_unpoison(buf + dist, 32 - dist);

    if (dist < 16) {
        /* This simpler case still requires us to shuffle in 128 bit lanes, so we must apply a static offset after
         * broadcasting the first vector register to both halves. This is _marginally_ faster than doing two separate
         * shuffles and combining the halves later */
        const __m256i permute_xform =
            _mm256_setr_epi8(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                             16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16);
        __m256i perm_vec = _mm256_load_si256((__m256i*)(permute_table+lut_rem.idx));
        __m128i ret_vec0 = _mm_loadu_si128((__m128i*)buf);
        perm_vec = _mm256_add_epi8(perm_vec, permute_xform);
        ret_vec = _mm256_inserti128_si256(_mm256_castsi128_si256(ret_vec0), ret_vec0, 1);
        ret_vec = _mm256_shuffle_epi8(ret_vec, perm_vec);
    }  else {
        __m128i ret_vec0 = _mm_loadu_si128((__m128i*)buf);
        __m128i ret_vec1 = _mm_loadu_si128((__m128i*)(buf + 16));
        /* Take advantage of the fact that only the latter half of the 256 bit vector will actually differ */
        __m128i perm_vec1 = _mm_load_si128((__m128i*)(permute_table + lut_rem.idx));
        __m128i xlane_permutes = _mm_cmpgt_epi8(_mm_set1_epi8(16), perm_vec1);
        __m128i xlane_res  = _mm_shuffle_epi8(ret_vec0, perm_vec1);
        /* Since we can't wrap twice, we can simply keep the later half exactly how it is instead of having to _also_
         * shuffle those values */
        __m128i latter_half = _mm_blendv_epi8(ret_vec1, xlane_res, xlane_permutes);
        ret_vec = _mm256_inserti128_si256(_mm256_castsi128_si256(ret_vec0), latter_half, 1);
    }

    return ret_vec;
}

static inline void loadhalfchunk(uint8_t const *s, halfchunk_t *chunk) {
    *chunk = _mm_loadu_si128((__m128i *)s);
}

static inline void storehalfchunk(uint8_t *out, halfchunk_t *chunk) {
    _mm_storeu_si128((__m128i *)out, *chunk);
}

static inline chunk_t halfchunk2whole(halfchunk_t *chunk) {
    /* We zero extend mostly to appease some memory sanitizers. These bytes are ultimately
     * unlikely to be actually written or read from */
    return _mm256_zextsi128_si256(*chunk);
}

static inline halfchunk_t GET_HALFCHUNK_MAG(uint8_t *buf, uint32_t *chunk_rem, uint32_t dist) {
    lut_rem_pair lut_rem = perm_idx_lut[dist - 3];
    __m128i perm_vec, ret_vec;
    __msan_unpoison(buf + dist, 16 - dist);
    ret_vec = _mm_loadu_si128((__m128i*)buf);
    *chunk_rem = half_rem_vals[dist - 3];

    perm_vec = _mm_load_si128((__m128i*)(permute_table + lut_rem.idx));
    ret_vec = _mm_shuffle_epi8(ret_vec, perm_vec);

    return ret_vec;
}

#define CHUNKSIZE        chunksize_avx2
#define CHUNKCOPY        chunkcopy_avx2
#define CHUNKUNROLL      chunkunroll_avx2
#define CHUNKMEMSET      chunkmemset_avx2
#define CHUNKMEMSET_SAFE chunkmemset_safe_avx2

#include "chunkset_tpl.h"

#define INFLATE_FAST     inflate_fast_avx2

#include "inffast_tpl.h"

#endif
