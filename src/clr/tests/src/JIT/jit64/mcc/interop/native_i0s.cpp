// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "native.h"


struct MyValueType {
    int		count;
    __int64	sum;
    double	average;
    __int64	dummy1;
    double	dummy2;
};


MCC_API MyValueType WINAPI  sum(
    unsigned __int64 a01, unsigned __int64 a02, unsigned __int64 a03,
    unsigned __int64 a04, unsigned __int64 a05, unsigned __int64 a06,
    unsigned __int64 a07, unsigned __int64 a08, unsigned __int64 a09,
    unsigned __int64 a10, unsigned __int64 a11, unsigned __int64 a12) {
    MyValueType result;

    result.sum = static_cast<__int64>(a01 + a02 + a03 + a04 + a05 + a06 + a07 + a08 + a09 + a10 + a11 + a12);
    result.count = 12;
    result.average = (double)result.sum / result.count;
    result.dummy1 = result.sum;
    result.dummy2 = result.average;
    return result;
}
