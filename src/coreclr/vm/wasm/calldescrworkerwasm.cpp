// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <interpexec.h>

// Forward declaration
void ExecuteInterpretedMethodWithArgs(TADDR targetIp, int8_t* args, size_t argSize, void* retBuff, PCODE callerIp);

static void RunPrestub(MethodDesc* pMethod)
{
    GCX_PREEMP();
    (void)pMethod->DoPrestub(NULL /* MethodTable */, CallerGCMode::Coop);
}

extern "C" void STDCALL CallDescrWorkerInternal(CallDescrData* pCallDescrData)
{
    _ASSERTE(pCallDescrData != NULL);
    _ASSERTE(pCallDescrData->pTarget != (PCODE)NULL);

    MethodDesc* pMethod = PortableEntryPoint::GetMethodDesc(pCallDescrData->pTarget);
    InterpByteCodeStart* targetIp = pMethod->GetInterpreterCode();
    if (targetIp == NULL && pMethod->ShouldCallPrestub())
    {
        // DoPrestub may trigger a GC. Report the arguments through a PrestubMethodFrame, as the
        // prestub does for calls from managed code. DispatchCallSimple provides no TransitionBlock,
        // so its arguments are not reported here.
        TransitionBlock* pTransitionBlock = pCallDescrData->pTransitionBlock;
        if (pTransitionBlock != NULL)
        {
            pTransitionBlock->m_ReturnAddress = (TADDR)&CallDescrWorkerInternal;
            pTransitionBlock->m_StackPointer = 0;

            Thread* pThread = GetThread();
            PrestubMethodFrame frame(pTransitionBlock, pMethod);
            frame.Push(pThread);
            RunPrestub(pMethod);
            frame.Pop(pThread);
        }
        else
        {
            RunPrestub(pMethod);
        }
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
        InvokeManagedMethod(pMethod, args, (int8_t*)retBuff, (PCODE)NULL, nullptr);
        return;
    }

    ExecuteInterpretedMethodWithArgs((TADDR)targetIp, args, argsSize, retBuff, (PCODE)&CallDescrWorkerInternal);
}
