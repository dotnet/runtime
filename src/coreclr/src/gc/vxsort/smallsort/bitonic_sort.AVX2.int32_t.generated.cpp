// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "bitonic_sort.AVX2.int32_t.generated.h"

using namespace vxsort;

void vxsort::smallsort::bitonic<int32_t, vector_machine::AVX2 >::sort(int32_t *ptr, size_t length) {
    const auto fullvlength = length / N;
    const int remainder = (int) (length - fullvlength * N);
    const auto v = fullvlength + ((remainder > 0) ? 1 : 0);
    switch(v) {
        case 1: sort_01v_alt(ptr, remainder); break;
        case 2: sort_02v_alt(ptr, remainder); break;
        case 3: sort_03v_alt(ptr, remainder); break;
        case 4: sort_04v_alt(ptr, remainder); break;
        case 5: sort_05v_alt(ptr, remainder); break;
        case 6: sort_06v_alt(ptr, remainder); break;
        case 7: sort_07v_alt(ptr, remainder); break;
        case 8: sort_08v_alt(ptr, remainder); break;
        case 9: sort_09v_alt(ptr, remainder); break;
        case 10: sort_10v_alt(ptr, remainder); break;
        case 11: sort_11v_alt(ptr, remainder); break;
        case 12: sort_12v_alt(ptr, remainder); break;
        case 13: sort_13v_alt(ptr, remainder); break;
        case 14: sort_14v_alt(ptr, remainder); break;
        case 15: sort_15v_alt(ptr, remainder); break;
        case 16: sort_16v_alt(ptr, remainder); break;
    }
}
