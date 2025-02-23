// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

class ClassLayout;
enum class WellKnownArg : unsigned;

class ABIPassingSegment
{
    regNumberSmall m_register        = REG_NA;
    bool           m_isFullStackSlot = true;
    unsigned       m_stackOffset     = 0;

public:
    bool IsPassedInRegister() const;
    bool IsPassedOnStack() const;

    // Start offset of the segment within the parameter/argument. For example, a struct like { int32_t x; uint64_t y }
    // may have two segments
    // 1. Register(Offset=0, Type=TYP_INT, Size=4, Register=REG_ESI)
    // 2. Register(Offset=8, Type=TYP_LONG, Size=8, Register=REG_EDI)
    // on some ABIs, where the size of the first segment is not sufficient to
    // compute the offset of the second.
    unsigned Offset = 0;
    // Size of the segment being passed.
    unsigned Size = 0;

    // If this segment is passed in a register, return the particular register.
    regNumber GetRegister() const;

    regMaskTP GetRegisterMask() const;

    // If this segment is passed on the stack then return the particular stack
    // offset, relative to the base of stack arguments.
    unsigned GetStackOffset() const;

    // Get the size of stack consumed. Normally this is 'Size' rounded up to
    // the pointer size, but for apple arm64 ABI some primitives do not consume
    // full stack slots.
    unsigned GetStackSize() const;

    var_types GetRegisterType() const;

    static ABIPassingSegment InRegister(regNumber reg, unsigned offset, unsigned size);
    static ABIPassingSegment OnStack(unsigned stackOffset, unsigned offset, unsigned size);
    static ABIPassingSegment OnStackWithoutConsumingFullSlot(unsigned stackOffset, unsigned offset, unsigned size);

#ifdef DEBUG
    void Dump() const;
#endif
};

class ABIPassingSegmentIterator
{
    const ABIPassingSegment* m_value;
public:
    explicit ABIPassingSegmentIterator(const ABIPassingSegment* value)
        : m_value(value)
    {
    }

    const ABIPassingSegment& operator*() const
    {
        return *m_value;
    }
    const ABIPassingSegment* operator->() const
    {
        return m_value;
    }

    ABIPassingSegmentIterator& operator++()
    {
        m_value++;
        return *this;
    }

    bool operator==(const ABIPassingSegmentIterator& other) const
    {
        return m_value == other.m_value;
    }

    bool operator!=(const ABIPassingSegmentIterator& other) const
    {
        return m_value != other.m_value;
    }
};

struct ABIPassingInformation
{
private:
    union
    {
        ABIPassingSegment* m_segments;
        ABIPassingSegment  m_singleSegment;
    };

    bool m_passedByRef = false;

public:
    // The number of segments used to pass the value. Examples:
    // - On SysV x64, structs can be passed in two registers, resulting in two
    // register segments
    // - On arm64/arm32, HFAs can be passed in up to four registers, giving
    // four register segments
    // - On arm32, structs can be split out over register and stack, giving
    // multiple register segments and a struct segment.
    // - On Windows x64, all parameters always fit into one stack slot or
    // register, and thus always have NumSegments == 1
    // - On loongarch64/riscv64, structs can be passed in two registers or
    // can be split out over register and stack, giving
    // multiple register segments and a struct segment.
    unsigned NumSegments = 0;

    ABIPassingInformation()
    {
    }

    ABIPassingInformation(Compiler* comp, unsigned numSegments);

    ABIPassingSegment&                      Segment(unsigned index);
    const ABIPassingSegment&                Segment(unsigned index) const;
    IteratorPair<ABIPassingSegmentIterator> Segments() const;

    bool     IsPassedByReference() const;
    bool     HasAnyRegisterSegment() const;
    bool     HasAnyFloatingRegisterSegment() const;
    bool     HasAnyStackSegment() const;
    bool     HasExactlyOneRegisterSegment() const;
    bool     HasExactlyOneStackSegment() const;
    bool     IsSplitAcrossRegistersAndStack() const;
    unsigned CountRegsAndStackSlots() const;
    unsigned StackBytesConsumed() const;

    static ABIPassingInformation FromSegment(Compiler* comp, bool passedByRef, const ABIPassingSegment& segment);
    static ABIPassingInformation FromSegmentByValue(Compiler* comp, const ABIPassingSegment& segment);
    static ABIPassingInformation FromSegments(Compiler*                comp,
                                              const ABIPassingSegment& firstSegment,
                                              const ABIPassingSegment& secondSegment);


#ifdef WINDOWS_AMD64_ABI
    static bool GetShadowSpaceCallerOffsetForReg(regNumber reg, int* offset);
#endif

#ifdef DEBUG
    void Dump() const;
#endif
};

class RegisterQueue
{
    const regNumber* m_regs;
    unsigned int     m_numRegs;
    unsigned int     m_index = 0;

public:
    RegisterQueue(const regNumber* regs, unsigned int numRegs)
        : m_regs(regs)
        , m_numRegs(numRegs)
    {
    }

    unsigned Count() const
    {
        return m_numRegs - m_index;
    }

    regNumber Dequeue();
    regNumber Peek();
    void      Clear();
};

struct ClassifierInfo
{
    CorInfoCallConvExtension CallConv   = CorInfoCallConvExtension::Managed;
    bool                     IsVarArgs  = false;
    bool                     HasThis    = false;
    bool                     HasRetBuff = false;
};

class X86Classifier
{
    const ClassifierInfo& m_info;
    RegisterQueue         m_regs;
    unsigned              m_stackArgSize = 0;

public:
    X86Classifier(const ClassifierInfo& info);

    unsigned StackSize()
    {
        return m_stackArgSize;
    }

    ABIPassingInformation Classify(Compiler*    comp,
                                   var_types    type,
                                   ClassLayout* structLayout,
                                   WellKnownArg wellKnownParam);
};

class WinX64Classifier
{
    RegisterQueue m_intRegs;
    RegisterQueue m_floatRegs;
    unsigned      m_stackArgSize = 32;

public:
    WinX64Classifier(const ClassifierInfo& info);

    unsigned StackSize()
    {
        return m_stackArgSize;
    }

    ABIPassingInformation Classify(Compiler*    comp,
                                   var_types    type,
                                   ClassLayout* structLayout,
                                   WellKnownArg wellKnownParam);
};

class SysVX64Classifier
{
    RegisterQueue m_intRegs;
    RegisterQueue m_floatRegs;
    unsigned      m_stackArgSize = 0;

public:
    SysVX64Classifier(const ClassifierInfo& info);

    unsigned StackSize()
    {
        return m_stackArgSize;
    }

    ABIPassingInformation Classify(Compiler*    comp,
                                   var_types    type,
                                   ClassLayout* structLayout,
                                   WellKnownArg wellKnownParam);
};

class Arm64Classifier
{
    const ClassifierInfo& m_info;
    RegisterQueue         m_intRegs;
    RegisterQueue         m_floatRegs;
    unsigned              m_stackArgSize = 0;

public:
    Arm64Classifier(const ClassifierInfo& info);

    unsigned StackSize()
    {
        return roundUp(m_stackArgSize, TARGET_POINTER_SIZE);
    }

    ABIPassingInformation Classify(Compiler*    comp,
                                   var_types    type,
                                   ClassLayout* structLayout,
                                   WellKnownArg wellKnownParam);
};

class Arm32Classifier
{
    const ClassifierInfo& m_info;
    // 4 int regs are available for parameters. This gives the index of the
    // next one.
    // A.k.a. "NCRN": Next Core Register Number
    unsigned m_nextIntReg = 0;
    // 16 float regs are available for parameters. We keep them as a mask as
    // they can be backfilled.
    unsigned m_floatRegs = 0xFFFF;
    // A.k.a. "NSAA": Next Stack Argument Address
    unsigned m_stackArgSize = 0;

    ABIPassingInformation ClassifyFloat(Compiler* comp, var_types type, unsigned elems);

public:
    Arm32Classifier(const ClassifierInfo& info);

    unsigned StackSize()
    {
        return m_stackArgSize;
    }

    ABIPassingInformation Classify(Compiler*    comp,
                                   var_types    type,
                                   ClassLayout* structLayout,
                                   WellKnownArg wellKnownParam);
};

class RiscV64Classifier
{
    const ClassifierInfo& m_info;
    RegisterQueue         m_intRegs;
    RegisterQueue         m_floatRegs;
    unsigned              m_stackArgSize = 0;

public:
    RiscV64Classifier(const ClassifierInfo& info);

    unsigned StackSize()
    {
        return m_stackArgSize;
    }

    ABIPassingInformation Classify(Compiler*    comp,
                                   var_types    type,
                                   ClassLayout* structLayout,
                                   WellKnownArg wellKnownParam);
};

class LoongArch64Classifier
{
    const ClassifierInfo& m_info;
    RegisterQueue         m_intRegs;
    RegisterQueue         m_floatRegs;
    unsigned              m_stackArgSize = 0;

public:
    LoongArch64Classifier(const ClassifierInfo& info);

    unsigned StackSize()
    {
        return m_stackArgSize;
    }

    ABIPassingInformation Classify(Compiler*    comp,
                                   var_types    type,
                                   ClassLayout* structLayout,
                                   WellKnownArg wellKnownParam);
};

struct ABIReturningSegment
{
    uint16_t       m_offset = 0;
    uint8_t        m_size   = 0;
    regNumberSmall m_reg    = REG_NA;

public:
    ABIReturningSegment();
    ABIReturningSegment(regNumber reg, unsigned offset, unsigned size);

    regNumber GetRegister() const;
    unsigned  GetOffset() const;
    unsigned  GetSize() const;
    var_types GetRegisterType() const;
};

class ABIReturningSegmentIterator
{
    const ABIReturningSegment* m_value;
public:
    explicit ABIReturningSegmentIterator(const ABIReturningSegment* value)
        : m_value(value)
    {
    }

    const ABIReturningSegment& operator*() const
    {
        return *m_value;
    }
    const ABIReturningSegment* operator->() const
    {
        return m_value;
    }

    ABIReturningSegmentIterator& operator++()
    {
        m_value++;
        return *this;
    }

    bool operator==(const ABIReturningSegmentIterator& other) const
    {
        return m_value == other.m_value;
    }

    bool operator!=(const ABIReturningSegmentIterator& other) const
    {
        return m_value != other.m_value;
    }
};

struct ABIReturningInformation
{
private:
    union
    {
        ABIReturningSegment* m_segments;
        ABIReturningSegment  m_inlineSegments[sizeof(ABIReturningSegment*) / sizeof(ABIReturningSegment)];
    };

    bool m_returnedInRetBuffer = false;

    ABIReturningInformation();
public:
    // Number of registers used to return the value. Values returned via return buffer
    // will have 0 here and UsesReturnBuffer() will return true.
    // TYP_VOID returns will have 0 and UsesReturnBuffer() will return false.
    unsigned NumRegisters = 0;

    ABIReturningInformation(Compiler* comp, unsigned numRegisters);

    bool UsesRetBuffer() const;

    ABIReturningSegment&                      Segment(unsigned index);
    const ABIReturningSegment&                Segment(unsigned index) const;
    IteratorPair<ABIReturningSegmentIterator> Segments() const;

    static ABIReturningInformation FromSegment(Compiler* comp, const ABIReturningSegment& segment);
    static ABIReturningInformation FromSegments(Compiler*                  comp,
                                                const ABIReturningSegment& firstSegment,
                                                const ABIReturningSegment& secondSegment);
    static ABIReturningInformation InRetBuffer();
    static ABIReturningInformation Void();
};

struct ReturnClassifierInfo
{
    CorInfoCallConvExtension CallConv = CorInfoCallConvExtension::Managed;
};

class X86ReturnClassifier
{
    const ReturnClassifierInfo& m_info;

public:
    X86ReturnClassifier(const ReturnClassifierInfo& info);

    ABIReturningInformation Classify(Compiler* comp, var_types type, ClassLayout* structLayout);
};

class WinX64ReturnClassifier
{
    const ReturnClassifierInfo& m_info;

public:
    WinX64ReturnClassifier(const ReturnClassifierInfo& info);

    ABIReturningInformation Classify(Compiler* comp, var_types type, ClassLayout* structLayout);
};

class SysVX64ReturnClassifier
{
    const ReturnClassifierInfo& m_info;

public:
    SysVX64ReturnClassifier(const ReturnClassifierInfo& info);

    ABIReturningInformation Classify(Compiler* comp, var_types type, ClassLayout* structLayout);
};

class Arm64ReturnClassifier
{
    const ReturnClassifierInfo& m_info;

public:
    Arm64ReturnClassifier(const ReturnClassifierInfo& info);

    ABIReturningInformation Classify(Compiler* comp, var_types type, ClassLayout* structLayout);
};

class Arm32ReturnClassifier
{
    const ReturnClassifierInfo& m_info;

public:
    Arm32ReturnClassifier(const ReturnClassifierInfo& info);

    ABIReturningInformation Classify(Compiler* comp, var_types type, ClassLayout* structLayout);
};

#if defined(TARGET_X86)
typedef X86Classifier       PlatformClassifier;
typedef X86ReturnClassifier PlatformReturnClassifier;
#elif defined(WINDOWS_AMD64_ABI)
typedef WinX64Classifier       PlatformClassifier;
typedef WinX64ReturnClassifier PlatformReturnClassifier;
#elif defined(UNIX_AMD64_ABI)
typedef SysVX64Classifier       PlatformClassifier;
typedef SysVX64ReturnClassifier PlatformReturnClassifier;
#elif defined(TARGET_ARM64)
typedef Arm64Classifier       PlatformClassifier;
typedef Arm64ReturnClassifier PlatformReturnClassifier;
#elif defined(TARGET_ARM)
typedef Arm32Classifier       PlatformClassifier;
typedef Arm32ReturnClassifier PlatformReturnClassifier;
#elif defined(TARGET_RISCV64)
typedef RiscV64Classifier PlatformClassifier;
#elif defined(TARGET_LOONGARCH64)
typedef LoongArch64Classifier PlatformClassifier;
#endif

#ifdef SWIFT_SUPPORT
class SwiftABIClassifier
{
    PlatformClassifier m_classifier;

public:
    SwiftABIClassifier(const ClassifierInfo& info)
        : m_classifier(info)
    {
    }

    unsigned StackSize()
    {
        return m_classifier.StackSize();
    }

    ABIPassingInformation Classify(Compiler*    comp,
                                   var_types    type,
                                   ClassLayout* structLayout,
                                   WellKnownArg wellKnownParam);

    static ABIPassingInformation ClassifyReturn(Compiler* comp, var_types type, ClassLayout* structLayout);
};
#endif
