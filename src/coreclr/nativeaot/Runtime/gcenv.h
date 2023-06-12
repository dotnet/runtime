// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#ifndef __GCENV_H__
#define __GCENV_H__

#ifdef _MSC_VER
#pragma warning( disable: 4189 )  // 'hp': local variable is initialized but not referenced -- common in GC
#pragma warning( disable: 4127 )  // conditional expression is constant -- common in GC
#endif

#include <stdlib.h>
#include <stdint.h>
#include <assert.h>
#include <cstddef>
#include <string.h>

#include "sal.h"
#include "gcenv.structs.h"
#include "gcenv.interlocked.h"
#include "gcenv.base.h"
#include "gcenv.os.h"

#include "Crst.h"
#include "event.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "TargetPtrs.h"
#include "MethodTable.h"
#include "ObjectLayout.h"
#include "rheventtrace.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "gcrhinterface.h"
#include "gcenv.interlocked.inl"

#include "slist.h"
#include "shash.h"
#include "TypeManager.h"
#include "RuntimeInstance.h"
#include "MethodTable.inl"
#include "volatile.h"

#include "gcenv.inl"

#include "stressLog.h"
#ifdef FEATURE_ETW

    #ifndef _INC_WINDOWS
        typedef void* LPVOID;
        typedef uint32_t UINT;
        typedef void* PVOID;
        typedef uint64_t ULONGLONG;
        typedef uint32_t ULONG;
        typedef int64_t LONGLONG;
        typedef uint8_t BYTE;
        typedef uint16_t UINT16;
    #endif // _INC_WINDOWS

    #include "clretwallmain.h"
    #include "etwevents.h"
    #include "eventtrace.h"

#else // FEATURE_ETW

    #include "etmdummy.h"
    #define ETW_EVENT_ENABLED(e,f) false

#endif // FEATURE_ETW

#define LOG(x)

// Adapter for GC's view of Array
class ArrayBase : Array
{
public:
    DWORD GetNumComponents()
    {
        return m_Length;
    }

    static size_t GetOffsetOfNumComponents()
    {
        return offsetof(ArrayBase, m_Length);
    }
};

EXTERN_C uint32_t _tls_index;
inline uint16_t GetClrInstanceId()
{
    return (uint16_t)_tls_index;
}

class IGCHeap;
typedef DPTR(IGCHeap) PTR_IGCHeap;
typedef DPTR(uint32_t) PTR_uint32_t;

enum CLRDataEnumMemoryFlags : int;

struct GCHeapHardLimitInfo
{
    uint64_t heapHardLimit;
    uint64_t heapHardLimitPercent;
    uint64_t heapHardLimitSOH;
    uint64_t heapHardLimitLOH;
    uint64_t heapHardLimitPOH;
    uint64_t heapHardLimitSOHPercent;
    uint64_t heapHardLimitLOHPercent;
    uint64_t heapHardLimitPOHPercent;
};

/* _TRUNCATE */
#if !defined (_TRUNCATE)
#define _TRUNCATE ((size_t)-1)
#endif  /* !defined (_TRUNCATE) */

#endif // __GCENV_H__
