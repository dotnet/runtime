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
        NYI_WASM("WasmClassifier::Classify - structs");
    }

    regNumber         reg = MakeWasmReg(m_localIndex++, genActualType(type));
    ABIPassingSegment seg = ABIPassingSegment::InRegister(reg, 0, genTypeSize(type));
    return ABIPassingInformation::FromSegmentByValue(comp, seg);
}
