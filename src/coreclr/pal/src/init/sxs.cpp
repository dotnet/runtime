// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

SET_DEFAULT_DEBUG_CHANNEL(SXS);

PAL_ERROR AllocatePalThread(CPalThread **ppThread);

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
