// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef VXSORT_MACHINE_TRAITS_NEON_H
#define VXSORT_MACHINE_TRAITS_NEON_H

#include <assert.h>
#include <inttypes.h>
#include <type_traits>
#include "defs.h"
#include "machine_traits.h"
#include <arm_neon.h>
#include <bitset>

namespace vxsort {

const int T64_LINE_SIZE = 16;
const int T64_SIZE = (T64_LINE_SIZE * 4);
const int T32_LINE_SIZE = 16;
const int T32_SIZE = (T32_LINE_SIZE * 16);

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
class vxsort_machine_traits<uint32_t, NEON> {
   public:
    typedef uint32_t T;
    typedef uint32x4_t TV;
    typedef uint32_t TMASK;
    typedef uint32_t TPACK;
    typedef typename std::make_unsigned<T>::type TU;

    static const int32_t MAX_BITONIC_SORT_VECTORS = 16;
    static const int32_t SMALL_SORT_THRESHOLD_ELEMENTS = 32;
    static const int32_t MaxInnerUnroll = 3;
    static const vector_machine SMALL_SORT_TYPE = vector_machine::NEON;

    // Requires hardware support for a masked store.
    static constexpr bool supports_compress_writes() { return false; }

    static constexpr bool supports_packing() { return false; }

    template <int Shift>
    static constexpr bool can_pack(T span) {
        return false;
    }

    static INLINE TV load_vec(TV* p) { return vld1q_u32((T*)p); }

    static INLINE void store_vec(TV* ptr, TV v) { vst1q_u32((T*)ptr, v); }

    static void store_compress_vec(TV* ptr, TV v, TMASK mask) { not_supported(); }

    static INLINE TV partition_vector(TV v, TMASK mask) {
        assert(mask >= 0);
        assert(mask <= 16);
        uint8x16_t indexes = vld1q_u8((uint8_t*)(perm_table_32 + (mask * T32_LINE_SIZE)));
        uint8x16_t partitioned = vqtbl1q_u8((uint8x16_t)v, indexes);
        return (TV)partitioned;
    }

    static INLINE TV broadcast(T pivot) { return vdupq_n_u32(pivot); }

    // Compare. Use mask to get one bit per lane. Add across into a single 64bit int.
    static INLINE TMASK get_cmpgt_mask(TV a, TV b) {
        const uint32_t compare_mask_array[4] = {0b01, 0b10, 0b100, 0b1000};
        TV compare_mask = vld1q_u32 (&compare_mask_array[0]);

        return vaddvq_u32(vandq_u32(vcgtq_u32(a, b), compare_mask));
    }

    static TV shift_right(TV v, int i) { return vshlq_u32(v, vdupq_n_s32(-i)); }
    static TV shift_left(TV v, int i) { return (TV)vshlq_s32((int32x4_t)v, vdupq_n_s32(i)); }

    static INLINE TV add(TV a, TV b) { return vaddq_u32(a, b); }
    static INLINE TV sub(TV a, TV b) { return vsubq_u32(a, b); };

    static INLINE TV pack_ordered(TV a, TV b) { not_supported(); return a; }
    static INLINE TV pack_unordered(TV a, TV b) { not_supported(); return a; }
    static INLINE void unpack_ordered(TV p, TV& u1, TV& u2) { not_supported(); }

    template <int Shift>
    static T shift_n_sub(T v, T sub) {
        if (Shift > 0)
            v >>= Shift;
        v -= sub;
        return v;
    }

    template <int Shift>
    static T unshift_and_add(TPACK from, T add) {
        not_supported();
        add += from;
        if (Shift > 0)
            add = (T) (((TU) add) << Shift);
        return add;
    }

    static INLINE T mask_popcount(TMASK mask) {
        uint32x2_t maskv = vcreate_u32((uint64_t)mask);
        return vaddv_u8(vcnt_u8(vreinterpret_u8_u32(maskv)));
    }
};

template <>
class vxsort_machine_traits<uint64_t, NEON> {
   public:
    typedef uint64_t T;
    typedef uint64x2_t TV;
    typedef uint64_t TMASK;
    typedef uint32_t TPACK;
    typedef typename std::make_unsigned<T>::type TU;

    static const int N = sizeof(TV) / sizeof(T);
    static const int32_t MAX_BITONIC_SORT_VECTORS = 16;
    static const int32_t SMALL_SORT_THRESHOLD_ELEMENTS = MAX_BITONIC_SORT_VECTORS * N;
    static const int32_t MaxInnerUnroll = (MAX_BITONIC_SORT_VECTORS - 3) / 2;
    static const vector_machine SMALL_SORT_TYPE = vector_machine::scalar;

    // Requires hardware support for a masked store.
    static constexpr bool supports_compress_writes() { return false; }

    static constexpr bool supports_packing() { return true; }

    template <int Shift>
    static constexpr bool can_pack(T span) {
        return ((TU) span) < ((((TU) std::numeric_limits<uint32_t>::max() + 1)) << Shift);
    }

    static INLINE TV load_vec(TV* p) { return vld1q_u64((T*)p); }

    static INLINE void store_vec(TV* ptr, TV v) { vst1q_u64((T*)ptr, v); }

    static void store_compress_vec(TV* ptr, TV v, TMASK mask) { not_supported(); }

    static INLINE TV partition_vector(TV v, TMASK mask) {
        assert(mask >= 0);
        assert(mask <= 3);
        uint8x16_t indexes = vld1q_u8((uint8_t*)(perm_table_64 + (mask * T64_LINE_SIZE)));
        uint8x16_t partitioned = vqtbl1q_u8((uint8x16_t)v, indexes);
        return (TV)partitioned;
    }

    static INLINE TV broadcast(T pivot) { return vdupq_n_u64(pivot); }

    // Compare. Use mask to get one bit per lane. Add across into a single 64bit int.
    static INLINE TMASK get_cmpgt_mask(TV a, TV b) {
        static TV compare_mask = {0b01, 0b10};
        return vaddvq_u64(vandq_u64(vcgtq_u64(a, b), compare_mask));
    }

    static TV shift_right(TV v, int i) { return vshlq_u64(v, vdupq_n_s64(-i)); }
    static TV shift_left(TV v, int i) { return (TV)vshlq_s64((int64x2_t)v, vdupq_n_s64(i)); }

    static INLINE TV add(TV a, TV b) { return vaddq_u64(a, b); }
    static INLINE TV sub(TV a, TV b) { return vsubq_u64(a, b); };

    static INLINE TV pack_ordered(TV a, TV b) { printf("pack_ordered\n"); not_supported(); return a; }

    // Produces 32bit lanes: [a0, b1, a1, b0]
    static INLINE TV pack_unordered(TV a, TV b) {
        //[A,B,C,D] into [B,C,D,A]
        uint32x4_t b_rot = vextq_u32((uint32x4_t)b, (uint32x4_t)b, 1);

        const uint32_t odd_mask_array[4] = {0x00000000u, 0xFFFFFFFFu, 0x00000000u, 0xFFFFFFFFu};
        uint32x4_t odd_mask = vld1q_u32 (&odd_mask_array[0]);

        return vreinterpretq_u64_u32(vbslq_u32(odd_mask, b_rot, (uint32x4_t)a));
    }

    // static NOINLINE TV pack_unordered(TV a, TV b) { return vreinterpretq_u64_u32(vcombine_u32(vmovn_u64(a), vmovn_u64(b))); }

    static NOINLINE void unpack_ordered(TV p, TV& u1, TV& u2) {
        auto p01 = vget_low_u32(vreinterpretq_u32_u64(p));
        auto p02 = vget_high_u32(vreinterpretq_u32_u64(p));

        u1 = vmovl_u32(p01);
        u2 = vmovl_u32(p02);
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

    static INLINE T mask_popcount(TMASK mask) {
        uint64x1_t maskv = { mask };
        return vaddv_u8(vcnt_u8(vreinterpret_u8_u64(maskv)));
    }
};

}

#ifdef _DEBUG
#undef constexpr
#endif //_DEBUG

#endif  // VXSORT_VXSORT_NEON_H
