// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <interpexec.h>

// Forward declaration
void ExecuteInterpretedMethodWithArgs(TADDR targetIp, int8_t* args, size_t argSize, void* retBuff, PCODE callerIp);

// Mirrors the exception handling of PreStubWorker for a call from CallDescrWorkerInternal: exceptions are
// rethrown to the native caller, and the called method is added to the exception's stack trace. pPrepareMethod is
// the method to prepare, which differs from pMethod when pMethod's interpreter code belongs to another method.
// A method waiting for its R2R code to become available only needs to be activated.
static void RunPrestub(MethodDesc* pMethod, MethodDesc* pPrepareMethod, bool activateDelayedR2RCode, UINT_PTR stackTraceSP)
{
    EX_TRY
    {
        INSTALL_MANAGED_EXCEPTION_DISPATCHER_EX;
        INSTALL_UNWIND_AND_CONTINUE_HANDLER_EX;

        {
            GCX_PREEMP();
#ifdef FEATURE_AUXILIARY_R2R_MODULES
            if (activateDelayedR2RCode)
            {
                pPrepareMethod->TryActivateDelayedR2RCode(CallerGCMode::Coop);
            }
            else
#endif // FEATURE_AUXILIARY_R2R_MODULES
            {
                (void)pPrepareMethod->DoPrestub(NULL /* MethodTable */, CallerGCMode::Coop);
            }
        }

        UNINSTALL_UNWIND_AND_CONTINUE_HANDLER_EX(true /* nativeRethrow */);
        UNINSTALL_MANAGED_EXCEPTION_DISPATCHER_EX(true /* nativeRethrow */);
    }
    EX_CATCH
    {
        OBJECTHANDLE ohThrowable = GetThread()->LastThrownObjectHandle();
        if (ohThrowable != NULL)
        {
            StackTraceInfo::AppendElement(ObjectFromHandle(ohThrowable), 0, stackTraceSP, pMethod, NULL);
        }
        EX_RETHROW;
    }
    EX_END_CATCH
}

extern "C" void STDCALL CallDescrWorkerInternal(CallDescrData* pCallDescrData)
{
    _ASSERTE(pCallDescrData != NULL);
    _ASSERTE(pCallDescrData->pTarget != (PCODE)NULL);

    MethodDesc* pMethod = PortableEntryPoint::GetMethodDesc(pCallDescrData->pTarget);
    // The method whose code runs. It differs from pMethod when the interpreter code cached on pMethod belongs to
    // another method, such as the MethodImpl target of a declaration, which is also what activation applies to.
    MethodDesc* pTargetMethod = pMethod;
    InterpByteCodeStart* targetIp = pMethod->GetInterpreterCode();
    bool activateDelayedR2RCode = false;
    if (targetIp != NULL)
    {
        pTargetMethod = targetIp->Method->methodHnd;
#ifdef FEATURE_AUXILIARY_R2R_MODULES
        activateDelayedR2RCode = targetIp->Method->auxiliaryMethodInfo.ShouldTryActivateMethod();
#endif // FEATURE_AUXILIARY_R2R_MODULES
    }

    if ((targetIp == NULL && pMethod->ShouldCallPrestub()) || activateDelayedR2RCode)
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
            RunPrestub(pMethod, pTargetMethod, activateDelayedR2RCode, (UINT_PTR)pTransitionBlock);
            frame.Pop(pThread);
        }
        else
        {
            RunPrestub(pMethod, pTargetMethod, activateDelayedR2RCode, (UINT_PTR)pCallDescrData->pSrc);
        }
        targetIp = pTargetMethod->GetInterpreterCode();
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
        InvokeManagedMethod(pTargetMethod, args, (int8_t*)retBuff, (PCODE)NULL, nullptr);
        return;
    }

    ExecuteInterpretedMethodWithArgs((TADDR)targetIp, args, argsSize, retBuff, (PCODE)&CallDescrWorkerInternal);
}
