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

const regNumber fltArgRegs [] = {REG_FLTARG_0, REG_FLTARG_1, REG_FLTARG_2, REG_FLTARG_3, REG_FLTARG_4, REG_FLTARG_5, REG_FLTARG_6, REG_FLTARG_7 };
const regMaskTP fltArgMasks[] = {RBM_FLTARG_0, RBM_FLTARG_1, RBM_FLTARG_2, RBM_FLTARG_3, RBM_FLTARG_4, RBM_FLTARG_5, RBM_FLTARG_6, RBM_FLTARG_7 };
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
    StructFloatFieldInfoFlags flags     = STRUCT_NO_FLOAT_FIELD;
    unsigned                  intFields = 0, floatFields = 0;
    unsigned                  passedSize;

    if (varTypeIsStruct(type))
    {
        passedSize = structLayout->GetSize();
        if (passedSize > MAX_PASS_MULTIREG_BYTES)
        {
            passedSize = TARGET_POINTER_SIZE; // pass by reference
        }
        else if (!structLayout->IsBlockLayout())
        {
            flags = (StructFloatFieldInfoFlags)comp->info.compCompHnd->getRISCV64PassStructInRegisterFlags(
                structLayout->GetClassHandle());

            if ((flags & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
            {
                floatFields = 1;
            }
            else if ((flags & STRUCT_FLOAT_FIELD_ONLY_TWO) != 0)
            {
                floatFields = 2;
            }
            else if (flags != STRUCT_NO_FLOAT_FIELD)
            {
                assert((flags & (STRUCT_FLOAT_FIELD_FIRST | STRUCT_FLOAT_FIELD_SECOND)) != 0);
                floatFields = 1;
                intFields   = 1;
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
            if (flags == STRUCT_NO_FLOAT_FIELD)
                assert(varTypeIsFloating(type)); // standalone floating-point real
            else
                assert((flags & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0); // struct containing just one FP real

            return ABIPassingInformation::FromSegment(comp, ABIPassingSegment::InRegister(m_floatRegs.Dequeue(), 0,
                                                                                          passedSize));
        }
        else
        {
            assert(varTypeIsStruct(type));
            assert((floatFields + intFields) == 2);
            assert(flags != STRUCT_NO_FLOAT_FIELD);
            assert((flags & STRUCT_FLOAT_FIELD_ONLY_ONE) == 0);

            unsigned firstSize  = ((flags & STRUCT_FIRST_FIELD_SIZE_IS8) != 0) ? 8 : 4;
            unsigned secondSize = ((flags & STRUCT_SECOND_FIELD_SIZE_IS8) != 0) ? 8 : 4;
            unsigned offset = max(firstSize, secondSize); // TODO: cover empty fields and custom offsets / alignments

            bool isFirstFloat  = (flags & (STRUCT_FLOAT_FIELD_ONLY_TWO | STRUCT_FLOAT_FIELD_FIRST)) != 0;
            bool isSecondFloat = (flags & (STRUCT_FLOAT_FIELD_ONLY_TWO | STRUCT_FLOAT_FIELD_SECOND)) != 0;
            assert(isFirstFloat || isSecondFloat);

            regNumber firstReg  = (isFirstFloat ? m_floatRegs : m_intRegs).Dequeue();
            regNumber secondReg = (isSecondFloat ? m_floatRegs : m_intRegs).Dequeue();

            return ABIPassingInformation::FromSegments(comp, ABIPassingSegment::InRegister(firstReg, 0, firstSize),
                                                       ABIPassingSegment::InRegister(secondReg, offset, secondSize));
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
                return ABIPassingInformation::FromSegment(comp, seg);
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
            return ABIPassingInformation::FromSegment(comp, passOnStack(0, passedSize));
        }
    }
}

#endif // TARGET_RISCV64
