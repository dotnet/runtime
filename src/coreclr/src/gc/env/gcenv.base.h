//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//
#ifndef __GCENV_BASE_INCLUDED__
#define __GCENV_BASE_INCLUDED__
//
// Sets up basic environment for CLR GC
//

#define FEATURE_REDHAWK 1
#define FEATURE_CONSERVATIVE_GC 1

#define GCENV_INCLUDED

#define REDHAWK_PALIMPORT extern "C"
#define REDHAWK_PALAPI __stdcall

#ifndef _MSC_VER
#define __stdcall
#ifdef __clang__
#define __forceinline __attribute__((always_inline))
#else // __clang__
#define __forceinline inline
#endif // __clang__
#endif // !_MSC_VER

#define SIZE_T_MAX ((size_t)-1)
#define SSIZE_T_MAX ((ptrdiff_t)(SIZE_T_MAX / 2))

#ifndef _INC_WINDOWS
// -----------------------------------------------------------------------------------------------------------
//
// Aliases for Win32 types
//

typedef uint32_t BOOL;
typedef uint32_t DWORD;
typedef void* LPVOID;
typedef uint32_t UINT;
typedef int32_t LONG;
typedef uintptr_t ULONG_PTR;
typedef void VOID;
typedef void* PVOID;
typedef void * LPSECURITY_ATTRIBUTES;
typedef void const * LPCVOID;
typedef wchar_t * PWSTR, *LPWSTR;
typedef const wchar_t *LPCWSTR, *PCWSTR;
typedef size_t SIZE_T;

typedef void * HANDLE;

// -----------------------------------------------------------------------------------------------------------
// HRESULT subset.

#ifdef PLATFORM_UNIX
typedef int32_t HRESULT;
#else
// this must exactly match the typedef used by windows.h
typedef long HRESULT;
#endif

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
#define ERROR_TIMEOUT           1460

#define TRUE true
#define FALSE false

#define CALLBACK __stdcall
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

#ifdef PLATFORM_UNIX
#define  _vsnprintf vsnprintf
#define sprintf_s snprintf
#define swprintf_s swprintf
#endif

#define WINAPI __stdcall

typedef DWORD (WINAPI *PTHREAD_START_ROUTINE)(void* lpThreadParameter);

#define WAIT_OBJECT_0           0
#define WAIT_TIMEOUT            258
#define WAIT_FAILED             0xFFFFFFFF

#if defined(_MSC_VER) 
 #if defined(_ARM_)
  
  __forceinline void YieldProcessor() { }
  extern "C" void __emit(const unsigned __int32 opcode);
  #pragma intrinsic(__emit)
  #define MemoryBarrier() { __emit(0xF3BF); __emit(0x8F5F); }
 
 #elif defined(_ARM64_)

  extern "C" void __yield(void);
  #pragma intrinsic(__yield)
  __forceinline void YieldProcessor() { __yield();}

  extern "C" void __dmb(const unsigned __int32 _Type);
  #pragma intrinsic(__dmb)
  #define MemoryBarrier() { __dmb(_ARM64_BARRIER_SY); }

 #elif defined(_AMD64_)
  
  extern "C" VOID
  _mm_pause (
      VOID
      );
  
  extern "C" VOID
  _mm_mfence (
      VOID
      );
  
  #pragma intrinsic(_mm_pause)
  #pragma intrinsic(_mm_mfence)
  
  #define YieldProcessor _mm_pause
  #define MemoryBarrier _mm_mfence
  
 #elif defined(_X86_)
  
  #define YieldProcessor() __asm { rep nop }
  
  __forceinline void MemoryBarrier()
  {
      int32_t Barrier;
      __asm {
          xchg Barrier, eax
      }
  }
  
 #else // !_ARM_ && !_AMD64_ && !_X86_
  #error Unsupported architecture
 #endif
#else // _MSC_VER

#endif // _MSC_VER

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
#ifdef DACCESS_COMPILE
#include "daccess.h"
#else // DACCESS_COMPILE
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

struct _DacGlobals;
#endif // DACCESS_COMPILE

typedef DPTR(size_t)    PTR_size_t;
typedef DPTR(uint8_t)   PTR_uint8_t;

// -----------------------------------------------------------------------------------------------------------

#define DATA_ALIGNMENT sizeof(uintptr_t)

#define RAW_KEYWORD(x) x

#define DECLSPEC_ALIGN(x)   __declspec(align(x))

#define OS_PAGE_SIZE 4096

#ifndef _ASSERTE
#define _ASSERTE(_expr) ASSERT(_expr)
#endif

#define CONSISTENCY_CHECK(_expr) ASSERT(_expr)

#define PREFIX_ASSUME(cond) ASSERT(cond)

#define EEPOLICY_HANDLE_FATAL_ERROR(error) ASSERT(!"EEPOLICY_HANDLE_FATAL_ERROR")

#define UI64(_literal) _literal##ULL

class ObjHeader;
class MethodTable;
class Object;
class ArrayBase;

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

#define VALIDATEOBJECTREF(_objref) _objref;

#define VOLATILE(T) T volatile

//
// This code is extremely compiler- and CPU-specific, and will need to be altered to 
// support new compilers and/or CPUs.  Here we enforce that we can only compile using
// VC++, or Clang on x86, AMD64, ARM and ARM64.
// 
#if !defined(_MSC_VER) && !defined(__clang__)
#error The Volatile type is currently only defined for Visual C++ and Clang
#endif

#if defined(__clang__) && !defined(_X86_) && !defined(_AMD64_) && !defined(_ARM_) && !defined(_ARM64_)
#error The Volatile type is currently only defined for Clang when targeting x86, AMD64, ARM or ARM64 CPUs
#endif

#if defined(__clang__)
#if defined(_ARM_) || defined(_ARM64_)
// This is functionally equivalent to the MemoryBarrier() macro used on ARM on Windows.
#define VOLATILE_MEMORY_BARRIER() asm volatile ("dmb sy" : : : "memory")
#else
//
// For Clang, we prevent reordering by the compiler by inserting the following after a volatile
// load (to prevent subsequent operations from moving before the read), and before a volatile 
// write (to prevent prior operations from moving past the write).  We don't need to do anything
// special to prevent CPU reorderings, because the x86 and AMD64 architectures are already
// sufficiently constrained for our purposes.  If we ever need to run on weaker CPU architectures
// (such as PowerPC), then we will need to do more work.
// 
// Please do not use this macro outside of this file.  It is subject to change or removal without
// notice.
//
#define VOLATILE_MEMORY_BARRIER() asm volatile ("" : : : "memory")
#endif // !_ARM_
#elif defined(_ARM_) && _ISO_VOLATILE
// ARM has a very weak memory model and very few tools to control that model. We're forced to perform a full
// memory barrier to preserve the volatile semantics. Technically this is only necessary on MP systems but we
// currently don't have a cheap way to determine the number of CPUs from this header file. Revisit this if it
// turns out to be a performance issue for the uni-proc case.
#define VOLATILE_MEMORY_BARRIER() MemoryBarrier()
#else
//
// On VC++, reorderings at the compiler and machine level are prevented by the use of the 
// "volatile" keyword in VolatileLoad and VolatileStore.  This should work on any CPU architecture
// targeted by VC++ with /iso_volatile-.
//
#define VOLATILE_MEMORY_BARRIER()
#endif

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

template<typename T>
inline
T VolatileLoadWithoutBarrier(T const * pt)
{
#ifndef DACCESS_COMPILE
    T val = *(T volatile const *)pt;
#else
    T val = *pt;
#endif
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

extern GCSystemInfo g_SystemInfo;

extern MethodTable * g_pFreeObjectMethodTable;

extern int32_t g_TrapReturningThreads;

extern bool g_fFinalizerRunOnShutDown;

//
// Locks
//

struct alloc_context;
class Thread;

Thread * GetThread();

typedef void (CALLBACK *HANDLESCANPROC)(PTR_UNCHECKED_OBJECTREF pref, uintptr_t *pExtraInfo, uintptr_t param1, uintptr_t param2);

class FinalizerThread
{
public:
    static bool Initialize();
    static void EnableFinalization();

    static bool HaveExtraWorkForFinalizer();

    static bool IsCurrentThreadFinalizer();
    static void Wait(DWORD timeout, bool allowReentrantWait = false);
    static bool WatchDog();
    static void SignalFinalizationDone(bool fFinalizer);
    static void SetFinalizerThread(Thread * pThread);
    static HANDLE GetFinalizerEvent();
};

#ifdef FEATURE_REDHAWK
typedef uint32_t (__stdcall *BackgroundCallback)(void* pCallbackContext);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalStartBackgroundGCThread(BackgroundCallback callback, void* pCallbackContext);

enum PalCapability
{
    WriteWatchCapability                = 0x00000001,   // GetWriteWatch() and friends
    LowMemoryNotificationCapability     = 0x00000002,   // CreateMemoryResourceNotification() and friends
    GetCurrentProcessorNumberCapability = 0x00000004,   // GetCurrentProcessorNumber()
};

REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalHasCapability(PalCapability capability);
#endif // FEATURE_REDHAWK

void DestroyThread(Thread * pThread);

bool IsGCSpecialThread();

inline bool dbgOnly_IsSpecialEEThread()
{
    return false;
}

#define ClrFlsSetThreadType(type)

//
// Performance logging
//

#define COUNTER_ONLY(x)

//#include "etmdummy.h"
//#define ETW_EVENT_ENABLED(e,f) false

namespace ETW
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

//
// Logging
//

#ifdef _MSC_VER
#define SUPPRESS_WARNING_4127   \
    __pragma(warning(push))     \
    __pragma(warning(disable:4127)) /* conditional expression is constant*/
#define POP_WARNING_STATE       \
    __pragma(warning(pop))
#else // _MSC_VER
#define SUPPRESS_WARNING_4127
#define POP_WARNING_STATE
#endif // _MSC_VER

#define WHILE_0             \
    SUPPRESS_WARNING_4127   \
    while(0)                \
    POP_WARNING_STATE       \

#define LOG(x)

VOID LogSpewAlways(const char *fmt, ...);

#define LL_INFO10 4

#define STRESS_LOG_VA(msg)                                              do { } WHILE_0
#define STRESS_LOG0(facility, level, msg)                               do { } WHILE_0
#define STRESS_LOG1(facility, level, msg, data1)                        do { } WHILE_0
#define STRESS_LOG2(facility, level, msg, data1, data2)                 do { } WHILE_0
#define STRESS_LOG3(facility, level, msg, data1, data2, data3)          do { } WHILE_0
#define STRESS_LOG4(facility, level, msg, data1, data2, data3, data4)   do { } WHILE_0
#define STRESS_LOG5(facility, level, msg, data1, data2, data3, data4, data5)   do { } WHILE_0
#define STRESS_LOG6(facility, level, msg, data1, data2, data3, data4, data5, data6)   do { } WHILE_0
#define STRESS_LOG7(facility, level, msg, data1, data2, data3, data4, data5, data6, data7)   do { } WHILE_0
#define STRESS_LOG_PLUG_MOVE(plug_start, plug_end, plug_delta)          do { } WHILE_0
#define STRESS_LOG_ROOT_PROMOTE(root_addr, objPtr, methodTable)         do { } WHILE_0
#define STRESS_LOG_ROOT_RELOCATE(root_addr, old_value, new_value, methodTable) do { } WHILE_0
#define STRESS_LOG_GC_START(gcCount, Gen, collectClasses)               do { } WHILE_0
#define STRESS_LOG_GC_END(gcCount, Gen, collectClasses)                 do { } WHILE_0
#define STRESS_LOG_OOM_STACK(size)   do { } while(0)
#define STRESS_LOG_RESERVE_MEM(numChunks) do {} while (0)
#define STRESS_LOG_GC_STACK

#define DEFAULT_GC_PRN_LVL 3

// -----------------------------------------------------------------------------------------------------------

void StompWriteBarrierEphemeral();
void StompWriteBarrierResize(bool bReqUpperBoundsCheck);

class CLRConfig
{
public:
    enum CLRConfigTypes
    {
        UNSUPPORTED_GCLogEnabled,
        UNSUPPORTED_GCLogFile,
        UNSUPPORTED_GCLogFileSize,
        UNSUPPORTED_GCConfigLogEnabled,
        UNSUPPORTED_GCConfigLogFile,
        UNSUPPORTED_BGCSpinCount,
        UNSUPPORTED_BGCSpin,
        EXTERNAL_GCStressStart,
        INTERNAL_GCStressStartAtJit,
        INTERNAL_DbgDACSkipVerifyDlls,
        Config_COUNT
    };

    typedef CLRConfigTypes ConfigDWORDInfo;
    typedef CLRConfigTypes ConfigStringInfo;

    static uint32_t GetConfigValue(ConfigDWORDInfo eType);
    static HRESULT GetConfigValue(ConfigStringInfo /*eType*/, wchar_t * * outVal);
};

inline bool FitsInU1(uint64_t val)
{
    return val == (uint64_t)(uint8_t)val;
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
    static AppDomain *GetAppDomainAtIndex(ADIndex /*index*/) { return (AppDomain *)-1; }
    static AppDomain *AppDomainBeingUnloaded() { return NULL; }
    AppDomain *DefaultDomain() { return NULL; }
    DWORD GetTotalNumSizedRefHandles() { return 0; }
};

#ifdef STRESS_HEAP
namespace GCStressPolicy
{
    static volatile int32_t s_cGcStressDisables;

    inline bool IsEnabled() { return s_cGcStressDisables == 0; }
    inline void GlobalDisable() { Interlocked::Increment(&s_cGcStressDisables); }
    inline void GlobalEnable() { Interlocked::Decrement(&s_cGcStressDisables); }
}

enum gcs_trigger_points
{
    cfg_any,
};

template <enum gcs_trigger_points tp>
class GCStress
{
public:
    static inline bool IsEnabled()
    {
        return g_pConfig->GetGCStressLevel() != 0;
    }
};
#endif // STRESS_HEAP

#endif // __GCENV_BASE_INCLUDED__
