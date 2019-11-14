// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdarg.h>
#include "native.h"


MCC_API VType8 sum(double count1, int count2, __int64 count3, float count4, short count5, double count6, ...) {
    int count = (int)count1 + (int)count2 + (int)count3 + (int)count4 + (int)count5 + (int)count6;
    VType8 res;
    va_list args;

    // zero out res
    res.reset();

    // initialize variable arguments.
    va_start(args, count6);
    for (int i = 0; i < count; ++i) {
        VType8 val = va_arg(args, VType8);
        res.add(val);
    }
    // reset variable arguments.
    va_end(args);

    return res;
}
