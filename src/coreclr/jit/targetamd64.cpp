// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_AMD64)

#include "target.h"

const char*            Target::g_tgtCPUName           = "x64";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_R2L;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
#ifdef UNIX_AMD64_ABI
const regNumber intArgRegs [] = { REG_EDI, REG_ESI, REG_EDX, REG_ECX, REG_R8, REG_R9 };
const regMaskTP intArgMasks[] = { RBM_EDI, RBM_ESI, RBM_EDX, RBM_ECX, RBM_R8, RBM_R9 };
const regNumber fltArgRegs [] = { REG_XMM0, REG_XMM1, REG_XMM2, REG_XMM3, REG_XMM4, REG_XMM5, REG_XMM6, REG_XMM7 };
const regMaskTP fltArgMasks[] = { RBM_XMM0, RBM_XMM1, RBM_XMM2, RBM_XMM3, RBM_XMM4, RBM_XMM5, RBM_XMM6, RBM_XMM7 };
#else // !UNIX_AMD64_ABI
const regNumber intArgRegs [] = { REG_ECX, REG_EDX, REG_R8, REG_R9 };
const regMaskTP intArgMasks[] = { RBM_ECX, RBM_EDX, RBM_R8, RBM_R9 };
const regNumber fltArgRegs [] = { REG_XMM0, REG_XMM1, REG_XMM2, REG_XMM3 };
const regMaskTP fltArgMasks[] = { RBM_XMM0, RBM_XMM1, RBM_XMM2, RBM_XMM3 };
#endif // !UNIX_AMD64_ABI
// clang-format on

#ifdef UNIX_AMD64_ABI
//-----------------------------------------------------------------------------
// SysVX64Classifier:
//   Construct a new instance of the SysV x64 ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
SysVX64Classifier::SysVX64Classifier(const ClassifierInfo& info)
    : m_intRegs(intArgRegs, ArrLen(intArgRegs))
    , m_floatRegs(fltArgRegs, ArrLen(fltArgRegs))
{
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the SysV x64 ABI.
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
ABIPassingInformation SysVX64Classifier::Classify(Compiler*    comp,
                                                  var_types    type,
                                                  ClassLayout* structLayout,
                                                  WellKnownArg wellKnownParam)
{
    bool                                                canEnreg = false;
    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
    if (varTypeIsStruct(type))
    {
        comp->eeGetSystemVAmd64PassStructInRegisterDescriptor(structLayout->GetClassHandle(), &structDesc);

        if (structDesc.passedInRegisters)
        {
            unsigned intRegCount   = 0;
            unsigned floatRegCount = 0;

            for (unsigned int i = 0; i < structDesc.eightByteCount; i++)
            {
                if (structDesc.IsIntegralSlot(i))
                {
                    intRegCount++;
                }
                else if (structDesc.IsSseSlot(i))
                {
                    floatRegCount++;
                }
                else
                {
                    assert(!"Invalid eightbyte classification type.");
                    break;
                }
            }

            canEnreg = (intRegCount <= m_intRegs.Count()) && (floatRegCount <= m_floatRegs.Count());
        }
    }
    else
    {
        unsigned availRegs = varTypeUsesFloatArgReg(type) ? m_floatRegs.Count() : m_intRegs.Count();
        canEnreg           = availRegs > 0;
    }

    ABIPassingInformation info;
    if (canEnreg)
    {
        if (varTypeIsStruct(type))
        {
            info = ABIPassingInformation(comp, structDesc.eightByteCount);

            for (unsigned i = 0; i < structDesc.eightByteCount; i++)
            {
                regNumber reg = structDesc.IsIntegralSlot(i) ? m_intRegs.Dequeue() : m_floatRegs.Dequeue();
                info.Segment(i) =
                    ABIPassingSegment::InRegister(reg, structDesc.eightByteOffsets[i], structDesc.eightByteSizes[i]);
            }
        }
        else
        {
            regNumber reg = varTypeUsesFloatArgReg(type) ? m_floatRegs.Dequeue() : m_intRegs.Dequeue();
            info = ABIPassingInformation::FromSegment(comp, ABIPassingSegment::InRegister(reg, 0, genTypeSize(type)));
        }
    }
    else
    {
        assert((m_stackArgSize % TARGET_POINTER_SIZE) == 0);
        unsigned size = type == TYP_STRUCT ? structLayout->GetSize() : genTypeSize(type);
        info          = ABIPassingInformation::FromSegment(comp, ABIPassingSegment::OnStack(m_stackArgSize, 0, size));
        m_stackArgSize += roundUp(size, TARGET_POINTER_SIZE);
    }

    return info;
}

#else // !UNIX_AMD64_ABI

//-----------------------------------------------------------------------------
// WinX64Classifier:
//   Construct a new instance of the Windows x64 ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
WinX64Classifier::WinX64Classifier(const ClassifierInfo& info)
    : m_intRegs(intArgRegs, ArrLen(intArgRegs))
    , m_floatRegs(fltArgRegs, ArrLen(fltArgRegs))
{
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the Windows x64 ABI.
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
ABIPassingInformation WinX64Classifier::Classify(Compiler*    comp,
                                                 var_types    type,
                                                 ClassLayout* structLayout,
                                                 WellKnownArg wellKnownParam)
{
    // On windows-x64 ABI all parameters take exactly 1 stack slot (structs
    // that do not fit are passed implicitly by reference). Passing a parameter
    // in an int register also consumes the corresponding float register and
    // vice versa.
    assert(m_intRegs.Count() == m_floatRegs.Count());

    unsigned typeSize = type == TYP_STRUCT ? structLayout->GetSize() : genTypeSize(type);
    if ((typeSize > TARGET_POINTER_SIZE) || !isPow2(typeSize))
    {
        typeSize = TARGET_POINTER_SIZE; // Passed by implicit byref
    }

    ABIPassingSegment segment;
    if (m_intRegs.Count() > 0)
    {
        regNumber reg = varTypeUsesFloatArgReg(type) ? m_floatRegs.Peek() : m_intRegs.Peek();
        segment       = ABIPassingSegment::InRegister(reg, 0, typeSize);
        m_intRegs.Dequeue();
        m_floatRegs.Dequeue();
    }
    else
    {
        segment = ABIPassingSegment::OnStack(m_stackArgSize, 0, typeSize);
        m_stackArgSize += TARGET_POINTER_SIZE;
    }

    return ABIPassingInformation::FromSegment(comp, segment);
}

//-----------------------------------------------------------------------------
// GetShadowSpaceCallerOffsetForReg:
//   Get the offset (starting at 0) at which a parameter register has shadow
//   stack space allocated by the caller.
//
// Parameters:
//   reg    - The register
//   offset - [out] Offset, starting at 0.
//
// Returns:
//   True if the register is a parameter register with shadow space allocated
//   by the caller; otherwise false.
//
bool ABIPassingInformation::GetShadowSpaceCallerOffsetForReg(regNumber reg, int* offset)
{
    switch (reg)
    {
        case REG_ECX:
        case REG_XMM0:
            *offset = 0;
            return true;
        case REG_EDX:
        case REG_XMM1:
            *offset = 8;
            return true;
        case REG_R8:
        case REG_XMM2:
            *offset = 16;
            return true;
        case REG_R9:
        case REG_XMM3:
            *offset = 24;
            return true;
        default:
            return false;
    }
}

#endif

#endif // TARGET_AMD64
