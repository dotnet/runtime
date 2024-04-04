// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "abi.h"

//-----------------------------------------------------------------------------
// IsPassedInRegister:
//   Check if this segment is passed in a register.
//
// Return Value:
//   True if this is passed in a register.
//
bool ABIPassingSegment::IsPassedInRegister() const
{
    return m_register != REG_NA;
}

//-----------------------------------------------------------------------------
// IsPassedOnStack:
//   Check if this segment is passed on the stack.
//
// Return Value:
//   True if this is passed on the stack.
//
bool ABIPassingSegment::IsPassedOnStack() const
{
    return m_register == REG_NA;
}

//-----------------------------------------------------------------------------
// GetRegister:
//   Get the register that this segment is passed in.
//
// Return Value:
//   The register.
//
regNumber ABIPassingSegment::GetRegister() const
{
    assert(IsPassedInRegister());
    return m_register;
}

//-----------------------------------------------------------------------------
// GetStackOffset:
//   Get the stack offset where this segment is passed.
//
// Return Value:
//   Offset relative to the first stack argument.
//
unsigned ABIPassingSegment::GetStackOffset() const
{
    assert(IsPassedOnStack());
    return m_stackOffset;
}

//-----------------------------------------------------------------------------
// InRegister:
//   Create an ABIPassingSegment representing that a segment is passed in a
//   register.
//
// Parameters:
//   reg    - The register the segment is passed in
//   offset - The offset of the segment that is passed in the register
//   size   - The size of the segment passed in the register
//
// Return Value:
//   New instance of ABIPassingSegment.
//
ABIPassingSegment ABIPassingSegment::InRegister(regNumber reg, unsigned offset, unsigned size)
{
    assert(reg != REG_NA);
    ABIPassingSegment segment;
    segment.m_register    = reg;
    segment.m_stackOffset = 0;
    segment.Offset        = offset;
    segment.Size          = size;
    return segment;
}

//-----------------------------------------------------------------------------
// OnStack:
//   Create an ABIPassingSegment representing that a segment is passed on the
//   stack.
//
// Parameters:
//   stackOffset - Offset relative to the first stack parameter/argument
//   offset      - The offset of the segment that is passed in the register
//   size        - The size of the segment passed in the register
//
// Return Value:
//   New instance of ABIPassingSegment.
//
ABIPassingSegment ABIPassingSegment::OnStack(unsigned stackOffset, unsigned offset, unsigned size)
{
    ABIPassingSegment segment;
    segment.m_register    = REG_NA;
    segment.m_stackOffset = stackOffset;
    segment.Offset        = offset;
    segment.Size          = size;
    return segment;
}

//-----------------------------------------------------------------------------
// IsSplitAcrossRegistersAndStack:
//   Check if this ABIPassingInformation represents passing a value in both
//   registers and on stack.
//
// Return Value:
//   True if the value is passed in both registers and on stack.
//
bool ABIPassingInformation::IsSplitAcrossRegistersAndStack() const
{
    bool anyReg   = false;
    bool anyStack = false;
    for (unsigned i = 0; i < NumSegments; i++)
    {
        anyReg |= Segments[i].IsPassedInRegister();
        anyStack |= Segments[i].IsPassedOnStack();
    }
    return anyReg && anyStack;
}

//-----------------------------------------------------------------------------
// FromSegment:
//   Create ABIPassingInformation from a single segment.
//
// Parameters:
//   comp    - Compiler instance
//   segment - The single segment that represents the passing information
//
// Return Value:
//   An instance of ABIPassingInformation.
//
ABIPassingInformation ABIPassingInformation::FromSegment(Compiler* comp, const ABIPassingSegment& segment)
{
    ABIPassingInformation info;
    info.NumSegments = 1;
    info.Segments    = new (comp, CMK_ABI) ABIPassingSegment(segment);
    return info;
}

#ifdef DEBUG
//-----------------------------------------------------------------------------
// Dump:
//   Dump the ABIPassingInformation to stdout.
//
void ABIPassingInformation::Dump() const
{
    if (NumSegments != 1)
    {
        printf("%u segments\n", NumSegments);
    }

    for (unsigned i = 0; i < NumSegments; i++)
    {
        if (NumSegments > 1)
        {
            printf("  [%u] ", i);
        }

        const ABIPassingSegment& seg = Segments[i];

        if (Segments[i].IsPassedInRegister())
        {
            printf("[%02u..%02u) reg %s\n", seg.Offset, seg.Offset + seg.Size, getRegName(seg.GetRegister()));
        }
        else
        {
            printf("[%02u..%02u) stack @ +%02u\n", seg.Offset, seg.Offset + seg.Size, seg.GetStackOffset());
        }
    }
}
#endif

//-----------------------------------------------------------------------------
// RegisterQueue::Dequeue:
//   Dequeue a register from the queue.
//
// Return Value:
//   The dequeued register.
//
regNumber RegisterQueue::Dequeue()
{
    assert(Count() > 0);
    return static_cast<regNumber>(m_regs[m_index++]);
}

//-----------------------------------------------------------------------------
// RegisterQueue::Peek:
//   Peek at the head of the queue.
//
// Return Value:
//   The head register in the queue.
//
regNumber RegisterQueue::Peek()
{
    assert(Count() > 0);
    return static_cast<regNumber>(m_regs[m_index]);
}

//-----------------------------------------------------------------------------
// RegisterQueue::Clear:
//   Clear the register queue.
//
void RegisterQueue::Clear()
{
    m_index = m_numRegs;
}

#ifdef SWIFT_SUPPORT
//-----------------------------------------------------------------------------
// Classify:
//   Classify a parameter for the Swift ABI.
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
ABIPassingInformation SwiftABIClassifier::Classify(Compiler*    comp,
                                                   var_types    type,
                                                   ClassLayout* structLayout,
                                                   WellKnownArg wellKnownParam)
{
#ifdef TARGET_AMD64
    if (wellKnownParam == WellKnownArg::RetBuffer)
    {
        return ABIPassingInformation::FromSegment(comp, ABIPassingSegment::InRegister(REG_SWIFT_ARG_RET_BUFF, 0,
                                                                                      TARGET_POINTER_SIZE));
    }
#endif

    if (wellKnownParam == WellKnownArg::SwiftSelf)
    {
        return ABIPassingInformation::FromSegment(comp, ABIPassingSegment::InRegister(REG_SWIFT_SELF, 0,
                                                                                      TARGET_POINTER_SIZE));
    }

    return m_classifier.Classify(comp, type, structLayout, wellKnownParam);
}
#endif
