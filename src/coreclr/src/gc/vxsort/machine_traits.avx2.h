//
// Created by dans on 6/1/20.
//

#ifndef VXSORT_MACHINE_TRAITS_AVX2_H
#define VXSORT_MACHINE_TRAITS_AVX2_H

#include "vxsort_targets_enable_avx2.h"

#include <immintrin.h>
//#include <stdexcept>
#include <assert.h>

#include "defs.h"
#include "machine_traits.h"

#define i2d _mm256_castsi256_pd
#define d2i _mm256_castpd_si256
#define i2s _mm256_castsi256_ps
#define s2i _mm256_castps_si256
#define s2d _mm256_castps_pd
#define d2s _mm256_castpd_ps

namespace vxsort {
extern const int8_t perm_table_64[128];
extern const int8_t perm_table_32[2048];

static void not_supported()
{
    assert(!"operation is unsupported");
}

#ifdef _DEBUG
// in _DEBUG, we #define return to be something more complicated,
// containing a statement, so #define away constexpr for _DEBUG
#define constexpr
#endif //_DEBUG

template <>
class vxsort_machine_traits<int32_t, AVX2> {
 public:
  typedef __m256i TV;
  typedef uint32_t TMASK;

  static constexpr bool supports_compress_writes() { return false; }

  static INLINE TV load_vec(TV* p) {
    return _mm256_lddqu_si256(p);
  }

  static INLINE void store_vec(TV* ptr, TV v) {
    _mm256_storeu_si256(ptr, v);
  }

  static void store_compress_vec(TV* ptr, TV v, TMASK mask) { not_supported(); }

  static INLINE TV partition_vector(TV v, int mask) {
    assert(mask >= 0);
    assert(mask <= 255);
    return s2i(_mm256_permutevar8x32_ps(i2s(v), _mm256_cvtepu8_epi32(_mm_loadu_si128((__m128i*)(perm_table_32 + mask * 8)))));
  }

  static INLINE TV get_vec_pivot(int32_t pivot) {
    return _mm256_set1_epi32(pivot);
  }
  static INLINE TMASK get_cmpgt_mask(TV a, TV b) {
    return _mm256_movemask_ps(i2s(_mm256_cmpgt_epi32(a, b)));
  }
};

template <>
class vxsort_machine_traits<uint32_t, AVX2> {
 public:
  typedef __m256i TV;
  typedef uint32_t TMASK;

  static constexpr bool supports_compress_writes() { return false; }

  static INLINE TV load_vec(TV* p) {
    return _mm256_lddqu_si256(p);
  }

  static INLINE void store_vec(TV* ptr, TV v) {
    _mm256_storeu_si256(ptr, v);
  }

  static void store_compress_vec(TV* ptr, TV v, TMASK mask) { not_supported(); }

  static INLINE TV partition_vector(TV v, int mask) {
    assert(mask >= 0);
    assert(mask <= 255);
    return s2i(_mm256_permutevar8x32_ps(i2s(v), _mm256_cvtepu8_epi32(_mm_loadu_si128((__m128i*)(perm_table_32 + mask * 8)))));
  }

  static INLINE TV get_vec_pivot(uint32_t pivot) {
    return _mm256_set1_epi32(pivot);
  }
  static INLINE TMASK get_cmpgt_mask(TV a, TV b) {
    __m256i top_bit = _mm256_set1_epi32(1U << 31);
    return _mm256_movemask_ps(i2s(_mm256_cmpgt_epi32(_mm256_xor_si256(top_bit, a), _mm256_xor_si256(top_bit, b))));
  }
};

template <>
class vxsort_machine_traits<float, AVX2> {
 public:
  typedef __m256 TV;
  typedef uint32_t TMASK;

  static constexpr bool supports_compress_writes() { return false; }

  static INLINE TV load_vec(TV* p) {
    return _mm256_loadu_ps((float *)p);
  }

  static INLINE void store_vec(TV* ptr, TV v) {
    _mm256_storeu_ps((float *) ptr, v);
  }

  static void store_compress_vec(TV* ptr, TV v, TMASK mask) { not_supported(); }

  static INLINE TV partition_vector(TV v, int mask) {
    assert(mask >= 0);
    assert(mask <= 255);
    return _mm256_permutevar8x32_ps(v, _mm256_cvtepu8_epi32(_mm_loadu_si128((__m128i*)(perm_table_32 + mask * 8))));
  }

  static INLINE TV get_vec_pivot(float pivot) {
    return _mm256_set1_ps(pivot);
  }

  static INLINE TMASK get_cmpgt_mask(TV a, TV b) {
    ///    0x0E: Greater-than (ordered, signaling) \n
    ///    0x1E: Greater-than (ordered, non-signaling)
    return _mm256_movemask_ps(_mm256_cmp_ps(a, b, _CMP_GT_OS));
  }
};

template <>
class vxsort_machine_traits<int64_t, AVX2> {
 public:
  typedef __m256i TV;
  typedef uint32_t TMASK;

  static bool supports_compress_writes() { return false; }

  static INLINE TV load_vec(TV* p) {
    return _mm256_lddqu_si256(p);
  }

  static INLINE void store_vec(TV* ptr, TV v) {
    _mm256_storeu_si256(ptr, v);
  }

  static void store_compress_vec(TV* ptr, TV v, TMASK mask) { not_supported(); }

  static INLINE TV partition_vector(TV v, int mask) {
    assert(mask >= 0);
    assert(mask <= 15);
    return s2i(_mm256_permutevar8x32_ps(i2s(v), _mm256_cvtepu8_epi32(_mm_loadu_si128((__m128i*)(perm_table_64 + mask * 8)))));
  }

  static INLINE TV get_vec_pivot(int64_t pivot) {
    return _mm256_set1_epi64x(pivot);
  }
  static INLINE TMASK get_cmpgt_mask(TV a, TV b) {
    return _mm256_movemask_pd(i2d(_mm256_cmpgt_epi64(a, b)));
  }
};

template <>
class vxsort_machine_traits<uint64_t, AVX2> {
 public:
  typedef __m256i TV;
  typedef uint32_t TMASK;

  static constexpr bool supports_compress_writes() { return false; }

  static INLINE TV load_vec(TV* p) {
    return _mm256_lddqu_si256(p);
  }

  static INLINE void store_vec(TV* ptr, TV v) {
    _mm256_storeu_si256(ptr, v);
  }

  static void store_compress_vec(TV* ptr, TV v, TMASK mask) { not_supported(); }

  static INLINE TV partition_vector(TV v, int mask) {
    assert(mask >= 0);
    assert(mask <= 15);
    return s2i(_mm256_permutevar8x32_ps(i2s(v), _mm256_cvtepu8_epi32(_mm_loadu_si128((__m128i*)(perm_table_64 + mask * 8)))));
  }
  static INLINE TV get_vec_pivot(int64_t pivot) {
    return _mm256_set1_epi64x(pivot);
  }
  static INLINE TMASK get_cmpgt_mask(TV a, TV b) {
    __m256i top_bit = _mm256_set1_epi64x(1LLU << 63);
    return _mm256_movemask_pd(i2d(_mm256_cmpgt_epi64(_mm256_xor_si256(top_bit, a), _mm256_xor_si256(top_bit, b))));
  }
};

template <>
class vxsort_machine_traits<double, AVX2> {
 public:
  typedef __m256d TV;
  typedef uint32_t TMASK;

  static constexpr bool supports_compress_writes() { return false; }

  static INLINE TV load_vec(TV* p) {
    return _mm256_loadu_pd((double *) p);
  }

  static INLINE void store_vec(TV* ptr, TV v) {
    _mm256_storeu_pd((double *) ptr, v);
  }

  static void store_compress_vec(TV* ptr, TV v, TMASK mask) { not_supported(); }

  static INLINE TV partition_vector(TV v, int mask) {
    assert(mask >= 0);
    assert(mask <= 15);
    return s2d(_mm256_permutevar8x32_ps(d2s(v), _mm256_cvtepu8_epi32(_mm_loadu_si128((__m128i*)(perm_table_64 + mask * 8)))));
  }

  static INLINE TV get_vec_pivot(double pivot) {
    return _mm256_set1_pd(pivot);
  }
  static INLINE TMASK get_cmpgt_mask(TV a, TV b) {
    ///    0x0E: Greater-than (ordered, signaling) \n
    ///    0x1E: Greater-than (ordered, non-signaling)
    return _mm256_movemask_pd(_mm256_cmp_pd(a, b, _CMP_GT_OS));
  }
};

}

#undef i2d
#undef d2i
#undef i2s
#undef s2i
#undef s2d
#undef d2s

#ifdef _DEBUG
#undef constexpr
#endif //_DEBUG

#include "vxsort_targets_disable.h"


#endif  // VXSORT_VXSORT_AVX2_H
