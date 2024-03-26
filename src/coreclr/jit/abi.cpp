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

ABIPassingInformation ABIPassingInformation::FromSegment(Compiler* comp, const ABIPassingSegment& segment)
{
    ABIPassingInformation info;
    info.NumSegments = 1;
    info.Segments    = new (comp, CMK_ABI) ABIPassingSegment(segment);
    return info;
}

regNumber RegisterQueue::Dequeue()
{
    assert(Count() > 0);
    return static_cast<regNumber>(m_regs[m_index++]);
}

regNumber RegisterQueue::Peek()
{
    assert(Count() > 0);
    return static_cast<regNumber>(m_regs[m_index]);
}

void RegisterQueue::Clear()
{
    m_index = m_numRegs;
}

static unsigned TypeSize(var_types type, ClassLayout* structLayout)
{
    return type == TYP_STRUCT ? structLayout->GetSize() : genTypeSize(type);
}

#ifdef TARGET_X86
X86Classifier::X86Classifier(const ClassifierInfo& info) : m_regs(nullptr, 0)
{
    switch (info.CallConv)
    {
        case CorInfoCallConvExtension::Thiscall:
        {
            static const regNumberSmall thiscallRegs[] = {REG_ECX};
            m_regs                                     = RegisterQueue(thiscallRegs, ArrLen(thiscallRegs));
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
            static const regNumberSmall regs[]  = {REG_ECX, REG_EDX};
            unsigned                    numRegs = ArrLen(regs);
            if (info.IsVarArgs)
            {
                // In varargs methods we only enregister the this pointer or retbuff.
                numRegs = info.HasThis || info.HasRetBuff ? 1 : 0;
            }
            m_regs = RegisterQueue(regs, numRegs);
            break;
        }
    }
}

ABIPassingInformation X86Classifier::Classify(Compiler*    comp,
                                              var_types    type,
                                              ClassLayout* structLayout,
                                              WellKnownArg wellKnownParam)
{
    unsigned size     = TypeSize(type, structLayout);
    unsigned numSlots = (size + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE;

    bool canEnreg = false;
    if (m_regs.Count() >= numSlots)
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
        segment = ABIPassingSegment::OnStack(m_stackArgSize, 0, size);
        m_stackArgSize += roundUp(size, TARGET_POINTER_SIZE);
    }

    return ABIPassingInformation::FromSegment(comp, segment);
}
#endif

#ifdef WINDOWS_AMD64_ABI
static const regNumberSmall WinX64IntArgRegs[]   = {REG_RCX, REG_RDX, REG_R8, REG_R9};
static const regNumberSmall WinX64FloatArgRegs[] = {REG_XMM0, REG_XMM1, REG_XMM2, REG_XMM3};

WinX64Classifier::WinX64Classifier(const ClassifierInfo& info)
    : m_intRegs(WinX64IntArgRegs, ArrLen(WinX64IntArgRegs)), m_floatRegs(WinX64FloatArgRegs, ArrLen(WinX64FloatArgRegs))
{
}

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

    unsigned typeSize = TypeSize(type, structLayout);
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
#endif

#ifdef UNIX_AMD64_ABI
static const regNumberSmall SysVX64IntArgRegs[]   = {REG_EDI, REG_ESI, REG_EDX, REG_ECX, REG_R8, REG_R9};
static const regNumberSmall SysVX64FloatArgRegs[] = {REG_XMM0, REG_XMM1, REG_XMM2, REG_XMM3,
                                                     REG_XMM4, REG_XMM5, REG_XMM6, REG_XMM7};

SysVX64Classifier::SysVX64Classifier(const ClassifierInfo& info)
    : m_intRegs(SysVX64IntArgRegs, ArrLen(SysVX64IntArgRegs))
    , m_floatRegs(SysVX64FloatArgRegs, ArrLen(SysVX64FloatArgRegs))
{
}

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
            info.NumSegments = structDesc.eightByteCount;
            info.Segments    = new (comp, CMK_ABI) ABIPassingSegment[structDesc.eightByteCount];

            for (unsigned i = 0; i < structDesc.eightByteCount; i++)
            {
                regNumber reg = structDesc.IsIntegralSlot(i) ? m_intRegs.Dequeue() : m_floatRegs.Dequeue();
                info.Segments[i] =
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
        unsigned size = TypeSize(type, structLayout);
        info          = ABIPassingInformation::FromSegment(comp, ABIPassingSegment::OnStack(m_stackArgSize, 0, size));
        m_stackArgSize += roundUp(size, TARGET_POINTER_SIZE);
    }

    return info;
}
#endif

#ifdef TARGET_ARM64
static const regNumberSmall Arm64IntArgRegs[]   = {REG_R0, REG_R1, REG_R2, REG_R3, REG_R4, REG_R5, REG_R6, REG_R7};
static const regNumberSmall Arm64FloatArgRegs[] = {REG_V0, REG_V1, REG_V2, REG_V3, REG_V4, REG_V5, REG_V6, REG_V7};

Arm64Classifier::Arm64Classifier(const ClassifierInfo& info)
    : m_info(info)
    , m_intRegs(Arm64IntArgRegs, ArrLen(Arm64IntArgRegs))
    , m_floatRegs(Arm64FloatArgRegs, ArrLen(Arm64FloatArgRegs))
{
}

ABIPassingInformation Arm64Classifier::Classify(Compiler*    comp,
                                                var_types    type,
                                                ClassLayout* structLayout,
                                                WellKnownArg wellKnownParam)
{
    if ((wellKnownParam == WellKnownArg::RetBuffer) && hasFixedRetBuffReg(m_info.CallConv))
    {
        return ABIPassingInformation::FromSegment(comp, ABIPassingSegment::InRegister(REG_ARG_RET_BUFF, 0,
                                                                                      TARGET_POINTER_SIZE));
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
                info.NumSegments = slots;
                info.Segments    = new (comp, CMK_ABI) ABIPassingSegment[slots];

                for (unsigned i = 0; i < slots; i++)
                {
                    info.Segments[i] = ABIPassingSegment::InRegister(m_floatRegs.Dequeue(), i * elemSize, elemSize);
                }
            }
            else
            {
                unsigned alignment = compAppleArm64Abi() ? elemSize : TARGET_POINTER_SIZE;
                m_stackArgSize     = roundUp(m_stackArgSize, alignment);
                info = ABIPassingInformation::FromSegment(comp, ABIPassingSegment::OnStack(m_stackArgSize, 0,
                                                                                           structLayout->GetSize()));
                m_stackArgSize += roundUp(structLayout->GetSize(), TARGET_POINTER_SIZE);
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
        info.NumSegments = 2;
        info.Segments    = new (comp, CMK_ABI) ABIPassingSegment[2];
        info.Segments[0] = ABIPassingSegment::InRegister(m_intRegs.Dequeue(), 0, TARGET_POINTER_SIZE);
        info.Segments[1] = ABIPassingSegment::OnStack(m_stackArgSize, TARGET_POINTER_SIZE,
                                                      structLayout->GetSize() - TARGET_POINTER_SIZE);
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
            info.NumSegments  = slots;
            info.Segments     = new (comp, CMK_ABI) ABIPassingSegment[slots];
            unsigned slotSize = varTypeIsStruct(type) ? TARGET_POINTER_SIZE : genTypeSize(type);
            info.Segments[0]  = ABIPassingSegment::InRegister(regs->Dequeue(), 0, slotSize);
            if (slots == 2)
            {
                assert(varTypeIsStruct(type));
                unsigned tailSize = structLayout->GetSize() - slotSize;
                info.Segments[1]  = ABIPassingSegment::InRegister(regs->Dequeue(), slotSize, tailSize);
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

#ifdef SWIFT_SUPPORT
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
