// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <interpexec.h>

// Forward declaration
void ExecuteInterpretedMethodWithArgs(TADDR targetIp, int8_t* args, size_t argSize, void* retBuff, PCODE callerIp);

#define SPECIAL_ARG_ADDR(pos) (((int8_t*)pCallDescrData->pSrc) + ((pos)*INTERP_STACK_SLOT_SIZE))

extern "C" void STDCALL CallDescrWorkerInternal(CallDescrData* pCallDescrData)
{
    _ASSERTE(pCallDescrData != NULL);
    _ASSERTE(pCallDescrData->pTarget != (PCODE)NULL);

    // WASM-TODO: This path has a flaw. The DoPrestub call may trigger a GC, and there is no
    // explicit protection for the arguments. All platforms assume part of the call is
    // a no GC trigger region, but DoPrestub may trigger a GC. Therefore this needs to be
    // revisited to ensure correctness.

    MethodDesc* pMethod = PortableEntryPoint::GetMethodDesc(pCallDescrData->pTarget);
    InterpByteCodeStart* targetIp = pMethod->GetInterpreterCode();
    if (targetIp == NULL)
    {
        GCX_PREEMP();
        (void)pMethod->DoPrestub(NULL /* MethodTable */, CallerGCMode::Coop);
        targetIp = pMethod->GetInterpreterCode();
    }

    size_t argsSize = pCallDescrData->nArgsSize;
    if (pCallDescrData->hasRetBuff) {
        argsSize -= INTERP_STACK_SLOT_SIZE;
    }

    ExecuteInterpretedMethodWithArgs((TADDR)targetIp, SPECIAL_ARG_ADDR(pCallDescrData->hasRetBuff ? 1 : 0), argsSize, pCallDescrData->hasRetBuff ? *(int8_t**)SPECIAL_ARG_ADDR(0) : (int8_t*)pCallDescrData->returnValue, (PCODE)&CallDescrWorkerInternal);
}
