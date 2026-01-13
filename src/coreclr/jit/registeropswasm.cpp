// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

using RegNumUnderlyingType                               = regNumberSmall;
static const RegNumUnderlyingType WASM_REG_TYPE_BITS     = 3;
static const RegNumUnderlyingType WASM_REG_TYPE_SHIFT    = 8 * sizeof(RegNumUnderlyingType) - WASM_REG_TYPE_BITS;
static const RegNumUnderlyingType WASM_REG_TYPE_MASK     = ~0u << WASM_REG_TYPE_SHIFT;
static const unsigned             WASM_LOCAL_INDEX_LIMIT = WASM_REG_TYPE_MASK;

static_assert(sizeof(RegNumUnderlyingType) >= sizeof(unsigned));
static_assert(((static_cast<RegNumUnderlyingType>(WasmValueType::Count) - 1) >> WASM_REG_TYPE_BITS) == 0);

//------------------------------------------------------------------------
// MakeWasmReg: Create a register number representing a WASM local.
//
// Arguments:
//    index - The index of the WASM local
//    type  - Its type, must be directly mappable to the WASM type system
//
// Return Value:
//    'regNumber' representing a tuple of '(index, type)'.
//
regNumber MakeWasmReg(unsigned index, var_types type)
{
    // WASM engines have a much smaller limit on the number of locals (50K) than this.
    if ((index & WASM_REG_TYPE_MASK) != 0)
    {
        IMPL_LIMITATION("Too many locals");
    }
    // clang-format off
    static const WasmValueType s_mapping[] = {
        WasmValueType::Invalid, // TYP_UNDEF,
        WasmValueType::Invalid, // TYP_VOID,
        WasmValueType::Invalid, // TYP_BYTE,
        WasmValueType::Invalid, // TYP_UBYTE,
        WasmValueType::Invalid, // TYP_SHORT,
        WasmValueType::Invalid, // TYP_USHORT,
        WasmValueType::I32,     // TYP_INT,
        WasmValueType::Invalid, // TYP_UINT,
        WasmValueType::I64,     // TYP_LONG,
        WasmValueType::Invalid, // TYP_ULONG,
        WasmValueType::F32,     // TYP_FLOAT,
        WasmValueType::F64,     // TYP_DOUBLE,
        WasmValueType::I,       // TYP_REF,
        WasmValueType::I,       // TYP_BYREF,
        WasmValueType::Invalid, // TYP_STRUCT
        WasmValueType::Invalid, // TYP_UNKNOWN
    };
    static_assert(ArrLen(s_mapping) == TYP_COUNT);
    // clang-format on

    WasmValueType wasmType = s_mapping[type];
    assert(wasmType != WasmValueType::Invalid);

    RegNumUnderlyingType regValue = index | (static_cast<RegNumUnderlyingType>(wasmType) << WASM_REG_TYPE_SHIFT);
    return static_cast<regNumber>(regValue);
}

const char* WasmValueTypeName(WasmValueType type)
{
    // clang-format off
    static const char* const WasmValueTypeNames[] = {
        "Invalid",
        "i32",
        "i64",
        "f32",
        "f64",
    };
    static_assert(ArrLen(WasmValueTypeNames) == static_cast<unsigned>(WasmValueType::Count));
    // clang-format on

    assert(WasmValueType::Invalid <= type && type < WasmValueType::Count);
    return WasmValueTypeNames[(unsigned)type];
}

//------------------------------------------------------------------------
// UnpackWasmReg: Extract the WASM local's index and type out of a register
//
// Arguments:
//    reg   - The register number representing '(index, type)'
//    pType - Optional [out] parameter for the type
//
// Return Value:
//    The index represented by 'reg'.
//
unsigned UnpackWasmReg(regNumber reg, WasmValueType* pType)
{
    RegNumUnderlyingType regValue = static_cast<RegNumUnderlyingType>(reg);
    if (pType != nullptr)
    {
        *pType = static_cast<WasmValueType>(regValue >> WASM_REG_TYPE_SHIFT);
    }
    return regValue & ~WASM_REG_TYPE_MASK;
}

//------------------------------------------------------------------------
// UnpackWasmReg: Extract the WASM local's index out of a register
//
// Arguments:
//    reg   - The register number representing '(index, type)'
//
// Return Value:
//    The index represented by 'reg'.
//
unsigned WasmRegToIndex(regNumber reg)
{
    return UnpackWasmReg(reg);
}

bool genIsValidReg(regNumber reg)
{
    WasmValueType wasmType;
    UnpackWasmReg(reg, &wasmType);
    return (WasmValueType::Invalid < wasmType) && (wasmType < WasmValueType::Count);
}

// TODO-WASM: implement the following functions in terms of a "locals registry" that would hold information
// about the registers.

bool genIsValidIntReg(regNumber reg)
{
    WasmValueType type;
    UnpackWasmReg(reg, &type);
    return (type == WasmValueType::I32) || (type == WasmValueType::I64);
}

bool genIsValidIntOrFakeReg(regNumber reg)
{
    return genIsValidIntReg(reg);
}

bool genIsValidFloatReg(regNumber reg)
{
    WasmValueType type;
    UnpackWasmReg(reg, &type);
    return (type == WasmValueType::F32) || (type == WasmValueType::F64);
}

const char* getRegName(regNumber reg)
{
    if (reg == REG_NA)
    {
        return "NA";
    }

    // TODO-Wasm: remove this hardcoded list workaround by refactoring this API.
    // clang-format off
    static const char* const s_names[] = {
        "$0",  "$1",  "$2",  "$3",  "$4",  "$5",  "$6",  "$7",  "$8",  "$9",
        "$10", "$11", "$12", "$13", "$14", "$15", "$16", "$17", "$18", "$19",
    };
    // clang-format on

    unsigned index = UnpackWasmReg(reg);
    if (index < ArrLen(s_names))
    {
        return s_names[index];
    }

    return "$<too large to print>";
}
