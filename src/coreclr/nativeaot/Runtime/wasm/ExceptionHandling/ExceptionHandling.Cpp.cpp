// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CommonTypes.h"
#include "CommonMacros.h"

extern "C" void __cxa_end_catch();

FCIMPL0(void, RhpThrowNativeException)
{
    throw 0;
}
FCIMPLEND

FCIMPL0(void, RhpReleaseNativeException)
{
    __cxa_end_catch();
}
FCIMPLEND
