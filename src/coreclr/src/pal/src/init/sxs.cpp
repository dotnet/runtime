//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++





--*/

#include "pal/dbgmsg.h"
#include "pal/thread.hpp"
#include "../thread/procprivate.hpp"
#include "pal/module.h"
#include "pal/seh.hpp"

using namespace CorUnix;

#ifdef FEATURE_PAL_SXS

SET_DEFAULT_DEBUG_CHANNEL(SXS);

#if _DEBUG
/*++
Function:
  CheckPalThread

Abstract:
  This function is called by the ENTRY macro to validate consistency:
  Whenever a PAL function is called, that thread must have previously
  registered the fact that it is currently executing code that depends
  on this PAL by means of PAL_ReverseEnter or PAL_Enter.
--*/
extern "C" void CheckPalThread()
{
    if (PALIsInitialized())
    {
        CPalThread *pThread = InternalGetCurrentThread();
        if (!pThread)
        {
            ASSERT("PAL function called on a thread unknown to this PAL\n");
        }
        else if (!pThread->IsInPal())
        {
            // There are several outstanding issues where we are not maintaining
            // correct in- vs. out-of-thePAL state. With the advent of 
            // single registration of Mach EH handling per thread, there's no
            // need to actually be in the PAL any more, and so the following
            // is being made into a warning, and we'll deprecate the 
            // entire concept later.
            WARN("PAL function called on a thread external to this PAL\n");
        }
    }
}
#endif // _DEBUG


/*++
Function:
  PAL_IsSelf

Abstract:
  Returns TRUE iff the argument module corresponds to this PAL.
  In other words, clients should not call PAL_Leave when calling
  functions obtained from this module using GetProcAddress.
--*/
BOOL
PALAPI
PAL_IsSelf(HMODULE hModule)
{
    ENTRY("PAL_IsSelf(hModule=%p)\n", hModule);

    MODSTRUCT *module = (MODSTRUCT *) hModule;
    BOOL fIsSelf = (module->dl_handle == pal_module.dl_handle);

    LOGEXIT("PAL_IsSelf returns %d\n", fIsSelf);
    return fIsSelf;
}

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
    CPalThread *pThread = InternalGetCurrentThread();
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
            
        palError = CreateThreadData(&pThread);
        if (NO_ERROR != palError)
        {
            ERROR("Unable to create thread data: error %d\n", palError);
            goto EXIT;
        }

        HANDLE hThread;
        palError = CreateThreadObject(pThread, pThread, &hThread);
        if (NO_ERROR != palError)
        {
            ERROR("Unable to create thread object: error %d\n", palError);
            pthread_setspecific(thObjKey, NULL);
            pThread->ReleaseThreadReference();
            goto EXIT;
        }
        
        // Like CreateInitialProcessAndThreadObjects, we do not need this 
        // thread handle, since we're not returning it to anyone who will 
        // possibly release it.
        (void)g_pObjectManager->RevokeHandle(pThread, hThread);

        PROCAddThread(pThread, pThread);
    }

EXIT:
    LOGEXIT("PAL_Enter returns %d\n", palError);
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

    CPalThread *pThread = InternalGetCurrentThread();
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

    CPalThread *pThread = InternalGetCurrentThread();
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

    CPalThread *pThread = InternalGetCurrentThread();
    if (pThread == NULL)
    {
        ASSERT("PAL_ReenterForEH called on a thread unknown to this PAL\n");
    }
    else if (!pThread->IsInPal())
    {
#if !_NO_DEBUG_MESSAGES_                
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
    
    CPalThread *pThread = InternalGetCurrentThread();
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

#ifdef _DEBUG
void CPalThread::CheckGuard()
{
    _ASSERT_MSG(m_dwGuard == 0, "Guard is 0x%lX.\n", m_dwGuard);
}
#endif

#endif // FEATURE_PAL_SXS
