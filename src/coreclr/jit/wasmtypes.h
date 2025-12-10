// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef _WASMTYPESDEF_H_
#define _WASMTYPESDEF_H_
/*****************************************************************************/

#include <cstdint>

enum class instWasmValueType : uint8_t
{
    I32 = 0x7F,
    I64 = 0x7E,
    F32 = 0x7D,
    F64 = 0x7C,
};

/*****************************************************************************/
#endif // _WASMTYPESDEF_H_
/*****************************************************************************/

