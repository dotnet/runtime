// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_RISCV64)

#include "target.h"

const char*            Target::g_tgtCPUName           = "riscv64";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_R2L;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
const regNumber intArgRegs [] = {REG_A0, REG_A1, REG_A2, REG_A3, REG_A4, REG_A5, REG_A6, REG_A7};
const regMaskTP intArgMasks[] = {RBM_A0, RBM_A1, RBM_A2, RBM_A3, RBM_A4, RBM_A5, RBM_A6, RBM_A7};

const regNumber fltArgRegs [] = {REG_FA0, REG_FA1, REG_FA2, REG_FA3, REG_FA4, REG_FA5, REG_FA6, REG_FA7 };
const regMaskTP fltArgMasks[] = {RBM_FA0, RBM_FA1, RBM_FA2, RBM_FA3, RBM_FA4, RBM_FA5, RBM_FA6, RBM_FA7 };
// clang-format on

//-----------------------------------------------------------------------------
// RiscV64Classifier:
//   Construct a new instance of the RISC-V 64 ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
RiscV64Classifier::RiscV64Classifier(const ClassifierInfo& info)
    : m_info(info)
    , m_intRegs(intArgRegs, ArrLen(intArgRegs))
    , m_floatRegs(fltArgRegs, ArrLen(fltArgRegs))
{
    assert(!m_info.IsVarArgs); // TODO: varargs currently not supported on RISC-V
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the RISC-V 64 ABI.
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
ABIPassingInformation RiscV64Classifier::Classify(Compiler*    comp,
                                                  var_types    type,
                                                  ClassLayout* structLayout,
                                                  WellKnownArg /*wellKnownParam*/)
{
    const CORINFO_FPSTRUCT_LOWERING* lowering = nullptr;

    unsigned intFields = 0, floatFields = 0;
    unsigned passedSize;
    bool     passedByRef = false;

    if (varTypeIsStruct(type))
    {
        passedSize = structLayout->GetSize();
        if (passedSize > MAX_PASS_MULTIREG_BYTES)
        {
            passedByRef = true;
            passedSize  = TARGET_POINTER_SIZE;
        }
        else if (!structLayout->IsBlockLayout())
        {
            lowering = comp->GetFpStructLowering(structLayout->GetClassHandle());
            if (!lowering->byIntegerCallConv)
            {
                assert((lowering->numLoweredElements == 1) || (lowering->numLoweredElements == 2));
                INDEBUG(unsigned debugIntFields = 0;)
                for (size_t i = 0; i < lowering->numLoweredElements; ++i)
                {
                    var_types type = JITtype2varType(lowering->loweredElements[i]);
                    floatFields += (unsigned)varTypeIsFloating(type);
                    INDEBUG(debugIntFields += (unsigned)varTypeIsIntegralOrI(type);)
                }
                intFields = static_cast<unsigned>(lowering->numLoweredElements) - floatFields;
                assert(debugIntFields == intFields);
            }
        }
    }
    else
    {
        passedSize = genTypeSize(type);
        assert(passedSize <= TARGET_POINTER_SIZE);
        floatFields = varTypeIsFloating(type) ? 1 : 0;
    }

    assert((floatFields > 0) || (intFields == 0));

    if ((floatFields > 0) && (m_floatRegs.Count() >= floatFields) && (m_intRegs.Count() >= intFields))
    {
        // Hardware floating-point calling convention
        if ((floatFields == 1) && (intFields == 0))
        {
            unsigned offset = 0;
            if (lowering != nullptr)
            {
                assert(lowering->numLoweredElements == 1); // struct containing just one FP real
                type       = JITtype2varType(lowering->loweredElements[0]);
                passedSize = genTypeSize(type);
                offset     = lowering->offsets[0];
            }
            assert(varTypeIsFloating(type));

            ABIPassingSegment seg = ABIPassingSegment::InRegister(m_floatRegs.Dequeue(), offset, passedSize);
            return ABIPassingInformation::FromSegmentByValue(comp, seg);
        }
        else
        {
            assert(varTypeIsStruct(type));
            assert((floatFields + intFields) == 2);
            assert(lowering != nullptr);
            assert(!lowering->byIntegerCallConv);
            assert(lowering->numLoweredElements == 2);

            var_types type0 = JITtype2varType(lowering->loweredElements[0]);
            var_types type1 = JITtype2varType(lowering->loweredElements[1]);
            assert(varTypeIsFloating(type0) || varTypeIsFloating(type1));
            RegisterQueue& queue0 = varTypeIsFloating(type0) ? m_floatRegs : m_intRegs;
            RegisterQueue& queue1 = varTypeIsFloating(type1) ? m_floatRegs : m_intRegs;

            auto seg0 = ABIPassingSegment::InRegister(queue0.Dequeue(), lowering->offsets[0], genTypeSize(type0));
            auto seg1 = ABIPassingSegment::InRegister(queue1.Dequeue(), lowering->offsets[1], genTypeSize(type1));
            return ABIPassingInformation::FromSegments(comp, seg0, seg1);
        }
    }
    else
    {
        // Integer calling convention
        auto passOnStack = [this](unsigned offset, unsigned size) -> ABIPassingSegment {
            assert(size > 0);
            assert(size <= 2 * TARGET_POINTER_SIZE);
            assert((m_stackArgSize % TARGET_POINTER_SIZE) == 0);
            ABIPassingSegment seg = ABIPassingSegment::OnStack(m_stackArgSize, offset, size);
            m_stackArgSize += (size > TARGET_POINTER_SIZE) ? (2 * TARGET_POINTER_SIZE) : TARGET_POINTER_SIZE;
            return seg;
        };

        if (m_intRegs.Count() > 0)
        {
            if (passedSize <= TARGET_POINTER_SIZE)
            {
                ABIPassingSegment seg = ABIPassingSegment::InRegister(m_intRegs.Dequeue(), 0, passedSize);
                return ABIPassingInformation::FromSegment(comp, passedByRef, seg);
            }
            else
            {
                assert(varTypeIsStruct(type));
                unsigned int tailSize = passedSize - TARGET_POINTER_SIZE;

                ABIPassingSegment head = ABIPassingSegment::InRegister(m_intRegs.Dequeue(), 0, TARGET_POINTER_SIZE);
                ABIPassingSegment tail =
                    (m_intRegs.Count() > 0)
                        ? ABIPassingSegment::InRegister(m_intRegs.Dequeue(), TARGET_POINTER_SIZE, tailSize)
                        : passOnStack(TARGET_POINTER_SIZE, tailSize);
                return ABIPassingInformation::FromSegments(comp, head, tail);
            }
        }
        else
        {
            return ABIPassingInformation::FromSegment(comp, passedByRef, passOnStack(0, passedSize));
        }
    }
}

#endif // TARGET_RISCV64
