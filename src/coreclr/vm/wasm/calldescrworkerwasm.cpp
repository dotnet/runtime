// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <interpexec.h>

extern "C" void* STDCALL ExecuteInterpretedMethodWithArgs(TransitionBlock* pTransitionBlock, TADDR byteCodeAddr, int8_t* pArgs, size_t size, void* retBuff);

extern "C" void STDCALL CallDescrWorkerInternal(CallDescrData* pCallDescrData)
{
    MethodDesc* pMethod = PortableEntryPoint::GetMethodDesc(pCallDescrData->pTarget);
    InterpByteCodeStart* targetIp = pMethod->GetInterpreterCode();
    if (targetIp == NULL)
    {
        GCX_PREEMP();
        (void)pMethod->DoPrestub(NULL /* MethodTable */, CallerGCMode::Coop);
        targetIp = pMethod->GetInterpreterCode();
    }

    TransitionBlock dummy{};
    ExecuteInterpretedMethodWithArgs(&dummy, (TADDR)targetIp, (int8_t*)pCallDescrData->pSrc, pCallDescrData->nArgsSize, pCallDescrData->returnValue);
}
