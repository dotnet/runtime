// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//


#ifndef __CLRHOST_H__
#define __CLRHOST_H__

#include "windows.h" // worth to include before mscoree.h so we are guaranteed to pick few definitions
#ifdef CreateSemaphore
#undef CreateSemaphore
#endif
#include "mscoree.h"
#include "clrinternal.h"
#include "switches.h"
#include "holder.h"
#include "new.hpp"
#include "staticcontract.h"
#include "predeftlsslot.h"
#include "safemath.h"
#include "debugreturn.h"
#include "yieldprocessornormalized.h"

#if !defined(_DEBUG_IMPL) && defined(_DEBUG) && !defined(DACCESS_COMPILE)
#define _DEBUG_IMPL 1
#endif

#define BEGIN_PRESERVE_LAST_ERROR \
    { \
        DWORD __dwLastError = ::GetLastError(); \
        DEBUG_ASSURE_NO_RETURN_BEGIN(PRESERVE_LAST_ERROR); \
            {

#define END_PRESERVE_LAST_ERROR \
            } \
        DEBUG_ASSURE_NO_RETURN_END(PRESERVE_LAST_ERROR); \
        ::SetLastError(__dwLastError); \
    }

//
// TRASH_LASTERROR macro sets bogus last error in debug builds to help find places that fail to save it
//
#ifdef _DEBUG

#define LAST_ERROR_TRASH_VALUE 42424 /* = 0xa5b8 */

#define TRASH_LASTERROR \
    SetLastError(LAST_ERROR_TRASH_VALUE)

#else // _DEBUG

#define TRASH_LASTERROR

#endif // _DEBUG


LPVOID ClrVirtualAlloc(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect);
BOOL ClrVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType);
SIZE_T ClrVirtualQuery(LPCVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer, SIZE_T dwLength);
BOOL ClrVirtualProtect(LPVOID lpAddress, SIZE_T dwSize, DWORD flNewProtect, PDWORD lpflOldProtect);

#ifdef HOST_WINDOWS
HANDLE ClrGetProcessExecutableHeap();
#endif

#ifdef FAILPOINTS_ENABLED
extern int RFS_HashStack();
#endif

// Critical section support for CLR DLLs other than the EE.
// Include the header defining each Crst type and its corresponding level (relative rank). This is
// auto-generated from a tool that takes a high-level description of each Crst type and its dependencies.
#include "crsttypes_generated.h"

// critical section api
CRITSEC_COOKIE ClrCreateCriticalSection(CrstType type, CrstFlags flags);
void ClrDeleteCriticalSection(CRITSEC_COOKIE cookie);
void ClrEnterCriticalSection(CRITSEC_COOKIE cookie);
void ClrLeaveCriticalSection(CRITSEC_COOKIE cookie);

DWORD ClrSleepEx(DWORD dwMilliseconds, BOOL bAlertable);

// Rather than use the above APIs directly, it is recommended that holder classes
// be used.  This guarantees that the locks will be vacated when the scope is popped,
// either on exception or on return.

typedef Holder<CRITSEC_COOKIE, ClrEnterCriticalSection, ClrLeaveCriticalSection, NULL> CRITSEC_Holder;

// Use this holder to manage CRITSEC_COOKIE allocation to ensure it will be released if anything goes wrong
FORCEINLINE void VoidClrDeleteCriticalSection(CRITSEC_COOKIE cs) { if (cs != NULL) ClrDeleteCriticalSection(cs); }
typedef Wrapper<CRITSEC_COOKIE, DoNothing<CRITSEC_COOKIE>, VoidClrDeleteCriticalSection, NULL> CRITSEC_AllocationHolder;

#ifndef DACCESS_COMPILE
// Suspend/resume APIs that fail-fast on errors
#ifdef TARGET_WINDOWS
DWORD ClrSuspendThread(HANDLE hThread);
#endif // TARGET_WINDOWS
DWORD ClrResumeThread(HANDLE hThread);
#endif // !DACCESS_COMPILE

DWORD GetClrModulePathName(SString& buffer);

extern thread_local int t_CantAllocCount;

inline void IncCantAllocCount()
{
    t_CantAllocCount++;
}

inline void DecCantAllocCount()
{
    t_CantAllocCount--;
}

class CantAllocHolder
{
public:
    CantAllocHolder ()
    {
        IncCantAllocCount ();
    }
    ~CantAllocHolder()
    {
	    DecCantAllocCount ();
    }
};

// At places where want to allocate stress log, we need to first check if we are allowed to do so.
inline bool IsInCantAllocRegion ()
{
    return t_CantAllocCount != 0;
}
inline BOOL IsInCantAllocStressLogRegion()
{
    return t_CantAllocCount != 0;
}

#endif
