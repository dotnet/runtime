// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdio.h>
#include "native.h"


MCC_API VType8 WINAPI  sum(
    unsigned __int64 c1, VType8 v1,
    double c2, VType8 v2,
    float c3, VType8 v3,
    int c4, VType8 v4,
    unsigned short c5, VType8 v5,
    unsigned int c6, VType8 v6,
    float c7, VType8 v7,
    __int64 c8, VType8 v8,
    float c9, VType8 v9,
    double c10, VType8 v10,
    float c11, VType8 v11,
    short c12, VType8 v12) {
    VType8 res;

    // zero out res
    res.reset();

    // check values of parameters c1 thru c12
    int nfail = 12;
    if (c1 != (unsigned __int64)1) {
        printf("ERROR! Parameter c1 => expected %d, actual %d.\n", 1, (int)c1);
    }
    else {
        nfail--;
    }
    if (c2 != (double)2.0) {
        printf("ERROR! Parameter c2 => expected %d, actual %d.\n", 2, (int)c2);
    }
    else {
        nfail--;
    }
    if (c3 != (float)3.0) {
        printf("ERROR! Parameter c3 => expected %d, actual %d.\n", 3, (int)c3);
    }
    else {
        nfail--;
    }
    if (c4 != (int)4) {
        printf("ERROR! Parameter c4 => expected %d, actual %d.\n", 4, (int)c4);
    }
    else {
        nfail--;
    }
    if (c5 != (unsigned short)5) {
        printf("ERROR! Parameter c5 => expected %d, actual %d.\n", 5, (int)c5);
    }
    else {
        nfail--;
    }
    if (c6 != (unsigned int)6) {
        printf("ERROR! Parameter c6 => expected %d, actual %d.\n", 6, (int)c6);
    }
    else {
        nfail--;
    }
    if (c7 != (float)7.0) {
        printf("ERROR! Parameter c7 => expected %d, actual %d.\n", 7, (int)c7);
    }
    else {
        nfail--;
    }
    if (c8 != (__int64)8) {
        printf("ERROR! Parameter c8 => expected %d, actual %d.\n", 8, (int)c8);
    }
    else {
        nfail--;
    }
    if (c9 != (float)9.0) {
        printf("ERROR! Parameter c9 => expected %d, actual %d.\n", 9, (int)c9);
    }
    else {
        nfail--;
    }
    if (c10 != (double)10.0) {
        printf("ERROR! Parameter c10 => expected %d, actual %d.\n", 10, (int)c10);
    }
    else {
        nfail--;
    }
    if (c11 != (float)11.0) {
        printf("ERROR! Parameter c11 => expected %d, actual %d.\n", 11, (int)c11);
    }
    else {
        nfail--;
    }
    if (c12 != (short)12) {
        printf("ERROR! Parameter c12 => expected %d, actual %d.\n", 12, (int)c12);
    }
    else {
        nfail--;
    }

    if (nfail == 0) {
        res.add(v1);
        res.add(v2);
        res.add(v3);
        res.add(v4);
        res.add(v5);
        res.add(v6);
        res.add(v7);
        res.add(v8);
        res.add(v9);
        res.add(v10);
        res.add(v11);
        res.add(v12);
    }

    return res;
}
