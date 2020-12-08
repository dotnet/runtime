// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "native.h"

struct MyValueType {
    float count;
    float sum;
    float average;
    float count1;
    float sum1;
    float average1;
    float count2;
    float sum2;
    float average2;
    float count3;
    float sum3;
    float average3;
    float count4;
    float sum4;
    float average4;
    float count5;
    float sum5;
    float average5;
};


MCC_API MyValueType WINAPI  sum(
    float a01, float a02, float a03,
    float a04, float a05, float a06,
    float a07, float a08, float a09,
    float a10, float a11, float a12) {
    MyValueType result;

    int count = 12;
    float sum = a01 + a02 + a03 + a04 + a05 + a06 + a07 + a08 + a09 + a10 + a11 + a12;

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
