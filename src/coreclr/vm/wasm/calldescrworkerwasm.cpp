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
        // The target method has no interpreter code because it was compiled to native (R2R) code.
        // Invoke it as a compiled managed method through the interpreter->R2R thunk, mirroring the
        // fallback already present in ExecuteInterpretedMethodWithArgs_PortableEntryPoint_Complex and
        // the CALL_INTERP_METHOD path in InterpExecMethod. Without this, the NULL bytecode pointer
        // would be handed to the interpreter and dispatched as INTOP_INVALID.
        ManagedMethodParam param = { pMethod, args, (int8_t*)retBuff, (PCODE)NULL, nullptr };
        InvokeManagedMethod(&param);
        return;
    }

    ExecuteInterpretedMethodWithArgs((TADDR)targetIp, args, argsSize, retBuff, (PCODE)&CallDescrWorkerInternal);
}
