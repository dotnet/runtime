// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    tls.cpp

Abstract:

    Implementation of Thread local storage functions.



--*/

#include "pal/thread.hpp"
#include "procprivate.hpp"

#include <pthread.h>

#include "pal/dbgmsg.h"
#include "pal/misc.h"
#include "pal/virtual.h"
#include "pal/process.h"
#include "pal/init.h"
#include "pal/malloc.hpp"
#include "pal_endian.h"

#include <stddef.h>
using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(THREAD);

// In safemath.h, Template SafeInt uses macro _ASSERTE, which need to use variable
// defdbgchan defined by SET_DEFAULT_DEBUG_CHANNEL. Therefore, the include statement
// should be placed after the SET_DEFAULT_DEBUG_CHANNEL(THREAD)
#include <safemath.h>

/* This tracks the slots that are used for TlsAlloc. Its size in bits
   must be the same as TLS_SLOT_SIZE in pal/thread.h. Since this is
   static, it is initialized to 0, which is what we want. */
static unsigned __int64 sTlsSlotFields;

/*++
Function:
  TlsAlloc

See MSDN doc.
--*/
DWORD
PALAPI
TlsAlloc(
    VOID)
{
    DWORD dwIndex;
    unsigned int i;

    PERF_ENTRY(TlsAlloc);
    ENTRY("TlsAlloc()\n");

    /* Yes, this could be ever so slightly improved. It's not
       likely to be called enough to matter, though, so we won't
       optimize here until or unless we need to. */
       
    PROCProcessLock();
    
    for(i = 0; i < sizeof(sTlsSlotFields) * 8; i++)
    {
        if ((sTlsSlotFields & ((unsigned __int64) 1 << i)) == 0)
        {
            sTlsSlotFields |= ((unsigned __int64) 1 << i);
            break;
        }
    }
    if (i == sizeof(sTlsSlotFields) * 8)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        dwIndex = TLS_OUT_OF_INDEXES;
    }
    else
    {
        dwIndex = i;
    }
    
    PROCProcessUnlock();

    LOGEXIT("TlsAlloc returns DWORD %u\n", dwIndex);
    PERF_EXIT(TlsAlloc);
    return dwIndex;
}


/*++
Function:
  TlsGetValue

See MSDN doc.
--*/
LPVOID
PALAPI
TlsGetValue(
        IN DWORD dwTlsIndex)
{
    CPalThread *pThread;
    
    PERF_ENTRY(TlsGetValue);
    ENTRY("TlsGetValue()\n");
    
    if (dwTlsIndex == (DWORD) -1 || dwTlsIndex >= TLS_SLOT_SIZE)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return 0;
    }

    pThread = InternalGetCurrentThread();

    /* From MSDN : "The TlsGetValue function calls SetLastError to clear a
       thread's last error when it succeeds." */
    pThread->SetLastError(NO_ERROR);
    
    LOGEXIT("TlsGetValue \n" );
    PERF_EXIT(TlsGetValue);
    
    return pThread->tlsInfo.tlsSlots[dwTlsIndex];
}


/*++
Function:
  TlsSetValue

See MSDN doc.
--*/
BOOL
PALAPI
TlsSetValue(
        IN DWORD dwTlsIndex,
        IN LPVOID lpTlsValue)
{
    CPalThread *pThread;
    BOOL bRet = FALSE;
    PERF_ENTRY(TlsSetValue);
    ENTRY("TlsSetValue(dwTlsIndex=%u, lpTlsValue=%p)\n", dwTlsIndex, lpTlsValue);

    if (dwTlsIndex == (DWORD) -1 || dwTlsIndex >= TLS_SLOT_SIZE)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }

    pThread = InternalGetCurrentThread();
    pThread->tlsInfo.tlsSlots[dwTlsIndex] = lpTlsValue;
    bRet = TRUE;
    
EXIT:
    LOGEXIT("TlsSetValue returns BOOL %d\n", bRet);
    PERF_EXIT(TlsSetValue);
    return bRet;
}


/*++
Function:
  TlsFree

See MSDN doc.
--*/
BOOL
PALAPI
TlsFree(
    IN DWORD dwTlsIndex)
{
    CPalThread *pThread;

    PERF_ENTRY(TlsFree);
    ENTRY("TlsFree(dwTlsIndex=%u)\n", dwTlsIndex);

    
    if (dwTlsIndex == (DWORD) -1 || dwTlsIndex >= TLS_SLOT_SIZE)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        LOGEXIT("TlsFree returns BOOL FALSE\n");
        PERF_EXIT(TlsFree);
        return FALSE;
    }

    PROCProcessLock();

    /* Reset all threads' values to zero for this index. */
    for(pThread = pGThreadList; 
        pThread != NULL; pThread = pThread->GetNext())
    {
        pThread->tlsInfo.tlsSlots[dwTlsIndex] = 0;
    }
    sTlsSlotFields &= ~((unsigned __int64) 1 << dwTlsIndex);
    
    PROCProcessUnlock();

    LOGEXIT("TlsFree returns BOOL TRUE\n");
    PERF_EXIT(TlsFree);
    return TRUE;
}

PAL_ERROR
CThreadTLSInfo::InitializePostCreate(
    CPalThread *pThread,
    SIZE_T threadId,
    DWORD dwLwpId
    )
{
    PAL_ERROR palError = NO_ERROR;
    
    if (pthread_setspecific(thObjKey, reinterpret_cast<void*>(pThread)))
    {
        ASSERT("Unable to set the thread object key's value\n");
        palError = ERROR_INTERNAL_ERROR;
    }

    return palError;
}

