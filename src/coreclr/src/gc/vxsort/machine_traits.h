// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Created by dans on 6/1/20.
//

#ifndef VXSORT_MACHINE_TRAITS_H
#define VXSORT_MACHINE_TRAITS_H

//#include <cstdint>

namespace vxsort {

enum vector_machine {
    NONE,
    AVX2,
    AVX512,
    SVE,
};

template <typename T, vector_machine M>
struct vxsort_machine_traits {
   public:
    typedef int TV;
    typedef int TMASK;

    static constexpr bool supports_compress_writes();

    static TV load_vec(TV* ptr);
    static void store_vec(TV* ptr, TV v);
    static void store_compress_vec(TV* ptr, TV v, TMASK mask);
    static TV partition_vector(TV v, int mask);
    static TV broadcast(T pivot);
    static TMASK get_cmpgt_mask(TV a, TV b);

    static TV shift_right(TV v, int i);
    static TV shift_left(TV v, int i);

    static TV add(TV a, TV b);
    static TV sub(TV a, TV b);

    static TV pack_ordered(TV a, TV b);
    static TV pack_unordered(TV a, TV b);

    static void unpack_ordered_signed(TV p, TV& u1, TV& u2);
    static void unpack_ordered_unsigned(TV p, TV& u1, TV& u2);


};
}

#endif  // VXSORT_MACHINE_TRAITS_H
