//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/**************************************************************/
/*                       gmsAMD64.cpp                         */
/**************************************************************/

#include "common.h"
#include "gmscpu.h"

#if defined(DACCESS_COMPILE)
static BOOL DacReadAllAdapter(SIZE_T address, SIZE_T *value)
{
    HRESULT hr = DacReadAll((TADDR)address, (PVOID)value, sizeof(*value), false);
    return SUCCEEDED(hr);
}
#endif //DACCESS_COMPILE

void LazyMachState::unwindLazyState(LazyMachState* baseState,
                                    MachState* unwoundState,
                                    int funCallDepth /* = 1 */,
                                    HostCallPreference hostCallPreference /* = (HostCallPreference)(-1) */)
{
    CONTRACTL 
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    CONTEXT                         ctx;
    KNONVOLATILE_CONTEXT_POINTERS   nonVolRegPtrs;

    ctx.Rip = baseState->m_CaptureRip;
    ctx.Rsp = baseState->m_CaptureRsp + 8; // +8 for return addr pushed before calling LazyMachStateCaptureState
    
#define CALLEE_SAVED_REGISTER(regname) ctx.regname = unwoundState->m_Capture.regname = baseState->m_Capture.regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#if !defined(DACCESS_COMPILE)

    // For DAC, if we get here, it means that the LazyMachState is uninitialized and we have to unwind it.
    // The API we use to unwind in DAC is StackWalk64(), which does not support the context pointers.
#define CALLEE_SAVED_REGISTER(regname) nonVolRegPtrs.regname = (PDWORD64)&unwoundState->m_Capture.regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#endif // !DACCESS_COMPILE

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    LazyMachState::unwindLazyState(ip:%p,sp:%p)\n", baseState->m_CaptureRip, baseState->m_CaptureRsp));

    PCODE pvControlPc;
    
    do 
    {

#ifndef FEATURE_PAL
        pvControlPc = Thread::VirtualUnwindCallFrame(&ctx, &nonVolRegPtrs);
#else // !FEATURE_PAL
        
#if defined(DACCESS_COMPILE)
        DWORD pid;
        HRESULT hr = DacGetPid(&pid);
        if (SUCCEEDED(hr))
        {
            if (!PAL_VirtualUnwindOutOfProc(&ctx, &nonVolRegPtrs, pid, DacReadAllAdapter))
            {
                DacError(E_FAIL);   
            }
        } 
        else 
        {
            DacError(hr);
        }
#else
        PAL_VirtualUnwind(&ctx, &nonVolRegPtrs);
#endif  // DACCESS_COMPILE    

        pvControlPc = GetIP(&ctx);
#endif // !FEATURE_PAL

        if (funCallDepth > 0)
        {
            --funCallDepth;
            if (funCallDepth == 0)
                break;
        }
        else
        {
            // Determine  whether given IP resides in JITted code. (It returns nonzero in that case.) 
            // Use it now to see if we've unwound to managed code yet.
            BOOL fFailedReaderLock = FALSE;
            BOOL fIsManagedCode = ExecutionManager::IsManagedCode(pvControlPc, hostCallPreference, &fFailedReaderLock);            
            if (fFailedReaderLock)
            {
                // We don't know if we would have been able to find a JIT
                // manager, because we couldn't enter the reader lock without
                // yielding (and our caller doesn't want us to yield).  So abort
                // now.
                
                // Invalidate the lazyState we're returning, so the caller knows
                // we aborted before we could fully unwind
                unwoundState->_pRetAddr = NULL;                
                return;
            }

            if (fIsManagedCode)
                break;
        }    
    }
    while(TRUE);

    //
    // Update unwoundState so that HelperMethodFrameRestoreState knows which
    // registers have been potentially modified.  
    //

    unwoundState->m_Rip = ctx.Rip;
    unwoundState->m_Rsp = ctx.Rsp;

    // For DAC, the return value of this function may be used after unwoundState goes out of scope. so we cannot do
    // "unwoundState->_pRetAddr = PTR_TADDR(&unwoundState->m_Rip)".
    unwoundState->_pRetAddr = PTR_TADDR(unwoundState->m_Rsp - 8);

#ifdef FEATURE_PAL
#define CALLEE_SAVED_REGISTER(regname) unwoundState->m_Unwound.regname = ctx.regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
#endif

#if defined(DACCESS_COMPILE)

    // For DAC, we have to update the registers directly, since we don't have context pointers.
#define CALLEE_SAVED_REGISTER(regname) unwoundState->m_Capture.regname = ctx.regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#else  // !DACCESS_COMPILE

#define CALLEE_SAVED_REGISTER(regname) unwoundState->m_Ptrs.p##regname = PTR_ULONG64(nonVolRegPtrs.regname);
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#endif // DACCESS_COMPILE 
}
