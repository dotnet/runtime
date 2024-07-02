// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_LOONGARCH64)

#include "target.h"

const char*            Target::g_tgtCPUName           = "loongarch64";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_R2L;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
const regNumber intArgRegs [] = {REG_A0, REG_A1, REG_A2, REG_A3, REG_A4, REG_A5, REG_A6, REG_A7};
const regMaskTP intArgMasks[] = {RBM_A0, RBM_A1, RBM_A2, RBM_A3, RBM_A4, RBM_A5, RBM_A6, RBM_A7};

const regNumber fltArgRegs [] = {REG_F0, REG_F1, REG_F2, REG_F3, REG_F4, REG_F5, REG_F6, REG_F7 };
const regMaskTP fltArgMasks[] = {RBM_F0, RBM_F1, RBM_F2, RBM_F3, RBM_F4, RBM_F5, RBM_F6, RBM_F7 };
// clang-format on

//-----------------------------------------------------------------------------
// LoongArch64Classifier:
//   Construct a new instance of the LoongArch64 ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
LoongArch64Classifier::LoongArch64Classifier(const ClassifierInfo& info)
    : m_info(info)
    , m_intRegs(intArgRegs, ArrLen(intArgRegs))
    , m_floatRegs(fltArgRegs, ArrLen(fltArgRegs))
{
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the LoongArch64 ABI.
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
ABIPassingInformation LoongArch64Classifier::Classify(Compiler*    comp,
                                                      var_types    type,
                                                      ClassLayout* structLayout,
                                                      WellKnownArg wellKnownParam)
{
    assert(!m_info.IsVarArgs);

    unsigned  passedSize;
    unsigned  slots               = 0;
    var_types argRegTypeInStruct1 = TYP_UNKNOWN;
    var_types argRegTypeInStruct2 = TYP_UNKNOWN;

    bool canPassArgInRegisters = false;
    if (varTypeIsStruct(type))
    {
        passedSize = structLayout->GetSize();
        if (passedSize > MAX_PASS_MULTIREG_BYTES)
        {
            slots                 = 1; // Passed by implicit byref
            passedSize            = TARGET_POINTER_SIZE;
            canPassArgInRegisters = m_intRegs.Count() > 0;
        }
        else
        {
            assert(!structLayout->IsBlockLayout());

            FpStructInRegistersInfo fpInfo;
            CORINFO_CLASS_HANDLE    typeHnd = structLayout->GetClassHandle();

            fpInfo = comp->GetPassFpStructInRegistersInfo(typeHnd);

            if (fpInfo.flags != FpStruct::UseIntCallConv)
            {
                if ((fpInfo.flags & FpStruct::OnlyOne) != 0)
                {
                    assert(passedSize <= TARGET_POINTER_SIZE);

                    slots                 = 1;
                    canPassArgInRegisters = m_floatRegs.Count() > 0;

                    argRegTypeInStruct1 = (passedSize == 8) ? TYP_DOUBLE : TYP_FLOAT;
                }
                else if ((fpInfo.flags & FpStruct::BothFloat) != 0)
                {
                    slots                 = 2;
                    canPassArgInRegisters = m_floatRegs.Count() >= 2;

                    argRegTypeInStruct1 = (fpInfo.SizeShift1st() == 3) ? TYP_DOUBLE : TYP_FLOAT;
                    argRegTypeInStruct2 = (fpInfo.SizeShift2nd() == 3) ? TYP_DOUBLE : TYP_FLOAT;
                }
                else if ((fpInfo.flags & FpStruct::FloatInt) != 0)
                {
                    slots                 = 2;
                    canPassArgInRegisters = (m_floatRegs.Count() > 0) && (m_intRegs.Count() > 0);

                    argRegTypeInStruct1 = (fpInfo.SizeShift1st() == 3) ? TYP_DOUBLE : TYP_FLOAT;
                    argRegTypeInStruct2 = (fpInfo.SizeShift2nd() == 3) ? TYP_LONG : TYP_INT;
                }
                else if ((fpInfo.flags & FpStruct::IntFloat) != 0)
                {
                    slots                 = 2;
                    canPassArgInRegisters = (m_floatRegs.Count() > 0) && (m_intRegs.Count() > 0);

                    argRegTypeInStruct1 = (fpInfo.SizeShift1st() == 3) ? TYP_LONG : TYP_INT;
                    argRegTypeInStruct2 = (fpInfo.SizeShift2nd() == 3) ? TYP_DOUBLE : TYP_FLOAT;
                }

                assert((slots == 1) || (slots == 2));

                if (!canPassArgInRegisters)
                {
                    slots = (passedSize + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE;
                    // On LoongArch64, if there aren't any remaining floating-point registers to pass the argument,
                    // integer registers (if any) are used instead.
                    canPassArgInRegisters = m_intRegs.Count() >= slots;

                    argRegTypeInStruct1 = TYP_UNKNOWN;
                    argRegTypeInStruct2 = TYP_UNKNOWN;
                }
            }
            else
            {
                slots                 = (passedSize + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE;
                canPassArgInRegisters = m_intRegs.Count() >= slots;
            }

            if (!canPassArgInRegisters && (slots == 2))
            {
                // Here a struct-arg which needs two registers but only one integer register available,
                // it has to be split.
                if (m_intRegs.Count() > 0)
                {
                    canPassArgInRegisters = true;
                }
            }
        }
    }
    else
    {
        assert(genTypeSize(type) <= TARGET_POINTER_SIZE);

        slots      = 1;
        passedSize = genTypeSize(type);
        if (varTypeIsFloating(type))
        {
            canPassArgInRegisters = m_floatRegs.Count() > 0;
            if (!canPassArgInRegisters)
            {
                type                  = TYP_I_IMPL;
                canPassArgInRegisters = m_intRegs.Count() > 0;
            }
        }
        else
        {
            canPassArgInRegisters = m_intRegs.Count() > 0;
        }
    }

    ABIPassingInformation info;
    if (canPassArgInRegisters)
    {
        info = ABIPassingInformation(comp, slots);
        if (argRegTypeInStruct1 != TYP_UNKNOWN)
        {
            RegisterQueue* regs = varTypeIsFloating(argRegTypeInStruct1) ? &m_floatRegs : &m_intRegs;
            assert(regs->Count() > 0);

            passedSize      = genTypeSize(argRegTypeInStruct1);
            info.Segment(0) = ABIPassingSegment::InRegister(regs->Dequeue(), 0, passedSize);

            if (argRegTypeInStruct2 != TYP_UNKNOWN)
            {
                unsigned slotSize = genTypeSize(argRegTypeInStruct2);

                regs = varTypeIsFloating(argRegTypeInStruct2) ? &m_floatRegs : &m_intRegs;
                assert(regs->Count() > 0);

                passedSize      = max(passedSize, slotSize);
                info.Segment(1) = ABIPassingSegment::InRegister(regs->Dequeue(), passedSize, slotSize);
            }
        }
        else
        {
            RegisterQueue* regs     = varTypeIsFloating(type) ? &m_floatRegs : &m_intRegs;
            unsigned       slotSize = min(passedSize, (unsigned)TARGET_POINTER_SIZE);
            info.Segment(0)         = ABIPassingSegment::InRegister(regs->Dequeue(), 0, slotSize);
            if (slots == 2)
            {
                assert(varTypeIsStruct(type));
                assert(passedSize > TARGET_POINTER_SIZE);
                unsigned tailSize = passedSize - slotSize;
                if (m_intRegs.Count() > 0)
                {
                    info.Segment(1) = ABIPassingSegment::InRegister(m_intRegs.Dequeue(), slotSize, tailSize);
                }
                else
                {
                    assert(m_intRegs.Count() == 0);
                    assert(m_stackArgSize == 0);
                    info.Segment(1) = ABIPassingSegment::OnStack(0, TARGET_POINTER_SIZE, tailSize);
                    m_stackArgSize += TARGET_POINTER_SIZE;
                }
            }
        }
    }
    else
    {
        assert((m_stackArgSize % TARGET_POINTER_SIZE) == 0);

        info = ABIPassingInformation::FromSegment(comp, ABIPassingSegment::OnStack(m_stackArgSize, 0, passedSize));

        m_stackArgSize += roundUp(passedSize, TARGET_POINTER_SIZE);

        // As soon as we pass something on stack we cannot go back and
        // enregister something else.
        // The float had been cleared before and only integer type go here.
        m_intRegs.Clear();
    }

    return info;
}

#endif // TARGET_LOONGARCH64
