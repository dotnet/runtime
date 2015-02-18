//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/**************************************************************/
/*                       gmsAMD64.cpp                         */
/**************************************************************/

#include "common.h"
#include "gmscpu.h"

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
    
#ifndef UNIX_AMD64_ABI
    ctx.Rdi = unwoundState->m_CaptureRdi = baseState->m_CaptureRdi;
    ctx.Rsi = unwoundState->m_CaptureRsi = baseState->m_CaptureRsi;
#endif
    ctx.Rbx = unwoundState->m_CaptureRbx = baseState->m_CaptureRbx;
    ctx.Rbp = unwoundState->m_CaptureRbp = baseState->m_CaptureRbp;
    ctx.R12 = unwoundState->m_CaptureR12 = baseState->m_CaptureR12;
    ctx.R13 = unwoundState->m_CaptureR13 = baseState->m_CaptureR13;
    ctx.R14 = unwoundState->m_CaptureR14 = baseState->m_CaptureR14;
    ctx.R15 = unwoundState->m_CaptureR15 = baseState->m_CaptureR15;
    
#if !defined(DACCESS_COMPILE)
    // For DAC, if we get here, it means that the LazyMachState is uninitialized and we have to unwind it.
    // The API we use to unwind in DAC is StackWalk64(), which does not support the context pointers.
#ifndef UNIX_AMD64_ABI
    nonVolRegPtrs.Rdi = &unwoundState->m_CaptureRdi;
    nonVolRegPtrs.Rsi = &unwoundState->m_CaptureRsi;
#endif
    nonVolRegPtrs.Rbx = &unwoundState->m_CaptureRbx;
    nonVolRegPtrs.Rbp = &unwoundState->m_CaptureRbp;
    nonVolRegPtrs.R12 = &unwoundState->m_CaptureR12;
    nonVolRegPtrs.R13 = &unwoundState->m_CaptureR13;
    nonVolRegPtrs.R14 = &unwoundState->m_CaptureR14;
    nonVolRegPtrs.R15 = &unwoundState->m_CaptureR15;
#endif // !DACCESS_COMPILE

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    LazyMachState::unwindLazyState(ip:%p,sp:%p)\n", baseState->m_CaptureRip, baseState->m_CaptureRsp));

    PCODE pvControlPc;
    
    do 
    {

#ifndef FEATURE_PAL
        pvControlPc = Thread::VirtualUnwindCallFrame(&ctx, &nonVolRegPtrs);
#else // !FEATURE_PAL
        PAL_VirtualUnwind(&ctx, &nonVolRegPtrs);
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
    
#if defined(DACCESS_COMPILE)
    // For DAC, we have to update the registers directly, since we don't have context pointers.
#ifndef UNIX_AMD64_ABI
    unwoundState->m_CaptureRdi = ctx.Rdi;
    unwoundState->m_CaptureRsi = ctx.Rsi;
#endif
    unwoundState->m_CaptureRbx = ctx.Rbx;
    unwoundState->m_CaptureRbp = ctx.Rbp;
    unwoundState->m_CaptureR12 = ctx.R12;
    unwoundState->m_CaptureR13 = ctx.R13;
    unwoundState->m_CaptureR14 = ctx.R14;
    unwoundState->m_CaptureR15 = ctx.R15;

#else  // !DACCESS_COMPILE 
#ifndef UNIX_AMD64_ABI
    unwoundState->m_pRdi = PTR_ULONG64(nonVolRegPtrs.Rdi);
    unwoundState->m_pRsi = PTR_ULONG64(nonVolRegPtrs.Rsi);
#endif
    unwoundState->m_pRbx = PTR_ULONG64(nonVolRegPtrs.Rbx);
    unwoundState->m_pRbp = PTR_ULONG64(nonVolRegPtrs.Rbp);
    unwoundState->m_pR12 = PTR_ULONG64(nonVolRegPtrs.R12);
    unwoundState->m_pR13 = PTR_ULONG64(nonVolRegPtrs.R13);
    unwoundState->m_pR14 = PTR_ULONG64(nonVolRegPtrs.R14);
    unwoundState->m_pR15 = PTR_ULONG64(nonVolRegPtrs.R15);
#endif // DACCESS_COMPILE 
}
