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

    // If this segment is passed on the stack then return the particular stack
    // offset, relative to the first stack argument's offset.
    unsigned GetStackOffset() const;

    static ABIPassingSegment InRegister(regNumber reg, unsigned offset, unsigned size);
    static ABIPassingSegment OnStack(unsigned stackOffset, unsigned offset, unsigned size);
};

struct ABIPassingInformation
{
    // The number of segments used to pass the value. Examples:
    // - On x86, TYP_LONG can be passed in two registers, resulting in two
    // register segments
    // - On SysV x64, structs can be passed in two registers, resulting in two
    // register segments
    // - On arm64/arm32, HFAs can be passed in up to four registers, giving
    // four register segments
    // - On arm32, structs can be split out over register and stack, giving
    // multiple register segments and a struct segment.
    // - On Windows x64, all parameters always belong into one stack slot or register,
    // and thus always have NumSegments == 1
    unsigned           NumSegments = 0;
    ABIPassingSegment* Segments    = nullptr;

    bool IsSplitAcrossRegistersAndStack() const;
};
