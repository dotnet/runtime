// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Definition of the Unwind API functions.
// Taken from the ABI documentation.
//



#ifndef __PAL_UNWIND_H__
#define __PAL_UNWIND_H__

#if FEATURE_PAL_SXS

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

    //
    // Exception Handling ABI Level I: Base ABI
    //

    typedef enum
    {
        _URC_NO_REASON = 0,
        _URC_FOREIGN_EXCEPTION_CAUGHT = 1,
        _URC_FATAL_PHASE2_ERROR = 2,
        _URC_FATAL_PHASE1_ERROR = 3,
        _URC_NORMAL_STOP = 4,
        _URC_END_OF_STACK = 5,
        _URC_HANDLER_FOUND = 6,
        _URC_INSTALL_CONTEXT = 7,
        _URC_CONTINUE_UNWIND = 8,
    } _Unwind_Reason_Code;

    typedef enum
    {
        _UA_SEARCH_PHASE = 1,
        _UA_CLEANUP_PHASE = 2,
        _UA_HANDLER_FRAME = 4,
        _UA_FORCE_UNWIND = 8,
    } _Unwind_Action;
    #define _UA_PHASE_MASK (_UA_SEARCH_PHASE|_UA_CLEANUP_PHASE)

    struct _Unwind_Context;

    void *_Unwind_GetIP(struct _Unwind_Context *context);
    void _Unwind_SetIP(struct _Unwind_Context *context, void *new_value);
    void *_Unwind_GetCFA(struct _Unwind_Context *context);
    void *_Unwind_GetGR(struct _Unwind_Context *context, int index);
    void _Unwind_SetGR(struct _Unwind_Context *context, int index, void *new_value);

    struct _Unwind_Exception;

    typedef void (*_Unwind_Exception_Cleanup_Fn)(
        _Unwind_Reason_Code urc,
        struct _Unwind_Exception *exception_object);

    struct _Unwind_Exception
    {
        ULONG64 exception_class;
        _Unwind_Exception_Cleanup_Fn exception_cleanup;
        UINT_PTR private_1;
        UINT_PTR private_2;
    } __attribute__((aligned));

    void _Unwind_DeleteException(struct _Unwind_Exception *exception_object);

    typedef _Unwind_Reason_Code (*_Unwind_Trace_Fn)(struct _Unwind_Context *context, void *pvParam);
    _Unwind_Reason_Code _Unwind_Backtrace(_Unwind_Trace_Fn pfnTrace, void *pvParam);

    _Unwind_Reason_Code _Unwind_RaiseException(struct _Unwind_Exception *exception_object);
    __attribute__((noreturn)) void _Unwind_Resume(struct _Unwind_Exception *exception_object);

    //
    // Exception Handling ABI Level II: C++ ABI
    //

    void *__cxa_begin_catch(void *exceptionObject);
    void __cxa_end_catch();

#ifdef __cplusplus
};
#endif // __cplusplus

#endif // FEATURE_PAL_SXS

#endif // __PAL_UNWIND_H__
