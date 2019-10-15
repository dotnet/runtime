// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdarg.h>
#include "native.h"


MCC_API VType0 sum(unsigned __int64 first, ...) {
    VType0 result;

    int count = 0;
    __int64 sum = 0;
    unsigned __int64 val = first;
    va_list args;

    // initialize variable arguments.
    va_start(args, first);
    while (val != (unsigned __int64)-1) {
        sum += val;
        count++;
        val = va_arg(args, unsigned __int64);
    }
    // reset variable arguments.
    va_end(args);

    result.sum = sum;
    result.count = count;
    result.average = (double)sum / count;
    result.dummy1 = result.sum;
    result.dummy2 = result.average;

    return result;
}
