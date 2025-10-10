// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// RuntimeExceptionKind.h
//

#ifndef __exkind_h__
#define __exkind_h__

#include <cstdint>

//==========================================================================
// Identifies exception kinds.
//==========================================================================
enum class ExKind : uint8_t
{
    None = 0,
    Throw = 1,
    HardwareFault = 2,
    KindMask = 3,

    RethrowFlag = 4,

    SupersededFlag = 8,

    InstructionFaultFlag = 0x10
};

#endif  // __exkind_h__
