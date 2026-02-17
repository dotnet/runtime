// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

enum class WasmValueType : unsigned
{
    Invalid, // This effectively reserves the "0"th type for various sentinels like REG_NA.
    I32,
    I64,
    F32,
    F64,
    Count,

    First = I32,
#ifdef TARGET_64BIT
    I = I64,
#else
    I = I32,
#endif
};

inline WasmValueType& operator++(WasmValueType& type)
{
    return type = static_cast<WasmValueType>(static_cast<unsigned>(type) + 1);
}

regNumber     MakeWasmReg(unsigned index, var_types type);
regNumber     MakeWasmReg(unsigned index, WasmValueType type);
WasmValueType TypeToWasmValueType(var_types type);
const char*   WasmValueTypeName(WasmValueType type);
unsigned      UnpackWasmReg(regNumber reg, WasmValueType* pType = nullptr);
unsigned      WasmRegToIndex(regNumber reg);
WasmValueType WasmRegToType(regNumber reg);
bool          genIsValidReg(regNumber reg);
bool          genIsValidIntReg(regNumber reg);
bool          genIsValidIntOrFakeReg(regNumber reg);
bool          genIsValidFloatReg(regNumber reg);
