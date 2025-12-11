// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef _WASMTYPESDEF_H_
#define _WASMTYPESDEF_H_
/*****************************************************************************/

#include <cstdint>
#include "vartypesdef.h"

#if defined(TARGET_WASM)
enum class instWasmValueType : uint8_t
{
    F64 = 0x7C,
    F32 = 0x7D,
    I64 = 0x7E,
    I32 = 0x7F,
};

const char* const instWasmValueTypeNames[4] = {
    "f64",
    "f32",
    "i64",
    "i32",
};

//------------------------------------------------------------------------
// instWasmValueTypeToStr: map wasm type to string
//
inline const char* const instWasmValueTypeToStr(instWasmValueType type)
{
    assert((type >= instWasmValueType::F64) && (type <= instWasmValueType::I32));
    return instWasmValueTypeNames[static_cast<uint8_t>(type) - static_cast<uint8_t>(instWasmValueType::F64)];
}

//------------------------------------------------------------------------
// genWasmTypeFromVarType: map var_type to wasm type
//
inline instWasmValueType genWasmTypeFromVarType(var_types type)
{
    switch (type)
    {
        case TYP_INT:
        case TYP_REF:
        case TYP_BYREF:
            return instWasmValueType::I32;
        case TYP_LONG:
            return instWasmValueType::I64;
        case TYP_FLOAT:
            return instWasmValueType::F32;
        case TYP_DOUBLE:
            return instWasmValueType::F64;
        default:
            unreached();
    }
}
#endif

/*****************************************************************************/
#endif // _WASMTYPESDEF_H_
/*****************************************************************************/
