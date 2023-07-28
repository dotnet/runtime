// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Created by dans on 6/1/20.
//

#ifndef VXSORT_MACHINE_TRAITS_AVX2_H
#define VXSORT_MACHINE_TRAITS_AVX2_H

#include "vxsort_targets_enable_avx2.h"

#include <immintrin.h>
#include <assert.h>
#include <inttypes.h>
#include "defs.h"
#include "machine_traits.h"

#define i2d _mm256_castsi256_pd
#define d2i _mm256_castpd_si256
#define i2s _mm256_castsi256_ps
#define s2i _mm256_castps_si256
#define s2d _mm256_castps_pd
#define d2s _mm256_castpd_ps

namespace vxsort {

// We might read the last 8 bytes into a 128-bit vector for the purpose of permutation
// Most compilers actually fuse the pair of instructions: _mm256_cvtepi8_epiNN + _mm_loadu_si128
// into a single instruction that will not read more that 4/8 bytes..
// vpmovsxb[dq] ymm0, dword [...], eliminating the 128-bit load completely and effectively
// reading exactly 4/8 (depending if the instruction is vpmovsxbd or vpmovsxbq)
// without generating an out of bounds read at all.
// But, life is harsh, and we can't trust the compiler to do the right thing if it is not
// contractual
const int T64_SIZE = 128 + 8;
const int T32_SIZE = 2048 + 8;

extern const uint8_t perm_table_64[T64_SIZE];
extern const uint8_t perm_table_32[T32_SIZE];

static void not_supported()
{
    assert(!"operation is unsupported");
}

#ifdef _DEBUG
// in _DEBUG, we #define return to be something more complicated,
// containing a statement, so #define away constexpr for _DEBUG
#define constexpr
#endif  //_DEBUG

template <>
class vxsort_machine_traits<int32_t, AVX2> {
   public:
    typedef int32_t T;
    typedef __m256i TV;
    typedef uint32_t TMASK;
    typedef int32_t TPACK;
    typedef typename std::make_unsigned<T>::type TU;

    static constexpr bool supports_compress_writes() { return false; }

    static constexpr bool supports_packing() { return false; }

    template <int Shift>
    static constexpr bool can_pack(T span) { return false; }

    static INLINE TV load_vec(TV* p) { return _mm256_lddqu_si256(p); }

    static INLINE void store_vec(TV* ptr, TV v) { _mm256_storeu_si256(ptr, v); }

    static void store_compress_vec(TV* ptr, TV v, TMASK mask) { not_supported(); }

    static INLINE TV partition_vector(TV v, int mask) {
        assert(mask >= 0);
        assert(mask <= 255);
        return s2i(_mm256_permutevar8x32_ps(i2s(v), _mm256_cvtepi8_epi32(_mm_loadu_si128((__m128i*)(perm_table_32 + mask * 8)))));
    }

    static INLINE TV broadcast(int32_t pivot) { return _mm256_set1_epi32(pivot); }
    static INLINE TMASK get_cmpgt_mask(TV a, TV b) { return _mm256_movemask_ps(i2s(_mm256_cmpgt_epi32(a, b))); }

    static TV shift_right(TV v, int i) { return _mm256_srli_epi32(v, i); }
    static TV shift_left(TV v, int i) { return _mm256_slli_epi32(v, i); }

    static INLINE TV add(TV a, TV b) { return _mm256_add_epi32(a, b); }
    static INLINE TV sub(TV a, TV b) { return _mm256_sub_epi32(a, b); };

    static INLINE TV pack_ordered(TV a, TV b) { return a; }
    static INLINE TV pack_unordered(TV a, TV b) { return a; }
    static INLINE void unpack_ordered(TV p, TV& u1, TV& u2) { }

    template <int Shift>
    static T shift_n_sub(T v, T sub) {
        if (Shift > 0)
            v >>= Shift;
        v -= sub;
        return v;
    }

    template <int Shift>
    static T unshift_and_add(TPACK from, T add) {
        add += from;
        if (Shift > 0)
            add = (T) (((TU) add) << Shift);
        return add;
    }
};

template <>
class vxsort_machine_traits<int64_t, AVX2> {
   public:
    typedef int64_t T;
    typedef __m256i TV;
    typedef uint32_t TMASK;
    typedef int32_t TPACK;
    typedef typename std::make_unsigned<T>::type TU;

    static constexpr bool supports_compress_writes() { return false; }

    static constexpr bool supports_packing() { return true; }

    template <int Shift>
    static constexpr bool can_pack(T span) {
        const auto PACK_LIMIT = (((TU) std::numeric_limits<uint32_t>::Max() + 1)) << Shift;
        return ((TU) span) < PACK_LIMIT;
    }

    static INLINE TV load_vec(TV* p) { return _mm256_lddqu_si256(p); }

    static INLINE void store_vec(TV* ptr, TV v) { _mm256_storeu_si256(ptr, v); }

    static void store_compress_vec(TV* ptr, TV v, TMASK mask) { not_supported(); }

    static INLINE TV partition_vector(TV v, int mask) {
        assert(mask >= 0);
        assert(mask <= 15);
        return s2i(_mm256_permutevar8x32_ps(i2s(v), _mm256_cvtepu8_epi32(_mm_loadu_si128((__m128i*)(perm_table_64 + mask * 8)))));
    }

    static INLINE TV broadcast(int64_t pivot) { return _mm256_set1_epi64x(pivot); }
    static INLINE TMASK get_cmpgt_mask(TV a, TV b) { return _mm256_movemask_pd(i2d(_mm256_cmpgt_epi64(a, b))); }

    static TV shift_right(TV v, int i) { return _mm256_srli_epi64(v, i); }
    static TV shift_left(TV v, int i) { return _mm256_slli_epi64(v, i); }

    static INLINE TV add(TV a, TV b) { return _mm256_add_epi64(a, b); }
    static INLINE TV sub(TV a, TV b) { return _mm256_sub_epi64(a, b); };

    static INLINE TV pack_ordered(TV a, TV b) {
        a = _mm256_permute4x64_epi64(_mm256_shuffle_epi32(a, _MM_PERM_DBCA), _MM_PERM_DBCA);
        b = _mm256_permute4x64_epi64(_mm256_shuffle_epi32(b, _MM_PERM_DBCA), _MM_PERM_CADB);
        return _mm256_blend_epi32(a, b, 0b11110000);
    }

    static INLINE TV pack_unordered(TV a, TV b) {
        b = _mm256_shuffle_epi32(b, _MM_PERM_CDAB);
        return _mm256_blend_epi32(a, b, 0b10101010);
    }

    static INLINE void unpack_ordered(TV p, TV& u1, TV& u2) {
        auto p01 = _mm256_extracti128_si256(p, 0);
        auto p02 = _mm256_extracti128_si256(p, 1);

        u1 = _mm256_cvtepi32_epi64(p01);
        u2 = _mm256_cvtepi32_epi64(p02);
    }

    template <int Shift>
    static T shift_n_sub(T v, T sub) {
        if (Shift > 0)
            v >>= Shift;
        v -= sub;
        return v;
    }

    template <int Shift>
    static T unshift_and_add(TPACK from, T add) {
        add += from;
        if (Shift > 0)
            add = (T) (((TU) add) << Shift);
        return add;
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
