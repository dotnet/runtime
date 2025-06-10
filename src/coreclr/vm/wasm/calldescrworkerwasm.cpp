// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <interpexec.h>

extern "C" void* STDCALL ExecuteInterpretedMethodWithArgs(TransitionBlock* pTransitionBlock, TADDR byteCodeAddr, int8_t* pArgs, size_t size, void* retBuff);

extern "C" void STDCALL CallDescrWorkerInternal(CallDescrData * pCallDescrData)
{
    PCODE code = pCallDescrData->pMD->GetNativeCode();
    if (!code)
    {
        GCX_PREEMP();
        pCallDescrData->pMD->PrepareInitialCode(CallerGCMode::Coop);
        code = pCallDescrData->pMD->GetNativeCode();
    }

    ExecuteInterpretedMethodWithArgs(((TransitionBlock*)pCallDescrData->pSrc) - 1, code, (int8_t*)pCallDescrData->pSrc, pCallDescrData->nArgsSize, pCallDescrData->returnValue);
}
