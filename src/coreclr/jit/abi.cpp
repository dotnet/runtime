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
