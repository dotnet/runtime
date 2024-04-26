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
// GetRegisterMask:
//   Get the mask of registers that this segment is passed in.
//
// Return Value:
//   The register mask.
//
regMaskTP ABIPassingSegment::GetRegisterMask() const
{
    assert(IsPassedInRegister());
    regMaskTP reg = genRegMask(m_register);

#ifdef TARGET_ARM
    if (genIsValidFloatReg(m_register) && (Size == 8))
    {
        reg |= genRegMask(REG_NEXT(m_register));
    }
#endif

    return reg;
}

//-----------------------------------------------------------------------------
// GetStackOffset:
//   Get the stack offset where this segment is passed.
//
// Return Value:
//   Offset relative to the first stack argument.
//
// Remarks:
//   On x86, where arguments are pushed in order and thus come in reverse order
//   in the callee, this is the offset to subtract from the top of the stack to
//   get the argument's address. By top of the stack is meant esp on entry + 4
//   for the return address + total size of stack arguments. In varargs methods
//   the varargs cookie contains the information required to allow the
//   computation of the total size of stack arguments.
//
//   Outside x86 this is the offset to add to the first argument's address.
//
unsigned ABIPassingSegment::GetStackOffset() const
{
    assert(IsPassedOnStack());
    return m_stackOffset;
}

//-----------------------------------------------------------------------------
// GetRegisterType:
//  Return the smallest type larger or equal to Size that most naturally
//  represents the register this segment is passed in.
//
// Return Value:
//   A type that matches ABIPassingSegment::Size and the register.
//
var_types ABIPassingSegment::GetRegisterType() const
{
    assert(IsPassedInRegister());
    if (genIsValidFloatReg(m_register))
    {
        switch (Size)
        {
            case 4:
                return TYP_FLOAT;
            case 8:
                return TYP_DOUBLE;
#ifdef FEATURE_SIMD
            case 16:
                return TYP_SIMD16;
#endif
            default:
                assert(!"Unexpected size for floating point register");
                return TYP_UNDEF;
        }
    }
    else
    {
        switch (Size)
        {
            case 1:
                return TYP_UBYTE;
            case 2:
                return TYP_USHORT;
            case 3:
            case 4:
                return TYP_INT;
#ifdef TARGET_64BIT
            case 5:
            case 6:
            case 7:
            case 8:
                return TYP_LONG;
#endif
            default:
                assert(!"Unexpected size for integer register");
                return TYP_UNDEF;
        }
    }
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
// HasAnyRegisterSegment:
//   Check if any part of this value is passed in a register.
//
// Return Value:
//   True if so.
//
bool ABIPassingInformation::HasAnyRegisterSegment() const
{
    for (unsigned i = 0; i < NumSegments; i++)
    {
        if (Segments[i].IsPassedInRegister())
        {
            return true;
        }
    }
    return false;
}

//-----------------------------------------------------------------------------
// HasAnyStackSegment:
//   Check if any part of this value is passed on the stack.
//
// Return Value:
//   True if so.
//
bool ABIPassingInformation::HasAnyStackSegment() const
{
    for (unsigned i = 0; i < NumSegments; i++)
    {
        if (Segments[i].IsPassedOnStack())
        {
            return true;
        }
    }
    return false;
}

//-----------------------------------------------------------------------------
// HasExactlyOneRegisterSegment:
//   Check if this value is passed as a single register segment.
//
// Return Value:
//   True if so.
//
bool ABIPassingInformation::HasExactlyOneRegisterSegment() const
{
    return (NumSegments == 1) && Segments[0].IsPassedInRegister();
}

//-----------------------------------------------------------------------------
// HasExactlyOneStackSegment:
//   Check if this value is passed as a single stack segment.
//
// Return Value:
//   True if so.
//
bool ABIPassingInformation::HasExactlyOneStackSegment() const
{
    return (NumSegments == 1) && Segments[0].IsPassedOnStack();
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
    return {1, new (comp, CMK_ABI) ABIPassingSegment(segment)};
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

    if (wellKnownParam == WellKnownArg::SwiftError)
    {
        // We aren't actually going to pass the SwiftError* parameter in REG_SWIFT_ERROR.
        // We won't be using this parameter at all, and shouldn't allocate registers/stack space for it,
        // as that will mess with other args.
        // Quirk: To work around the JIT for now, "pass" it in REG_SWIFT_ERROR,
        // and let CodeGen::genFnProlog handle the rest.
        return ABIPassingInformation::FromSegment(comp, ABIPassingSegment::InRegister(REG_SWIFT_ERROR, 0,
                                                                                      TARGET_POINTER_SIZE));
    }

    if (type == TYP_STRUCT)
    {
        const CORINFO_SWIFT_LOWERING* lowering = comp->GetSwiftLowering(structLayout->GetClassHandle());
        if (lowering->byReference)
        {
            return m_classifier.Classify(comp, TYP_I_IMPL, nullptr, WellKnownArg::None);
        }

        ArrayStack<ABIPassingSegment> segments(comp->getAllocator(CMK_ABI));
        for (unsigned i = 0; i < lowering->numLoweredElements; i++)
        {
            var_types             elemType = JITtype2varType(lowering->loweredElements[i]);
            ABIPassingInformation elemInfo = m_classifier.Classify(comp, elemType, nullptr, WellKnownArg::None);

            for (unsigned j = 0; j < elemInfo.NumSegments; j++)
            {
                ABIPassingSegment newSegment = elemInfo.Segments[j];
                newSegment.Offset += lowering->offsets[i];
                // Adjust the tail size if necessary; the lowered sequence can
                // pass the tail as a larger type than the tail size.
                newSegment.Size = min(newSegment.Size, structLayout->GetSize() - newSegment.Offset);
                segments.Push(newSegment);
            }
        }

        ABIPassingInformation result;
        result.NumSegments = static_cast<unsigned>(segments.Height());
        result.Segments    = new (comp, CMK_ABI) ABIPassingSegment[result.NumSegments];
        for (int i = 0; i < segments.Height(); i++)
        {
            result.Segments[i] = segments.Bottom(i);
        }

        return result;
    }

    return m_classifier.Classify(comp, type, structLayout, wellKnownParam);
}
#endif
