// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef BITONIC_SORT_H
#define BITONIC_SORT_H

#include <stdint.h>
#include "../defs.h"
#include "../machine_traits.h"

namespace vxsort {
namespace smallsort {
template <typename T, vector_machine M>
struct bitonic {
 public:
  static void sort(T* ptr, size_t length);
};
}  // namespace smallsort
}  // namespace gcsort
#endif
