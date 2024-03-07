// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**************************************************************/
/*                       gmsAMD64.cpp                         */
/**************************************************************/

#include "common.h"
#include "gmscpu.h"

void LazyMachState::unwindLazyState(LazyMachState* baseState,
                                    MachState* unwoundState,
                                    DWORD threadId,
                                    int funCallDepth /* = 1 */,
                                    HostCallPreference hostCallPreference /* = (HostCallPreference)(-1) */)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    CONTEXT                         ctx;
    KNONVOLATILE_CONTEXT_POINTERS   nonVolRegPtrs;

    ctx.ContextFlags = 0; // Read by PAL_VirtualUnwind.

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

#ifndef TARGET_UNIX
        pvControlPc = Thread::VirtualUnwindCallFrame(&ctx, &nonVolRegPtrs);
#else // !TARGET_UNIX

#if defined(DACCESS_COMPILE)
        HRESULT hr = DacVirtualUnwind(threadId, &ctx, &nonVolRegPtrs);
        if (FAILED(hr))
        {
            DacError(hr);
        }
#else
        BOOL success = PAL_VirtualUnwind(&ctx, &nonVolRegPtrs);
        if (!success)
        {
            _ASSERTE(!"unwindLazyState: Unwinding failed");
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }
#endif  // DACCESS_COMPILE

        pvControlPc = GetIP(&ctx);
#endif // !TARGET_UNIX

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

#ifdef TARGET_UNIX
#define CALLEE_SAVED_REGISTER(regname) unwoundState->m_Unwound.regname = ctx.regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
#endif

#if defined(DACCESS_COMPILE)

    // For DAC, we have to update the registers directly, since we don't have context pointers.
#define CALLEE_SAVED_REGISTER(regname) unwoundState->m_Capture.regname = ctx.regname;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    // Since we don't have context pointers in this case, just assing them to NULL.
#define CALLEE_SAVED_REGISTER(regname) unwoundState->m_Ptrs.p##regname = NULL;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#else  // !DACCESS_COMPILE

#define CALLEE_SAVED_REGISTER(regname) unwoundState->m_Ptrs.p##regname = PTR_TADDR(nonVolRegPtrs.regname);
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

#endif // DACCESS_COMPILE
}
