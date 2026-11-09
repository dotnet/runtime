// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "interpmethod.h"

const char*            Target::g_tgtCPUName           = "interp";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_L2R;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_L2R;

//-----------------------------------------------------------------------------
// InterpClassifier:
//   Construct a new instance of the interpreter ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
InterpClassifier::InterpClassifier(const ClassifierInfo& info)
{
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the interpreter calling convention.
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
// Notes:
//   Every argument is passed by value on the interpreter stack, in order, in a
//   slot whose size is rounded up to INTERP_STACK_SLOT_SIZE. This must match how
//   the interpreter lays out call arguments.
//
ABIPassingInformation InterpClassifier::Classify(Compiler*    comp,
                                                 var_types    type,
                                                 ClassLayout* structLayout,
                                                 WellKnownArg wellKnownParam)
{
    unsigned size = varTypeIsStruct(type) ? structLayout->GetSize() : genTypeSize(type);

    // TODO-Interp: arguments that need INTERP_STACK_ALIGNMENT (e.g. Vector128) are not handled yet.
    unsigned          offset = m_stackArgSize;
    ABIPassingSegment seg    = ABIPassingSegment::OnStack(offset, 0, size);
    m_stackArgSize += roundUp(size, INTERP_STACK_SLOT_SIZE);
    return ABIPassingInformation::FromSegmentByValue(comp, seg);
}
