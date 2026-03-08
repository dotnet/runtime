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

    return ABIPassingInformation::FromSegmentByValue(comp, segment);
}

#ifdef VECTORCALL_SUPPORT
// Vectorcall uses XMM0-XMM5 for vector arguments on x86.
// clang-format off
static const regNumber vectorcallFltArgRegs [] = { REG_XMM0, REG_XMM1, REG_XMM2, REG_XMM3, REG_XMM4, REG_XMM5 };
// clang-format on

//-----------------------------------------------------------------------------
// VectorcallX86Classifier:
//   Construct a new instance of the vectorcall x86 ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
VectorcallX86Classifier::VectorcallX86Classifier(const ClassifierInfo& info)
    : m_info(info)
    , m_intRegs(intArgRegs, ArrLen(intArgRegs))
    , m_floatRegs(vectorcallFltArgRegs, ArrLen(vectorcallFltArgRegs))
{
    // Vectorcall on x86 uses ECX, EDX for integer arguments (like fastcall)
    // and XMM0-XMM5 for vector/float arguments
    if (info.IsVarArgs)
    {
        // In varargs methods we only enregister the this pointer or retbuff.
        unsigned numRegs = info.HasThis || info.HasRetBuff ? 1 : 0;
        m_intRegs        = RegisterQueue(intArgRegs, numRegs);
        // Varargs don't use XMM registers
        m_floatRegs.Clear();
    }
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the vectorcall x86 ABI.
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
//   Unlike standard x86 calling conventions that pass floats on the stack,
//   vectorcall passes float/double/SIMD types in XMM0-XMM5. Integer types
//   still use ECX, EDX (like fastcall).
//
ABIPassingInformation VectorcallX86Classifier::Classify(Compiler*    comp,
                                                        var_types    type,
                                                        ClassLayout* structLayout,
                                                        WellKnownArg wellKnownParam)
{
    unsigned size     = type == TYP_STRUCT ? structLayout->GetSize() : genTypeSize(type);
    unsigned numSlots = (size + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE;

    ABIPassingSegment segment;
    bool              useFloatReg = varTypeUsesFloatArgReg(type);

    if (useFloatReg && (m_floatRegs.Count() > 0))
    {
        // Float/double/SIMD types go in XMM registers
        regNumber reg = m_floatRegs.Dequeue();
        segment       = ABIPassingSegment::InRegister(reg, 0, size);
    }
    else if (!useFloatReg && (m_intRegs.Count() >= numSlots) && (wellKnownParam != WellKnownArg::X86TailCallSpecialArg))
    {
        // Check if we can enregister integer types
        bool canEnreg = false;
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

        if (canEnreg)
        {
            assert(numSlots == 1);
            segment = ABIPassingSegment::InRegister(m_intRegs.Dequeue(), 0, size);
        }
        else
        {
            // Pass on stack
            unsigned offset         = m_stackArgSize;
            unsigned roundedArgSize = roundUp(size, TARGET_POINTER_SIZE);
            m_stackArgSize += roundedArgSize;
            segment = ABIPassingSegment::OnStack(offset, 0, size);
        }
    }
    else
    {
        // Pass on stack
        unsigned offset         = m_stackArgSize;
        unsigned roundedArgSize = roundUp(size, TARGET_POINTER_SIZE);
        m_stackArgSize += roundedArgSize;
        segment = ABIPassingSegment::OnStack(offset, 0, size);
    }

    return ABIPassingInformation::FromSegmentByValue(comp, segment);
}
#endif // VECTORCALL_SUPPORT

#endif // TARGET_X86
