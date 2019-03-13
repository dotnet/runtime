// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// DebugMacros.h
//
// Wrappers for Debugging purposes.
//
//*****************************************************************************

#ifndef __DebugMacros_h__
#define __DebugMacros_h__

#include "stacktrace.h"
#include "debugmacrosext.h"
#include "palclr.h"

#undef _ASSERTE
#undef VERIFY

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

#if defined(_DEBUG)

class SString;
bool GetStackTraceAtContext(SString & s, struct _CONTEXT * pContext);

void _cdecl DbgWriteEx(LPCTSTR szFmt, ...);
bool _DbgBreakCheck(LPCSTR szFile, int iLine, LPCSTR szExpr, BOOL fConstrained = FALSE);

extern VOID ANALYZER_NORETURN DbgAssertDialog(const char *szFile, int iLine, const char *szExpr);

#define TRACE_BUFF_SIZE (cchMaxAssertStackLevelStringLen * cfrMaxAssertStackLevels + cchMaxAssertExprLen + 1)
extern char g_szExprWithStack[TRACE_BUFF_SIZE];

extern int _DbgBreakCount;

#define PRE_ASSERTE         /* if you need to change modes before doing asserts override */
#define POST_ASSERTE        /* put it back */

#if !defined(_ASSERTE_MSG)                                              
  #define _ASSERTE_MSG(expr, msg)                                           \
        do {                                                                \
             if (!(expr)) {                                                 \
                PRE_ASSERTE                                                 \
                DbgAssertDialog(__FILE__, __LINE__, msg);                   \
                POST_ASSERTE                                                \
             }                                                              \
        } while (0)
#endif // _ASSERTE_MSG

#if !defined(_ASSERTE)
  #define _ASSERTE(expr) _ASSERTE_MSG(expr, #expr)
#endif  // !_ASSERTE


#define VERIFY(stmt) _ASSERTE((stmt))

#define _ASSERTE_ALL_BUILDS(file, expr) _ASSERTE((expr))

#define FreeBuildDebugBreak() DebugBreak()

#else // !_DEBUG

#define _DbgBreakCount  0

#define _ASSERTE(expr) ((void)0)
#define _ASSERTE_MSG(expr, msg) ((void)0)
#define VERIFY(stmt) (void)(stmt)

void __FreeBuildDebugBreak();
void DECLSPEC_NORETURN __FreeBuildAssertFail(const char *szFile, int iLine, const char *szExpr);

#define FreeBuildDebugBreak() __FreeBuildDebugBreak()

// At this point, EEPOLICY_HANDLE_FATAL_ERROR may or may not be defined. It will be defined
// if we are building the VM folder, but outside VM, its not necessarily defined.
//
// Thus, if EEPOLICY_HANDLE_FATAL_ERROR is not defined, we will call into __FreeBuildAssertFail,
// but if it is defined, we will use it.
//
// Failing here implies an error in the runtime - hence we use COR_E_EXECUTIONENGINE.

#ifdef EEPOLICY_HANDLE_FATAL_ERROR
#define _ASSERTE_ALL_BUILDS(file, expr) if (!(expr)) EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
#else // !EEPOLICY_HANDLE_FATAL_ERROR
#define _ASSERTE_ALL_BUILDS(file, expr) if (!(expr)) __FreeBuildAssertFail(file, __LINE__, #expr);
#endif // EEPOLICY_HANDLE_FATAL_ERROR

#endif


#define ASSERT_AND_CHECK(x) {       \
    BOOL bResult = x;               \
    if (!bResult)                   \
    {                               \
        _ASSERTE(x);                \
        return FALSE;               \
    }                               \
}
    
    
#ifdef _DEBUG_IMPL

// A macro to execute a statement only in _DEBUG_IMPL.
#define DEBUG_IMPL_STMT(stmt) stmt
    
#define _ASSERTE_IMPL(expr) _ASSERTE((expr))

#if     defined(_M_IX86)
#if defined(_MSC_VER)
#define _DbgBreak() __asm { int 3 }
#elif defined(__GNUC__)
#define _DbgBreak() __asm__ ("int $3");
#else
#error Unknown compiler
#endif
#else
#define _DbgBreak() DebugBreak()
#endif

extern VOID DebBreak();
extern VOID DebBreakHr(HRESULT hr);

#ifndef IfFailGoto
#define IfFailGoto(EXPR, LABEL) \
do { hr = (EXPR); if(FAILED(hr)) { DebBreakHr(hr); goto LABEL; } } while (0)
#endif

#ifndef IfFailRet
#define IfFailRet(EXPR) \
do { hr = (EXPR); if(FAILED(hr)) { DebBreakHr(hr); return (hr); } } while (0)
#endif

#ifndef IfFailWin32Ret
#define IfFailWin32Ret(EXPR) \
do { hr = (EXPR); if(hr != ERROR_SUCCESS) { hr = HRESULT_FROM_WIN32(hr); DebBreakHr(hr); return hr;} } while (0)
#endif

#ifndef IfFailWin32Goto
#define IfFailWin32Goto(EXPR, LABEL) \
do { hr = (EXPR); if(hr != ERROR_SUCCESS) { hr = HRESULT_FROM_WIN32(hr); DebBreakHr(hr); goto LABEL; } } while (0)
#endif

#ifndef IfFailGo
#define IfFailGo(EXPR) IfFailGoto(EXPR, ErrExit)
#endif

#ifndef IfFailWin32Go
#define IfFailWin32Go(EXPR) IfFailWin32Goto(EXPR, ErrExit)
#endif

#else // _DEBUG_IMPL

#define _DbgBreak() {}

#define DEBUG_IMPL_STMT(stmt)

#define _ASSERTE_IMPL(expr)

#define IfFailGoto(EXPR, LABEL) \
do { hr = (EXPR); if(FAILED(hr)) { goto LABEL; } } while (0)

#define IfFailRet(EXPR) \
do { hr = (EXPR); if(FAILED(hr)) { return (hr); } } while (0)

#define IfFailWin32Ret(EXPR) \
do { hr = (EXPR); if(hr != ERROR_SUCCESS) { hr = HRESULT_FROM_WIN32(hr); return hr;} } while (0)

#define IfFailWin32Goto(EXPR, LABEL) \
do { hr = (EXPR); if(hr != ERROR_SUCCESS) { hr = HRESULT_FROM_WIN32(hr); goto LABEL; } } while (0)

#define IfFailGo(EXPR) IfFailGoto(EXPR, ErrExit)

#define IfFailWin32Go(EXPR) IfFailWin32Goto(EXPR, ErrExit)

#endif // _DEBUG_IMPL


#define IfNullGoto(EXPR, LABEL) \
    do { if ((EXPR) == NULL) { OutOfMemory(); IfFailGoto(E_OUTOFMEMORY, LABEL); } } while (false)

#ifndef IfNullRet
#define IfNullRet(EXPR) \
    do { if ((EXPR) == NULL) { OutOfMemory(); return E_OUTOFMEMORY; } } while (false)
#endif //!IfNullRet

#define IfNullGo(EXPR) IfNullGoto(EXPR, ErrExit)

#ifdef __cplusplus
}

#endif // __cplusplus


#undef assert
#define assert _ASSERTE
#undef _ASSERT
#define _ASSERT _ASSERTE


#if defined(_DEBUG) && !defined(FEATURE_PAL)

// This function returns the EXE time stamp (effectively a random number)
// Under retail it always returns 0.  This is meant to be used in the
// RandomOnExe macro
unsigned DbgGetEXETimeStamp();

// returns true 'fractionOn' amount of the time using the EXE timestamp
// as the random number seed.  For example DbgRandomOnExe(.1) returns true 1/10
// of the time.  We use the line number so that different uses of DbgRandomOnExe
// will not be coorelated with each other (9973 is prime).  Returns false on a retail build
#define DbgRandomOnHashAndExe(hash, fractionOn) \
    (((DbgGetEXETimeStamp() * __LINE__ * ((hash) ? (hash) : 1)) % 9973) < \
     unsigned(fractionOn * 9973))
#define DbgRandomOnExe(fractionOn) DbgRandomOnHashAndExe(0, fractionOn)
#define DbgRandomOnStringAndExe(string, fractionOn) DbgRandomOnHashAndExe(HashStringA(string), fractionOn)

#else

#define DbgGetEXETimeStamp() 0
#define DbgRandomOnHashAndExe(hash, fractionOn)  0
#define DbgRandomOnExe(fractionOn)  0
#define DbgRandomOnStringAndExe(fractionOn)  0

#endif // _DEBUG && !FEATUREPAL

#ifdef _DEBUG
namespace clr
{
    namespace dbg
    {
        // In debug builds, this can be used to write known bad values into
        // memory. One example is in ComUtil::IUnknownCommon::~IUnknownCommon,
        // which overwrites its instance memory with a known bad value after
        // completing its destructor.
        template < typename T >
        void PoisonMem(T &val)
        {
            ZeroMemory((void*)&val, sizeof(T));
        }

        template < typename T >
        void PoisonMem(T* ptr, size_t len)
        {
            ZeroMemory((void*)ptr, sizeof(T)* len);
        }
    }
}
#else

// Empty versions of the functions in retail that will be inlined
// and completely elided.
namespace clr
{
    namespace dbg
    {
        template < typename T >
        inline void PoisonMem(T &) {}

        template < typename T >
        void PoisonMem(T* ptr, size_t len){}
    }
}
#endif

#endif 
