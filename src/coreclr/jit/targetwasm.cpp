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
        bool      passByRef = true;
        var_types abiType   = TYP_I_IMPL;

        CORINFO_CLASS_HANDLE clsHnd = structLayout->GetClassHandle();
        assert(clsHnd != NO_CLASS_HANDLE);
        assert(comp->info.compCompHnd->isValueClass(clsHnd));

        unsigned const structSize = comp->info.compCompHnd->getClassSize(clsHnd);

        switch (structSize)
        {
            case 1:
            case 2:
            case 4:
            case 8:
            {
                var_types primtiveType = GetPrimitiveTypeForTrivialStruct(comp, clsHnd);
                if (primtiveType != TYP_UNDEF)
                {
                    abiType   = genActualType(primtiveType);
                    passByRef = false;
                }
                break;
            }
            default:
                break;
        }

        regNumber         reg = MakeWasmReg(m_localIndex++, abiType);
        ABIPassingSegment seg = ABIPassingSegment::InRegister(reg, 0, genTypeSize(abiType));
        return ABIPassingInformation::FromSegment(comp, passByRef, seg);
    }

    regNumber         reg = MakeWasmReg(m_localIndex++, genActualType(type));
    ABIPassingSegment seg = ABIPassingSegment::InRegister(reg, 0, genTypeSize(type));
    return ABIPassingInformation::FromSegmentByValue(comp, seg);
}

//-----------------------------------------------------------------------------
// GetPrimitiveTypeForTrivialStruct:
//   Get the primitive type for a trivial struct for the Wasm ABI.
//
// Parameters:
//   comp   - Compiler instance
//   clsHnd - Class handle of the struct
//
// Returns:
//   The primitive type for the struct, or TYP_UNDEF if it cannot be represented as a primitive.
//
// TODO-Wasm: Union types? 128-bit types? SIMD?
//
var_types WasmClassifier::GetPrimitiveTypeForTrivialStruct(Compiler* comp, CORINFO_CLASS_HANDLE clsHnd)
{
    for (;;)
    {
        // all of class chain must be of value type and must have only one field
        if (!comp->info.compCompHnd->isValueClass(clsHnd) ||
            comp->info.compCompHnd->getClassNumInstanceFields(clsHnd) != 1)
        {
            return TYP_UNDEF;
        }

        CORINFO_CLASS_HANDLE* pClsHnd   = &clsHnd;
        CORINFO_FIELD_HANDLE  fldHnd    = comp->info.compCompHnd->getFieldInClass(clsHnd, 0);
        CorInfoType           fieldType = comp->info.compCompHnd->getFieldType(fldHnd, pClsHnd);

        var_types vt = JITtype2varType(fieldType);

        if (fieldType == CORINFO_TYPE_VALUECLASS)
        {
            clsHnd = *pClsHnd;
        }
        else
        {
            assert(vt != TYP_STRUCT);
            return vt;
        }
    }
}
