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

#ifdef TARGET_UNIX
#include <pthread.h>
#endif

#include "rhassert.h"
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
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "gcenv.interlocked.inl"

#include "slist.h"
#include "shash.h"
#include "TypeManager.h"
#include "RuntimeInstance.h"
#include "MethodTable.inl"
#include "volatile.h"

#include "gcenv.inl"

#include "stressLog.h"

#ifndef SKIP_TRACING_DEFINITIONS
#ifdef FEATURE_EVENT_TRACE

    #include "clretwallmain.h"
    #include "eventtrace.h"

#else // FEATURE_EVENT_TRACE

    #include "etmdummy.h"
    #define ETW_EVENT_ENABLED(e,f) false

#endif // FEATURE_EVENT_TRACE
#endif //SKIP_TRACING_DEFINITIONS

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
#ifdef HOST_WINDOWS
    return (uint16_t)_tls_index;
#else
    return 0;
#endif
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
