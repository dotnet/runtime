// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_ARM64)

#include "common.h"
#include "alignment.h"
#include "machine_traits.neon.h"

namespace vxsort {

alignas(32) const uint8_t perm_table_64[T64_SIZE] = {
        0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15, // 0b00 (0)
        8,  9, 10, 11, 12, 13, 14, 15,  0,  1,  2,  3,  4,  5,  6,  7, // 0b01 (1)
        0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15, // 0b10 (2)
        0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15, // 0b11 (3)
};

}

#endif