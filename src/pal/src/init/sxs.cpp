// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++





--*/

#include "pal/dbgmsg.h"
#include "pal/thread.hpp"
#include "../thread/procprivate.hpp"
#include "pal/module.h"
#include "pal/process.h"
#include "pal/seh.hpp"
#include "pal/signal.hpp"

using namespace CorUnix;

#ifdef FEATURE_PAL_SXS

SET_DEFAULT_DEBUG_CHANNEL(SXS);

PAL_ERROR AllocatePalThread(CPalThread **ppThread);

/************************* Enter *************************/

/*++
Function:
  PAL_Enter

Abstract:
  This function needs to be called on a thread when it enters
  a region of code that depends on this instance of the PAL
  in the process, and the current thread may or may not be
  known to the PAL.  This function can fail (for something else
  than an internal error) if this is the first time that the
  current thread entered this PAL.  Note that PAL_Initialize
  implies a call to this function.

  NOTE: This function must not modify LastError.
--*/
PAL_ERROR
PALAPI
PAL_Enter(PAL_Boundary boundary)
{
    ENTRY_EXTERNAL("PAL_Enter(boundary=%u)\n", boundary);

    PAL_ERROR palError = ERROR_SUCCESS;
    CPalThread *pThread = GetCurrentPalThread();
    if (pThread != NULL)
    {
        palError = pThread->Enter(boundary);
    }
    else
    {
        // If this assert fires, we'll have to pipe this information so that 
        // CPalThread's RunPostCreateInitializers call to SEHEnable 
        // can know what direction.
        _ASSERT_MSG(PAL_BoundaryTop == boundary, "How are we entering a PAL "
            "thread for the first time not from the top? (boundary=%u)", boundary);
            
        palError = AllocatePalThread(&pThread);
        if (NO_ERROR != palError)
        {
            ERROR("Unable to allocate pal thread: error %d\n", palError);
        }
    }

    LOGEXIT("PAL_Enter returns %d\n", palError);
    return palError;
}

/*++
Function:
  CreateCurrentThreadData

Abstract:
  This function is called by the InternalGetOrCreateCurrentThread inlined 
  function to create the thread data when it is null meaning the thread has
  never been in this PAL. 

Warning:
  If the allocation fails, this function asserts and exits the process.
--*/
extern "C" CPalThread *
CreateCurrentThreadData()
{
    CPalThread *pThread = NULL;

    if (PALIsThreadDataInitialized()) {
        PAL_ERROR palError = AllocatePalThread(&pThread);
        if (NO_ERROR != palError)
        {
            ASSERT("Unable to allocate pal thread: error %d - aborting\n", palError);
            PROCAbort();
        }
    }

    return pThread;
}

PAL_ERROR
AllocatePalThread(CPalThread **ppThread)
{
    CPalThread *pThread = NULL;
    PAL_ERROR palError;

    palError = CreateThreadData(&pThread);
    if (NO_ERROR != palError)
    {
        goto exit;
    }

#if !HAVE_MACH_EXCEPTIONS
    // Ensure alternate stack for SIGSEGV handling. Our SIGSEGV handler is set to
    // run on an alternate stack and the stack needs to be allocated per thread.
    if (!pThread->EnsureSignalAlternateStack())
    {
        ERROR("Cannot allocate alternate stack for SIGSEGV handler!\n");
        palError = ERROR_NOT_ENOUGH_MEMORY;
        goto exit;
    }
#endif // !HAVE_MACH_EXCEPTIONS

    HANDLE hThread;
    palError = CreateThreadObject(pThread, pThread, &hThread);
    if (NO_ERROR != palError)
    {
        pthread_setspecific(thObjKey, NULL);
        pThread->ReleaseThreadReference();
        goto exit;
    }
    
    // Like CreateInitialProcessAndThreadObjects, we do not need this 
    // thread handle, since we're not returning it to anyone who will 
    // possibly release it.
    (void)g_pObjectManager->RevokeHandle(pThread, hThread);

    PROCAddThread(pThread, pThread);

exit:
    *ppThread = pThread;
    return palError;
}

PALIMPORT
DWORD
PALAPI
PAL_EnterTop()
{
    return PAL_Enter(PAL_BoundaryTop);
}


/*++
Function:
  PAL_Reenter

Abstract:
  This function needs to be called on a thread when it enters
  a region of code that depends on this instance of the PAL
  in the process, and the current thread is already known to
  the PAL.

  NOTE: This function must not modify LastError.
--*/
VOID
PALAPI
PAL_Reenter(PAL_Boundary boundary)
{
    ENTRY_EXTERNAL("PAL_Reenter(boundary=%u)\n", boundary);

    CPalThread *pThread = GetCurrentPalThread();
    if (pThread == NULL)
    {
        ASSERT("PAL_Reenter called on a thread unknown to this PAL\n");
    }

    // We ignore the return code.  This call should only fail on internal
    // error, and we assert at the actual failure.
    pThread->Enter(boundary);

    LOGEXIT("PAL_Reenter returns\n");
}

/*++
Function:
  PAL_HasEntered

Abstract:
  This function can be called to determine if the thread has entered the
  PAL through PAL_Enter or related calls.
--*/
BOOL
PALAPI
PAL_HasEntered()
{
    ENTRY_EXTERNAL("PAL_HasEntered()\n");

    CPalThread *pThread = GetCurrentPalThread();
    if (pThread == NULL)
    {
        ASSERT("PAL_Reenter called on a thread unknown to this PAL\n");
    }

    LOGEXIT("PAL_HasEntered returned\n");
    
    return pThread->IsInPal();
}

/*++
Function:
  PAL_ReenterForEH

Abstract:
  This function needs to be called on a thread when it enters
  a region of code that depends on this instance of the PAL
  in the process, and it is unknown whether the current thread
  is already running in the PAL.  Returns TRUE if and only if
  the thread was not running in the PAL previously.

  NOTE: This function must not modify LastError.
--*/
BOOL
PALAPI
PAL_ReenterForEH()
{
    // Only trace if we actually reenter (otherwise, too verbose)
    // ENTRY_EXTERNAL("PAL_ReenterForEH()\n");
    // Thus we have to split up what ENTRY_EXTERNAL does.
    CHECK_STACK_ALIGN;

    BOOL fEntered = FALSE;

    CPalThread *pThread = GetCurrentPalThread();
    if (pThread == NULL)
    {
        ASSERT("PAL_ReenterForEH called on a thread unknown to this PAL\n");
    }
    else if (!pThread->IsInPal())
    {
#if _ENABLE_DEBUG_MESSAGES_
        DBG_PRINTF(DLI_ENTRY, defdbgchan, TRUE)("PAL_ReenterForEH()\n");
#endif

        // We ignore the return code.  This call should only fail on internal
        // error, and we assert at the actual failure.
        pThread->Enter(PAL_BoundaryEH);
        fEntered = TRUE;
        LOGEXIT("PAL_ReenterForEH returns TRUE\n");
    }
    else
    {
        // LOGEXIT("PAL_ReenterForEH returns FALSE\n");
    }

    return fEntered;
}

PAL_ERROR CPalThread::Enter(PAL_Boundary /* boundary */)
{
    if (m_fInPal)
    {
        WARN("Enter called on a thread that already runs in this PAL\n");
        return NO_ERROR;
    }
    m_fInPal = TRUE;

    return ERROR_SUCCESS;
}


/************************* Leave *************************/

/*++
Function:
  PAL_Leave

Abstract:
  This function needs to be called on a thread when it leaves a region
  of code that depends on this instance of the PAL in the process.

  NOTE: This function must not modify LastError.
--*/
VOID
PALAPI
PAL_Leave(PAL_Boundary boundary)
{
    ENTRY("PAL_Leave(boundary=%u)\n", boundary);
    
    CPalThread *pThread = GetCurrentPalThread();
    // We ignore the return code.  This call should only fail on internal
    // error, and we assert at the actual failure.
    pThread->Leave(boundary);

    LOGEXIT("PAL_Leave returns\n");
}

PALIMPORT
VOID
PALAPI
PAL_LeaveBottom()
{
    PAL_Leave(PAL_BoundaryBottom);
}

PALIMPORT
VOID
PALAPI
PAL_LeaveTop()
{
    PAL_Leave(PAL_BoundaryTop);
}


PAL_ERROR CPalThread::Leave(PAL_Boundary /* boundary */)
{
    if (!m_fInPal)
    {
        WARN("Leave called on a thread that is not running in this PAL\n");
        return ERROR_NOT_SUPPORTED;
    }

    PAL_ERROR palError = ERROR_SUCCESS;

    m_fInPal = FALSE;

    return palError;
}

#endif // FEATURE_PAL_SXS
