#include <interpexec.h>

extern "C" void STDCALL ExecuteInterpretedMethodWithArgs(TransitionBlock* pTransitionBlock, TADDR byteCodeAddr, int8_t* pArgs, size_t size, int8_t* pReturnValue, size_t returnValueSize);

extern "C" void STDCALL CallDescrWorkerInternal(CallDescrData * pCallDescrData)
{
    PCODE code = pCallDescrData->pMD->GetNativeCode();
    if (!code)
    {
        GCX_PREEMP();
        pCallDescrData->pMD->PrepareInitialCode(CallerGCMode::Coop);
        code = pCallDescrData->pMD->GetNativeCode();
    }

    ExecuteInterpretedMethodWithArgs(((TransitionBlock*)pCallDescrData->pSrc) - 1, code, (int8_t*)pCallDescrData->pSrc, pCallDescrData->nArgsSize, (int8_t*)pCallDescrData->returnValue, sizeof(pCallDescrData->returnValue));
}
