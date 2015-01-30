//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// ===========================================================================
// File: palclr.h
//
// Various macros and constants that are necessary to make the CLR portable.
//

// ===========================================================================

#ifndef __PALCLR_WIN_H__
#define __PALCLR_WIN_H__

// PAL SEH
// Macros for portable exception handling. The Win32 SEH is emulated using
// these macros and setjmp/longjmp on Unix
//
// Usage notes:
//
// - The filter has to be a function taking two parameters:
// LONG MyFilter(PEXCEPTION_POINTERS *pExceptionInfo, PVOID pv)
//
// - It is not possible to directly use the local variables in the filter.
// All the local information that the filter has to need to know about should
// be passed through pv parameter
//  
// - Do not use goto to jump out of the PAL_TRY block
// (jumping out of the try block is not a good idea even on Win32, because of
// it causes stack unwind)
//
//
// Simple examples:
//
// PAL_TRY {
//   ....
// } WIN_PAL_FINALLY {
//   ....
// }
// WIN_PAL_ENDTRY
//
//
// PAL_TRY {
//   ....
// } WIN_PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER) {
//   ....
// }
// WIN_PAL_ENDTRY
// 
//
// LONG MyFilter(PEXCEPTION_POINTERS *pExceptionInfo, PVOID pv)
// {
// ...
// }
// PAL_TRY {
//   ....
// } WIN_PAL_EXCEPT_FILTER(MyFilter, NULL) {
//   ....
// }
// WIN_PAL_ENDTRY
//
//
// Complex example:
//
// struct MyParams
// {
//     ...
// } params;
//
// PAL_TRY {
//   PAL_TRY {
//       ...
//       if (error) goto Done;
//       ...
//   Done: ;
//   } WIN_PAL_EXCEPT_FILTER(OtherFilter, &params) {
//   ...
//   }
//   WIN_PAL_ENDTRY
// }
// WIN_PAL_FINALLY {
// }
// WIN_PAL_ENDTRY
//

#if !defined(FEATURE_CORECLR)

#include "staticcontract.h"

#define WIN_PAL_TRY_NAKED                                                       \
    {                                                                           \
        bool __exHandled; __exHandled = false;                                  \
        DWORD __exCode; __exCode = 0;                                           \
        __try                                                                   \
        {

#define WIN_PAL_TRY                                                             \
    {                                                                           \
        WIN_PAL_TRY_NAKED                                                       \
        WIN_PAL_TRY_HANDLER_DBG_BEGIN

#define WIN_PAL_TRY_FOR_DLLMAIN(_reason)                                        \
    {                                                                           \
        WIN_PAL_TRY_NAKED                                                       \
        WIN_PAL_TRY_HANDLER_DBG_BEGIN_DLLMAIN(_reason)

// Note: PAL_SEH_RESTORE_GUARD_PAGE is only ever defined in clrex.h, so we only restore guard pages automatically
// when these macros are used from within the VM.
#define WIN_PAL_SEH_RESTORE_GUARD_PAGE PAL_SEH_RESTORE_GUARD_PAGE
 
#define WIN_PAL_EXCEPT_NAKED(Disposition)                                       \
    } __except(__exCode = GetExceptionCode(), Disposition) {                    \
        __exHandled = true;                                                     \
        WIN_PAL_SEH_RESTORE_GUARD_PAGE

#define WIN_PAL_EXCEPT(Disposition)                                             \
        WIN_PAL_TRY_HANDLER_DBG_END                                             \
        WIN_PAL_EXCEPT_NAKED(Disposition)

#define WIN_PAL_EXCEPT_FILTER_NAKED(pfnFilter, pvFilterParameter)                                       \
    } __except(__exCode = GetExceptionCode(), pfnFilter(GetExceptionInformation(), pvFilterParameter)) {  \
        __exHandled = true;                                                     \
        WIN_PAL_SEH_RESTORE_GUARD_PAGE

#define WIN_PAL_EXCEPT_FILTER(pfnFilter, pvFilterParameter)                     \
        WIN_PAL_TRY_HANDLER_DBG_END                                             \
        WIN_PAL_EXCEPT_FILTER_NAKED(pfnFilter, pvFilterParameter)

#define WIN_PAL_FINALLY_NAKED                                                   \
    } __finally {                                                               \

#define WIN_PAL_FINALLY                                                             \
        WIN_PAL_TRY_HANDLER_DBG_END                                             \
        WIN_PAL_FINALLY_NAKED

#define WIN_PAL_ENDTRY_NAKED                                                    \
        }                                                                       \
    }                                                                           \

#define WIN_PAL_ENDTRY                                                          \
            }                                                                   \
            WIN_PAL_ENDTRY_NAKED_DBG                                            \
        }                                                                       \
    }

#define WIN_PAL_CPP_TRY try
#define WIN_PAL_CPP_ENDTRY
#define WIN_PAL_CPP_THROW(type, obj) throw obj;
#define WIN_PAL_CPP_RETHROW throw;
#define WIN_PAL_CPP_CATCH_EXCEPTION(obj) catch (Exception * obj)
#define WIN_PAL_CPP_CATCH_DERIVED(type, obj) catch (type * obj)
#define WIN_PAL_CPP_CATCH_ALL catch (...)
#define WIN_PAL_CPP_CATCH_EXCEPTION_NOARG catch (Exception *)

#endif // !PAL_WIN_SEH


#if defined(_DEBUG_IMPL) && !defined(JIT_BUILD) && !defined(JIT64_BUILD) && !defined(_ARM_) // @ARMTODO
#define WIN_PAL_TRY_HANDLER_DBG_BEGIN                                           \
    BOOL ___oldOkayToThrowValue = FALSE;                                        \
    BOOL ___oldSOTolerantState = FALSE;                                         \
    ClrDebugState *___pState = GetClrDebugState();                              \
    __try                                                                       \
    {                                                                           \
        ___oldOkayToThrowValue = ___pState->IsOkToThrow();                      \
        ___oldSOTolerantState = ___pState->IsSOTolerant();                      \
        ___pState->SetOkToThrow(TRUE);                                          \
        ANNOTATION_TRY_BEGIN;

// Special version that avoids touching the debug state after doing work in a DllMain for process or thread detach.
#define WIN_PAL_TRY_HANDLER_DBG_BEGIN_DLLMAIN(_reason)                          \
    BOOL ___oldOkayToThrowValue = FALSE;                                        \
    BOOL ___oldSOTolerantState = FALSE;                                         \
    ClrDebugState *___pState = CheckClrDebugState();                            \
    __try                                                                       \
    {                                                                           \
        if (___pState)                                                          \
        {                                                                       \
            ___oldOkayToThrowValue = ___pState->IsOkToThrow();                  \
            ___oldSOTolerantState = ___pState->IsSOTolerant();                  \
            ___pState->SetOkToThrow(TRUE);                                      \
        }                                                                       \
        if ((_reason == DLL_PROCESS_DETACH) || (_reason == DLL_THREAD_DETACH))  \
        {                                                                       \
            ___pState = NULL;                                                   \
        }                                                                       \
        ANNOTATION_TRY_BEGIN;

#define WIN_PAL_TRY_HANDLER_DBG_END                                             \
        ANNOTATION_TRY_END;                                                     \
    }                                                                           \
    __finally                                                                   \
    {                                                                           \
        if (___pState != NULL)                                                  \
        {                                                                       \
            _ASSERTE(___pState == CheckClrDebugState());                        \
            ___pState->SetOkToThrow(___oldOkayToThrowValue);                    \
            ___pState->SetSOTolerance(___oldSOTolerantState);                   \
        }                                                                       \
    }

#define WIN_PAL_ENDTRY_NAKED_DBG                                                \
    if (__exHandled)                                                            \
    {                                                                           \
        RESTORE_SO_TOLERANCE_STATE;                                             \
    }                                                                           \
    
#else
#define WIN_PAL_TRY_HANDLER_DBG_BEGIN                   ANNOTATION_TRY_BEGIN;
#define WIN_PAL_TRY_HANDLER_DBG_BEGIN_DLLMAIN(_reason)  ANNOTATION_TRY_BEGIN;
#define WIN_PAL_TRY_HANDLER_DBG_END                     ANNOTATION_TRY_END;
#define WIN_PAL_ENDTRY_NAKED_DBG                                                          
#endif // defined(ENABLE_CONTRACTS_IMPL) && !defined(JIT64_BUILD)

#endif	// __PALCLR_WIN_H__
