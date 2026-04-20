// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CommonTypes.h"
#include "CommonMacros.h"

extern "C" thread_local int RhpExceptionThrown = 0;

FCIMPL0(void, RhpThrowNativeException)
{
    RhpExceptionThrown = 1;
}
FCIMPLEND

FCIMPL0(void, RhpReleaseNativeException)
{
    ASSERT(RhpExceptionThrown == 0);
}
FCIMPLEND
