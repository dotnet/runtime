// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef VXSORT_PACKER_H
#define VXSORT_PACKER_H

#include "vxsort_targets_enable_avx2.h"

//#include <cstdint>
//#include <limits>
//#include <type_traits>
#//include <cassert>
#include "alignment.h"
#include "machine_traits.h"
#include "machine_traits.avx2.h"
#include "machine_traits.avx512.h"

#include <immintrin.h>
//#include <cstdio>

namespace vxsort {

template <typename TFrom, typename TTo, vector_machine M, int Shift = 0, int MinLength=1, bool RespectPackingOrder=false>
class packer {
  static_assert(Shift <= 31, "Shift must be in the range 0..31");
  using MT = vxsort_machine_traits<TFrom, M>;
  typedef typename MT::TV TV;
  typedef typename std::make_unsigned<TFrom>::type TU;
  static const int N = sizeof(TV) / sizeof(TFrom);
  typedef alignment_hint<sizeof(TV)> AH;

  static const size_t ALIGN = AH::ALIGN;
  static const size_t ALIGN_MASK = ALIGN - 1;

  static INLINE void pack_scalar(const TFrom offset, TFrom*& mem_read, TTo*& mem_write) {
    auto d = *(mem_read++);
    if (Shift > 0)
      d >>= Shift;
    d -= offset;
    *(mem_write++) = (TTo) d;
  }

  static INLINE void unpack_scalar(const TFrom offset, TTo*& mem_read, TFrom*& mem_write) {
    TFrom d = *(--mem_read);

    d += offset;

    if (Shift > 0)
      d = (TFrom) (((TU) d) << Shift);

    *(--mem_write) = d;
  }

 public:

  static void pack(TFrom *mem, size_t len, TFrom base) {
    TFrom offset = (base >> Shift) - std::numeric_limits<TTo>::Min();
    auto baseVec = MT::broadcast(offset);

    auto pre_aligned_mem = reinterpret_cast<TFrom*>(reinterpret_cast<size_t>(mem) & ~ALIGN_MASK);

    auto mem_read = mem;
    auto mem_write = (TTo *) mem;

    // Include a "special" pass to handle very short scalar
    // passes
    if (MinLength < N && len < N) {
      while (len--) {
        pack_scalar(offset, mem_read, mem_write);
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
        pack_scalar(offset, mem_read, mem_write);
      }
    }

    assert(AH::is_aligned(mem_read));

    auto memv_read = (TV *) mem_read;
    auto memv_write = (TV *) mem_write;

    auto lenv = len / N;
    len -= (lenv * N);

    while (lenv >= 2) {
      assert(memv_read >= memv_write);

      auto d01 = MT::load_vec(memv_read);
      auto d02 = MT::load_vec(memv_read + 1);
      if (Shift > 0) { // This is statically compiled in/out
        d01 = MT::shift_right(d01, Shift);
        d02 = MT::shift_right(d02, Shift);
      }
      d01 = MT::sub(d01, baseVec);
      d02 = MT::sub(d02, baseVec);

      auto packed_data = RespectPackingOrder ?
          MT::pack_ordered(d01, d02) :
          MT::pack_unordered(d01, d02);

      MT::store_vec(memv_write, packed_data);

      memv_read += 2;
      memv_write++;
      lenv -= 2;
    }

    len += lenv * N;

    mem_read = (TFrom *) memv_read;
    mem_write = (TTo *) memv_write;

    while (len-- > 0) {
      pack_scalar(offset, mem_read, mem_write);
    }
  }

  static void unpack(TTo *mem, size_t len, TFrom base) {
    TFrom offset = (base >> Shift) - std::numeric_limits<TTo>::Min();
    auto baseVec = MT::broadcast(offset);

    auto mem_read = mem + len;
    auto mem_write = ((TFrom *) mem) + len;


    // Include a "special" pass to handle very short scalar
    // passers
    if (MinLength < 2*N && len < 2*N) {
      while (len--) {
        unpack_scalar(offset, mem_read, mem_write);
      }
      return;
    }

    auto pre_aligned_mem = reinterpret_cast<TTo*>(reinterpret_cast<size_t>(mem_read) & ~ALIGN_MASK);

    if (pre_aligned_mem < mem_read) {
      len -= (mem_read - pre_aligned_mem);
      while (mem_read > pre_aligned_mem) {
        unpack_scalar(offset, mem_read, mem_write);
      }
    }

    assert(AH::is_aligned(mem_read));

    auto lenv = len / (N*2);
    auto memv_read = ((TV *) mem_read) - 1;
    auto memv_write = ((TV *) mem_write) - 2;
    len -= lenv * N * 2;

    while (lenv > 0) {
      assert(memv_read <= memv_write);
      TV d01, d02;

      if (std::numeric_limits<TTo>::Min() < 0)
          MT::unpack_ordered_signed(MT::load_vec(memv_read), d01, d02);
      else
          MT::unpack_ordered_unsigned(MT::load_vec(memv_read), d01, d02);

      d01 = MT::add(d01, baseVec);
      d02 = MT::add(d02, baseVec);

      if (Shift > 0) { // This is statically compiled in/out
        d01 = MT::shift_left(d01, Shift);
        d02 = MT::shift_left(d02, Shift);
      }

      MT::store_vec(memv_write, d01);
      MT::store_vec(memv_write + 1, d02);

      memv_read -= 1;
      memv_write -= 2;
      lenv--;
    }

    mem_read = (TTo *) (memv_read + 1);
    mem_write = (TFrom *) (memv_write + 2);

    while (len-- > 0) {
      unpack_scalar(offset, mem_read, mem_write);
    }
  }

};

}

#include "vxsort_targets_disable.h"

#endif  // VXSORT_PACKER_H
