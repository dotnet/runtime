//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

//
// Setups standalone environment for CLR GC
//

#define FEATURE_REDHAWK 1
#define FEATURE_CONSERVATIVE_GC 1

#ifndef _INC_WINDOWS

// -----------------------------------------------------------------------------------------------------------
//
// Aliases for Win32 types
//

typedef uint32_t BOOL;
typedef uint16_t WORD;
typedef uint16_t USHORT;
typedef uint32_t DWORD;
typedef uintptr_t DWORD_PTR;
typedef uint8_t BYTE;
typedef int8_t SBYTE;
typedef BYTE* PBYTE;
typedef void* LPVOID;
typedef int8_t INT8;
typedef uint32_t UINT;
typedef uint32_t UINT32;
typedef uint16_t UINT16;
typedef uint8_t UINT8;
typedef int16_t INT16;
typedef int32_t INT32;
typedef int32_t LONG;
typedef int64_t LONGLONG;
typedef uint32_t ULONG;
typedef uint32_t ULONG32;
typedef intptr_t INT_PTR;
typedef uintptr_t UINT_PTR;
typedef uintptr_t ULONG_PTR;
typedef uint64_t UINT64;
typedef uint64_t ULONG64;
typedef uint64_t ULONGLONG;
typedef uint64_t DWORDLONG;
typedef int64_t INT64;
typedef void VOID;
typedef void* PVOID;
typedef uintptr_t LPARAM;
typedef void* LPCGUID;
typedef void * LPSECURITY_ATTRIBUTES;
typedef void const * LPCVOID;
typedef uint32_t * PULONG;
typedef char * PSTR;
typedef wchar_t * PWSTR, *LPWSTR;
typedef const wchar_t *LPCWSTR, *PCWSTR;
typedef size_t SIZE_T;
typedef ptrdiff_t ssize_t;
typedef ptrdiff_t SSIZE_T;

typedef void * HANDLE;

typedef union _LARGE_INTEGER {
    struct {
#if BIGENDIAN
        LONG HighPart;
        DWORD LowPart;
#else
        DWORD LowPart;
        LONG HighPart;
#endif
    } u;
    LONGLONG QuadPart;
} LARGE_INTEGER, *PLARGE_INTEGER;

#define SIZE_T_MAX ((size_t)-1)
#define SSIZE_T_MAX ((ssize_t)(SIZE_T_MAX / 2))

// -----------------------------------------------------------------------------------------------------------
// HRESULT subset.

typedef int32_t HRESULT;

#define SUCCEEDED(_hr)          ((HRESULT)(_hr) >= 0)
#define FAILED(_hr)             ((HRESULT)(_hr) < 0)

inline HRESULT HRESULT_FROM_WIN32(unsigned long x)
{
    return (HRESULT)(x) <= 0 ? (HRESULT)(x) : (HRESULT) (((x) & 0x0000FFFF) | (7 << 16) | 0x80000000);
}

#define S_OK                    0x0
#define S_FALSE                 0x1
#define E_FAIL                  0x80004005
#define E_OUTOFMEMORY           0x8007000E
#define E_UNEXPECTED            0x8000FFFF
#define E_NOTIMPL               0x80004001
#define E_INVALIDARG            0x80070057

#define NOERROR                 0x0
#define ERROR_TIMEOUT             1460

#define TRUE true
#define FALSE false

#define CALLBACK
#define FORCEINLINE inline

#define INFINITE 0xFFFFFFFF

#define ZeroMemory(Destination,Length) memset((Destination),0,(Length))

#ifndef _countof
#define _countof(_array) (sizeof(_array)/sizeof(_array[0]))
#endif

#ifndef min
#define min(a,b) (((a) < (b)) ? (a) : (b))
#endif

#ifndef max
#define max(a,b) (((a) > (b)) ? (a) : (b))
#endif

#define C_ASSERT(cond) static_assert( cond, #cond )

#define UNREFERENCED_PARAMETER(P)          (void)(P)

#define INVALID_HANDLE_VALUE    ((HANDLE)-1)

#pragma pack(push, 8)

typedef struct _RTL_CRITICAL_SECTION {
    PVOID DebugInfo;

    //
    //  The following three fields control entering and exiting the critical
    //  section for the resource
    //

    LONG LockCount;
    LONG RecursionCount;
    HANDLE OwningThread;        // from the thread's ClientId->UniqueThread
    HANDLE LockSemaphore;
    ULONG_PTR SpinCount;        // force size on 64-bit systems when packed
} CRITICAL_SECTION, RTL_CRITICAL_SECTION, *PRTL_CRITICAL_SECTION;

#pragma pack(pop)

typedef struct _MEMORYSTATUSEX {
  DWORD     dwLength;
  DWORD     dwMemoryLoad;
  DWORDLONG ullTotalPhys;
  DWORDLONG ullAvailPhys;
  DWORDLONG ullTotalPageFile;
  DWORDLONG ullAvailPageFile;
  DWORDLONG ullTotalVirtual;
  DWORDLONG ullAvailVirtual;
  DWORDLONG ullAvailExtendedVirtual;
} MEMORYSTATUSEX, *LPMEMORYSTATUSEX;

typedef DWORD (__stdcall *PTHREAD_START_ROUTINE)(void* lpThreadParameter);

#define WINBASEAPI extern "C"
#define WINAPI __stdcall

WINBASEAPI
void 
WINAPI
DebugBreak();

WINBASEAPI
BOOL
WINAPI
VirtualUnlock(
    LPVOID lpAddress,
    SIZE_T dwSize
    );

WINBASEAPI
DWORD
WINAPI
GetLastError();

WINBASEAPI
UINT 
WINAPI
GetWriteWatch(
  DWORD dwFlags,
  PVOID lpBaseAddress,
  SIZE_T dwRegionSize,
  PVOID *lpAddresses,
  ULONG_PTR * lpdwCount,
  ULONG * lpdwGranularity
);

WINBASEAPI
UINT 
WINAPI
ResetWriteWatch(
  LPVOID lpBaseAddress,
  SIZE_T dwRegionSize
);

WINBASEAPI
VOID 
WINAPI
FlushProcessWriteBuffers();

WINBASEAPI
DWORD
WINAPI
GetTickCount();

WINBASEAPI
BOOL
WINAPI
QueryPerformanceCounter(LARGE_INTEGER *lpPerformanceCount);

WINBASEAPI
BOOL
WINAPI
QueryPerformanceFrequency(LARGE_INTEGER *lpFrequency);

WINBASEAPI
DWORD
WINAPI
GetCurrentThreadId(
           VOID);

WINBASEAPI
BOOL
WINAPI
CloseHandle(
        HANDLE hObject);

#define WAIT_OBJECT_0           0
#define WAIT_TIMEOUT            258
#define WAIT_FAILED             0xFFFFFFFF

#define GENERIC_WRITE           0x40000000
#define FILE_SHARE_READ         0x00000001
#define CREATE_ALWAYS           2
#define FILE_ATTRIBUTE_NORMAL               0x00000080

WINBASEAPI
BOOL
WINAPI
WriteFile(
      HANDLE hFile,
      LPCVOID lpBuffer,
      DWORD nNumberOfBytesToWrite,
      DWORD * lpNumberOfBytesWritten,
      PVOID lpOverlapped);

#define FILE_BEGIN              0

WINBASEAPI
DWORD
WINAPI
SetFilePointer(
           HANDLE hFile,
           LONG lDistanceToMove,
           LONG * lpDistanceToMoveHigh,
           DWORD dwMoveMethod);

WINBASEAPI
BOOL
WINAPI
FlushFileBuffers(
         HANDLE hFile);

extern "C" VOID
_mm_pause (
    VOID
    );

#pragma intrinsic(_mm_pause)

#define YieldProcessor _mm_pause

#endif // _INC_WINDOWS

// -----------------------------------------------------------------------------------------------------------
//
// The subset of the contract code required by the GC/HandleTable sources. If Redhawk moves to support
// contracts these local definitions will disappear and be replaced by real implementations.
//

#define LEAF_CONTRACT
#define LIMITED_METHOD_CONTRACT
#define LIMITED_METHOD_DAC_CONTRACT
#define WRAPPER_CONTRACT
#define WRAPPER_NO_CONTRACT
#define STATIC_CONTRACT_LEAF
#define STATIC_CONTRACT_DEBUG_ONLY
#define STATIC_CONTRACT_NOTHROW
#define STATIC_CONTRACT_CAN_TAKE_LOCK
#define STATIC_CONTRACT_SO_TOLERANT
#define STATIC_CONTRACT_GC_NOTRIGGER
#define STATIC_CONTRACT_MODE_COOPERATIVE
#define CONTRACTL
#define CONTRACT(_expr)
#define CONTRACT_VOID
#define THROWS
#define NOTHROW
#define INSTANCE_CHECK
#define MODE_COOPERATIVE
#define MODE_ANY
#define SO_INTOLERANT
#define SO_TOLERANT
#define GC_TRIGGERS
#define GC_NOTRIGGER
#define CAN_TAKE_LOCK
#define SUPPORTS_DAC
#define FORBID_FAULT
#define CONTRACTL_END
#define CONTRACT_END
#define TRIGGERSGC()
#define WRAPPER(_contract)
#define DISABLED(_contract)
#define INJECT_FAULT(_expr)
#define INJECTFAULT_HANDLETABLE 0x1
#define INJECTFAULT_GCHEAP 0x2
#define FAULT_NOT_FATAL()
#define BEGIN_DEBUG_ONLY_CODE
#define END_DEBUG_ONLY_CODE
#define BEGIN_GETTHREAD_ALLOWED
#define END_GETTHREAD_ALLOWED
#define LEAF_DAC_CONTRACT
#define PRECONDITION(_expr)
#define POSTCONDITION(_expr)
#define RETURN return
#define CONDITIONAL_CONTRACT_VIOLATION(_violation, _expr)

// -----------------------------------------------------------------------------------------------------------
//
// Data access macros
//

typedef uintptr_t TADDR;

#define PTR_TO_TADDR(ptr) ((TADDR)(ptr))

#define DPTR(type) type*
#define SPTR(type) type*

#define GVAL_DECL(type, var) \
    extern type var
#define GVAL_IMPL(type, var) \
    type var

#define GPTR_DECL(type, var) \
    extern type* var
#define GPTR_IMPL(type, var) \
    type* var
#define GPTR_IMPL_INIT(type, var, init) \
    type* var = init

#define SPTR_DECL(type, var) \
    static type* var
#define SPTR_IMPL(type, cls, var) \
    type * cls::var
#define SPTR_IMPL_NS(type, ns, cls, var) \
    type * cls::var
#define SPTR_IMPL_NS_INIT(type, ns, cls, var, init) \
    type * cls::var = init

#define SVAL_DECL(type, var) \
    static type var
#define SVAL_IMPL_NS(type, ns, cls, var) \
    type cls::var
#define SVAL_IMPL_NS_INIT(type, ns, cls, var, init) \
    type cls::var = init

#define GARY_DECL(type, var, size) \
    extern type var[size]
#define GARY_IMPL(type, var, size) \
    type var[size]

typedef DPTR(size_t)    PTR_size_t;
typedef DPTR(BYTE)      PTR_BYTE;

struct _DacGlobals;

// -----------------------------------------------------------------------------------------------------------

#define DATA_ALIGNMENT sizeof(uintptr_t)

#define RAW_KEYWORD(x) x

#define DECLSPEC_ALIGN(x)   __declspec(align(x))

#define OS_PAGE_SIZE 4096

#if defined(_DEBUG)
#ifndef _DEBUG_IMPL
#define _DEBUG_IMPL 1
#endif
#define ASSERT(_expr) assert(_expr)
#else
#define ASSERT(_expr)
#endif

#ifndef _ASSERTE
#define _ASSERTE(_expr) ASSERT(_expr)
#endif

#define CONSISTENCY_CHECK(_expr) ASSERT(_expr)

#define PREFIX_ASSUME(cond) ASSERT(cond)

#define EEPOLICY_HANDLE_FATAL_ERROR(error) ASSERT(!"EEPOLICY_HANDLE_FATAL_ERROR")

#define UI64(_literal) _literal##ULL

int32_t FastInterlockIncrement(int32_t volatile *lpAddend);
int32_t FastInterlockDecrement(int32_t volatile *lpAddend);
int32_t FastInterlockExchange(int32_t volatile *Target, LONG Value);
int32_t FastInterlockCompareExchange(int32_t volatile *Destination, int32_t Exchange, int32_t Comperand);
int32_t FastInterlockExchangeAdd(int32_t volatile *Addend, int32_t Value);

void * _FastInterlockExchangePointer(void * volatile *Target, void * Value);
void * _FastInterlockCompareExchangePointer(void * volatile *Destination, void * Exchange, void * Comperand);

template <typename T>
inline T FastInterlockExchangePointer(
    T volatile * target,
    T            value)
{
    return (T)_FastInterlockExchangePointer((void **)target, value);
}

template <typename T>
inline T FastInterlockExchangePointer(
    T volatile * target,
    nullptr_t    value)
{
    return (T)_FastInterlockExchangePointer((void **)target, value);
}

template <typename T>
inline T FastInterlockCompareExchangePointer(
    T volatile * destination,
    T            exchange,
    T            comparand)
{
    return (T)_FastInterlockCompareExchangePointer((void **)destination, exchange, comparand);
}

template <typename T>
inline T FastInterlockCompareExchangePointer(
    T volatile * destination,
    T            exchange,
    nullptr_t    comparand)
{
    return (T)_FastInterlockCompareExchangePointer((void **)destination, exchange, comparand);
}


void FastInterlockOr(uint32_t volatile *p, uint32_t msk);
void FastInterlockAnd(uint32_t volatile *p, uint32_t msk);

#define CALLER_LIMITS_SPINNING 0
bool __SwitchToThread (uint32_t dwSleepMSec, uint32_t dwSwitchCount);

//-------------------------------------------------------------------------------------------------
//
// Low-level types describing GC object layouts.
//

// Bits stolen from the sync block index that the GC/HandleTable knows about (currently these are at the same
// positions as the mainline runtime but we can change this below when it becomes apparent how Redhawk will
// handle sync blocks).
#define BIT_SBLK_GC_RESERVE                 0x20000000
#define BIT_SBLK_FINALIZER_RUN              0x40000000

// The sync block index header (small structure that immediately precedes every object in the GC heap). Only
// the GC uses this so far, and only to store a couple of bits of information.
class ObjHeader
{
private:
#if defined(_WIN64)
    uint32_t m_uAlignpad;
#endif // _WIN64
    uint32_t m_uSyncBlockValue;

public:
    uint32_t GetBits() { return m_uSyncBlockValue; }
    void SetBit(uint32_t uBit) { FastInterlockOr(&m_uSyncBlockValue, uBit); }
    void ClrBit(uint32_t uBit) { FastInterlockAnd(&m_uSyncBlockValue, ~uBit); }
    void SetGCBit() { m_uSyncBlockValue |= BIT_SBLK_GC_RESERVE; }
    void ClrGCBit() { m_uSyncBlockValue &= ~BIT_SBLK_GC_RESERVE; }
};

#define MTFlag_ContainsPointers 1
#define MTFlag_HasFinalizer 2

class MethodTable
{
public:
    uint32_t    m_baseSize;
    uint16_t    m_componentSize;
    uint16_t    m_flags;

public:
    void InitializeFreeObject()
    {
        m_baseSize = 3 * sizeof(void *);
        m_componentSize = 1;
        m_flags = 0;
    }

    uint32_t GetBaseSize()
    {
        return m_baseSize;
    }

    uint16_t RawGetComponentSize()
    {
        return m_componentSize;
    }

    bool ContainsPointers()
    {
        return (m_flags & MTFlag_ContainsPointers) != 0;
    }

    bool ContainsPointersOrCollectible()
    {
        return ContainsPointers();
    }

    bool HasComponentSize()
    {
        return m_componentSize != 0;
    }

    bool HasFinalizer()
    {
        return (m_flags & MTFlag_HasFinalizer) != 0;
    }

    bool HasCriticalFinalizer()
    {
        return false;
    }

    bool SanityCheck()
    {
        return true;
    }
};
#define EEType MethodTable

class Object
{
    MethodTable * m_pMethTab;

public:
    ObjHeader * GetHeader()
    { 
        return ((ObjHeader *)this) - 1;
    }

    MethodTable * RawGetMethodTable() const
    {
        return m_pMethTab;
    }

    void RawSetMethodTable(MethodTable * pMT)
    {
        m_pMethTab = pMT;
    }

    void SetMethodTable(MethodTable * pMT)
    {
        m_pMethTab = pMT;
    }
};
#define MIN_OBJECT_SIZE     (2*sizeof(BYTE*) + sizeof(ObjHeader))

class ArrayBase : public Object
{
    DWORD m_dwLength;

public:
    DWORD GetNumComponents()
    {
        return m_dwLength;
    }

    static SIZE_T GetOffsetOfNumComponents()
    {
        return offsetof(ArrayBase, m_dwLength);
    }
};

// Various types used to refer to object references or handles. This will get more complex if we decide
// Redhawk wants to wrap object references in the debug build.
typedef DPTR(Object) PTR_Object;
typedef DPTR(PTR_Object) PTR_PTR_Object;

typedef PTR_Object OBJECTREF;
typedef PTR_PTR_Object PTR_OBJECTREF;
typedef PTR_Object _UNCHECKED_OBJECTREF;
typedef PTR_PTR_Object PTR_UNCHECKED_OBJECTREF;

#ifndef DACCESS_COMPILE
struct OBJECTHANDLE__
{
    void* unused;
};
typedef struct OBJECTHANDLE__* OBJECTHANDLE;
#else
typedef TADDR OBJECTHANDLE;
#endif

// With no object reference wrapping the following macros are very simple.
#define ObjectToOBJECTREF(_obj) (OBJECTREF)(_obj)
#define OBJECTREFToObject(_obj) (Object*)(_obj)

#define VALIDATEOBJECTREF(_objref)

#define VOLATILE(T) T volatile

#define VOLATILE_MEMORY_BARRIER()

//
// VolatileLoad loads a T from a pointer to T.  It is guaranteed that this load will not be optimized
// away by the compiler, and that any operation that occurs after this load, in program order, will
// not be moved before this load.  In general it is not guaranteed that the load will be atomic, though
// this is the case for most aligned scalar data types.  If you need atomic loads or stores, you need
// to consult the compiler and CPU manuals to find which circumstances allow atomicity.
//
template<typename T>
inline
T VolatileLoad(T const * pt)
{
    T val = *(T volatile const *)pt;
    VOLATILE_MEMORY_BARRIER();
    return val;
}

//
// VolatileStore stores a T into the target of a pointer to T.  Is is guaranteed that this store will
// not be optimized away by the compiler, and that any operation that occurs before this store, in program
// order, will not be moved after this store.  In general, it is not guaranteed that the store will be
// atomic, though this is the case for most aligned scalar data types.  If you need atomic loads or stores,
// you need to consult the compiler and CPU manuals to find which circumstances allow atomicity.
//
template<typename T>
inline
void VolatileStore(T* pt, T val)
{
    VOLATILE_MEMORY_BARRIER();
    *(T volatile *)pt = val;
}

struct GCSystemInfo
{
    DWORD dwNumberOfProcessors;
    DWORD dwPageSize;
    DWORD dwAllocationGranularity;
};

extern GCSystemInfo g_SystemInfo;
void InitializeSystemInfo();

void
GetProcessMemoryLoad(
            LPMEMORYSTATUSEX lpBuffer);

extern MethodTable * g_pFreeObjectMethodTable;

extern int32_t g_TrapReturningThreads;

extern bool g_fFinalizerRunOnShutDown;

//
// Memory allocation
//
#define MEM_COMMIT              0x1000
#define MEM_RESERVE             0x2000
#define MEM_DECOMMIT            0x4000
#define MEM_RELEASE             0x8000
#define MEM_RESET               0x80000

#define PAGE_NOACCESS           0x01
#define PAGE_READWRITE          0x04

void * ClrVirtualAlloc(
    void * lpAddress,
    size_t dwSize,
    uint32_t flAllocationType,
    uint32_t flProtect);

void * ClrVirtualAllocAligned(
    void * lpAddress,
    size_t dwSize,
    uint32_t flAllocationType,
    uint32_t flProtect,
    size_t dwAlignment);

bool ClrVirtualFree(
        void * lpAddress,
        size_t dwSize,
        uint32_t dwFreeType);

bool
ClrVirtualProtect(
           void * lpAddress,
           size_t dwSize,
           uint32_t flNewProtect,
           uint32_t * lpflOldProtect);

//
// Locks
//

struct alloc_context;

class Thread
{
    uint32_t m_fPreemptiveGCDisabled;
    uintptr_t m_alloc_context[16]; // Reserve enough space to fix allocation context

    friend class ThreadStore;
    Thread * m_pNext;

public:
    Thread()
    {
    }

    bool PreemptiveGCDisabled()
    {
        return !!m_fPreemptiveGCDisabled;
    }

    void EnablePreemptiveGC()
    {
        m_fPreemptiveGCDisabled = false;
    }

    void DisablePreemptiveGC()
    {
        m_fPreemptiveGCDisabled = true;
    }

    alloc_context* GetAllocContext()
    {
        return (alloc_context *)&m_alloc_context;
    }

    void SetGCSpecial(bool fGCSpecial)
    {
    }

    bool CatchAtSafePoint()
    {
        // This is only called by the GC on a background GC worker thread that's explicitly interested in letting
        // a foreground GC proceed at that point. So it's always safe to return true.
        return true;
    }
};

Thread * GetThread();

class ThreadStore
{
public:
    static Thread * GetThreadList(Thread * pThread);

    static void AttachCurrentThread(bool fAcquireThreadStoreLock);
};

struct ScanContext;
typedef void promote_func(PTR_PTR_Object, ScanContext*, unsigned);

typedef void (CALLBACK *HANDLESCANPROC)(PTR_UNCHECKED_OBJECTREF pref, LPARAM *pExtraInfo, LPARAM param1, LPARAM param2);

class GCToEEInterface
{
public:
    //
    // Suspend/Resume callbacks
    //
    typedef enum
    {
        SUSPEND_FOR_GC,
        SUSPEND_FOR_GC_PREP
    } SUSPEND_REASON;

    static void SuspendEE(SUSPEND_REASON reason);
    static void RestartEE(bool bFinishedGC); //resume threads.

    // 
    // The stack roots enumeration callback
    //
    static void ScanStackRoots(Thread * pThread, promote_func* fn, ScanContext* sc);

    // Optional static GC refs scanning for better parallelization of server GC marking
    static void ScanStaticGCRefsOpportunistically(promote_func* fn, ScanContext* sc);

    // 
    // Callbacks issues during GC that the execution engine can do its own bookeeping
    //

    // start of GC call back - single threaded
    static void GcStartWork(int condemned, int max_gen); 

    //EE can perform post stack scanning action, while the 
    // user threads are still suspended 
    static void AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc);

    // Called before BGC starts sweeping, the heap is walkable
    static void GcBeforeBGCSweepWork();

    // post-gc callback.
    static void GcDone(int condemned);

    // Sync block cache management
    static void SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, LPARAM lp1, LPARAM lp2) { }
    static void SyncBlockCacheDemote(int max_gen) { }
    static void SyncBlockCachePromotionsGranted(int max_gen) { }
};

class FinalizerThread
{
public:
    static void EnableFinalization();

    static bool HaveExtraWorkForFinalizer()
    {
        return false;
    }
};

typedef uint32_t (__stdcall *BackgroundCallback)(void* pCallbackContext);
bool PalStartBackgroundGCThread(BackgroundCallback callback, void* pCallbackContext);

void DestroyThread(Thread * pThread);

bool IsGCSpecialThread();

inline bool dbgOnly_IsSpecialEEThread()
{
    return false;
}

#define ClrFlsSetThreadType(type)

void UnsafeInitializeCriticalSection(CRITICAL_SECTION * lpCriticalSection);
void UnsafeEEEnterCriticalSection(CRITICAL_SECTION *lpCriticalSection);
void UnsafeEELeaveCriticalSection(CRITICAL_SECTION * lpCriticalSection);
void UnsafeDeleteCriticalSection(CRITICAL_SECTION *lpCriticalSection);


//
// Performance logging
//

#define COUNTER_ONLY(x)

#include "etmdummy.h"

#define ETW_EVENT_ENABLED(e,f) false

namespace ETW
{
    class GCLog
    {
    public:
        struct ETW_GC_INFO
        {
            typedef  enum _GC_ROOT_KIND {
                GC_ROOT_STACK = 0,
                GC_ROOT_FQ = 1,
                GC_ROOT_HANDLES = 2,
                GC_ROOT_OLDER = 3,
                GC_ROOT_SIZEDREF = 4,
                GC_ROOT_OVERFLOW = 5
            } GC_ROOT_KIND;
        };
    };
};

//
// Logging
//

#define LOG(x)

inline VOID LogSpewAlways(const char *fmt, ...)
{
}

#define LL_INFO10 0

#define STRESS_LOG_VA(msg)                                              do { } while(0)
#define STRESS_LOG0(facility, level, msg)                               do { } while(0)
#define STRESS_LOG1(facility, level, msg, data1)                        do { } while(0)
#define STRESS_LOG2(facility, level, msg, data1, data2)                 do { } while(0)
#define STRESS_LOG3(facility, level, msg, data1, data2, data3)          do { } while(0)
#define STRESS_LOG4(facility, level, msg, data1, data2, data3, data4)   do { } while(0)
#define STRESS_LOG5(facility, level, msg, data1, data2, data3, data4, data5)   do { } while(0)
#define STRESS_LOG6(facility, level, msg, data1, data2, data3, data4, data5, data6)   do { } while(0)
#define STRESS_LOG7(facility, level, msg, data1, data2, data3, data4, data5, data6, data7)   do { } while(0)
#define STRESS_LOG_PLUG_MOVE(plug_start, plug_end, plug_delta)          do { } while(0)
#define STRESS_LOG_ROOT_PROMOTE(root_addr, objPtr, methodTable)         do { } while(0)
#define STRESS_LOG_ROOT_RELOCATE(root_addr, old_value, new_value, methodTable) do { } while(0)
#define STRESS_LOG_GC_START(gcCount, Gen, collectClasses)               do { } while(0)
#define STRESS_LOG_GC_END(gcCount, Gen, collectClasses)                 do { } while(0)
#define STRESS_LOG_OOM_STACK(size)   do { } while(0)
#define STRESS_LOG_RESERVE_MEM(numChunks) do {} while (0)
#define STRESS_LOG_GC_STACK

typedef void* MUTEX_COOKIE;

inline MUTEX_COOKIE ClrCreateMutex(LPSECURITY_ATTRIBUTES lpMutexAttributes, BOOL bInitialOwner, LPCWSTR lpName)
{
    _ASSERTE(!"ClrCreateMutex");
    return NULL;
}

inline void ClrCloseMutex(MUTEX_COOKIE mutex)
{
    _ASSERTE(!"ClrCloseMutex");
}

inline BOOL ClrReleaseMutex(MUTEX_COOKIE mutex)
{
    _ASSERTE(!"ClrReleaseMutex");
    return true;
}

inline DWORD ClrWaitForMutex(MUTEX_COOKIE mutex, DWORD dwMilliseconds, BOOL bAlertable)
{
    _ASSERTE(!"ClrWaitForMutex");
    return WAIT_OBJECT_0;
}

inline
HANDLE 
PalCreateFileW(LPCWSTR pFileName, DWORD desiredAccess, DWORD shareMode, void* pSecurityAttributes, DWORD creationDisposition, DWORD flagsAndAttributes, HANDLE hTemplateFile)
{
    return INVALID_HANDLE_VALUE;
}


#define DEFAULT_GC_PRN_LVL 3

// -----------------------------------------------------------------------------------------------------------

enum PalCapability
{
    WriteWatchCapability                = 0x00000001,   // GetWriteWatch() and friends
    LowMemoryNotificationCapability     = 0x00000002,   // CreateMemoryResourceNotification() and friends
    GetCurrentProcessorNumberCapability = 0x00000004,   // GetCurrentProcessorNumber()
};

bool PalHasCapability(PalCapability capability);

inline void StompWriteBarrierEphemeral()
{
}

inline void StompWriteBarrierResize(BOOL bReqUpperBoundsCheck)
{
}

// -----------------------------------------------------------------------------------------------------------
// Config file enumulation
//

class EEConfig
{
public:
    enum HeapVerifyFlags {
        HEAPVERIFY_NONE             = 0,
        HEAPVERIFY_GC               = 1,   // Verify the heap at beginning and end of GC
        HEAPVERIFY_BARRIERCHECK     = 2,   // Verify the brick table
        HEAPVERIFY_SYNCBLK          = 4,   // Verify sync block scanning

        // the following options can be used to mitigate some of the overhead introduced
        // by heap verification.  some options might cause heap verifiction to be less
        // effective depending on the scenario.

        HEAPVERIFY_NO_RANGE_CHECKS  = 0x10,   // Excludes checking if an OBJECTREF is within the bounds of the managed heap
        HEAPVERIFY_NO_MEM_FILL      = 0x20,   // Excludes filling unused segment portions with fill pattern
        HEAPVERIFY_POST_GC_ONLY     = 0x40,   // Performs heap verification post-GCs only (instead of before and after each GC)
        HEAPVERIFY_DEEP_ON_COMPACT  = 0x80    // Performs deep object verfication only on compacting GCs.
    };

    enum  GCStressFlags {
        GCSTRESS_NONE               = 0,
        GCSTRESS_ALLOC              = 1,    // GC on all allocs and 'easy' places
        GCSTRESS_TRANSITION         = 2,    // GC on transitions to preemtive GC
        GCSTRESS_INSTR_JIT          = 4,    // GC on every allowable JITed instr
        GCSTRESS_INSTR_NGEN         = 8,    // GC on every allowable NGEN instr
        GCSTRESS_UNIQUE             = 16,   // GC only on a unique stack trace
    };
   
    int     GetHeapVerifyLevel();
    bool    IsHeapVerifyEnabled()                 { return GetHeapVerifyLevel() != 0; }

    GCStressFlags GetGCStressLevel()        const { return GCSTRESS_NONE; }
    bool    IsGCStressMix()                 const { return false; }

    int     GetGCtraceStart()               const { return 0; }
    int     GetGCtraceEnd  ()               const { return 0; }//1000000000; }
    int     GetGCtraceFac  ()               const { return 0; }
    int     GetGCprnLvl    ()               const { return 0; }
    bool    IsGCBreakOnOOMEnabled()         const { return false; }
    int     GetGCgen0size  ()               const { return 0; }
    int     GetSegmentSize ()               const { return 0; }
    int     GetGCconcurrent()               const { return 1; }
    int     GetGCLatencyMode()              const { return 1; }
    int     GetGCForceCompact()             const { return 0; }
    int     GetGCRetainVM ()                const { return 0; }
    int     GetGCTrimCommit()               const { return 0; }
    int     GetGCLOHCompactionMode()        const { return 0; }

    bool    GetGCAllowVeryLargeObjects ()   const { return false; }

    bool    GetGCConservative()             const { return true; }
};

extern EEConfig * g_pConfig;

class CLRConfig
{
public:
    enum CLRConfigTypes
    {
        UNSUPPORTED_GCLogEnabled,
        UNSUPPORTED_GCLogFile,
        UNSUPPORTED_GCLogFileSize,
        UNSUPPORTED_BGCSpinCount,
        UNSUPPORTED_BGCSpin,
        EXTERNAL_GCStressStart,
        INTERNAL_GCStressStartAtJit,
        INTERNAL_DbgDACSkipVerifyDlls,
        Config_COUNT
    };

    static DWORD GetConfigValue(CLRConfigTypes eType)
    {
        switch (eType)
        {
        case UNSUPPORTED_BGCSpinCount:
            return 140;

        case UNSUPPORTED_BGCSpin:
            return 2;

        case UNSUPPORTED_GCLogEnabled:
        case UNSUPPORTED_GCLogFile:
        case UNSUPPORTED_GCLogFileSize:
        case EXTERNAL_GCStressStart:
        case INTERNAL_GCStressStartAtJit:
        case INTERNAL_DbgDACSkipVerifyDlls:
            return 0;

        case Config_COUNT:
        default:
#pragma warning(suppress:4127) // Constant conditional expression in ASSERT below
            ASSERT(!"Unknown config value type");
            return 0;
        }
    }

    static HRESULT GetConfigValue(CLRConfigTypes eType, PWSTR * outVal) 
    { 
        *outVal = NULL; 
        return 0; 
    }
};


// -----------------------------------------------------------------------------------------------------------
//
// Helper classes expected by the GC
//

class EEThreadId
{
public:
    EEThreadId(UINT32 uiId) : m_uiId(uiId) {}
    bool IsSameThread()
    {
        return m_uiId == GetCurrentThreadId();
    }

private:
    UINT32 m_uiId;
};

#define CRST_REENTRANCY         0
#define CRST_UNSAFE_SAMELEVEL   0
#define CRST_UNSAFE_ANYMODE     0
#define CRST_DEBUGGER_THREAD    0
#define CRST_DEFAULT            0

#define CrstHandleTable         0

typedef int CrstFlags;
typedef int CrstType;

class CrstStatic
{
    CRITICAL_SECTION m_cs;
#ifdef _DEBUG
    UINT32 m_holderThreadId;
#endif

public:
    bool InitNoThrow(CrstType eType, CrstFlags eFlags = CRST_DEFAULT)
    {
        UnsafeInitializeCriticalSection(&m_cs);
        return true;
    }

    void Destroy()
    {
        UnsafeDeleteCriticalSection(&m_cs);
    }

    void Enter()
    {
        UnsafeEEEnterCriticalSection(&m_cs);
#ifdef _DEBUG
        m_holderThreadId = GetCurrentThreadId();
#endif
    }

    void Leave()
    {
#ifdef _DEBUG
        m_holderThreadId = 0;
#endif
        UnsafeEELeaveCriticalSection(&m_cs);
    }

#ifdef _DEBUG
    EEThreadId GetHolderThreadId()
    {
        return m_holderThreadId;
    }

    bool OwnedByCurrentThread()
    {
        return GetHolderThreadId().IsSameThread();
    }
#endif
};

class CLREventStatic
{
public:
    void CreateManualEvent(bool bInitialState);
    void CreateAutoEvent(bool bInitialState);
    void CreateOSManualEvent(bool bInitialState);
    void CreateOSAutoEvent(bool bInitialState);
    void CloseEvent();
    bool IsValid() const;
    bool Set();
    bool Reset();
    uint32_t Wait(uint32_t dwMilliseconds, bool bAlertable);
    HANDLE GetOSEvent();

private:
    HANDLE  m_hEvent;
    bool    m_fInitialized;
};

class CrstHolder
{
    CrstStatic * m_pLock;

public:
    CrstHolder(CrstStatic * pLock)
        : m_pLock(pLock)
    {
        m_pLock->Enter();
    }

    ~CrstHolder()
    {
        m_pLock->Leave();
    }
};

class CrstHolderWithState
{
    CrstStatic * m_pLock;
    bool m_fAcquired;

public:
    CrstHolderWithState(CrstStatic * pLock, bool fAcquire = true)
        : m_pLock(pLock), m_fAcquired(fAcquire)
    {
        if (fAcquire)
            m_pLock->Enter();
    }

    ~CrstHolderWithState()
    {
        if (m_fAcquired)
            m_pLock->Leave();
    }

    void Acquire()
    {
        if (!m_fAcquired)
        {
            m_pLock->Enter();
            m_fAcquired = true;
        }
    }

    void Release()
    {
        if (m_fAcquired)
        {
            m_pLock->Leave();
            m_fAcquired = false;
        }
    }

    CrstStatic * GetValue()
    {
        return m_pLock;
    }
};


template <typename TYPE>
class NewHolder
{
    TYPE * m_value;
    bool m_fSuppressRelease;

public:
    NewHolder(TYPE * value)
        : m_value(value), m_fSuppressRelease(false)
    {
    }

    FORCEINLINE operator TYPE *() const
    {
        return this->m_value;
    }
    FORCEINLINE const TYPE * &operator->() const
    {
        return this->m_value;
    }

    void SuppressRelease()
    {
        m_fSuppressRelease = true;
    }

    ~NewHolder()
    {
        if (!m_fSuppressRelease)
            delete m_value;
    }
};

inline bool FitsInU1(unsigned __int64 val)
{
    return val == (unsigned __int64)(unsigned __int8)val;
}

// -----------------------------------------------------------------------------------------------------------
//
// AppDomain emulation. The we don't have these in Redhawk so instead we emulate the bare minimum of the API
// touched by the GC/HandleTable and pretend we have precisely one (default) appdomain.
//

#define RH_DEFAULT_DOMAIN_ID 1

struct ADIndex
{
    DWORD m_dwIndex;

    ADIndex () : m_dwIndex(RH_DEFAULT_DOMAIN_ID) {}
    explicit ADIndex (DWORD id) : m_dwIndex(id) {}
    BOOL operator==(const ADIndex& ad) const { return m_dwIndex == ad.m_dwIndex; }
    BOOL operator!=(const ADIndex& ad) const { return m_dwIndex != ad.m_dwIndex; }
};

class AppDomain
{
public:
    ADIndex GetIndex() { return ADIndex(RH_DEFAULT_DOMAIN_ID); }
    BOOL IsRudeUnload() { return FALSE; }
    BOOL NoAccessToHandleTable() { return FALSE; }
    void DecNumSizedRefHandles() {}
};

class SystemDomain
{
public:
    static SystemDomain *System() { return NULL; }
    static AppDomain *GetAppDomainAtIndex(ADIndex index) { return (AppDomain *)-1; }
    static AppDomain *AppDomainBeingUnloaded() { return NULL; }
    AppDomain *DefaultDomain() { return NULL; }
    DWORD GetTotalNumSizedRefHandles() { return 0; }
};
