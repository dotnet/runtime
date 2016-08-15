// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// util.hpp
//

//
// Miscellaneous useful functions
//

#ifndef _H_UTIL
#define _H_UTIL

#define MAX_UINT32_HEX_CHAR_LEN 8 // max number of chars representing an unsigned int32, not including terminating null char.
#define MAX_INT32_DECIMAL_CHAR_LEN 11 // max number of chars representing an int32, including sign, not including terminating null char.

#include "utilcode.h"
#include "metadata.h"
#include "holderinst.h"
#include "clrdata.h"
#include "xclrdata.h"
#include "posterror.h"
#include "clr_std/type_traits"

                  
// Prevent the use of UtilMessageBox and WszMessageBox from inside the EE.
#undef UtilMessageBoxCatastrophic
#undef UtilMessageBoxCatastrophicNonLocalized
#undef UtilMessageBoxCatastrophic
#undef UtilMessageBoxCatastrophicNonLocalizedVA
#undef UtilMessageBox
#undef UtilMessageBoxNonLocalized
#undef UtilMessageBoxVA
#undef UtilMessageBoxNonLocalizedVA
#undef WszMessageBox
#define UtilMessageBoxCatastrophic __error("Use one of the EEMessageBox APIs (defined in eemessagebox.h) from inside the EE")
#define UtilMessageBoxCatastrophicNonLocalized __error("Use one of the EEMessageBox APIs (defined in eemessagebox.h) from inside the EE")
#define UtilMessageBoxCatastrophicVA __error("Use one of the EEMessageBox APIs (defined in eemessagebox.h) from inside the EE")
#define UtilMessageBoxCatastrophicNonLocalizedVA __error("Use one of the EEMessageBox APIs (defined in eemessagebox.h) from inside the EE")
#define UtilMessageBox __error("Use one of the EEMessageBox APIs (defined in eemessagebox.h) from inside the EE")
#define UtilMessageBoxNonLocalized __error("Use one of the EEMessageBox APIs (defined in eemessagebox.h) from inside the EE")
#define UtilMessageBoxVA __error("Use one of the EEMessageBox APIs (defined in eemessagebox.h) from inside the EE")
#define UtilMessageBoxNonLocalizedVA __error("Use one of the EEMessageBox APIs (defined in eemessagebox.h) from inside the EE")
#define WszMessageBox __error("Use one of the EEMessageBox APIs (defined in eemessagebox.h) from inside the EE")

//========================================================================
// More convenient names for integer types of a guaranteed size.
//========================================================================

typedef __int8              I1;
typedef ArrayDPTR(I1)       PTR_I1;
typedef unsigned __int8     U1;
typedef __int16             I2;
typedef unsigned __int16    U2;
typedef __int32             I4;
typedef unsigned __int32    U4;
typedef __int64             I8;
typedef unsigned __int64    U8;
typedef float               R4;
typedef double              R8;

//
// Forward the FastInterlock methods to the matching Win32 APIs. They are implemented
// using compiler intrinsics so they are as fast as they can possibly be.
//

//
// these don't have corresponding compiler intrinsics
//

#ifdef FEATURE_SINGLE_THREADED
#define FastInterlockIncrement(a)   (++(*a))
#define FastInterlockDecrement(a)   (--(*a))
#define FastInterlockOr(a, b)   (*a |= (DWORD)b)
#define FastInterlockAnd(a, b)   (*a &= (DWORD)b)
#define FastInterlockIncrementLong(a)   (++(*a))
#define FastInterlockDecrementLong(a)   (--(*a))
#define FastInterlockOrLong(a, b)   (*a |= (UINT64)b)
#define FastInterlockAndLong(a, b)   (*a &= (UINT64)b)
#define FastInterlockCompareExchange        InterlockedCompareExchange
#define FastInterlockCompareExchangePointer InterlockedCompareExchangeT

#else

//
// these DO have corresponding compiler intrinsics
//
#define FastInterlockIncrement              InterlockedIncrement
#define FastInterlockDecrement              InterlockedDecrement
#define FastInterlockExchange               InterlockedExchange
#define FastInterlockCompareExchange        InterlockedCompareExchange
#define FastInterlockExchangeAdd            InterlockedExchangeAdd
#define FastInterlockExchangeLong           InterlockedExchange64
#define FastInterlockCompareExchangeLong    InterlockedCompareExchange64
#define FastInterlockExchangeAddLong        InterlockedExchangeAdd64

//
// Forward FastInterlock[Compare]ExchangePointer to the 
// Utilcode Interlocked[Compare]ExchangeT.
// 
#define FastInterlockExchangePointer        InterlockedExchangeT
#define FastInterlockCompareExchangePointer InterlockedCompareExchangeT

FORCEINLINE void FastInterlockOr(DWORD RAW_KEYWORD(volatile) *p, const int msk)
{
    LIMITED_METHOD_CONTRACT;

    InterlockedOr((LONG *)p, msk);
}

FORCEINLINE void FastInterlockAnd(DWORD RAW_KEYWORD(volatile) *p, const int msk)
{
    LIMITED_METHOD_CONTRACT;

    InterlockedAnd((LONG *)p, msk);
}

#endif

#ifndef FEATURE_PAL
// Copied from malloc.h: don't want to bring in the whole header file.
void * __cdecl _alloca(size_t);
#endif // !FEATURE_PAL

#ifdef _PREFAST_
// Suppress prefast warning #6255: alloca indicates failure by raising a stack overflow exception
#pragma warning(disable:6255)
#endif // _PREFAST_

// Function to parse apart a command line and return the
// arguments just like argv and argc
LPWSTR* CommandLineToArgvW(__in LPWSTR lpCmdLine, DWORD *pNumArgs);
#define ISWWHITE(x) ((x)==W(' ') || (x)==W('\t') || (x)==W('\n') || (x)==W('\r') )

BOOL inline FitsInI1(__int64 val)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return val == (__int64)(__int8)val;
}

BOOL inline FitsInI2(__int64 val)
{
    LIMITED_METHOD_CONTRACT;
    return val == (__int64)(__int16)val;
}

BOOL inline FitsInI4(__int64 val)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return val == (__int64)(__int32)val;
}

BOOL inline FitsInU1(unsigned __int64 val)
{
    LIMITED_METHOD_CONTRACT;
    return val == (unsigned __int64)(unsigned __int8)val;
}

BOOL inline FitsInU2(unsigned __int64 val)
{
    LIMITED_METHOD_CONTRACT;
    return val == (unsigned __int64)(unsigned __int16)val;
}

BOOL inline FitsInU4(unsigned __int64 val)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return val == (unsigned __int64)(unsigned __int32)val;
}

// returns FALSE if overflows 15 bits: otherwise, (*pa) is incremented by b
BOOL inline SafeAddUINT15(UINT16 *pa, ULONG b)
{
    LIMITED_METHOD_CONTRACT;

    UINT16 a = *pa;
    // first check if overflows 16 bits
    if ( ((UINT16)b) != b )
    {
        return FALSE;
    }
    // now make sure that doesn't overflow 15 bits
    if (((ULONG)a + b) > 0x00007FFF)
    {
        return FALSE;
    }
    (*pa) += (UINT16)b;
    return TRUE;
}


// returns FALSE if overflows 16 bits: otherwise, (*pa) is incremented by b
BOOL inline SafeAddUINT16(UINT16 *pa, ULONG b)
{
    UINT16 a = *pa;
    if ( ((UINT16)b) != b )
    {
        return FALSE;
    }
    // now make sure that doesn't overflow 16 bits
    if (((ULONG)a + b) > 0x0000FFFF)
    {
        return FALSE;
    }
    (*pa) += (UINT16)b;
    return TRUE;
}


// returns FALSE if overflow: otherwise, (*pa) is incremented by b
BOOL inline SafeAddUINT32(UINT32 *pa, UINT32 b)
{
    LIMITED_METHOD_CONTRACT;

    UINT32 a = *pa;
    if ( ((UINT32)(a + b)) < a)
    {
        return FALSE;
    }
    (*pa) += b;
    return TRUE;
}

// returns FALSE if overflow: otherwise, (*pa) is incremented by b
BOOL inline SafeAddULONG(ULONG *pa, ULONG b)
{
    LIMITED_METHOD_CONTRACT;

    ULONG a = *pa;
    if ( ((ULONG)(a + b)) < a)
    {
        return FALSE;
    }
    (*pa) += b;
    return TRUE;
}

// returns FALSE if overflow: otherwise, (*pa) is multiplied by b
BOOL inline SafeMulSIZE_T(SIZE_T *pa, SIZE_T b)
{
    LIMITED_METHOD_CONTRACT;

#ifdef _DEBUG_IMPL
    {
        //Make sure SIZE_T is unsigned
        SIZE_T m = ((SIZE_T)(-1));
        SIZE_T z = 0;
        _ASSERTE(m > z);
    }
#endif


    SIZE_T a = *pa;
    const SIZE_T m = ((SIZE_T)(-1));
    if ( (m / b) < a )
    {
        return FALSE;
    }
    (*pa) *= b;
    return TRUE;
}



//************************************************************************
// CQuickHeap
//
// A fast non-multithread-safe heap for short term use.
// Destroying the heap frees all blocks allocated from the heap.
// Blocks cannot be freed individually.
//
// The heap uses COM+ exceptions to report errors.
//
// The heap does not use any internal synchronization so it is not
// multithreadsafe.
//************************************************************************
class CQuickHeap
{
    public:
        CQuickHeap();
        ~CQuickHeap();

        //---------------------------------------------------------------
        // Allocates a block of "sz" bytes. If there's not enough
        // memory, throws an OutOfMemoryError.
        //---------------------------------------------------------------
        LPVOID Alloc(UINT sz);


    private:
        enum {
#ifdef _DEBUG
            kBlockSize = 24
#else
            kBlockSize = 1024
#endif
        };

        // The QuickHeap allocates QuickBlock's as needed and chains
        // them in a single-linked list. Most QuickBlocks have a size
        // of kBlockSize bytes (not counting m_next), and individual
        // allocation requests are suballocated from them.
        // Allocation requests of greater than kBlockSize are satisfied
        // by allocating a special big QuickBlock of the right size.
        struct QuickBlock
        {
            QuickBlock  *m_next;
            BYTE         m_bytes[1];
        };


        // Linked list of QuickBlock's.
        QuickBlock      *m_pFirstQuickBlock;

        // Offset to next available byte in m_pFirstQuickBlock.
        LPBYTE           m_pNextFree;

        // Linked list of big QuickBlock's
        QuickBlock      *m_pFirstBigQuickBlock;

};

//======================================================================
// String Helpers
//
//
//
ULONG StringHashValueW(__in LPWSTR wzString);
ULONG StringHashValueA(LPCSTR szString);

void PrintToStdOutA(const char *pszString);
void PrintToStdOutW(const WCHAR *pwzString);
void PrintToStdErrA(const char *pszString);
void PrintToStdErrW(const WCHAR *pwzString);
void NPrintToStdOutA(const char *pszString, size_t nbytes);
void NPrintToStdOutW(const WCHAR *pwzString, size_t nchars);
void NPrintToStdErrA(const char *pszString, size_t nbytes);
void NPrintToStdErrW(const WCHAR *pwzString, size_t nchars);


//=====================================================================
// Function for formatted text output to the debugger
//
//
void __cdecl VMDebugOutputA(__in LPSTR format, ...);
void __cdecl VMDebugOutputW(__in LPWSTR format, ...);

//=====================================================================
// VM-safe wrapper for PostError.
//
HRESULT VMPostError(                    // Returned error.
    HRESULT     hrRpt,                  // Reported error.
    ...);                               // Error arguments.
    
//=====================================================================
// Displays the messaage box or logs the message, corresponding to the last COM+ error occurred
void VMDumpCOMErrors(HRESULT hrErr);

#include "nativevaraccessors.h"

//======================================================================
// Stack friendly registry helpers
//
LONG UtilRegEnumKey(HKEY hKey,            // handle to key to query
                    DWORD dwIndex,        // index of subkey to query
                    CQuickWSTR* lpName);// buffer for subkey name

LONG UtilRegQueryStringValueEx(HKEY hKey,            // handle to key to query
                               LPCWSTR lpValueName,  // address of name of value to query
                               LPDWORD lpReserved,   // reserved
                               LPDWORD lpType,       // address of buffer for value type
                               CQuickWSTR* lpData);// data buffer

//======================================================================
// Event logging

BOOL ReportEventCLR (
     IN WORD       wType,       // Event type - warning, error, success, etc
     IN WORD       wCategory,   // Event category
     IN DWORD      dwEventID,   // Event identifier (defined in shimr\msg.mc)
     IN PSID       lpUserSid,   // user's security identifier
     IN SString  * message      // message to log
    );

// --------------------------------------------------------------------------------
// GCX macros
//
// These are the normal way to change or assert the GC mode of a thread.  They handle
// the required stack discipline in mode switches with an autodestructor which
// automatically triggers on leaving the current scope.
//
// Usage:
// GCX_COOP();              Switch to cooperative mode, assume thread is setup
// GCX_PREEMP();            Switch to preemptive mode, NOP if no thread setup
// GCX_COOP_THREAD_EXISTS(Thread*);    Fast switch to cooperative mode, must pass non-null Thread
// GCX_PREEMP_THREAD_EXISTS(Thread*);  Fast switch to preemptive mode, must pass non-null Thread
//
// (There is an intentional asymmetry between GCX_COOP and GCX_PREEMP. GCX_COOP
// asserts if you call it without having a Thread setup. GCX_PREEMP becomes a NOP.
// This is because all unmanaged threads are effectively preemp.)
//
// (There is actually one more case here - an "EE worker thread" such as the debugger
// thread or GC thread, which we don't want to call SetupThread() on, but which is
// effectively in cooperative mode due to explicit cooperation with the collector.
// This case is not handled by these macros; the current working assumption is that
// such threads never use them. But at some point we may have to consider
// this case if there is utility code which is called from those threads.)
//
// GCX_MAYBE_*(BOOL);       Same as above, but only do the switch if BOOL is TRUE.
//
// GCX_ASSERT_*();          Same as above, but assert mode rather than switch to mode.
//                          Note that assert is applied during backout as well.
//                          No overhead in a free build.
//
// GCX_FORBID();            Add "ForbidGC" semantics to a cooperative mode situation.
//                          Asserts that the thread will not trigger a GC or
//                          reach a GC-safe point, or call anything that might
//                          do one of these things.
//
// GCX_NOTRIGGER();         "ForbidGC" without the automatic assertion for coop mode.
//
// --------------------------------------------------------------------------------

template<BOOL COOPERATIVE>
class AutoCleanupGCAssert;

template<BOOL COOPERATIVE>
class GCAssert;


typedef AutoCleanupGCAssert<TRUE>                   AutoCleanupGCAssertCoop;
typedef AutoCleanupGCAssert<FALSE>                  AutoCleanupGCAssertPreemp;

typedef GCAssert<TRUE>                  GCAssertCoop;
typedef GCAssert<FALSE>                 GCAssertPreemp;

#if !defined(CROSSGEN_COMPILE) && !defined(DACCESS_COMPILE)

#ifdef ENABLE_CONTRACTS_IMPL
#define GCX_COOP()                      GCCoop __gcHolder("GCX_COOP", __FUNCTION__, __FILE__, __LINE__)
#define GCX_COOP_NO_DTOR()              GCCoopNoDtor __gcHolder; __gcHolder.Enter(TRUE, "GCX_COOP_NO_DTOR", __FUNCTION__, __FILE__, __LINE__)
#define GCX_COOP_NO_DTOR_END()          __gcHolder.Leave();
#else
#define GCX_COOP()                      GCCoop __gcHolder
#define GCX_COOP_NO_DTOR()              GCCoopNoDtor __gcHolder; __gcHolder.Enter(TRUE)
#define GCX_COOP_NO_DTOR_END()          __gcHolder.Leave();
#endif

#ifdef ENABLE_CONTRACTS_IMPL
#define GCX_PREEMP()                                    GCPreemp __gcHolder("GCX_PREEMP", __FUNCTION__, __FILE__, __LINE__)
#define GCX_PREEMP_NO_DTOR()                            GCPreempNoDtor __gcHolder; __gcHolder.Enter(TRUE, "GCX_PREEMP_NO_DTOR", __FUNCTION__, __FILE__, __LINE__)
#define GCX_PREEMP_NO_DTOR_HAVE_THREAD(curThreadNullOk) GCPreempNoDtor __gcHolder; __gcHolder.Enter(curThreadNullOk, TRUE, "GCX_PREEMP_NO_DTOR_HAVE_THREAD", __FUNCTION__, __FILE__, __LINE__)
#define GCX_PREEMP_NO_DTOR_END()                        __gcHolder.Leave();
#else
#define GCX_PREEMP()                                    GCPreemp __gcHolder
#define GCX_PREEMP_NO_DTOR_HAVE_THREAD(curThreadNullOk) GCPreempNoDtor __gcHolder; __gcHolder.Enter(curThreadNullOk, TRUE)
#define GCX_PREEMP_NO_DTOR()                            GCPreempNoDtor __gcHolder; __gcHolder.Enter(TRUE)
#define GCX_PREEMP_NO_DTOR_END()                        __gcHolder.Leave()
#endif

#ifdef ENABLE_CONTRACTS_IMPL
#define GCX_COOP_THREAD_EXISTS(curThread)   GCCoopThreadExists __gcHolder((curThread), "GCX_COOP_THREAD_EXISTS",  __FUNCTION__, __FILE__, __LINE__)
#else
#define GCX_COOP_THREAD_EXISTS(curThread)   GCCoopThreadExists __gcHolder((curThread))
#endif

#ifdef ENABLE_CONTRACTS_IMPL
#define GCX_PREEMP_THREAD_EXISTS(curThread) GCPreempThreadExists __gcHolder((curThread), "GCX_PREEMP_THREAD_EXISTS",  __FUNCTION__, __FILE__, __LINE__)
#else
#define GCX_PREEMP_THREAD_EXISTS(curThread) GCPreempThreadExists __gcHolder((curThread))
#endif

#ifdef ENABLE_CONTRACTS_IMPL
#define GCX_MAYBE_COOP(_cond)                             GCCoop __gcHolder(_cond, "GCX_MAYBE_COOP",  __FUNCTION__, __FILE__, __LINE__)
#define GCX_MAYBE_COOP_NO_DTOR(_cond)   GCCoopNoDtor __gcHolder; __gcHolder.Enter(_cond, "GCX_MAYBE_COOP_NO_DTOR", __FUNCTION__, __FILE__, __LINE__)
#define GCX_MAYBE_COOP_NO_DTOR_END()    __gcHolder.Leave();
#else
#define GCX_MAYBE_COOP(_cond)                             GCCoop __gcHolder(_cond)
#define GCX_MAYBE_COOP_NO_DTOR(_cond)   GCCoopNoDtor __gcHolder; __gcHolder.Enter(_cond)
#define GCX_MAYBE_COOP_NO_DTOR_END()    __gcHolder.Leave();
#endif

#ifdef ENABLE_CONTRACTS_IMPL
#define GCX_MAYBE_PREEMP(_cond)                           GCPreemp __gcHolder(_cond, "GCX_MAYBE_PREEMP",  __FUNCTION__, __FILE__, __LINE__)
#define GCX_MAYBE_PREEMP_NO_DTOR(_cond) GCPreempNoDtor __gcHolder; __gcHolder.Enter(_cond, "GCX_MAYBE_PREEMP_NO_DTOR", __FUNCTION__, __FILE__, __LINE__)
#define GCX_MAYBE_PREEMP_NO_DTOR_END()  __gcHolder.Leave();
#else
#define GCX_MAYBE_PREEMP(_cond)                           GCPreemp __gcHolder(_cond)
#define GCX_MAYBE_PREEMP_NO_DTOR(_cond) GCPreempNoDtor __gcHolder; __gcHolder.Enter(_cond)
#define GCX_MAYBE_PREEMP_NO_DTOR_END()  __gcHolder.Leave()
#endif

#ifdef ENABLE_CONTRACTS_IMPL
#define GCX_MAYBE_COOP_THREAD_EXISTS(curThread, _cond)    GCCoopThreadExists __gcHolder((curThread), (_cond), "GCX_MAYBE_COOP_THREAD_EXISTS",  __FUNCTION__, __FILE__, __LINE__)
#else
#define GCX_MAYBE_COOP_THREAD_EXISTS(curThread, _cond)    GCCoopThreadExists __gcHolder((curThread), (_cond))
#endif

#ifdef ENABLE_CONTRACTS_IMPL
#define GCX_MAYBE_PREEMP_THREAD_EXISTS(curThread, _cond)  GCPreempThreadExists __gcHolder((curThread), (_cond), "GCX_MAYBE_PREEMP_THREAD_EXISTS",  __FUNCTION__, __FILE__, __LINE__)
#else
#define GCX_MAYBE_PREEMP_THREAD_EXISTS(curThread, _cond)  GCPreempThreadExists __gcHolder((curThread), (_cond))
#endif

// This has a potential race with the GC thread.  It is currently
// used for a few cases where (a) we potentially haven't started up the EE yet, or
// (b) we are on a "special thread". 
#ifdef ENABLE_CONTRACTS_IMPL
#define GCX_COOP_NO_THREAD_BROKEN()                 GCCoopHackNoThread __gcHolder("GCX_COOP_NO_THREAD_BROKEN",  __FUNCTION__, __FILE__, __LINE__)
#else
#define GCX_COOP_NO_THREAD_BROKEN()                 GCCoopHackNoThread __gcHolder
#endif


#ifdef ENABLE_CONTRACTS_IMPL
#define GCX_MAYBE_COOP_NO_THREAD_BROKEN(_cond)      GCCoopHackNoThread __gcHolder(_cond, "GCX_MAYBE_COOP_NO_THREAD_BROKEN",  __FUNCTION__, __FILE__, __LINE__)
#else
#define GCX_MAYBE_COOP_NO_THREAD_BROKEN(_cond)      GCCoopHackNoThread __gcHolder(_cond)
#endif

#else // !defined(CROSSGEN_COMPILE) && !defined(DACCESS_COMPILE)

#define GCX_COOP()
#define GCX_COOP_NO_DTOR()
#define GCX_COOP_NO_DTOR_END()

#define GCX_PREEMP()
#define GCX_PREEMP_NO_DTOR()
#define GCX_PREEMP_NO_DTOR_HAVE_THREAD(curThreadNullOk)
#define GCX_PREEMP_NO_DTOR_END()

#define GCX_MAYBE_PREEMP(_cond)

#define GCX_COOP_NO_THREAD_BROKEN()
#define GCX_MAYBE_COOP_NO_THREAD_BROKEN(_cond)

#define GCX_PREEMP_THREAD_EXISTS(curThread)
#define GCX_COOP_THREAD_EXISTS(curThread)

#define GCX_POP()

#endif // !defined(CROSSGEN_COMPILE) && !defined(DACCESS_COMPILE)

#if defined(_DEBUG_IMPL) && !defined(CROSSGEN_COMPILE)

#define GCX_ASSERT_PREEMP()                 ::AutoCleanupGCAssertPreemp __gcHolder
#define GCX_ASSERT_COOP()                   ::AutoCleanupGCAssertCoop __gcHolder

#define BEGIN_GCX_ASSERT_COOP                                   \
    {                                                           \
        GCAssertCoop    __gcHolder;                             \
        __gcHolder.BeginGCAssert()

#define END_GCX_ASSERT_COOP                                     \
        __gcHolder.EndGCAssert();                               \
    }

#define BEGIN_GCX_ASSERT_PREEMP                                 \
    {                                                           \
        GCAssertPreemp  __gcHolder;                             \
        __gcHolder.BeginGCAssert()

#define END_GCX_ASSERT_PREEMP                                   \
        __gcHolder.EndGCAssert();                               \
    }


#else

#define GCX_ASSERT_PREEMP()
#define GCX_ASSERT_COOP()

#define BEGIN_GCX_ASSERT_COOP                                   \
    {
#define END_GCX_ASSERT_COOP                                     \
    }
#define BEGIN_GCX_ASSERT_PREEMP                                 \
    {
#define END_GCX_ASSERT_PREEMP                                   \
    }

#endif

#ifdef ENABLE_CONTRACTS_IMPL

#define GCX_FORBID()                        ::GCForbid __gcForbidHolder(__FUNCTION__, __FILE__, __LINE__)
#define GCX_NOTRIGGER()                     ::GCNoTrigger __gcNoTriggerHolder(__FUNCTION__, __FILE__, __LINE__)

#define GCX_MAYBE_FORBID(fConditional)      ::GCForbid __gcForbidHolder(fConditional, __FUNCTION__, __FILE__, __LINE__)
#define GCX_MAYBE_NOTRIGGER(fConditional)   ::GCNoTrigger __gcNoTriggerHolder(fConditional, __FUNCTION__, __FILE__, __LINE__)

#else


#define GCX_FORBID()
#define GCX_NOTRIGGER()

#define GCX_MAYBE_FORBID(fConditional)
#define GCX_MAYBE_NOTRIGGER(fConditional)

#endif

typedef BOOL (*FnLockOwner)(LPVOID);
struct LockOwner
{
    LPVOID lock;
    FnLockOwner lockOwnerFunc;
};

// this is the standard lockowner for things that require a lock owner but which really don't 
// need any validation due to their simple/safe semantics
// the classic example of this is a hash table that is initialized and then never grows
extern LockOwner g_lockTrustMeIAmThreadSafe;

// The OS ThreadId is not a stable ID for a thread we a host uses fiber instead of Thread.
// For each managed Thread, we have a stable and unique id in Thread object.  For other threads,
// e.g. Server GC or Concurrent GC thread, debugger helper thread, we do not have a Thread object,
// and we use OS ThreadId to identify them since they are not managed by a host.
class EEThreadId
{
private:
    void *m_FiberPtrId;
public:
#ifdef _DEBUG
    EEThreadId()
    : m_FiberPtrId(NULL)
    {
        LIMITED_METHOD_CONTRACT;
    }
#endif

    void SetToCurrentThread()
    {
        WRAPPER_NO_CONTRACT;

        m_FiberPtrId = ClrTeb::GetFiberPtrId();
    }

    bool IsCurrentThread() const
    {
        WRAPPER_NO_CONTRACT;

        return (m_FiberPtrId == ClrTeb::GetFiberPtrId());
    }

    
#ifdef _DEBUG
    bool IsUnknown() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_FiberPtrId == NULL;
    }
#endif
    void Clear()
    {
        LIMITED_METHOD_CONTRACT;
        m_FiberPtrId = NULL;
    }
};

#define CLRHOSTED           0x80000000

GVAL_DECL(DWORD, g_fHostConfig);


inline BOOL CLRHosted()
{
    LIMITED_METHOD_CONTRACT;

    return g_fHostConfig;
}

#ifndef FEATURE_PAL
HMODULE CLRGetModuleHandle(LPCWSTR lpModuleFileName);

// Equivalent to CLRGetModuleHandle(NULL) but doesn't have the INJECT_FAULT contract associated
// with CLRGetModuleHandle.
HMODULE CLRGetCurrentModuleHandle();

HMODULE CLRLoadLibraryEx(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
#endif // !FEATURE_PAL

HMODULE CLRLoadLibrary(LPCWSTR lpLibFileName);

BOOL CLRFreeLibrary(HMODULE hModule);
VOID CLRFreeLibraryAndExitThread(HMODULE hModule, DWORD dwExitCode);

LPVOID
CLRMapViewOfFileEx(
    IN HANDLE hFileMappingObject,
    IN DWORD dwDesiredAccess,
    IN DWORD dwFileOffsetHigh,
    IN DWORD dwFileOffsetLow,
    IN SIZE_T dwNumberOfBytesToMap,
    IN LPVOID lpBaseAddress
    );

LPVOID
CLRMapViewOfFile(
    IN HANDLE hFileMappingObject,
    IN DWORD dwDesiredAccess,
    IN DWORD dwFileOffsetHigh,
    IN DWORD dwFileOffsetLow,
    IN SIZE_T dwNumberOfBytesToMap
    );


BOOL
CLRUnmapViewOfFile(
    IN LPVOID lpBaseAddress
    );

BOOL CompareFiles(HANDLE hFile1,HANDLE hFile2);


#ifndef DACCESS_COMPILE
FORCEINLINE void VoidCLRUnmapViewOfFile(void *ptr) { CLRUnmapViewOfFile(ptr); }
typedef Wrapper<void *, DoNothing, VoidCLRUnmapViewOfFile> CLRMapViewHolder;
#else
typedef Wrapper<void *, DoNothing, DoNothing> CLRMapViewHolder;
#endif

void GetProcessMemoryLoad(LPMEMORYSTATUSEX pMSEX);

void ProcessEventForHost(EClrEvent event, void *data);
void ProcessSOEventForHost(EXCEPTION_POINTERS *pExceptionInfo, BOOL fInSoTolerant);
BOOL IsHostRegisteredForEvent(EClrEvent event);

#define SetupThreadForComCall(OOMRetVal)            \
    MAKE_CURRENT_THREAD_AVAILABLE_EX(GetThreadNULLOk()); \
    if (CURRENT_THREAD == NULL)                     \
    {                                               \
        CURRENT_THREAD = SetupThreadNoThrow();      \
        if (CURRENT_THREAD == NULL)                 \
            return OOMRetVal;                       \
    }                                               \


#define InternalSetupForComCall(CannotEnterRetVal, OOMRetVal, SORetVal, CheckCanRunManagedCode) \
SetupThreadForComCall(OOMRetVal);                       \
if (CheckCanRunManagedCode && !CanRunManagedCode())     \
    return CannotEnterRetVal;                           \
SO_INTOLERANT_CODE_NOTHROW(CURRENT_THREAD, return SORetVal)

#define SetupForComCallHRNoHostNotif() InternalSetupForComCall(HOST_E_CLRNOTAVAILABLE, E_OUTOFMEMORY, COR_E_STACKOVERFLOW, true)
#define SetupForComCallHRNoHostNotifNoCheckCanRunManagedCode() InternalSetupForComCall(HOST_E_CLRNOTAVAILABLE, E_OUTOFMEMORY, COR_E_STACKOVERFLOW, false)
#define SetupForComCallDWORDNoHostNotif() InternalSetupForComCall(-1, -1, -1, true)

#define SetupForComCallHR()                                                                \
InternalSetupForComCall(HOST_E_CLRNOTAVAILABLE, E_OUTOFMEMORY, COR_E_STACKOVERFLOW, true)

#define SetupForComCallHRNoCheckCanRunManagedCode()                                        \
InternalSetupForComCall(HOST_E_CLRNOTAVAILABLE, E_OUTOFMEMORY, COR_E_STACKOVERFLOW, false)

#ifdef FEATURE_CORRUPTING_EXCEPTIONS

// Since Corrupting exceptions can escape COM interop boundaries,
// these macros will be used to setup the initial SO-Intolerant transition.
#define InternalSetupForComCallWithEscapingCorruptingExceptions(CannotEnterRetVal, OOMRetVal, SORetVal, CheckCanRunManagedCode) \
if (CheckCanRunManagedCode && !CanRunManagedCode())                         \
    return CannotEnterRetVal;                                               \
SetupThreadForComCall(OOMRetVal);                                           \
BEGIN_SO_INTOLERANT_CODE_NOTHROW(CURRENT_THREAD, SORetVal)                  \

#define BeginSetupForComCallHRWithEscapingCorruptingExceptions()            \
HRESULT __hr = S_OK;                                                        \
InternalSetupForComCallWithEscapingCorruptingExceptions(HOST_E_CLRNOTAVAILABLE, E_OUTOFMEMORY, COR_E_STACKOVERFLOW, true)              \
                                                                            \
if (SUCCEEDED(__hr))                                                        \
{                                                                           \

#define EndSetupForComCallHRWithEscapingCorruptingExceptions()              \
}                                                                           \
END_SO_INTOLERANT_CODE;                                                     \
                                                                            \
if (FAILED(__hr))                                                           \
{                                                                           \
    return __hr;                                                            \
}                                                                           \

#endif // FEATURE_CORRUPTING_EXCEPTIONS

#define SetupForComCallDWORD()                                              \
InternalSetupForComCall(-1, -1, -1, true)

// Special version of SetupForComCallDWORD that doesn't call
// CanRunManagedCode() to avoid firing LoaderLock MDA
#define SetupForComCallDWORDNoCheckCanRunManagedCode()                      \
InternalSetupForComCall(-1, -1, -1, false)

#include "unsafe.h"

inline void UnsafeTlsFreeForHolder(DWORD* addr)
{
    WRAPPER_NO_CONTRACT;

    if (addr && *addr != TLS_OUT_OF_INDEXES) {
        UnsafeTlsFree(*addr);
        *addr = TLS_OUT_OF_INDEXES;
    }
}

// A holder to make sure tls slot is released and memory for allocated one is set to TLS_OUT_OF_INDEXES
typedef Holder<DWORD*, DoNothing<DWORD*>, UnsafeTlsFreeForHolder> TlsHolder;

// A holder for HMODULE.
FORCEINLINE void VoidFreeLibrary(HMODULE h) { WRAPPER_NO_CONTRACT; CLRFreeLibrary(h); }

typedef Wrapper<HMODULE, DoNothing<HMODULE>, VoidFreeLibrary, NULL> ModuleHandleHolder;

#ifndef FEATURE_PAL

// A holder for memory blocks allocated by Windows.  This holder (and any OS APIs you call
// that allocate objects on your behalf) should not be used when the CLR is memory-hosted.

FORCEINLINE void VoidFreeWinAllocatedBlock(LPVOID pv)
{
    LIMITED_METHOD_CONTRACT;

#pragma push_macro("GetProcessHeap")
#pragma push_macro("HeapFree")
#undef GetProcessHeap
#undef HeapFree
    // 0: no special flags
    ::HeapFree(::GetProcessHeap(), 0, pv);
#pragma pop_macro("HeapFree")
#pragma pop_macro("GetProcessHeap")
}

typedef Wrapper<LPVOID, DoNothing<LPVOID>, VoidFreeWinAllocatedBlock, NULL> WinAllocatedBlockHolder;

#endif // !FEATURE_PAL

// For debugging, we can track arbitrary Can't-Stop regions.
// In V1.0, this was on the Thread object, but we need to track this for threads w/o a Thread object.
FORCEINLINE void IncCantStopCount()
{
    ClrFlsIncrementValue(TlsIdx_CantStopCount, 1);
}

FORCEINLINE void DecCantStopCount()
{
    ClrFlsIncrementValue(TlsIdx_CantStopCount, -1);
}

typedef StateHolder<IncCantStopCount, DecCantStopCount> CantStopHolder;

#ifdef _DEBUG
// For debug-only, this can be used w/ a holder to ensure that we're keeping our CS count balanced.
// We should never use this w/ control flow.
inline int GetCantStopCount()
{
    return (int) (size_t) ClrFlsGetValue(TlsIdx_CantStopCount);
}

// At places where we know we're calling out to native code, we can assert that we're NOT in a CS region.
// This is _debug only since we only use it for asserts; not for real code-flow control in a retail build.
inline bool IsInCantStopRegion()
{    
    return (GetCantStopCount() > 0);
}
#endif // _DEBUG


// PAL does not support per-thread locales. The holder is no-op for FEATURE_PALs
class ThreadLocaleHolder
{
#ifndef FEATURE_PAL
public:

    ThreadLocaleHolder()
    {
        m_locale = GetThreadLocale();
    }

    ~ThreadLocaleHolder();

private:
    LCID m_locale;
#endif // !FEATURE_PAL
};



BOOL IsValidMethodCodeNotification(USHORT Notification);
    
typedef DPTR(struct JITNotification) PTR_JITNotification;
struct JITNotification
{
    USHORT state; // values from CLRDataMethodCodeNotification
    TADDR clrModule;
    mdToken methodToken;
    
    JITNotification() { SetFree(); } 
    BOOL IsFree() { return state == CLRDATA_METHNOTIFY_NONE; }
    void SetFree() { state = CLRDATA_METHNOTIFY_NONE; clrModule = NULL; methodToken = 0; }
    void SetState(TADDR moduleIn, mdToken tokenIn, USHORT NType) 
    { 
        _ASSERTE(IsValidMethodCodeNotification(NType)); 
        clrModule = moduleIn; 
        methodToken = tokenIn; 
        state = NType; 
    }
};

// The maximum number of TADDR sized arguments that the SOS exception notification can use
#define MAX_CLR_NOTIFICATION_ARGS 3
GARY_DECL(size_t, g_clrNotificationArguments, MAX_CLR_NOTIFICATION_ARGS);
extern void InitializeClrNotifications();

GPTR_DECL(JITNotification, g_pNotificationTable);
GVAL_DECL(ULONG32, g_dacNotificationFlags);

#if defined(FEATURE_PAL) && !defined(DACCESS_COMPILE)

inline void
InitializeJITNotificationTable()
{
    g_pNotificationTable = new (nothrow) JITNotification[1001];
}

#endif // FEATURE_PAL && !DACCESS_COMPILE

class JITNotifications
{
public:
    JITNotifications(JITNotification *jitTable);    
    BOOL SetNotification(TADDR clrModule, mdToken token, USHORT NType);
    USHORT Requested(TADDR clrModule, mdToken token); 

    // if clrModule is NULL, all active notifications are changed to NType
    BOOL SetAllNotifications(TADDR clrModule,USHORT NType,BOOL *changedOut);
    inline BOOL IsActive() { LIMITED_METHOD_CONTRACT; return m_jitTable!=NULL; }    

    UINT GetTableSize();    
#ifdef DACCESS_COMPILE
    static JITNotification *InitializeNotificationTable(UINT TableSize);
    // Updates target table from host copy
    BOOL UpdateOutOfProcTable();
#endif

private:
    UINT GetLength();
    void IncrementLength();
    void DecrementLength();

    BOOL FindItem(TADDR clrModule, mdToken token, UINT *indexOut);
    
    JITNotification *m_jitTable;
};

typedef DPTR(struct GcNotification) PTR_GcNotification;

inline
BOOL IsValidGcNotification(GcEvt_t evType)
{ return (evType < GC_EVENT_TYPE_MAX); }

#define CLRDATA_GC_NONE  0

struct GcNotification
{
    GcEvtArgs ev;

    GcNotification() { SetFree(); } 
    BOOL IsFree() { return ev.typ == CLRDATA_GC_NONE; }
    void SetFree() { memset(this, 0, sizeof(*this)); ev.typ = (GcEvt_t) CLRDATA_GC_NONE; }
    void Set(GcEvtArgs ev_)
    {
        _ASSERTE(IsValidGcNotification(ev_.typ)); 
        ev = ev_;
    }
    BOOL IsMatch(GcEvtArgs ev_)
    {
        LIMITED_METHOD_CONTRACT;
        if (ev.typ != ev_.typ)
        {
            return FALSE;
        }
        switch (ev.typ)
        {
        case GC_MARK_END:
            if (ev_.condemnedGeneration == 0 ||
                (ev.condemnedGeneration & ev_.condemnedGeneration) != 0)
            {
                return TRUE;
            }
            break;
        default:
            break;
        }

        return FALSE;
    }
};

GPTR_DECL(GcNotification, g_pGcNotificationTable);

class GcNotifications
{
public:
    GcNotifications(GcNotification *gcTable);    
    BOOL SetNotification(GcEvtArgs ev);
    GcEvtArgs* GetNotification(GcEvtArgs ev)
    {
        LIMITED_METHOD_CONTRACT;
        UINT idx;
        if (FindItem(ev, &idx))
        {
            return &m_gcTable[idx].ev;
        }
        else
        {
            return NULL;
        }
    }

    // if clrModule is NULL, all active notifications are changed to NType
    inline BOOL IsActive() 
    { return m_gcTable != NULL; }    

    UINT GetTableSize()
    { return Size(); }

#ifdef DACCESS_COMPILE
    static GcNotification *InitializeNotificationTable(UINT TableSize);
    // Updates target table from host copy
    BOOL UpdateOutOfProcTable();
#endif

private:
    UINT& Length()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsActive());
        UINT *pLen = (UINT *) &(m_gcTable[-1].ev.typ);
        return *pLen;
    }
    UINT& Size()
    {
        _ASSERTE(IsActive());
        UINT *pLen = (UINT *) &(m_gcTable[-1].ev.typ);
        return *(pLen+1);
    }
    void IncrementLength()
    { ++Length(); }
    void DecrementLength()
    { --Length(); }

    BOOL FindItem(GcEvtArgs ev, UINT *indexOut);

    GcNotification *m_gcTable;
};


class MethodDesc;
class Module;

class DACNotify
{
public:
    // types
    enum {
        MODULE_LOAD_NOTIFICATION=1,
        MODULE_UNLOAD_NOTIFICATION=2,
        JIT_NOTIFICATION=3,
        JIT_PITCHING_NOTIFICATION=4,
        EXCEPTION_NOTIFICATION=5,
        GC_NOTIFICATION= 6,
        CATCH_ENTER_NOTIFICATION = 7,
    };
    
    // called from the runtime
    static void DoJITNotification(MethodDesc *MethodDescPtr);
    static void DoJITPitchingNotification(MethodDesc *MethodDescPtr);
    static void DoModuleLoadNotification(Module *Module);
    static void DoModuleUnloadNotification(Module *Module);
    static void DoExceptionNotification(class Thread* ThreadPtr);
    static void DoGCNotification(const GcEvtArgs& evtargs);
    static void DoExceptionCatcherEnterNotification(MethodDesc *MethodDescPtr, DWORD nativeOffset);

    // called from the DAC
    static int GetType(TADDR Args[]);
    static BOOL ParseJITNotification(TADDR Args[], TADDR& MethodDescPtr);
    static BOOL ParseJITPitchingNotification(TADDR Args[], TADDR& MethodDescPtr);
    static BOOL ParseModuleLoadNotification(TADDR Args[], TADDR& ModulePtr);
    static BOOL ParseModuleUnloadNotification(TADDR Args[], TADDR& ModulePtr);
    static BOOL ParseExceptionNotification(TADDR Args[], TADDR& ThreadPtr);
    static BOOL ParseGCNotification(TADDR Args[], GcEvtArgs& evtargs);
    static BOOL ParseExceptionCatcherEnterNotification(TADDR Args[], TADDR& MethodDescPtr, DWORD& nativeOffset);
};

void DACNotifyCompilationFinished(MethodDesc *pMethodDesc);
    
#ifdef _DEBUG
#ifndef FEATURE_PAL
// NOTE: Windows Vista RTM SDK defines CaptureStackBackTrace as RtlCaptureStackBackTrace (in winbase.h)
//       Renamed CaptureStackBackTrace to UtilCaptureBackTrace in order to avoid conflicts with the Windows definition
USHORT UtilCaptureStackBackTrace(
    ULONG FramesToSkip,
    ULONG FramesToCapture,
    PVOID * BackTrace,
    OUT PULONG BackTraceHash);
#endif // !FEATURE_PAL
#endif //_DEBUG


// These wrap the SString:L:CompareCaseInsenstive function in a way that makes it
// easy to fix code that uses _stricmp. _stricmp should be avoided as it uses the current
// C-runtime locale rather than the invariance culture.
//
// Note that unlike the real _stricmp, these functions unavoidably have a throws/gc_triggers/inject_fault
// contract. So if need a case-insensitive comparison in a place where you can't tolerate this contract,
// you've got a problem.
int __cdecl stricmpUTF8(const char* szStr1, const char* szStr2);

#ifdef _DEBUG
class DisableDelayLoadCheckForOleaut32
{
public:
    DisableDelayLoadCheckForOleaut32();
    ~DisableDelayLoadCheckForOleaut32();
};
#endif

extern LONG g_OLEAUT32_Loaded;

#define ENSURE_OLEAUT32_LOADED()

BOOL DbgIsExecutable(LPVOID lpMem, SIZE_T length);

#ifndef DACCESS_COMPILE
// returns if ARM was already enabled or not.
BOOL EnableARM();
#endif // !DACCESS_COMPILE

int GetRandomInt(int maxVal);

class InternalCasingHelper {

    private:
    // Convert szIn to lower case in the Invariant locale.
    // TODO: NLS Arrowhead -Called by the two ToLowers)
    static INT32 InvariantToLowerHelper(__out_bcount_opt(cMaxBytes) LPUTF8 szOut, int cMaxBytes, __in_z LPCUTF8 szIn, BOOL fAllowThrow);

    public:
    //
    // Native helper functions to do correct casing operations in
    // runtime native code.
    //

    // Convert szIn to lower case in the Invariant locale. (WARNING: May throw.)
    static INT32 InvariantToLower(__out_bcount_opt(cMaxBytes) LPUTF8 szOut, int cMaxBytes, __in_z LPCUTF8 szIn);

    // Convert szIn to lower case in the Invariant locale. (WARNING: This version
    // won't throw but it will use stack space as an intermediary (so don't
    // use for ridiculously long strings.)
    static INT32 InvariantToLowerNoThrow(__out_bcount_opt(cMaxBytes) LPUTF8 szOut, int cMaxBytes, __in_z LPCUTF8 szIn);
};

//
//
// COMCHARACTER
//
//
class COMCharacter {
public:
    //These are here for support from native code.  They are never called from our managed classes.
    static BOOL nativeIsWhiteSpace(WCHAR c);
    static BOOL nativeIsDigit(WCHAR c);
};

#ifdef _DEBUG
#define FORCEINLINE_NONDEBUG
#else
#define FORCEINLINE_NONDEBUG FORCEINLINE
#endif

#ifndef FEATURE_PAL
// Extract the file version from an executable.
HRESULT GetFileVersion(LPCWSTR wszFilePath, ULARGE_INTEGER* pFileVersion);
#endif // !FEATURE_PAL

#endif /* _H_UTIL */

