// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_POWERPC64)

#include "target.h"

const char*            Target::g_tgtCPUName           = "ppc64le";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_R2L;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
const regNumber intArgRegs [] = {REG_R3, REG_R4, REG_R5, REG_R6, REG_R7, REG_R8, REG_R9, REG_R10};
const regMaskTP intArgMasks[] = {RBM_R0, RBM_R1, RBM_R2, RBM_R3, RBM_R4, RBM_R5, RBM_R6, RBM_R7};

const regNumber fltArgRegs [] = {REG_F1, REG_F2, REG_F3, REG_F4, REG_F5, REG_F6, REG_F7, REG_F8, REG_F9, REG_F10, REG_F11, REG_F12, REG_F13};
const regMaskTP fltArgMasks[] = {RBM_F1, RBM_F2, RBM_F3, RBM_F4, RBM_F5, RBM_F6, RBM_F7, RBM_F8, RBM_F9, RBM_F10, RBM_F11, RBM_F12, RBM_F13};
// clang-format on

//-----------------------------------------------------------------------------
// S390xClassifier:
//   Construct a new instance of the S390X ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
Ppc64leClassifier::Ppc64leClassifier(const ClassifierInfo& info)
    : m_info(info)
    , m_intRegs(intArgRegs, ArrLen(intArgRegs))
    , m_floatRegs(fltArgRegs, ArrLen(fltArgRegs))
{
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the S390X ABI.
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
ABIPassingInformation Ppc64leClassifier::Classify(Compiler*    comp,
                                                  var_types    type,
                                                  ClassLayout* structLayout,
                                                  WellKnownArg wellKnownParam)
{
    if ((wellKnownParam == WellKnownArg::RetBuffer) && hasFixedRetBuffReg(m_info.CallConv))
    {
        return ABIPassingInformation::FromSegment(comp, ABIPassingSegment::InRegister(REG_ARG_RET_BUFF, 0, TARGET_POINTER_SIZE));
    }

    // First handle HFA/HVAs. These are allowed to be passed in more registers
    // than other structures.
    if (varTypeIsStruct(type) && !m_info.IsVarArgs)
    {
        var_types hfaType = comp->GetHfaType(structLayout->GetClassHandle());

        if (hfaType != TYP_UNDEF)
        {
            unsigned              elemSize = genTypeSize(hfaType);
            unsigned              slots    = structLayout->GetSize() / elemSize;
            ABIPassingInformation info;
            if (m_floatRegs.Count() >= slots)
            {
                info = ABIPassingInformation(comp, slots);

                for (unsigned i = 0; i < slots; i++)
                {
                    info.Segment(i) = ABIPassingSegment::InRegister(m_floatRegs.Dequeue(), i * elemSize, elemSize);
                }
            }
	    else
            {
                unsigned alignment =
                    compAppleArm64Abi() ? min(elemSize, (unsigned)TARGET_POINTER_SIZE) : TARGET_POINTER_SIZE;
		m_stackArgSize = roundUp(m_stackArgSize, alignment);
                info           = ABIPassingInformation::FromSegment(comp, ABIPassingSegment::OnStack(m_stackArgSize, 0, structLayout->GetSize()));
                m_stackArgSize += roundUp(structLayout->GetSize(), alignment);

                // After passing any float value on the stack, we should not enregister more float values.
		m_floatRegs.Clear();
            }

        return info;
        }
    }
    unsigned slots;
    unsigned passedSize;
    if (varTypeIsStruct(type))
    {
	unsigned size = structLayout->GetSize();
	if (size > 16)
	{
	    slots      = 1; // Passed by implicit byref
            passedSize = TARGET_POINTER_SIZE;
	}
	else
	{
	    slots      = (size + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE;
	    passedSize = size;
	}
    }
    else
    {
	assert(genTypeSize(type) <= TARGET_POINTER_SIZE);
        slots      = 1;
	passedSize = genTypeSize(type);
    }

    assert((slots == 1) || (slots == 2));

    ABIPassingInformation info;
    if (m_info.IsVarArgs && (slots == 2) && (m_intRegs.Count() == 1))
    {
	// For varargs we split structs between register and stack in this
	// case. Normally a struct that does not fit in registers will always
	// be passed on stack.
	assert(compFeatureArgSplit());
	info = ABIPassingInformation::FromSegments(comp,
						ABIPassingSegment::InRegister(m_intRegs.Dequeue(), 0, TARGET_POINTER_SIZE),
						ABIPassingSegment::OnStack(m_stackArgSize, TARGET_POINTER_SIZE, 
							structLayout->GetSize() - TARGET_POINTER_SIZE));

	m_stackArgSize += TARGET_POINTER_SIZE;
    }
    else
    {
	RegisterQueue* regs = &m_intRegs;

	// In varargs methods (only supported on Windows) all parameters go in
        // integer registers.
        if (varTypeUsesFloatArgReg(type) && !m_info.IsVarArgs)
        {
            regs = &m_floatRegs;
        }

        if (regs->Count() >= slots)
        {
	    info              = ABIPassingInformation(comp, slots);
	    unsigned slotSize = min(passedSize, (unsigned)TARGET_POINTER_SIZE);
	    info.Segment(0)   = ABIPassingSegment::InRegister(regs->Dequeue(), 0, slotSize);
	    if (slots == 2)
	    {
		assert(varTypeIsStruct(type));
		unsigned tailSize = structLayout->GetSize() - slotSize;
		info.Segment(1)   = ABIPassingSegment::InRegister(regs->Dequeue(), slotSize, tailSize);
	    }
	}
	else
        {
	    unsigned alignment;
	    if (compAppleArm64Abi())
	    {
		if (varTypeIsStruct(type))
	        {
	            alignment = TARGET_POINTER_SIZE;
	        }
	        else
		{
	            alignment = genTypeSize(type);
	        }

	        m_stackArgSize = roundUp(m_stackArgSize, alignment);
	    }
	    else
	    {
		alignment = TARGET_POINTER_SIZE;
		assert((m_stackArgSize % TARGET_POINTER_SIZE) == 0);
	    }

	    info = ABIPassingInformation::FromSegment(comp, ABIPassingSegment::OnStack(m_stackArgSize, 0, passedSize));

	    m_stackArgSize += roundUp(passedSize, alignment);

	    // As soon as we pass something on stack we cannot go back and
	    // enregister something else.
	   regs->Clear();
	}
    }

    return info;
}
#endif
