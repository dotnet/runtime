// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef BITONIC_SORT_H
#define BITONIC_SORT_H

#include "../defs.h"
#include "../machine_traits.h"

namespace vxsort {
namespace smallsort {
using namespace std;

// * We might read the last 4 bytes into a 128-bit vector for 64-bit element masking
// * We might read the last 8 bytes into a 128-bit vector for 32-bit element masking
// This mostly applies to debug mode, since without optimizations, most compilers
// actually execute the instruction stream _mm256_cvtepi8_epiNN + _mm_loadu_si128 as they are given.
// In contract, release/optimizing compilers, turn that very specific instruction pair to
// a more reasonable: vpmovsxbq ymm0, dword [rax*4 + mask_table_4], eliminating the 128-bit
// load completely and effectively reading 4/8 (depending if the instruction is vpmovsxb[q,d]
const int M4_SIZE = 16 + 12;
const int M8_SIZE = 64 + 8;

extern "C" const uint8_t mask_table_4[M4_SIZE];
extern "C" const uint8_t mask_table_8[M8_SIZE];

template <typename T, vector_machine M>
struct bitonic {
 public:
  static void sort(T* ptr, size_t length);
  static void sort_alt(T* ptr, size_t length);
};
}  // namespace smallsort
}  // namespace gcsort
#endif
