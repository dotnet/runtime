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
// const int T32_SIZE = 2048 + 8;

extern const uint8_t perm_table_64[T64_SIZE];
// extern const uint8_t perm_table_32[T32_SIZE];

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
    typedef uint32x4_t TMASK;
    typedef uint32_t TPACK;  // TODO: ??
    typedef typename std::make_unsigned<T>::type TU;

    static constexpr bool supports_compress_writes() { return false; }

    static constexpr bool supports_packing() { return false; }

    template <int Shift>
    static constexpr bool can_pack(T span) { return false; }

    static INLINE TV load_vec(TV* p) { not_supported(); return *p; }

    static INLINE void store_vec(TV* ptr, TV v) { not_supported(); }

    static void store_compress_vec(TV* ptr, TV v, TMASK mask) { not_supported(); }

    static INLINE TV partition_vector(TV v, TMASK mask) {
        not_supported();
        return v;
    }

    static INLINE TV broadcast(int32_t pivot) { not_supported(); return vdupq_n_u32(pivot); }

    static INLINE TMASK get_cmpgt_mask(TV a, TV b) { not_supported(); return vcgtq_u32(a, b); }

    static TV shift_right(TV v, int i) { not_supported(); return v; }
    static TV shift_left(TV v, int i) { not_supported(); return v; }

    static INLINE TV add(TV a, TV b) { not_supported(); return a; }
    static INLINE TV sub(TV a, TV b) { not_supported(); return a; }

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
        add += from;
        if (Shift > 0)
            add = (T) (((TU) add) << Shift);
        return add;
    }

    static INLINE T mask_popcount(TMASK mask) { not_supported(); return 0; }
};


template <>
class vxsort_machine_traits<uint64_t, NEON> {
   public:
    typedef uint64_t T;
    typedef uint64x2_t TV;
    typedef uint64_t TMASK;
    typedef uint64_t TPACK;  // TODO: ??
    typedef typename std::make_unsigned<T>::type TU;
    static constexpr TV compare_mask = {0b01, 0b10};

    static constexpr bool supports_compress_writes() { return false; }

    static constexpr bool supports_packing() { return false; }

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
        uint8x16_t indexes = reinterpret_cast<uint8x16_t>(vld1q_u64((T*)(perm_table_64 + (mask * T64_LINE_SIZE))));
        uint8x16_t partitioned = vqtbl1q_u8(reinterpret_cast<uint8x16_t>(v), indexes);
        return reinterpret_cast<TV>(partitioned);
    }

    static INLINE TV broadcast(T pivot) { return vdupq_n_u64(pivot); }

    // Compare. Use mask to get one bit per lane. Add across into a single 64bit int.
    static INLINE TMASK get_cmpgt_mask(TV a, TV b) { return vaddvq_u64(vandq_u64(vcgtq_u64(a, b), compare_mask)); }

    static TV shift_right(TV v, int i) { return vshrq_n_u64(v, i); }
    static TV shift_left(TV v, int i) { return vshlq_n_u64(v, i); }

    static INLINE TV add(TV a, TV b) { return vaddq_u64(a, b); }
    static INLINE TV sub(TV a, TV b) { return vsubq_u64(a, b); };

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

    static INLINE T mask_popcount(TMASK mask) { return __builtin_popcount(mask); }
};

}

#ifdef _DEBUG
#undef constexpr
#endif //_DEBUG

#endif  // VXSORT_VXSORT_NEON_H
