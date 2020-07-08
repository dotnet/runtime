// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    handleapi.cpp

Abstract:

    Implementation of the handle management APIs



--*/

#include "pal/handleapi.hpp"
#include "pal/handlemgr.hpp"
#include "pal/thread.hpp"
#include "pal/procobj.hpp"
#include "pal/dbgmsg.h"
#include "pal/process.h"

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(HANDLE);

CAllowedObjectTypes aotDuplicateHandle(TRUE);

PAL_ERROR
CloseSpecialHandle(
    HANDLE hObject
    );

/*++
Function:
  DuplicateHandle

See MSDN doc.

PAL-specific behavior :
    -Source and Target process needs to be the current process.
    -lpTargetHandle must be non-NULL
    -dwDesiredAccess is ignored
    -bInheritHandle must be FALSE
    -dwOptions must be a combo of DUPLICATE_SAME_ACCESS and
               DUPLICATE_CLOSE_SOURCE

--*/
BOOL
PALAPI
DuplicateHandle(
        IN HANDLE hSourceProcessHandle,
        IN HANDLE hSourceHandle,
        IN HANDLE hTargetProcessHandle,
        OUT LPHANDLE lpTargetHandle,
        IN DWORD dwDesiredAccess,
        IN BOOL bInheritHandle,
        IN DWORD dwOptions)
{
    PAL_ERROR palError;
    CPalThread *pThread;

    PERF_ENTRY(DuplicateHandle);
    ENTRY("DuplicateHandle( hSrcProcHandle=%p, hSrcHandle=%p, "
          "hTargetProcHandle=%p, lpTargetHandle=%p, dwAccess=%#x, "
          "bInheritHandle=%d, dwOptions=%#x) \n", hSourceProcessHandle,
          hSourceHandle, hTargetProcessHandle, lpTargetHandle,
          dwDesiredAccess, bInheritHandle, dwOptions);

    pThread = InternalGetCurrentThread();

    palError = InternalDuplicateHandle(
        pThread,
        hSourceProcessHandle,
        hSourceHandle,
        hTargetProcessHandle,
        lpTargetHandle,
        bInheritHandle,
        dwOptions
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("DuplicateHandle returns BOOL %d\n", (NO_ERROR == palError));
    PERF_EXIT(DuplicateHandle);
    return (NO_ERROR == palError);
}

PAL_ERROR
CorUnix::InternalDuplicateHandle(
    CPalThread *pThread,
    HANDLE hSourceProcess,
    HANDLE hSource,
    HANDLE hTargetProcess,
    LPHANDLE phDuplicate,
    BOOL bInheritHandle,
    DWORD dwOptions
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjSource = NULL;

    DWORD source_process_id;
    DWORD target_process_id;
    DWORD cur_process_id;

    cur_process_id = GetCurrentProcessId();
    source_process_id = PROCGetProcessIDFromHandle(hSourceProcess);
    target_process_id = PROCGetProcessIDFromHandle(hTargetProcess);

    /* Check validity of process handles */
    if (0 == source_process_id || 0 == target_process_id)
    {
        ASSERT("Can't duplicate handle: invalid source or destination process");
        palError = ERROR_INVALID_PARAMETER;
        goto InternalDuplicateHandleExit;
    }

    /* At least source or target process should be the current process. */
    if (source_process_id != cur_process_id
        && target_process_id != cur_process_id)
    {
        ASSERT("Can't duplicate handle : neither source or destination"
               "processes are from current process");
        palError = ERROR_INVALID_PARAMETER;
        goto InternalDuplicateHandleExit;
    }

    if (FALSE != bInheritHandle)
    {
        ASSERT("Can't duplicate handle : bInheritHandle is not FALSE.\n");
        palError = ERROR_INVALID_PARAMETER;
        goto InternalDuplicateHandleExit;
    }

    if (dwOptions & ~(DUPLICATE_SAME_ACCESS | DUPLICATE_CLOSE_SOURCE))
    {
        ASSERT(
            "Can't duplicate handle : dwOptions is %#x which is not "
            "a subset of (DUPLICATE_SAME_ACCESS|DUPLICATE_CLOSE_SOURCE) "
            "(%#x).\n",
            dwOptions,
            DUPLICATE_SAME_ACCESS | DUPLICATE_CLOSE_SOURCE);
        palError = ERROR_INVALID_PARAMETER;
        goto InternalDuplicateHandleExit;
    }

    if (0 == (dwOptions & DUPLICATE_SAME_ACCESS))
    {
        ASSERT(
            "Can't duplicate handle : dwOptions is %#x which does not "
            "include DUPLICATE_SAME_ACCESS (%#x).\n",
            dwOptions,
            DUPLICATE_SAME_ACCESS);
        palError = ERROR_INVALID_PARAMETER;
        goto InternalDuplicateHandleExit;
    }

    if (NULL == phDuplicate)
    {
        ASSERT("Can't duplicate handle : lpTargetHandle is NULL.\n");
        goto InternalDuplicateHandleExit;
    }

    /* Since handles can be remoted to others processes using PAL_LocalHsndleToRemote
       and PAL_RemoteHandleToLocal, DuplicateHandle needs some special handling
       when this scenario occurs.

       if hSourceProcessHandle is from another process OR
       hTargetProcessHandle is from another process but both aren't
       ( handled above ) return hSourceHandle.
    */
    if (source_process_id != cur_process_id
        || target_process_id != cur_process_id)
    {
        *phDuplicate = hSource;
        palError = NO_ERROR;
        goto InternalDuplicateHandleExit;
    }

    //
    // Obtain the source IPalObject
    //

    if (!HandleIsSpecial(hSource))
    {
        palError = g_pObjectManager->ReferenceObjectByHandle(
            pThread,
            hSource,
            &aotDuplicateHandle,
            &pobjSource
            );

        if (NO_ERROR != palError)
        {
            ERROR("Unable to get object for source handle %p (%i)\n", hSource, palError);
            goto InternalDuplicateHandleExit;
        }
    }
    else if (hPseudoCurrentProcess == hSource)
    {
        TRACE("Duplicating process pseudo handle(%p)\n", hSource);

        pobjSource = g_pobjProcess;
        pobjSource->AddReference();
    }
    else if (hPseudoCurrentThread == hSource)
    {
        TRACE("Duplicating thread pseudo handle(%p)\n", hSource);

        pobjSource = pThread->GetThreadObject();
        pobjSource->AddReference();
    }
    else
    {
        ASSERT("Duplication not supported for this special handle (%p)\n", hSource);
        palError = ERROR_INVALID_HANDLE;
        goto InternalDuplicateHandleExit;
    }

    palError = g_pObjectManager->ObtainHandleForObject(
        pThread,
        pobjSource,
        phDuplicate
        );

InternalDuplicateHandleExit:

    if (NULL != pobjSource)
    {
        pobjSource->ReleaseReference(pThread);
    }

    if (dwOptions & DUPLICATE_CLOSE_SOURCE)
    {
        //
        // Since DUPLICATE_CLOSE_SOURCE was specified the source handle
        // MUST be closed, even if an error occurred during the duplication
        // process
        //

        TRACE("DuplicateHandle closing source handle %p\n", hSource);
        InternalCloseHandle(pThread, hSource);
    }

    return palError;
}

/*++
Function:
  CloseHandle

See MSDN doc.

Note : according to MSDN, FALSE is returned in case of error. But also
according to MSDN, closing an invalid handle raises an exception when running a
debugger [or, alternately, if a special registry key is set]. This behavior is
not required in the PAL, so we'll always return FALSE.
--*/
BOOL
PALAPI
CloseHandle(
        IN OUT HANDLE hObject)
{
    CPalThread *pThread;
    PAL_ERROR palError;

    PERF_ENTRY(CloseHandle);
    ENTRY("CloseHandle (hObject=%p) \n", hObject);

    pThread = InternalGetCurrentThread();

    palError = InternalCloseHandle(
        pThread,
        hObject
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("CloseHandle returns BOOL %d\n", (NO_ERROR == palError));
    PERF_EXIT(CloseHandle);
    return (NO_ERROR == palError);
}

PAL_ERROR
CorUnix::InternalCloseHandle(
    CPalThread * pThread,
    HANDLE hObject
    )
{
    PAL_ERROR palError = NO_ERROR;

    if (!HandleIsSpecial(hObject))
    {
        palError = g_pObjectManager->RevokeHandle(
            pThread,
            hObject
            );
    }
    else
    {
        palError = CloseSpecialHandle(hObject);
    }

    return palError;
}

PAL_ERROR
CloseSpecialHandle(
    HANDLE hObject
    )
{
    if ((hObject == hPseudoCurrentThread) ||
        (hObject == hPseudoCurrentProcess))
    {
        return NO_ERROR;
    }

    return ERROR_INVALID_HANDLE;
}

