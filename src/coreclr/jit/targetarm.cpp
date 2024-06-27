// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_ARM)

#include "target.h"

const char*            Target::g_tgtCPUName           = "arm";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_R2L;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
const regNumber intArgRegs [] = {REG_R0, REG_R1, REG_R2, REG_R3};
const regMaskTP intArgMasks[] = {RBM_R0, RBM_R1, RBM_R2, RBM_R3};

const regNumber fltArgRegs [] = {REG_F0, REG_F1, REG_F2, REG_F3, REG_F4, REG_F5, REG_F6, REG_F7, REG_F8, REG_F9, REG_F10, REG_F11, REG_F12, REG_F13, REG_F14, REG_F15 };
const regMaskTP fltArgMasks[] = {RBM_F0, RBM_F1, RBM_F2, RBM_F3, RBM_F4, RBM_F5, RBM_F6, RBM_F7, RBM_F8, RBM_F9, RBM_F10, RBM_F11, RBM_F12, RBM_F13, RBM_F14, RBM_F15 };
// clang-format on

static_assert_no_msg(RBM_ALLDOUBLE == (RBM_ALLDOUBLE_HIGH >> 1));

//-----------------------------------------------------------------------------
// Arm32Classifier:
//   Construct a new instance of the arm32 ABI classifier.
//
// Parameters:
//   info - Info about the method being classified.
//
Arm32Classifier::Arm32Classifier(const ClassifierInfo& info)
    : m_info(info)
{
}

//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the arm32 ABI.
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
ABIPassingInformation Arm32Classifier::Classify(Compiler*    comp,
                                                var_types    type,
                                                ClassLayout* structLayout,
                                                WellKnownArg wellKnownParam)
{
    if (!comp->opts.compUseSoftFP)
    {
        if (varTypeIsStruct(type))
        {
            var_types hfaType = comp->GetHfaType(structLayout->GetClassHandle());

            if (hfaType != TYP_UNDEF)
            {
                unsigned slots = structLayout->GetSize() / genTypeSize(hfaType);
                return ClassifyFloat(comp, hfaType, slots);
            }
        }

        if (varTypeIsFloating(type))
        {
            return ClassifyFloat(comp, type, 1);
        }
    }

    unsigned alignment = 4;
    if ((type == TYP_LONG) || (type == TYP_DOUBLE) ||
        ((type == TYP_STRUCT) &&
         (comp->info.compCompHnd->getClassAlignmentRequirement(structLayout->GetClassHandle()) == 8)))
    {
        alignment    = 8;
        m_nextIntReg = roundUp(m_nextIntReg, 2);
    }

    unsigned size     = type == TYP_STRUCT ? structLayout->GetSize() : genTypeSize(type);
    unsigned numSlots = (size + 3) / 4;

    unsigned numInRegs  = min(numSlots, 4 - m_nextIntReg);
    bool     anyOnStack = numInRegs < numSlots;

    // If we already passed anything on stack (due to float args) then we
    // cannot split an arg.
    if ((numInRegs > 0) && anyOnStack && (m_stackArgSize != 0))
    {
        numInRegs = 0;
    }

    ABIPassingInformation info(comp, numInRegs + (anyOnStack ? 1 : 0));

    for (unsigned i = 0; i < numInRegs; i++)
    {
        unsigned endOffs = min((i + 1) * 4, size);
        info.Segment(i) =
            ABIPassingSegment::InRegister(static_cast<regNumber>(static_cast<unsigned>(REG_R0) + m_nextIntReg + i),
                                          i * 4, endOffs - (i * 4));
    }

    m_nextIntReg += numInRegs;

    if (anyOnStack)
    {
        m_stackArgSize          = roundUp(m_stackArgSize, alignment);
        unsigned stackSize      = size - (numInRegs * 4);
        info.Segment(numInRegs) = ABIPassingSegment::OnStack(m_stackArgSize, numInRegs * 4, stackSize);
        m_stackArgSize += roundUp(stackSize, 4);

        // As soon as any int arg goes on stack we cannot put anything else in
        // int registers. This situation can happen if an arg would normally be
        // split but wasn't because a float arg was already passed on stack.
        m_nextIntReg = 4;
    }

    return info;
}

//-----------------------------------------------------------------------------
// ClassifyFloat:
//   Classify a parameter that uses float registers.
//
// Parameters:
//   comp     - Compiler instance
//   type     - The type of the parameter
//   numElems - Number of elements for the parameter.
//
// Returns:
//   Classification information for the parameter.
//
// Remarks:
//   Float parameters can require multiple registers; the double registers are
//   overlaid on top of the float registers so that d0 = s0, s1, d1 = s2, s3
//   etc. This means that allocating a double register automatically makes the
//   two corresponding float registers unavailable.
//
//   The ABI also supports HFAs that similarly require multiple registers for
//   passing. When multiple registers are required for a single argument they
//   must always be allocated into consecutive float registers. However,
//   backfilling is allowed. For example, a signature like
//   Foo(float x, double y, float z) allocates x in REG_F0 = s0, y in REG_F2 =
//   d1, z in REG_F1 = s1.
//
ABIPassingInformation Arm32Classifier::ClassifyFloat(Compiler* comp, var_types type, unsigned numElems)
{
    assert((type == TYP_FLOAT) || (type == TYP_DOUBLE));

    unsigned numConsecutive = type == TYP_FLOAT ? numElems : (numElems * 2);

    // Find the first start index that has a consecutive run of
    // 'numConsecutive' bits set.
    unsigned startRegMask = m_floatRegs;
    for (unsigned i = 1; i < numConsecutive; i++)
    {
        startRegMask &= m_floatRegs >> i;
    }

    // Doubles can only start at even indices.
    if (type == TYP_DOUBLE)
    {
        startRegMask &= 0b0101010101010101;
    }

    if (startRegMask != 0)
    {
        unsigned startRegIndex = BitOperations::TrailingZeroCount(startRegMask);
        unsigned usedRegsMask  = ((1 << numConsecutive) - 1) << startRegIndex;
        // First consecutive run of numConsecutive bits start at startRegIndex
        assert((m_floatRegs & usedRegsMask) == usedRegsMask);

        m_floatRegs ^= usedRegsMask;
        ABIPassingInformation info(comp, numElems);
        unsigned              numRegsPerElem = type == TYP_FLOAT ? 1 : 2;
        for (unsigned i = 0; i < numElems; i++)
        {
            regNumber reg = static_cast<regNumber>(static_cast<unsigned>(REG_F0) + startRegIndex + i * numRegsPerElem);
            info.Segment(i) = ABIPassingSegment::InRegister(reg, i * genTypeSize(type), genTypeSize(type));
        }

        return info;
    }
    else
    {
        // As soon as any float arg goes on stack no other float arg can go in a register.
        m_floatRegs = 0;

        m_stackArgSize = roundUp(m_stackArgSize, genTypeSize(type));
        ABIPassingInformation info =
            ABIPassingInformation::FromSegment(comp, ABIPassingSegment::OnStack(m_stackArgSize, 0,
                                                                                numElems * genTypeSize(type)));
        m_stackArgSize += numElems * genTypeSize(type);

        return info;
    }
}

#endif // TARGET_ARM
