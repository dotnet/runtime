// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef VXSORT_PACKER_H
#define VXSORT_PACKER_H

#include "defs.h"
#include "alignment.h"
#include "machine_traits.h"

#include <immintrin.h>

namespace vxsort {

template<typename TFrom, typename TTo, vector_machine M, int Shift = 0, int Unroll = 1, int MinLength = 1, bool RespectPackingOrder = false>
class packer {
    static_assert(Shift <= 31, "Shift must be in the range 0..31");
    static_assert(Unroll >= 1, "Unroll can be in the range 1..4");
    static_assert(Unroll <= 4, "Unroll can be in the range 1..4");

    using MT = vxsort_machine_traits<TFrom, M>;
    typedef typename MT::TV TV;
    static const int N = sizeof(TV) / sizeof(TFrom);
    typedef alignment_hint<sizeof(TV)> AH;

    static const size_t ALIGN = AH::ALIGN;
    static const size_t ALIGN_MASK = ALIGN - 1;


    static INLINE TV pack_vectorized(const TV baseVec, TV d01, TV d02) {
        if (Shift > 0) { // This is statically compiled in/out
            d01 = MT::shift_right(d01, Shift);
            d02 = MT::shift_right(d02, Shift);
        }
        d01 = MT::sub(d01, baseVec);
        d02 = MT::sub(d02, baseVec);

        auto packed_data = RespectPackingOrder ?
                           MT::pack_ordered(d01, d02) :
                           MT::pack_unordered(d01, d02);
        return packed_data;
    }

    static NOINLINE void unpack_vectorized(const TV baseVec, TV d01, TV& u01, TV& u02) {
        MT::unpack_ordered(d01, u01, u02);

        u01 = MT::add(u01, baseVec);
        u02 = MT::add(u02, baseVec);

        if (Shift > 0) { // This is statically compiled in/out
            u01 = MT::shift_left(u01, Shift);
            u02 = MT::shift_left(u02, Shift);
        }
    }

   public:

    static void pack(TFrom *mem, size_t len, TFrom base) {
        TFrom offset = MT::template shift_n_sub<Shift>(base, (TFrom) std::numeric_limits<TTo>::Min());
        auto baseVec = MT::broadcast(offset);

        auto pre_aligned_mem = reinterpret_cast<TFrom *>(reinterpret_cast<size_t>(mem) & ~ALIGN_MASK);

        auto mem_read = mem;
        auto mem_write = (TTo *) mem;

        // Include a "special" pass to handle very short scalar
        // passes
        if (MinLength < N && len < N) {
            while (len--) {
                *(mem_write++) = (TTo) MT::template  shift_n_sub<Shift>(*(mem_read++), offset);
            }
            return;
        }

        // We have at least
        // one vector worth of data to handle
        // Let's try to align to vector size first

        if (pre_aligned_mem < mem) {
            const auto alignment_point = pre_aligned_mem + N;
            len -= (alignment_point - mem_read);
            while (mem_read < alignment_point) {
                *(mem_write++) = (TTo) MT::template shift_n_sub<Shift>(*(mem_read++), offset);
            }
        }

        assert(AH::is_aligned(mem_read));

        auto memv_read = (TV *) mem_read;
        auto memv_write = (TV *) mem_write;

        auto lenv = len / N;
        len -= (lenv * N);

        while (lenv >= 2 * Unroll) {
            assert(memv_read >= memv_write);

            TV d01, d02, d03, d04, d05, d06, d07, d08;

            do {
                d01 = MT::load_vec(memv_read + 0);
                d02 = MT::load_vec(memv_read + 1);
                if (Unroll == 1) break;
                d03 = MT::load_vec(memv_read + 2);
                d04 = MT::load_vec(memv_read + 3);
                if (Unroll == 2) break;
                d05 = MT::load_vec(memv_read + 4);
                d06 = MT::load_vec(memv_read + 5);
                if (Unroll == 3) break;
                d07 = MT::load_vec(memv_read + 6);
                d08 = MT::load_vec(memv_read + 7);
                break;
            } while (true);

            do {
                MT::store_vec(memv_write + 0, pack_vectorized(baseVec, d01, d02));
                if (Unroll == 1) break;
                MT::store_vec(memv_write + 1, pack_vectorized(baseVec, d03, d04));
                if (Unroll == 2) break;
                MT::store_vec(memv_write + 2, pack_vectorized(baseVec, d05, d06));
                if (Unroll == 3) break;
                MT::store_vec(memv_write + 3, pack_vectorized(baseVec, d07, d08));
                break;
            } while(true);

            memv_read += 2*Unroll;
            memv_write += Unroll;
            lenv -= 2*Unroll;
        }

        if (Unroll > 1) {
            while (lenv >= 2) {
                assert(memv_read >= memv_write);
                TV d01, d02;

                d01 = MT::load_vec(memv_read + 0);
                d02 = MT::load_vec(memv_read + 1);

                MT::store_vec(memv_write + 0, pack_vectorized(baseVec, d01, d02));
                memv_read += 2;
                memv_write++;
                lenv -= 2;
            }
        }

        len += lenv * N;

        mem_read = (TFrom *) memv_read;
        mem_write = (TTo *) memv_write;

        while (len-- > 0) {
            *(mem_write++) = (TTo) MT::template shift_n_sub<Shift>(*(mem_read++), offset);
        }
    }


    static void unpack(TTo *mem, size_t len, TFrom base) {
        TFrom offset = MT::template shift_n_sub<Shift>(base, (TFrom) std::numeric_limits<TTo>::Min());
        auto baseVec = MT::broadcast(offset);

        auto mem_read = mem + len;
        auto mem_write = ((TFrom *) mem) + len;


        // Include a "special" pass to handle very short scalar
        // passers
        if (MinLength < 2 * N && len < 2 * N) {
            while (len--) {
                *(--mem_write) = MT::template unshift_and_add<Shift>(*(--mem_read), offset);
            }
            return;
        }

        auto pre_aligned_mem = reinterpret_cast<TTo *>(reinterpret_cast<size_t>(mem_read) & ~ALIGN_MASK);

        if (pre_aligned_mem < mem_read) {
            len -= (mem_read - pre_aligned_mem);
            while (mem_read > pre_aligned_mem) {
                *(--mem_write) = MT::template unshift_and_add<Shift>(*(--mem_read), offset);
            }
        }

        assert(AH::is_aligned(mem_read));

        auto lenv = len / (N * 2);
        auto memv_read = ((TV *) mem_read) - 1;
        auto memv_write = ((TV *) mem_write) - 2;
        len -= lenv * N * 2;

        while (lenv >= Unroll) {
            assert(memv_read <= memv_write);

            TV d01, d02, d03, d04;
            TV u01, u02, u03, u04, u05, u06, u07, u08;

            do {
                d01 = MT::load_vec(memv_read + 0);
                if (Unroll == 1) break;
                d02 = MT::load_vec(memv_read - 1);
                if (Unroll == 2) break;
                d03 = MT::load_vec(memv_read - 2);
                if (Unroll == 3) break;
                d04 = MT::load_vec(memv_read - 3);
                break;
            } while(true);

            do {
                unpack_vectorized(baseVec, d01, u01, u02);
                MT::store_vec(memv_write + 0, u01);
                MT::store_vec(memv_write + 1, u02);
                if (Unroll == 1) break;
                unpack_vectorized(baseVec, d02, u03, u04);
                MT::store_vec(memv_write - 2, u03);
                MT::store_vec(memv_write - 1, u04);
                if (Unroll == 2) break;
                unpack_vectorized(baseVec, d03, u05, u06);
                MT::store_vec(memv_write - 4, u05);
                MT::store_vec(memv_write - 3, u06);
                if (Unroll == 3) break;
                unpack_vectorized(baseVec, d04, u07, u08);
                MT::store_vec(memv_write - 6, u07);
                MT::store_vec(memv_write - 5, u08);
                break;
            } while(true);

            memv_read -= Unroll;
            memv_write -= 2 * Unroll;
            lenv -= Unroll;
        }

        if (Unroll > 1) {
            while (lenv >= 1) {
                assert(memv_read <= memv_write);

                TV d01;
                TV u01, u02;

                d01 = MT::load_vec(memv_read + 0);

                unpack_vectorized(baseVec, d01, u01, u02);
                MT::store_vec(memv_write + 0, u01);
                MT::store_vec(memv_write + 1, u02);

                memv_read--;
                memv_write -= 2;
                lenv--;
            }
        }

        mem_read = (TTo *) (memv_read + 1);
        mem_write = (TFrom *) (memv_write + 2);

        while (len-- > 0) {
            *(--mem_write) = MT::template unshift_and_add<Shift>(*(--mem_read), offset);
        }
    }

};
}

#endif  // VXSORT_PACKER_H
