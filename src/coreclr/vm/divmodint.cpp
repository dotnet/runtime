// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef TARGET_32BIT

#include <common.h>

#include "divmodint.h"

#include <optsmallperfcritical.h>

FCIMPL2(int32_t, DivModInt::DivInt32, int32_t dividend, int32_t divisor)
    FCALL_CONTRACT;

    return dividend / divisor;
FCIMPLEND

FCIMPL2(uint32_t, DivModInt::DivUInt32, uint32_t dividend, uint32_t divisor)
    FCALL_CONTRACT;

    return dividend / divisor;
FCIMPLEND

FCIMPL2_VV(int64_t, DivModInt::DivInt64, int64_t dividend, int64_t divisor)
    FCALL_CONTRACT;

    return dividend / divisor;
FCIMPLEND

FCIMPL2_VV(uint64_t, DivModInt::DivUInt64, uint64_t dividend, uint64_t divisor)
    FCALL_CONTRACT;

    return dividend / divisor;
FCIMPLEND

FCIMPL2(int32_t, DivModInt::ModInt32, int32_t dividend, int32_t divisor)
    FCALL_CONTRACT;

    return dividend % divisor;
FCIMPLEND

FCIMPL2(uint32_t, DivModInt::ModUInt32, uint32_t dividend, uint32_t divisor)
    FCALL_CONTRACT;

    return dividend % divisor;
FCIMPLEND

FCIMPL2_VV(int64_t, DivModInt::ModInt64, int64_t dividend, int64_t divisor)
    FCALL_CONTRACT;

    return dividend % divisor;
FCIMPLEND

FCIMPL2_VV(uint64_t, DivModInt::ModUInt64, uint64_t dividend, uint64_t divisor)
    FCALL_CONTRACT;

    return dividend % divisor;
FCIMPLEND

#endif // TARGET_32BIT
