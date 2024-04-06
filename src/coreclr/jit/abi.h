// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

class ABIPassingSegment
{
    regNumber m_register    = REG_NA;
    unsigned  m_stackOffset = 0;

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
    // offset, relative to the first stack argument's offset.
    unsigned GetStackOffset() const;

    var_types GetRegisterStoreType() const;

    static ABIPassingSegment InRegister(regNumber reg, unsigned offset, unsigned size);
    static ABIPassingSegment OnStack(unsigned stackOffset, unsigned offset, unsigned size);
};

struct ABIPassingInformation
{
    // The number of segments used to pass the value. Examples:
    // - On SysV x64, structs can be passed in two registers, resulting in two
    // register segments
    // - On arm64/arm32, HFAs can be passed in up to four registers, giving
    // four register segments
    // - On arm32, structs can be split out over register and stack, giving
    // multiple register segments and a struct segment.
    // - On Windows x64, all parameters always fit into one stack slot or
    // register, and thus always have NumSegments == 1
    unsigned           NumSegments = 0;
    ABIPassingSegment* Segments    = nullptr;

    bool HasAnyRegisterSegment() const;
    bool HasAnyStackSegment() const;
    bool HasExactlyOneStackSegment() const;
    bool IsSplitAcrossRegistersAndStack() const;

    static ABIPassingInformation FromSegment(Compiler* comp, const ABIPassingSegment& segment);

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

    unsigned Count()
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
    RegisterQueue m_regs;
    unsigned      m_stackArgSize = 0;

public:
    X86Classifier(const ClassifierInfo& info);

    ABIPassingInformation Classify(Compiler*    comp,
                                   var_types    type,
                                   ClassLayout* structLayout,
                                   WellKnownArg wellKnownParam);
};

class WinX64Classifier
{
    RegisterQueue m_intRegs;
    RegisterQueue m_floatRegs;
    unsigned      m_stackArgSize = 0;

public:
    WinX64Classifier(const ClassifierInfo& info);

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

    ABIPassingInformation Classify(Compiler*    comp,
                                   var_types    type,
                                   ClassLayout* structLayout,
                                   WellKnownArg wellKnownParam);
};

#if defined(TARGET_X86)
typedef X86Classifier PlatformClassifier;
#elif defined(WINDOWS_AMD64_ABI)
typedef WinX64Classifier PlatformClassifier;
#elif defined(UNIX_AMD64_ABI)
typedef SysVX64Classifier PlatformClassifier;
#elif defined(TARGET_ARM64)
typedef Arm64Classifier PlatformClassifier;
#elif defined(TARGET_ARM)
typedef Arm32Classifier PlatformClassifier;
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

    ABIPassingInformation Classify(Compiler*    comp,
                                   var_types    type,
                                   ClassLayout* structLayout,
                                   WellKnownArg wellKnownParam);
};
#endif
