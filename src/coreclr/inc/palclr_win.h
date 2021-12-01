// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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



#if defined(_DEBUG_IMPL) && !defined(JIT_BUILD) && !defined(HOST_ARM) // @ARMTODO
#define WIN_PAL_TRY_HANDLER_DBG_BEGIN                                           \
    BOOL ___oldOkayToThrowValue = FALSE;                                        \
    ClrDebugState *___pState = GetClrDebugState();                              \
    __try                                                                       \
    {                                                                           \
        ___oldOkayToThrowValue = ___pState->IsOkToThrow();                      \
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

#define WIN_PAL_ENDTRY_NAKED_DBG

#else
#define WIN_PAL_TRY_HANDLER_DBG_BEGIN                   ANNOTATION_TRY_BEGIN;
#define WIN_PAL_TRY_HANDLER_DBG_BEGIN_DLLMAIN(_reason)  ANNOTATION_TRY_BEGIN;
#define WIN_PAL_TRY_HANDLER_DBG_END                     ANNOTATION_TRY_END;
#define WIN_PAL_ENDTRY_NAKED_DBG
#endif // defined(ENABLE_CONTRACTS_IMPL)

#if defined(HOST_WINDOWS)
// Native system libray handle.
// In Windows, NATIVE_LIBRARY_HANDLE is the same as HMODULE.
typedef HMODULE NATIVE_LIBRARY_HANDLE;
#endif // HOST_WINDOWS

#ifndef FALLTHROUGH
#define FALLTHROUGH __fallthrough
#endif // FALLTHROUGH

#endif	// __PALCLR_WIN_H__
