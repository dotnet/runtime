// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef VXSORT_ALIGNNMENT_H
#define VXSORT_ALIGNNMENT_H

//#include <cstdint>

namespace vxsort {

using namespace std;

template <int N>
struct alignment_hint {
 public:
  static const size_t ALIGN = N;
  static const int8_t REALIGN = 0x66;

  alignment_hint() : left_align(REALIGN), right_align(REALIGN) {}
  alignment_hint realign_left() {
    alignment_hint copy = *this;
    copy.left_align = REALIGN;
    return copy;
  }

  alignment_hint realign_right() {
    alignment_hint copy = *this;
    copy.right_align = REALIGN;
    return copy;
  }

  static bool is_aligned(void* p) { return (size_t)p % ALIGN == 0; }

  int left_align : 8;
  int right_align : 8;
};

}
#endif  // VXSORT_ALIGNNMENT_H
