// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_DIVMODINT_H
#define HAVE_DIVMODINT_H

#include <object.h>
#include <fcall.h>

class DivModInt {
public:
    FCDECL2(static int32_t, DivInt32, int32_t dividend, int32_t divisor);
    FCDECL2(static uint32_t, DivUInt32, uint32_t dividend, uint32_t divisor);
    FCDECL2_VV(static int64_t, DivInt64, int64_t dividend, int64_t divisor);
    FCDECL2_VV(static uint64_t, DivUInt64, uint64_t dividend, uint64_t divisor);
    FCDECL2(static int32_t, ModInt32, int32_t dividend, int32_t divisor);
    FCDECL2(static uint32_t, ModUInt32, uint32_t dividend, uint32_t divisor);
    FCDECL2_VV(static int64_t, ModInt64, int64_t dividend, int64_t divisor);
    FCDECL2_VV(static uint64_t, ModUInt64, uint64_t dividend, uint64_t divisor);
};

#endif // HAVE_DIVMODINT_H
