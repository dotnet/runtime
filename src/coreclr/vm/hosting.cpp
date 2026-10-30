// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "common.h"
#include <minipal/time.h>

#include "mscoree.h"
#include "corhost.h"
#include "threads.h"

// Locking routines supplied by the EE to the other DLLs of the CLR.  In a _DEBUG
// build of the EE, we poison the Crst as a poor man's attempt to do some argument
// validation.
#define POISON_BITS 3

static inline CRITSEC_COOKIE CrstToCookie(Crst * pCrst) {
    LIMITED_METHOD_CONTRACT;

    _ASSERTE((((uintptr_t) pCrst) & POISON_BITS) == 0);
#ifdef _DEBUG
    if (pCrst)
    {
    pCrst = (Crst *) (((uintptr_t) pCrst) | POISON_BITS);
    }
#endif
    return (CRITSEC_COOKIE) pCrst;
}

static inline Crst *CookieToCrst(CRITSEC_COOKIE cookie) {
    LIMITED_METHOD_CONTRACT;

    _ASSERTE((((uintptr_t) cookie) & POISON_BITS) == POISON_BITS);
#ifdef _DEBUG
    cookie = (CRITSEC_COOKIE) (((uintptr_t) cookie) & ~POISON_BITS);
#endif
    return (Crst *) cookie;
}

CRITSEC_COOKIE ClrCreateCriticalSection(CrstType crstType, CrstFlags flags) {
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CRITSEC_COOKIE ret = NULL;

    EX_TRY
    {
    // This may be controversial, but seems like the correct discipline.  If the
    // EE has called out to any other DLL of the CLR in cooperative mode, we
    // arbitrarily force lock acquisition to occur in preemptive mode.  See our
    // treatment of AcquireLock below.
    //_ASSERTE((flags & (CRST_UNSAFE_COOPGC | CRST_UNSAFE_ANYMODE)) == 0);
        ret = CrstToCookie(new Crst(crstType, flags));
    }
    EX_CATCH
    {
    }
    EX_END_CATCH

    // Note: we'll return NULL if the create fails. That's a true NULL, not a poisoned NULL.
    return ret;
}

void ClrDeleteCriticalSection(CRITSEC_COOKIE cookie)
{
    CONTRACTL
    {
        NOTHROW;
        WRAPPER(GC_NOTRIGGER);
    }
    CONTRACTL_END;

    Crst *pCrst = CookieToCrst(cookie);
    _ASSERTE(pCrst);

    delete pCrst;
}

DEBUG_NOINLINE void ClrEnterCriticalSection(CRITSEC_COOKIE cookie) {

    // Entering a critical section has many different contracts
    // depending on the flags used to initialize the critical section.
    // See CrstBase::Enter() for the actual contract. It's much too
    // complex to repeat here.

    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
    }
    CONTRACTL_END;

    Crst *pCrst = CookieToCrst(cookie);
    _ASSERTE(pCrst);

    pCrst->Enter();
}

DEBUG_NOINLINE void ClrLeaveCriticalSection(CRITSEC_COOKIE cookie) noexcept
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Crst *pCrst = CookieToCrst(cookie);
    _ASSERTE(pCrst);

    pCrst->Leave();
}

#ifndef DACCESS_COMPILE

NOINLINE static void FailFastOnApiErrorWithHandle(const CHAR *apiName, HANDLE handle)
{
    WRAPPER_NO_CONTRACT;

    DWORD errorCode = GetLastError();
    CHAR messageUtf8[128] = "\0";
    sprintf_s(
        messageUtf8,
        ARRAY_SIZE(messageUtf8),
        "%s failed with error %u. Handle: 0x%p",
        apiName,
        errorCode,
        (void*)handle);

    MAKE_WIDEPTR_FROMUTF8_NOTHROW(message, messageUtf8);
    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, message);
}

#ifdef TARGET_WINDOWS
DWORD ClrSuspendThread(HANDLE hThread)
{
    WRAPPER_NO_CONTRACT;

    DWORD result = SuspendThread(hThread);
    if (result == (DWORD)-1)
    {
        FailFastOnApiErrorWithHandle("SuspendThread", hThread);
    }

    return result;
}
#endif // TARGET_WINDOWS

DWORD ClrResumeThread(HANDLE hThread)
{
    WRAPPER_NO_CONTRACT;

    DWORD result = ResumeThread(hThread);
    if (result == (DWORD)-1)
    {
        FailFastOnApiErrorWithHandle("ResumeThread", hThread);
    }

    return result;
}

#endif // !DACCESS_COMPILE
