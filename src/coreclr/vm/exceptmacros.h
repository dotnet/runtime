// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// EXCEPTMACROS.H -
//
// This header file exposes mechanisms to:
//
//    1. Throw COM+ exceptions using the COMPlusThrow() function
//    2. Guard a block of code using EX_TRY, and catch
//       COM+ exceptions using EX_CATCH
//
// from the *unmanaged* portions of the EE. Much of the EE runs
// in a hybrid state where it runs like managed code but the code
// is produced by a classic unmanaged-code C++ compiler.
//
// THROWING A COM+ EXCEPTION
// -------------------------
// To throw a COM+ exception, call the function:
//
//      COMPlusThrow(OBJECTREF pThrowable);
//
// This function does not return. There are also various functions
// that wrap COMPlusThrow for convenience.
//
// COMPlusThrow() must only be called within the scope of a EX_TRY
// block. See below for more information.
//
//
// THROWING A RUNTIME EXCEPTION
// ----------------------------
// COMPlusThrow() is overloaded to take a constant describing
// the common EE-generated exceptions, e.g.
//
//    COMPlusThrow(kOutOfMemoryException);
//
// See rexcep.h for list of constants (prepend "k" to get the actual
// constant name.)
//
// You can also add a descriptive error string as follows:
//
//    - Add a descriptive error string and resource id to
//      COM99\src\dlls\mscorrc\resource.h and mscorrc.rc.
//      Embed "%1", "%2" or "%3" to leave room for runtime string
//      inserts.
//
//    - Pass the resource ID and inserts to COMPlusThrow, i.e.
//
//      COMPlusThrow(kSecurityException,
//                   IDS_CANTREFORMATCDRIVEBECAUSE,
//                   W("Formatting C drive permissions not granted."));
//
//
//
// TO CATCH COMPLUS EXCEPTIONS:
// ----------------------------
//
// Use the following syntax:
//
//      #include "exceptmacros.h"
//
//
//      OBJECTREF pThrownObject;
//
//      EX_TRY {
//          ...guarded code...
//      } EX_CATCH {
//          ...handler...
//      } EX_END_CATCH(SwallowAllExceptions)
//
//
// EX_TRY blocks can be nested.
//
// From within the handler, you can call the GET_THROWABLE() macro to
// obtain the object that was thrown.
//
// CRUCIAL POINTS
// --------------
// In order to call COMPlusThrow(), you *must* be within the scope
// of a EX_TRY block. Under _DEBUG, COMPlusThrow() will assert
// if you call it out of scope. This implies that just about every
// external entrypoint into the EE has to have a EX_TRY, in order
// to convert uncaught COM+ exceptions into some error mechanism
// more understandable to its non-COM+ caller.
//
// Any function that can throw a COM+ exception out to its caller
// has the same requirement. ALL such functions should be tagged
// with THROWS in CONTRACT. Aside from making the code
// self-document its contract, the checked version of this will fire
// an assert if the function is ever called without being in scope.
//
//
// AVOIDING EX_TRY GOTCHAS
// ----------------------------
// EX_TRY/EX_CATCH actually expands into a Win32 SEH
// __try/__except structure. It does a lot of goo under the covers
// to deal with pre-emptive GC settings.
//
//    1. Do not use C++ or SEH try/__try use EX_TRY instead.
//
//    2. Remember that any function marked THROWS
//       has the potential not to return. So be wary of allocating
//       non-gc'd objects around such calls because ensuring cleanup
//       of these things is not simple (you can wrap another EX_TRY
//       around the call to simulate a COM+ "try-finally" but EX_TRY
//       is relatively expensive compared to the real thing.)
//
//


#ifndef __exceptmacros_h__
#define __exceptmacros_h__

struct _EXCEPTION_REGISTRATION_RECORD;
class Thread;
class Frame;
class Exception;

VOID DECLSPEC_NORETURN RealCOMPlusThrowOM();

#include <excepcpu.h>

//==========================================================================
// Macros to allow catching exceptions from within the EE. These are lightweight
// handlers that do not install the managed frame handler.
//
//      struct Param { ... } param;
//      EE_TRY_FOR_FINALLY(Param *, pParam, &param) {
//          ...<guarded code>...
//      } EE_FINALLY {
//          ...<handler>...
//      } EE_END_FINALLY
//
//      EE_TRY(filter expr) {
//          ...<guarded code>...
//      } EE_CATCH {
//          ...<handler>...
//      }
//==========================================================================

// __GotException will only be FALSE if got all the way through the code
// guarded by the try, otherwise it will be TRUE, so we know if we got into the
// finally from an exception or not. In which case need to reset the GC state back
// to what it was for the finally to run in that state.

#define EE_TRY_FOR_FINALLY(ParamType, paramDef, paramRef)               \
    {                                                               \
        struct __EEParam                                                \
        {                                                               \
            BOOL fGCDisabled;                                           \
            BOOL GotException;                                          \
            ParamType param;                                            \
        } __EEparam;                                                    \
        __EEparam.fGCDisabled = GetThread()->PreemptiveGCDisabled();    \
        __EEparam.GotException = TRUE;                                  \
        __EEparam.param = paramRef;                                     \
        PAL_TRY(__EEParam *, __pEEParam, &__EEparam)                    \
        {                                                               \
            ParamType paramDef; paramDef = __pEEParam->param;

#define GOT_EXCEPTION() __EEparam.GotException

#define EE_FINALLY                                                                  \
            __pEEParam->GotException = FALSE;                                       \
        } PAL_FINALLY {                                                             \
            if (__EEparam.GotException) {                                           \
                if (__EEparam.fGCDisabled != GetThread()->PreemptiveGCDisabled()) { \
                    if (__EEparam.fGCDisabled)                                      \
                        GetThread()->DisablePreemptiveGC();                         \
                    else                                                            \
                        GetThread()->EnablePreemptiveGC();                          \
                }                                                                   \
            }

#define EE_END_FINALLY                                                  \
        }                                                               \
        PAL_ENDTRY                                                      \
    }




//==========================================================================
// Helpful macros to declare exception handlers, their implementaiton,
// and to call them.
//==========================================================================

#define _EXCEPTION_HANDLER_DECL(funcname)                                                               \
    EXCEPTION_DISPOSITION __cdecl funcname(EXCEPTION_RECORD *pExceptionRecord,                          \
                                           struct _EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,    \
                                           CONTEXT *pContext,                                           \
                                           DISPATCHER_CONTEXT *pDispatcherContext)

#define EXCEPTION_HANDLER_DECL(funcname) \
    extern "C"  _EXCEPTION_HANDLER_DECL(funcname)

#define EXCEPTION_HANDLER_IMPL(funcname) \
    _EXCEPTION_HANDLER_DECL(funcname)

#define EXCEPTION_HANDLER_FWD(funcname) \
    funcname(pExceptionRecord, pEstablisherFrame, pContext, pDispatcherContext)

//==========================================================================
// Declares a COM+ frame handler that can be used to make sure that
// exceptions that should be handled from within managed code
// are handled within and don't leak out to give other handlers a
// chance at them.
//==========================================================================
#define INSTALL_COMPLUS_EXCEPTION_HANDLER()                                     \
    DECLARE_CPFH_EH_RECORD(GET_THREAD());                                       \
    INSTALL_COMPLUS_EXCEPTION_HANDLER_NO_DECLARE()

#define INSTALL_COMPLUS_EXCEPTION_HANDLER_NO_DECLARE()                          \
{                                                                               \
    INSTALL_EXCEPTION_HANDLING_RECORD(&(___pExRecord->m_ExReg));                \
    /* work around unreachable code warning */                                  \
    if (true) {

#define UNINSTALL_COMPLUS_EXCEPTION_HANDLER()                                   \
    }                                                                           \
    UNINSTALL_EXCEPTION_HANDLING_RECORD(&(___pExRecord->m_ExReg));              \
}

#if !defined(FEATURE_EH_FUNCLETS)

#define INSTALL_NESTED_EXCEPTION_HANDLER(frame)                                                                       \
   NestedHandlerExRecord *__pNestedHandlerExRecord = (NestedHandlerExRecord*) _alloca(sizeof(NestedHandlerExRecord)); \
   __pNestedHandlerExRecord->m_handlerInfo.m_hThrowable = NULL;                                                       \
   __pNestedHandlerExRecord->Init((PEXCEPTION_ROUTINE)COMPlusNestedExceptionHandler, frame);                          \
   INSTALL_EXCEPTION_HANDLING_RECORD(&(__pNestedHandlerExRecord->m_ExReg));

#define UNINSTALL_NESTED_EXCEPTION_HANDLER()                                                                          \
   UNINSTALL_EXCEPTION_HANDLING_RECORD(&(__pNestedHandlerExRecord->m_ExReg));

#else // defined(FEATURE_EH_FUNCLETS)

#define INSTALL_NESTED_EXCEPTION_HANDLER(frame)
#define UNINSTALL_NESTED_EXCEPTION_HANDLER()

#endif // !defined(FEATURE_EH_FUNCLETS)

LONG WINAPI CLRVectoredExceptionHandler(PEXCEPTION_POINTERS pExceptionInfo);

// Actual UEF worker prototype for use by GCUnhandledExceptionFilter.
extern LONG InternalUnhandledExceptionFilter_Worker(PEXCEPTION_POINTERS pExceptionInfo);

VOID DECLSPEC_NORETURN RaiseTheExceptionInternalOnly(OBJECTREF throwable, BOOL rethrow, BOOL fForStackOverflow = FALSE);

#if defined(DACCESS_COMPILE)

#define INSTALL_UNWIND_AND_CONTINUE_HANDLER
#define UNINSTALL_UNWIND_AND_CONTINUE_HANDLER

#define INSTALL_UNWIND_AND_CONTINUE_HANDLER_NO_PROBE
#define UNINSTALL_UNWIND_AND_CONTINUE_HANDLER_NO_PROBE
#else // DACCESS_COMPILE

void UnwindAndContinueRethrowHelperInsideCatch(Frame* pEntryFrame, Exception* pException);
VOID DECLSPEC_NORETURN UnwindAndContinueRethrowHelperAfterCatch(Frame* pEntryFrame, Exception* pException);

#ifdef TARGET_UNIX
VOID DECLSPEC_NORETURN DispatchManagedException(PAL_SEHException& ex, bool isHardwareException);

#define INSTALL_MANAGED_EXCEPTION_DISPATCHER        \
        PAL_SEHException exCopy;                    \
        bool hasCaughtException = false;            \
        try {

#define UNINSTALL_MANAGED_EXCEPTION_DISPATCHER      \
        }                                           \
        catch (PAL_SEHException& ex)                \
        {                                           \
            exCopy = std::move(ex);                 \
            hasCaughtException = true;              \
        }                                           \
        if (hasCaughtException)                     \
        {                                           \
            DispatchManagedException(exCopy, false);\
        }

// Install trap that catches unhandled managed exception and dumps its stack
#define INSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP                                            \
        try {

// Uninstall trap that catches unhandled managed exception and dumps its stack
#define UNINSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP                                                  \
        }                                                                                           \
        catch (PAL_SEHException& ex)                                                                \
        {                                                                                           \
            if (!GetThread()->HasThreadStateNC(Thread::TSNC_ProcessedUnhandledException))           \
            {                                                                                       \
                LONG disposition = InternalUnhandledExceptionFilter_Worker(&ex.ExceptionPointers);  \
                _ASSERTE(disposition == EXCEPTION_CONTINUE_SEARCH);                                 \
            }                                                                                       \
            CrashDumpAndTerminateProcess(1);                                                        \
            UNREACHABLE();                                                                          \
        }

#else // TARGET_UNIX

#define INSTALL_MANAGED_EXCEPTION_DISPATCHER
#define UNINSTALL_MANAGED_EXCEPTION_DISPATCHER
#define INSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP
#define UNINSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP

#endif // TARGET_UNIX

#define INSTALL_UNWIND_AND_CONTINUE_HANDLER_NO_PROBE                                        \
    {                                                                                       \
        MAKE_CURRENT_THREAD_AVAILABLE();                                                    \
        Exception* __pUnCException  = NULL;                                                 \
        Frame*     __pUnCEntryFrame = CURRENT_THREAD->GetFrame();                           \
        bool       __fExceptionCatched = false;                                             \
        SCAN_EHMARKER();                                                                    \
        if (true) PAL_CPP_TRY {                                                             \
            SCAN_EHMARKER_TRY();                                                            \
            DEBUG_ASSURE_NO_RETURN_BEGIN(IUACH)

#define INSTALL_UNWIND_AND_CONTINUE_HANDLER                                                 \
    INSTALL_UNWIND_AND_CONTINUE_HANDLER_NO_PROBE                                            \
    /* The purpose of the INSTALL_UNWIND_AND_CONTINUE_HANDLER is to translate an exception to a managed */ \
    /* exception before it hits managed code. */

// Optimized version for helper method frame. Avoids redundant GetThread() calls.
#define INSTALL_UNWIND_AND_CONTINUE_HANDLER_FOR_HMF(pHelperFrame)                           \
    {                                                                                       \
        Exception* __pUnCException  = NULL;                                                 \
        Frame*     __pUnCEntryFrame = (pHelperFrame);                                       \
        bool       __fExceptionCatched = false;                                             \
        SCAN_EHMARKER();                                                                    \
        if (true) PAL_CPP_TRY {                                                             \
            SCAN_EHMARKER_TRY();                                                            \
            DEBUG_ASSURE_NO_RETURN_BEGIN(IUACH);

#define UNINSTALL_UNWIND_AND_CONTINUE_HANDLER_NO_PROBE                                      \
            DEBUG_ASSURE_NO_RETURN_END(IUACH)                                               \
            SCAN_EHMARKER_END_TRY();                                                        \
        }                                                                                   \
        PAL_CPP_CATCH_DERIVED (Exception, __pException)                                     \
        {                                                                                   \
            SCAN_EHMARKER_CATCH();                                                          \
            CONSISTENCY_CHECK(NULL != __pException);                                        \
            __pUnCException = __pException;                                                 \
            UnwindAndContinueRethrowHelperInsideCatch(__pUnCEntryFrame, __pUnCException);   \
            __fExceptionCatched = true;                                                     \
            SCAN_EHMARKER_END_CATCH();                                                      \
        }                                                                                   \
        PAL_CPP_ENDTRY                                                                      \
        if (__fExceptionCatched)                                                            \
        {                                                                                   \
            SCAN_EHMARKER_CATCH();                                                          \
            UnwindAndContinueRethrowHelperAfterCatch(__pUnCEntryFrame, __pUnCException);    \
        }                                                                                   \
    }                                                                                       \

#define UNINSTALL_UNWIND_AND_CONTINUE_HANDLER                                               \
    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER_NO_PROBE;

#endif // DACCESS_COMPILE


#define ENCLOSE_IN_EXCEPTION_HANDLER( func ) \
    { \
        struct exception_handler_wrapper \
        { \
            static void wrap() \
            { \
                INSTALL_UNWIND_AND_CONTINUE_HANDLER; \
                func(); \
                UNINSTALL_UNWIND_AND_CONTINUE_HANDLER; \
            } \
        }; \
    \
        exception_handler_wrapper::wrap(); \
    }


//==========================================================================
// Declares that a function can throw a COM+ exception.
//==========================================================================
#if defined(ENABLE_CONTRACTS) && !defined(DACCESS_COMPILE)

//==========================================================================
// Declares that a function cannot throw a COM+ exception.
// Adds a record to the contract chain.
//==========================================================================

#define CANNOTTHROWCOMPLUSEXCEPTION() ANNOTATION_NOTHROW; \
    COMPlusCannotThrowExceptionHelper _dummyvariable(TRUE, __FUNCTION__, __FILE__, __LINE__);

extern const char *g_ExceptionFile;
extern DWORD g_ExceptionLine;

#define THROWLOG() ( g_ExceptionFile = __FILE__, g_ExceptionLine = __LINE__, TRUE )

#define COMPlusThrow             if(THROWLOG() && 0) { } else RealCOMPlusThrow
#define COMPlusThrowNonLocalized if(THROWLOG() && 0) { } else RealCOMPlusThrowNonLocalized
#define COMPlusThrowHR           if(THROWLOG() && 0) { } else RealCOMPlusThrowHR
#define COMPlusThrowWin32        if(THROWLOG() && 0) { } else RealCOMPlusThrowWin32
#define COMPlusThrowOM           if(THROWLOG() && 0) { } else RealCOMPlusThrowOM
#define COMPlusThrowArithmetic   if(THROWLOG() && 0) { } else RealCOMPlusThrowArithmetic
#define COMPlusThrowArgumentNull if(THROWLOG() && 0) { } else RealCOMPlusThrowArgumentNull
#define COMPlusThrowArgumentOutOfRange if(THROWLOG() && 0) { } else RealCOMPlusThrowArgumentOutOfRange
#define COMPlusThrowArgumentException if(THROWLOG() && 0) { } else RealCOMPlusThrowArgumentException
#define COMPlusThrowInvalidCastException if(THROWLOG() && 0) { } else RealCOMPlusThrowInvalidCastException
#define COMPlusRareRethrow if(THROWLOG() && 0) { } else RealCOMPlusRareRethrow

#else // ENABLE_CONTRACTS && !DACCESS_COMPILE

#define CANNOTTHROWCOMPLUSEXCEPTION() ANNOTATION_NOTHROW
#define BEGINCANNOTTHROWCOMPLUSEXCEPTION_SEH() ANNOTATION_NOTHROW
#define ENDCANNOTTHROWCOMPLUSEXCEPTION_SEH()

#define COMPlusThrow                        RealCOMPlusThrow
#define COMPlusThrowNonLocalized            RealCOMPlusThrowNonLocalized
#ifndef DACCESS_COMPILE
#define COMPlusThrowHR                      RealCOMPlusThrowHR
#else
#define COMPlusThrowHR ThrowHR
#endif
#define COMPlusThrowWin32                   RealCOMPlusThrowWin32
#define COMPlusThrowOM                      RealCOMPlusThrowOM
#define COMPlusThrowArithmetic              RealCOMPlusThrowArithmetic
#define COMPlusThrowArgumentNull            RealCOMPlusThrowArgumentNull
#define COMPlusThrowArgumentOutOfRange      RealCOMPlusThrowArgumentOutOfRange
#define COMPlusThrowArgumentException       RealCOMPlusThrowArgumentException
#define COMPlusThrowInvalidCastException    RealCOMPlusThrowInvalidCastException

#endif // ENABLE_CONTRACTS && !DACCESS_COMPILE
/* Non-VM exception helpers to be rerouted inside the VM directory:
ThrowHR
ThrowWin32
ThrowLastError       -->ThrowWin32(GetLastError())
ThrowOutOfMemory        COMPlusThrowOM defers to this
ThrowStackOverflow      COMPlusThrowSO defers to this

*/

/* Ideally we could make these defines.  But the sources in the VM directory
   won't build with them as defines.  @todo: go through VM directory and
   eliminate calls to the non-VM style functions.

#define ThrowHR             COMPlusThrowHR
#define ThrowWin32          COMPlusThrowWin32
#define ThrowLastError()    COMPlusThrowWin32(GetLastError())

*/

//======================================================
// Used when we're entering the EE from unmanaged code
// and we can assert that the gc state is cooperative.
//
// If an exception is thrown through this transition
// handler, it will clean up the EE appropriately.  See
// the definition of COMPlusCooperativeTransitionHandler
// for the details.
//======================================================

void COMPlusCooperativeTransitionHandler(Frame* pFrame);


#define COOPERATIVE_TRANSITION_BEGIN()              \
  {                                                 \
    MAKE_CURRENT_THREAD_AVAILABLE();                \
    BEGIN_GCX_ASSERT_PREEMP;                        \
    CoopTransitionHolder __CoopTransition(CURRENT_THREAD); \
    DEBUG_ASSURE_NO_RETURN_BEGIN(COOP_TRANSITION)

#define COOPERATIVE_TRANSITION_END()                \
    DEBUG_ASSURE_NO_RETURN_END(COOP_TRANSITION)     \
    __CoopTransition.SuppressRelease();             \
    END_GCX_ASSERT_PREEMP;                          \
  }


extern LONG UserBreakpointFilter(EXCEPTION_POINTERS *ep);
extern LONG DefaultCatchFilter(EXCEPTION_POINTERS *ep, LPVOID pv);
extern LONG DefaultCatchNoSwallowFilter(EXCEPTION_POINTERS *ep, LPVOID pv);


// the only valid parameter for DefaultCatchFilter
#define COMPLUS_EXCEPTION_EXECUTE_HANDLER   (PVOID)EXCEPTION_EXECUTE_HANDLER
struct DefaultCatchFilterParam
{
    PVOID pv; // must be COMPLUS_EXCEPTION_EXECUTE_HANDLER
    DefaultCatchFilterParam() {}
    DefaultCatchFilterParam(PVOID _pv) : pv(_pv) {}
};

template <typename T>
LPCWSTR GetPathForErrorMessagesT(T *pImgObj)
{
    SUPPORTS_DAC_HOST_ONLY;
    if (pImgObj)
    {
        return pImgObj->GetPathForErrorMessages();
    }
    else
    {
        return W("");
    }
}

VOID ThrowBadFormatWorker(UINT resID, LPCWSTR imageName DEBUGARG(__in_z const char *cond));

template <typename T>
NOINLINE
VOID ThrowBadFormatWorkerT(UINT resID, T * pImgObj DEBUGARG(__in_z const char *cond))
{
#ifdef DACCESS_COMPILE
    ThrowBadFormatWorker(resID, nullptr DEBUGARG(cond));
#else
    LPCWSTR tmpStr = GetPathForErrorMessagesT(pImgObj);
    ThrowBadFormatWorker(resID, tmpStr DEBUGARG(cond));
#endif
}


// Worker macro for throwing BadImageFormat exceptions.
//
//     resID:     resource ID in mscorrc.rc. Message may not have substitutions. resID is permitted (but not encouraged) to be 0.
//     imgObj:    one of Module* or PEFile* or PEImage* (must support GetPathForErrorMessages method.)
//
#define IfFailThrowBF(hresult, resID, imgObj)   \
    do                                          \
        {                                       \
            if (FAILED(hresult))                \
                THROW_BAD_FORMAT(resID, imgObj); \
        }                                       \
    while(0)


#define THROW_BAD_FORMAT(resID, imgObj) do { THROW_BAD_FORMAT_MAYBE(FALSE, resID, imgObj); UNREACHABLE(); } while(0)


// Conditional version of THROW_BAD_FORMAT. Do not use for new callsites. This is really meant to be a drop-in replacement
// for the obsolete BAD_FORMAT_ASSERT.

#define THROW_BAD_FORMAT_MAYBE(cond, resID, imgObj)             \
    do                                                          \
        {                                                       \
            if (!(cond))                                        \
            {                                                   \
                ThrowBadFormatWorkerT((resID), (imgObj) DEBUGARG(#cond)); \
            }                                                   \
        }                                                       \
   while(0)


// Same as above, but allows you to specify your own HRESULT
#define THROW_HR_ERROR_WITH_INFO(hr, imgObj) RealCOMPlusThrowHR(hr, hr, GetPathForErrorMessagesT(imgObj))


#endif // __exceptmacros_h__
