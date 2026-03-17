// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "../../shared/arm64/primitives.cpp"

#ifndef FEATURE_EMULATE_SINGLESTEP
// Check if single stepping is enabled.
bool IsSSFlagEnabled(DT_CONTEXT *pContext, Thread *)
{
    _ASSERTE(pContext != NULL);
    return (pContext->Cpsr & 0x00200000) != 0;
}
#endif // !FEATURE_EMULATE_SINGLESTEP
