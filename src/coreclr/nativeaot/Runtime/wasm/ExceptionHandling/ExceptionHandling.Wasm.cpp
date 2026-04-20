// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CommonTypes.h"
#include "CommonMacros.h"

FCIMPL0(void, RhpThrowNativeException)
{
    __builtin_wasm_throw(/* CPP_EXCEPTION_TAG */ 0, nullptr);
}
FCIMPLEND

FCIMPL0(void, RhpReleaseNativeException)
{
}
FCIMPLEND
