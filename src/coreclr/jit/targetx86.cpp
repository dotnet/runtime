// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_X86)

#include "target.h"

const char*            Target::g_tgtCPUName           = "x86";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_L2R;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
const regNumber intArgRegs [] = {REG_ECX, REG_EDX};
const regMaskTP intArgMasks[] = {RBM_ECX, RBM_EDX};
// clang-format on

//-----------------------------------------------------------------------------
// X86Classifier:
//   Construct a new instance of the x86 ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
X86Classifier::X86Classifier(const ClassifierInfo& info)
    : m_info(info)
    , m_regs(nullptr, 0)
{
    switch (info.CallConv)
    {
        case CorInfoCallConvExtension::Thiscall:
        {
            static const regNumber thiscallRegs[] = {REG_ECX};
            m_regs                                = RegisterQueue(thiscallRegs, ArrLen(thiscallRegs));
            break;
        }
        case CorInfoCallConvExtension::C:
        case CorInfoCallConvExtension::Stdcall:
        case CorInfoCallConvExtension::CMemberFunction:
        case CorInfoCallConvExtension::StdcallMemberFunction:
        {
            break;
        }
        default:
        {
            unsigned numRegs = ArrLen(intArgRegs);
            if (info.IsVarArgs)
            {
                // In varargs methods we only enregister the this pointer or retbuff.
                numRegs = info.HasThis || info.HasRetBuff ? 1 : 0;
            }
            m_regs = RegisterQueue(intArgRegs, numRegs);
            break;
        }
    }
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the x86 ABI.
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
ABIPassingInformation X86Classifier::Classify(Compiler*    comp,
                                              var_types    type,
                                              ClassLayout* structLayout,
                                              WellKnownArg wellKnownParam)
{
    unsigned size     = type == TYP_STRUCT ? structLayout->GetSize() : genTypeSize(type);
    unsigned numSlots = (size + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE;

    bool canEnreg = false;
    if ((m_regs.Count() >= numSlots) && (wellKnownParam != WellKnownArg::X86TailCallSpecialArg))
    {
        switch (type)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            case TYP_SHORT:
            case TYP_USHORT:
            case TYP_INT:
            case TYP_REF:
            case TYP_BYREF:
                canEnreg = true;
                break;
            case TYP_STRUCT:
                canEnreg = comp->isTrivialPointerSizedStruct(structLayout->GetClassHandle());
                break;
            default:
                break;
        }
    }

    ABIPassingSegment segment;
    if (canEnreg)
    {
        assert(numSlots == 1);
        segment = ABIPassingSegment::InRegister(m_regs.Dequeue(), 0, size);
    }
    else
    {
        assert((m_stackArgSize % TARGET_POINTER_SIZE) == 0);

        unsigned offset;
        unsigned roundedArgSize = roundUp(size, TARGET_POINTER_SIZE);
        if (m_info.CallConv == CorInfoCallConvExtension::Managed)
        {
            // The managed ABI pushes parameters in left-to-right order. This
            // means that on the stack the first parameter is at the higher
            // offset (farthest away from ESP on entry). We model the stack
            // offset as the value to subtract from the top of the stack for
            // this ABI, see ABIPassingSegment::GetStackOffset.
            m_stackArgSize += roundedArgSize;
            offset = m_stackArgSize;
        }
        else
        {
            offset = m_stackArgSize;
            m_stackArgSize += roundedArgSize;
        }

        segment = ABIPassingSegment::OnStack(offset, 0, size);
    }

    return ABIPassingInformation::FromSegment(comp, segment);
}

#endif // TARGET_X86
