// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: palclr.h
//
// Various macros and constants that are necessary to make the CLR portable.
//

// ===========================================================================


#if defined(HOST_WINDOWS)

#ifndef __PALCLR_H__
#define __PALCLR_H__

// This macro is used to standardize the wide character string literals between UNIX and Windows.
// Unix L"" is UTF32, and on windows it's UTF16.  Because of built-in assumptions on the size
// of string literals, it's important to match behaviour between Unix and Windows.  Unix will be defined
// as u"" (char16_t)
#define W(str)  L##str

#include <windef.h>

#if !defined(_DEBUG_IMPL) && defined(_DEBUG) && !defined(DACCESS_COMPILE)
#define _DEBUG_IMPL 1
#endif

#if __GNUC__
#ifndef __cdecl
#define __cdecl	__attribute__((__cdecl__))
#endif
#endif

#ifndef NOTHROW_DECL
#ifdef _MSC_VER
#define NOTHROW_DECL __declspec(nothrow)
#else
#define NOTHROW_DECL __attribute__((nothrow))
#endif // !_MSC_VER
#endif // !NOTHROW_DECL

#ifndef NOINLINE
#ifdef _MSC_VER
#define NOINLINE __declspec(noinline)
#else
#define NOINLINE __attribute__((noinline))
#endif // !_MSC_VER
#endif // !NOINLINE

#define ANALYZER_NORETURN

#ifdef _MSC_VER
#define EMPTY_BASES_DECL __declspec(empty_bases)
#else
#define EMPTY_BASES_DECL
#endif // !_MSC_VER

//
// CPP_ASSERT() can be used within a class definition, to perform a
// compile-time assertion involving private names within the class.
//
// MS compiler doesn't allow redefinition of the typedef within a template.
// gcc doesn't allow redefinition of the typedef within a class, though
// it does at file scope.
#define CPP_ASSERT(n, e) typedef char __C_ASSERT__##n[(e) ? 1 : -1];


// PORTABILITY_ASSERT and PORTABILITY_WARNING macros are meant to be used to
// mark places in the code that needs attention for portability. The usual
// usage pattern is:
//
// int get_scratch_register() {
// #if defined(TARGET_X86)
//     return eax;
// #elif defined(TARGET_AMD64)
//     return rax;
// #elif defined(TARGET_ARM)
//     return r0;
// #else
//     PORTABILITY_ASSERT("scratch register");
//     return 0;
// #endif
// }
//
// PORTABILITY_ASSERT is meant to be used inside functions/methods. It can
// introduce compile-time and/or run-time errors.
// PORTABILITY_WARNING is meant to be used outside functions/methods. It can
// introduce compile-time errors or warnings only.
//
// People starting new ports will first define these to just cause run-time
// errors. Once they fix all the places that need attention for portability,
// they can define PORTABILITY_ASSERT and PORTABILITY_WARNING to cause
// compile-time errors to make sure that they haven't missed anything.
//
// If it is reasonably possible all codepaths containing PORTABILITY_ASSERT
// should be compilable (e.g. functions should return NULL or something if
// they are expected to return a value).
//
// The message in these two macros should not contain any keywords like TODO
// or NYI. It should be just the brief description of the problem.

#if defined(TARGET_X86)
// Finished ports - compile-time errors
#define PORTABILITY_WARNING(message)    NEED_TO_PORT_THIS_ONE(NEED_TO_PORT_THIS_ONE)
#define PORTABILITY_ASSERT(message)     NEED_TO_PORT_THIS_ONE(NEED_TO_PORT_THIS_ONE)
#else
// Ports in progress - run-time asserts only
#define PORTABILITY_WARNING(message)
#define PORTABILITY_ASSERT(message)     _ASSERTE(false && (message))
#endif

#define DIRECTORY_SEPARATOR_CHAR_A '\\'
#define DIRECTORY_SEPARATOR_STR_A "\\"
#define DIRECTORY_SEPARATOR_CHAR_W W('\\')
#define DIRECTORY_SEPARATOR_STR_W W("\\")

#define PATH_SEPARATOR_CHAR_W W(';')
#define PATH_SEPARATOR_STR_W W(";")

#define VOLUME_SEPARATOR_CHAR_W W(':')

// PAL Macros
// Not all compilers support fully anonymous aggregate types, so the
// PAL provides names for those types. To allow existing definitions of
// those types to continue to work, we provide macros that should be
// used to reference fields within those types.

#ifndef DECIMAL_SCALE
#define DECIMAL_SCALE(dec)       ((dec).scale)
#endif

#ifndef DECIMAL_SIGN
#define DECIMAL_SIGN(dec)        ((dec).sign)
#endif

#ifndef DECIMAL_SIGNSCALE
#define DECIMAL_SIGNSCALE(dec)   ((dec).signscale)
#endif

#ifndef DECIMAL_LO32
#define DECIMAL_LO32(dec)        ((dec).Lo32)
#endif

#ifndef DECIMAL_MID32
#define DECIMAL_MID32(dec)       ((dec).Mid32)
#endif

#ifndef DECIMAL_HI32
#define DECIMAL_HI32(dec)       ((dec).Hi32)
#endif

#ifndef DECIMAL_LO64_GET
#define DECIMAL_LO64_GET(dec)       ((dec).Lo64)
#endif

#ifndef DECIMAL_LO64_SET
#define DECIMAL_LO64_SET(dec,value)   {(dec).Lo64 = value; }
#endif

#ifndef IMAGE_RELOC_FIELD
#define IMAGE_RELOC_FIELD(img, f)      ((img).f)
#endif

#ifndef IMAGE_IMPORT_DESC_FIELD
#define IMAGE_IMPORT_DESC_FIELD(img, f)     ((img).f)
#endif

#define IMAGE_RDE_ID(img) ((img)->Id)

#define IMAGE_RDE_NAME(img) ((img)->Name)

#define IMAGE_RDE_OFFSET(img) ((img)->OffsetToData)

#ifndef IMAGE_RDE_NAME_FIELD
#define IMAGE_RDE_NAME_FIELD(img, f)    ((img)->f)
#endif

#define IMAGE_RDE_OFFSET_FIELD(img, f) ((img)->f)

#ifndef IMAGE_FE64_FIELD
#define IMAGE_FE64_FIELD(img, f)    ((img).f)
#endif

#ifndef IMPORT_OBJ_HEADER_FIELD
#define IMPORT_OBJ_HEADER_FIELD(obj, f)    ((obj).f)
#endif

#ifndef IMAGE_COR20_HEADER_FIELD
#define IMAGE_COR20_HEADER_FIELD(obj, f)    ((obj).f)
#endif


// PAL Numbers
// Used to ensure cross-compiler compatibility when declaring large
// integer constants. 64-bit integer constants should be wrapped in the
// declarations listed here.
//
// Each of the #defines here is wrapped to avoid conflicts with pal.h.

#if defined(_MSC_VER)

// MSVC's way of declaring large integer constants
// If you define these in one step, without the _HELPER macros, you
// get extra whitespace when composing these with other concatenating macros.
#ifndef I64
#define I64_HELPER(x) x ## i64
#define I64(x)        I64_HELPER(x)
#endif

#ifndef UI64
#define UI64_HELPER(x) x ## ui64
#define UI64(x)        UI64_HELPER(x)
#endif

#else

// GCC's way of declaring large integer constants
// If you define these in one step, without the _HELPER macros, you
// get extra whitespace when composing these with other concatenating macros.
#ifndef I64
#define I64_HELPER(x) x ## LL
#define I64(x)        I64_HELPER(x)
#endif

#ifndef UI64
#define UI64_HELPER(x) x ## ULL
#define UI64(x)        UI64_HELPER(x)
#endif

#endif


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
// - It is not possible to directly use the local variables in the try block.
// All the local information that the filter has to need to know about should
// be passed through pv parameter
//
//
// Simple examples:
//
// struct Param { ... local variables used in try block and filter ... } param;
// PAL_TRY(Param *, pParam, &param) { // read as: Param *pParam = &param;
//   ....
// } PAL_FINALLY {
//   ....
// }
// PAL_ENDTRY
//
//
// struct Param { ... local variables used in try block and filter ... } param;
// PAL_TRY(Param *, pParam, &param) {
//   ....
// } PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER) {
//   ....
// }
// PAL_ENDTRY
//
//
// LONG MyFilter(PEXCEPTION_POINTERS *pExceptionInfo, PVOID pv)
// {
// ...
// }
// PAL_TRY(void *, unused, NULL) {
//   ....
// } PAL_EXCEPT_FILTER(MyFilter) {
//   ....
// }
// PAL_ENDTRY
//
//
// Complex example:
//
// struct MyParams
// {
//     ...
// } params;
//
// PAL_TRY(MyParams *, pMyParamsOuter, &params) {
//   PAL_TRY(MyParams *, pMyParamsInnter, pMyParamsOuter) {
//       ...
//       if (error) goto Done;
//       ...
//   Done: ;
//   } PAL_EXCEPT_FILTER(OtherFilter) {
//   ...
//   }
//   PAL_ENDTRY
// }
// PAL_FINALLY {
// }
// PAL_ENDTRY
//

#include "staticcontract.h"

#define HardwareExceptionHolder

// Note: PAL_SEH_RESTORE_GUARD_PAGE is only ever defined in clrex.h, so we only restore guard pages automatically
// when these macros are used from within the VM.
#define PAL_SEH_RESTORE_GUARD_PAGE

#define PAL_TRY_NAKED                                                           \
    {                                                                           \
        bool __exHandled; __exHandled = false;                                  \
        DWORD __exCode; __exCode = 0;                                           \
        SCAN_EHMARKER();                                                        \
        __try                                                                   \
        {                                                                       \
            SCAN_EHMARKER_TRY();

#define PAL_EXCEPT_NAKED(Disposition)                                           \
        }                                                                       \
        __except(__exCode = GetExceptionCode(), Disposition)                    \
        {                                                                       \
            __exHandled = true;                                                 \
            SCAN_EHMARKER_CATCH();                                              \
            PAL_SEH_RESTORE_GUARD_PAGE

#define PAL_EXCEPT_FILTER_NAKED(pfnFilter, param)                               \
        }                                                                       \
        __except(__exCode = GetExceptionCode(),                                 \
                 pfnFilter(GetExceptionInformation(), param))                   \
        {                                                                       \
            __exHandled = true;                                                 \
            SCAN_EHMARKER_CATCH();                                              \
            PAL_SEH_RESTORE_GUARD_PAGE

#define PAL_FINALLY_NAKED                                                       \
        }                                                                       \
        __finally                                                               \
        {                                                                       \

#define PAL_ENDTRY_NAKED                                                        \
        }                                                                       \
        PAL_ENDTRY_NAKED_DBG                                                    \
    }                                                                           \


#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
//
// In debug mode, compile the try body as a method of a local class.
// This way, the compiler will check that the body is not directly
// accessing any local variables and arguments.
//
#define PAL_TRY(__ParamType, __paramDef, __paramRef)                            \
{                                                                               \
    __ParamType __param = __paramRef;                                           \
    __ParamType __paramToPassToFilter = __paramRef;                             \
    class __Body                                                                \
    {                                                                           \
    public:                                                                     \
        static void Run(__ParamType __paramDef)                                 \
    {                                                                           \
        PAL_TRY_HANDLER_DBG_BEGIN

// PAL_TRY implementation that abstracts usage of COMPILER_INSTANCE*, which is used by
// JIT64. On Windows, we dont need to do anything special as we dont have nested classes/methods
// as on PAL.
#define PAL_TRY_CI(__ParamType, __paramDef, __paramRef)                         \
{                                                                               \
    struct __HandlerData {                                                      \
        __ParamType __param;                                                    \
        COMPILER_INSTANCE *__ciPtr;                                             \
    };                                                                          \
    __HandlerData handlerData;                                                  \
    handlerData.__param = __paramRef;                                           \
    handlerData.__ciPtr = ciPtr;                                                \
     __HandlerData* __param = &handlerData;                                     \
    __ParamType __paramToPassToFilter = __paramRef;                             \
    class __Body                                                                \
    {                                                                           \
    public:                                                                     \
    static void Run(__HandlerData* __pHandlerData)                              \
    {                                                                           \
    PAL_TRY_HANDLER_DBG_BEGIN                                                   \
        COMPILER_INSTANCE *ciPtr = __pHandlerData->__ciPtr;                     \
        __ParamType __paramDef = __pHandlerData->__param;


#define PAL_TRY_FOR_DLLMAIN(__ParamType, __paramDef, __paramRef, __reason)      \
{                                                                               \
    __ParamType __param = __paramRef;                                           \
    __ParamType __paramToPassToFilter = __paramRef;                             \
    class __Body                                                                \
    {                                                                           \
    public:                                                                     \
        static void Run(__ParamType __paramDef)                                 \
    {                                                                           \
            PAL_TRY_HANDLER_DBG_BEGIN_DLLMAIN(__reason)

#define PAL_EXCEPT(Disposition)                                                 \
            PAL_TRY_HANDLER_DBG_END                                             \
        }                                                                       \
    };                                                                          \
        PAL_TRY_NAKED                                                           \
    __Body::Run(__param);                                                       \
    PAL_EXCEPT_NAKED(Disposition)

#define PAL_EXCEPT_FILTER(pfnFilter)                                            \
            PAL_TRY_HANDLER_DBG_END                                             \
        }                                                                       \
    };                                                                          \
    PAL_TRY_NAKED                                                               \
    __Body::Run(__param);                                                       \
    PAL_EXCEPT_FILTER_NAKED(pfnFilter, __paramToPassToFilter)

#define PAL_FINALLY                                                             \
            PAL_TRY_HANDLER_DBG_END                                             \
        }                                                                       \
    };                                                                          \
    PAL_TRY_NAKED                                                               \
    __Body::Run(__param);                                                       \
    PAL_FINALLY_NAKED

#define PAL_ENDTRY                                                              \
    PAL_ENDTRY_NAKED                                                            \
}

#else // _DEBUG

#define PAL_TRY(__ParamType, __paramDef, __paramRef)                            \
{                                                                               \
    __ParamType __param = __paramRef;                                           \
    __ParamType __paramDef = __param;                                           \
    PAL_TRY_NAKED                                                               \
    PAL_TRY_HANDLER_DBG_BEGIN

// PAL_TRY implementation that abstracts usage of COMPILER_INSTANCE*, which is used by
// JIT64. On Windows, we dont need to do anything special as we dont have nested classes/methods
// as on PAL.
#define PAL_TRY_CI(__ParamType, __paramDef, __paramRef) PAL_TRY(__ParamType, __paramDef, __paramRef)

#define PAL_TRY_FOR_DLLMAIN(__ParamType, __paramDef, __paramRef, __reason)      \
{                                                                               \
    __ParamType __param = __paramRef;                                           \
    __ParamType __paramDef; __paramDef = __param;                               \
    PAL_TRY_NAKED                                                               \
    PAL_TRY_HANDLER_DBG_BEGIN_DLLMAIN(__reason)

#define PAL_EXCEPT(Disposition)                                                 \
        PAL_TRY_HANDLER_DBG_END                                                 \
        PAL_EXCEPT_NAKED(Disposition)

#define PAL_EXCEPT_FILTER(pfnFilter)                                            \
        PAL_TRY_HANDLER_DBG_END                                                 \
        PAL_EXCEPT_FILTER_NAKED(pfnFilter, __param)

#define PAL_FINALLY                                                             \
        PAL_TRY_HANDLER_DBG_END                                                 \
        PAL_FINALLY_NAKED

#define PAL_ENDTRY                                                              \
    PAL_ENDTRY_NAKED                                                            \
    }

#endif // _DEBUG

// Executes the handler if the specified exception code matches
// the one in the exception. Otherwise, returns EXCEPTION_CONTINUE_SEARCH.
#define PAL_EXCEPT_IF_EXCEPTION_CODE(dwExceptionCode) PAL_EXCEPT((GetExceptionCode() == (dwExceptionCode))?EXCEPTION_EXECUTE_HANDLER:EXCEPTION_CONTINUE_SEARCH)

#define PAL_CPP_TRY try
#define PAL_CPP_ENDTRY
#define PAL_CPP_THROW(type, obj) do { SCAN_THROW_MARKER; throw obj; } while (false)
#define PAL_CPP_RETHROW do { SCAN_THROW_MARKER; throw; } while (false)
#define PAL_CPP_CATCH_DERIVED(type, obj) catch (type * obj)
#define PAL_CPP_CATCH_ALL catch (...)
#define PAL_CPP_CATCH_EXCEPTION_NOARG catch (Exception *)


#if defined(SOURCE_FORMATTING)
#define __annotation(x)
#endif


#if defined(_DEBUG_IMPL) && !defined(JIT_BUILD) && !defined(CROSS_COMPILE) && !defined(DISABLE_CONTRACTS)
#define PAL_TRY_HANDLER_DBG_BEGIN                                               \
    BOOL ___oldOkayToThrowValue = FALSE;                                        \
    ClrDebugState *___pState = ::GetClrDebugState();                            \
    __try                                                                       \
    {                                                                           \
        ___oldOkayToThrowValue = ___pState->IsOkToThrow();                      \
        ___pState->SetOkToThrow();

// Special version that avoids touching the debug state after doing work in a DllMain for process or thread detach.
#define PAL_TRY_HANDLER_DBG_BEGIN_DLLMAIN(_reason)                              \
    BOOL ___oldOkayToThrowValue = FALSE;                                        \
    ClrDebugState *___pState = NULL;                                            \
    if (_reason != DLL_PROCESS_ATTACH)                                          \
        ___pState = CheckClrDebugState();                                       \
    __try                                                                       \
    {                                                                           \
        if (___pState)                                                          \
        {                                                                       \
            ___oldOkayToThrowValue = ___pState->IsOkToThrow();                  \
            ___pState->SetOkToThrow();                                        \
        }                                                                       \
        if ((_reason == DLL_PROCESS_DETACH) || (_reason == DLL_THREAD_DETACH))  \
        {                                                                       \
            ___pState = NULL;                                                   \
        }

#define PAL_TRY_HANDLER_DBG_END                                                 \
    }                                                                           \
    __finally                                                                   \
    {                                                                           \
        if (___pState != NULL)                                                  \
        {                                                                       \
            _ASSERTE(___pState == CheckClrDebugState());                        \
            ___pState->SetOkToThrow( ___oldOkayToThrowValue );                \
        }                                                                       \
    }

#define PAL_ENDTRY_NAKED_DBG

#else
#define PAL_TRY_HANDLER_DBG_BEGIN                   ANNOTATION_TRY_BEGIN;
#define PAL_TRY_HANDLER_DBG_BEGIN_DLLMAIN(_reason)  ANNOTATION_TRY_BEGIN;
#define PAL_TRY_HANDLER_DBG_END                     ANNOTATION_TRY_END;
#define PAL_ENDTRY_NAKED_DBG
#endif // defined(ENABLE_CONTRACTS_IMPL)


#if !BIGENDIAN
// For little-endian machines, do nothing
#define VAL16(x) x
#define VAL32(x) x
#define VAL64(x) x
#define SwapString(x)
#define SwapStringLength(x, y)
#define SwapGuid(x)
#endif  // !BIGENDIAN

#ifdef _MSC_VER
// Get Unaligned values from a potentially unaligned object
#define GET_UNALIGNED_16(_pObject)  (*(UINT16 UNALIGNED *)(_pObject))
#define GET_UNALIGNED_32(_pObject)  (*(UINT32 UNALIGNED *)(_pObject))
#define GET_UNALIGNED_64(_pObject)  (*(UINT64 UNALIGNED *)(_pObject))

// Set Value on an potentially unaligned object
#define SET_UNALIGNED_16(_pObject, _Value)  (*(UNALIGNED UINT16 *)(_pObject)) = (UINT16)(_Value)
#define SET_UNALIGNED_32(_pObject, _Value)  (*(UNALIGNED UINT32 *)(_pObject)) = (UINT32)(_Value)
#define SET_UNALIGNED_64(_pObject, _Value)  (*(UNALIGNED UINT64 *)(_pObject)) = (UINT64)(_Value)

// Get Unaligned values from a potentially unaligned object and swap the value
#define GET_UNALIGNED_VAL16(_pObject) VAL16(GET_UNALIGNED_16(_pObject))
#define GET_UNALIGNED_VAL32(_pObject) VAL32(GET_UNALIGNED_32(_pObject))
#define GET_UNALIGNED_VAL64(_pObject) VAL64(GET_UNALIGNED_64(_pObject))

// Set a swap Value on an potentially unaligned object
#define SET_UNALIGNED_VAL16(_pObject, _Value) SET_UNALIGNED_16(_pObject, VAL16((UINT16)_Value))
#define SET_UNALIGNED_VAL32(_pObject, _Value) SET_UNALIGNED_32(_pObject, VAL32((UINT32)_Value))
#define SET_UNALIGNED_VAL64(_pObject, _Value) SET_UNALIGNED_64(_pObject, VAL64((UINT64)_Value))
#endif

#ifdef HOST_64BIT
#define VALPTR(x) VAL64(x)
#define GET_UNALIGNED_PTR(x) GET_UNALIGNED_64(x)
#define GET_UNALIGNED_VALPTR(x) GET_UNALIGNED_VAL64(x)
#define SET_UNALIGNED_PTR(p,x) SET_UNALIGNED_64(p,x)
#define SET_UNALIGNED_VALPTR(p,x) SET_UNALIGNED_VAL64(p,x)
#else
#define VALPTR(x) VAL32(x)
#define GET_UNALIGNED_PTR(x) GET_UNALIGNED_32(x)
#define GET_UNALIGNED_VALPTR(x) GET_UNALIGNED_VAL32(x)
#define SET_UNALIGNED_PTR(p,x) SET_UNALIGNED_32(p,x)
#define SET_UNALIGNED_VALPTR(p,x) SET_UNALIGNED_VAL32(p,x)
#endif

#define MAKEDLLNAME_W(name) name W(".dll")
#define MAKEDLLNAME_A(name) name  ".dll"

#ifdef UNICODE
#define MAKEDLLNAME(x) MAKEDLLNAME_W(x)
#else
#define MAKEDLLNAME(x) MAKEDLLNAME_A(x)
#endif

#if !defined(MAX_LONGPATH)
#define MAX_LONGPATH   260 /* max. length of full pathname */
#endif
#if !defined(MAX_PATH_FNAME)
#define MAX_PATH_FNAME   MAX_PATH /* max. length of full pathname */
#endif

#define __clr_reserved __reserved

#endif // __PALCLR_H__

#include "palclr_win.h"

#ifndef IMAGE_FILE_MACHINE_LOONGARCH64
#define IMAGE_FILE_MACHINE_LOONGARCH64       0x6264  // LOONGARCH64.
#endif

#endif // defined(HOST_WINDOWS)
