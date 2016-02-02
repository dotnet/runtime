// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdarg.h>
#include "native.h"

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wvarargs"
#endif

MCC_API VType1 sum(float first, ...) {
    VType1 result;

    int count = 0;
    float sum = 0.0;
    float val = first;
    va_list args;

    // initialize variable arguments.
    va_start(args, first);
    while (val != (float)-1) {
        sum += val;
        count++;
        val = va_arg(args, float);
    }
    // reset variable arguments.
    va_end(args);

    result.count = (float)count;
    result.sum = sum;
    result.average = result.sum / result.count;

    result.count1 = (float)count;
    result.sum1 = sum;
    result.average1 = result.sum1 / result.count1;

    result.count2 = (float)count;
    result.sum2 = sum;
    result.average2 = result.sum2 / result.count2;

    result.count3 = (float)count;
    result.sum3 = sum;
    result.average3 = result.sum3 / result.count3;

    result.count4 = (float)count;
    result.sum4 = sum;
    result.average4 = result.sum4 / result.count4;

    result.count5 = (float)count;
    result.sum5 = sum;
    result.average5 = result.sum5 / result.count5;

    return result;
}

#ifdef __clang__
#pragma clang diagnostic pop
#endif
