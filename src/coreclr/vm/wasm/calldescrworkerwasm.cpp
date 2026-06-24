// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <interpexec.h>

// Forward declaration
void ExecuteInterpretedMethodWithArgs(TADDR targetIp, int8_t* args, size_t argSize, void* retBuff, PCODE callerIp);

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
    void* retBuff;
    int8_t* args = (int8_t*)pCallDescrData->pSrc;
    if (pCallDescrData->hasRetBuff)
    {
        retBuff = pCallDescrData->pRetBuffArg;
    }
    else
    {
        retBuff = &pCallDescrData->returnValue;
    }

    if (targetIp == NULL)
    {
        // The target has no interpreter code because it is R2R (native Wasm) compiled.
        // Dispatch to the native entry point via the interpreter-to-R2R thunk, mirroring
        // ExecuteInterpretedMethodWithArgs_PortableEntryPoint_Complex. Calling the
        // interpreter with a NULL target would not execute the method and would leave the
        // return buffer aliasing the incoming arguments.
        ManagedMethodParam param = { pMethod, args, (int8_t*)retBuff, (PCODE)NULL, NULL };
        InvokeManagedMethod(&param);
    }
    else
    {
        ExecuteInterpretedMethodWithArgs((TADDR)targetIp, args, argsSize, retBuff, (PCODE)&CallDescrWorkerInternal);
    }
}
