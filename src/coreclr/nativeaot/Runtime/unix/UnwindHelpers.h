// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

// This class is used to encapsulate the internals of our unwinding implementation
// and any custom versions of libunwind structures that we use for performance
// reasons.
class UnwindHelpers
{
public:
    static bool StepFrame(REGDISPLAY *regs);
};
