// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_WASM32
#define CPU_NAME "wasm32";
#else
#define CPU_NAME "wasm64";
#endif

const char*            Target::g_tgtCPUName           = CPU_NAME;
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_R2L;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

//-----------------------------------------------------------------------------
// WasmClassifier:
//   Construct a new instance of the Wasm ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
WasmClassifier::WasmClassifier(const ClassifierInfo& info)
{
}

//-----------------------------------------------------------------------------
// ToJitType: translate CorInfoWasmType to var_types
//
// Parameters:
//   wasmType -- wasm type to translate
//
var_types WasmClassifier::ToJitType(CorInfoWasmType wasmType)
{
    switch (wasmType)
    {
        case CORINFO_WASM_TYPE_I32:
            return TYP_INT;
        case CORINFO_WASM_TYPE_I64:
            return TYP_LONG;
        case CORINFO_WASM_TYPE_F32:
            return TYP_FLOAT;
        case CORINFO_WASM_TYPE_F64:
            return TYP_DOUBLE;
        case CORINFO_WASM_TYPE_V128:
            // TODO-WASM: Simd support
            unreached();
        default:
            unreached();
    }
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the Wasm ABI.
//
// Parameters:
//   comp           - Compiler instance
//   type           - The type of the parameter
//   structLayout   - The layout of the struct. Expected to be non-null if
//                    varTypeIsStruct(type) is true.
//   wellKnownParam - Well known type of the parameter (if it may affect its ABI classification)
//
// Returns:
//   Classification information for the parameter.
//
ABIPassingInformation WasmClassifier::Classify(Compiler*    comp,
                                               var_types    type,
                                               ClassLayout* structLayout,
                                               WellKnownArg wellKnownParam)
{
    if (type == TYP_STRUCT)
    {
        CORINFO_CLASS_HANDLE clsHnd = structLayout->GetClassHandle();
        assert(clsHnd != NO_CLASS_HANDLE);
        CorInfoWasmType wasmAbiType = comp->info.compCompHnd->getWasmLowering(clsHnd);
        bool            passByRef   = false;
        var_types       abiType     = TYP_UNDEF;

        if (wasmAbiType == CORINFO_WASM_TYPE_VOID)
        {
            abiType   = TYP_I_IMPL;
            passByRef = true;
        }
        else
        {
            abiType = ToJitType(wasmAbiType);
        }

        regNumber         reg = MakeWasmReg(m_localIndex++, genActualType(abiType));
        ABIPassingSegment seg = ABIPassingSegment::InRegister(reg, 0, genTypeSize(abiType));
        return ABIPassingInformation::FromSegment(comp, passByRef, seg);
    }

    regNumber         reg = MakeWasmReg(m_localIndex++, genActualType(type));
    ABIPassingSegment seg = ABIPassingSegment::InRegister(reg, 0, genTypeSize(type));
    return ABIPassingInformation::FromSegmentByValue(comp, seg);
}
