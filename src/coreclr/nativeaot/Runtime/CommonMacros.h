// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __COMMONMACROS_H__
#define __COMMONMACROS_H__

#include "rhassert.h"
#include <minipal/utils.h>

#define EXTERN_C extern "C"

#if defined(HOST_X86) && !defined(HOST_UNIX)
#define FASTCALL __fastcall
#define STDCALL __stdcall
#else
#define FASTCALL
#define STDCALL
#endif

#define F_CALL_CONV FASTCALL
#define QCALLTYPE

#ifdef _MSC_VER

#define MSVC_SAVE_WARNING_STATE() __pragma(warning(push))
#define MSVC_DISABLE_WARNING(warn_num) __pragma(warning(disable: warn_num))
#define MSVC_RESTORE_WARNING_STATE() __pragma(warning(pop))

#else

#define MSVC_SAVE_WARNING_STATE()
#define MSVC_DISABLE_WARNING(warn_num)
#define MSVC_RESTORE_WARNING_STATE()

#endif // _MSC_VER

#include <stddef.h>

#ifndef __GCENV_BASE_INCLUDED__

#include <cstdint>

//
// This macro returns val rounded up as necessary to be a multiple of alignment; alignment must be a power of 2
//
inline uintptr_t ALIGN_UP(uintptr_t val, uintptr_t alignment);
template <typename T>
inline T* ALIGN_UP(T* val, uintptr_t alignment);

inline uintptr_t ALIGN_DOWN(uintptr_t val, uintptr_t alignment);
template <typename T>
inline T* ALIGN_DOWN(T* val, uintptr_t alignment);

#endif // !__GCENV_BASE_INCLUDED__

inline bool IS_ALIGNED(uintptr_t val, uintptr_t alignment);
template <typename T>
inline bool IS_ALIGNED(T* val, uintptr_t alignment);

#ifndef DACCESS_COMPILE

#ifndef ZeroMemory
#define ZeroMemory(Destination,Length) memset((Destination),0,(Length))
#endif

#endif // !DACCESS_COMPILE

//-------------------------------------------------------------------------------------------------
// Platform-specific defines

#ifdef HOST_64BIT

#define LOG2_PTRSIZE 3
#define POINTER_SIZE 8

#else // HOST_64BIT

#define LOG2_PTRSIZE 2
#define POINTER_SIZE 4

#endif // HOST_64BIT

#if defined(TARGET_ARM)
#define THUMB_CODE 1
#endif

// Type for external code location references inside the assembly code.
typedef uint8_t CODE_LOCATION;

//
// Define an unmanaged function called from managed code that needs to execute in co-operative GC mode. (There
// should be very few of these, most such functions will be simply p/invoked).
//

#define FCALL_METHOD_NAME(name, ...) name
#define FCALL_METHOD_NAME_(tuple) FCALL_METHOD_NAME tuple

#if defined(HOST_X86) && defined(HOST_WINDOWS)

// x86 is special case. It supports multiple calling conventions (fastcall, stdcall, cdecl)
// and mangles the method names according to the calling convention (eg. @fastcall@4, _stdcall@4,
// _cdecl).
//
// The managed code uses its own calling convention that is different from the native call
// conventions. It's similar to fastcall but pushes the arguments to stack in reverse order.
// Additionally, for the sake of simplicity we don't decorate the symbol names.
//
// In order to bridge the managed calling convention we use two tricks:
// - The FCIMPL and FCDECL macros reorder parameters for any method with 4 or more arguments.
// - A linker comment is used to pass the "/alternatename:foo=@foo@4" switch to allow the
//   symbols to be resolved to the fastcall decorated name.

#define FCALL_ARGHELPER_NAME(_0, _1, _2, _3, _4, _5, NAME, ...) NAME
#define FCALL_ARGHELPER_NAME_(tuple) FCALL_ARGHELPER_NAME tuple

#define FCALL_ARGHELPER0(dummy) ()
#define FCALL_ARGHELPER1(dummy, a) (a)
#define FCALL_ARGHELPER2(dummy, a, b) (a, b)
#define FCALL_ARGHELPER3(dummy, a, b, c) (a, b, c)
#define FCALL_ARGHELPER4(dummy, a, b, c, d) (a, b, d, c)
#define FCALL_ARGHELPER5(dummy, a, b, c, d, e) (a, b, e, d, c)

#define FCALL_STRINGIFY(s) #s
#define FCALL_XSTRINGIFY(s) FCALL_STRINGIFY(s)

#define FCALL_METHOD_ARGS(...) FCALL_ARGHELPER_NAME_((__VA_ARGS__, FCALL_ARGHELPER5, FCALL_ARGHELPER4, FCALL_ARGHELPER3, FCALL_ARGHELPER2, FCALL_ARGHELPER1, FCALL_ARGHELPER0)) (__VA_ARGS__)
#define FCALL_METHOD_ARGS_(tuple) FCALL_METHOD_ARGS tuple

#define FCALL_ARGHELPER_STACKSIZE(...) FCALL_ARGHELPER_NAME_((__VA_ARGS__, 20, 16, 12, 8, 4, 0))
#define FCALL_IMPL_ALTNAME(_method, _argSize) FCALL_XSTRINGIFY(/alternatename:_method=@_method@_argSize)
#define FCALL_DECL_ALTNAME(_method, _argSize) FCALL_XSTRINGIFY(/alternatename:@_method@_argSize=_method)
#define FCDECL_RENAME(_rettype, ...) \
    _Pragma(FCALL_XSTRINGIFY(comment (linker, FCALL_DECL_ALTNAME(FCALL_METHOD_NAME_((__VA_ARGS__)), FCALL_ARGHELPER_STACKSIZE(__VA_ARGS__)))))
#define FCIMPL_RENAME(_rettype, ...) \
    _Pragma(FCALL_XSTRINGIFY(comment (linker, FCALL_IMPL_ALTNAME(FCALL_METHOD_NAME_((__VA_ARGS__)), FCALL_ARGHELPER_STACKSIZE(__VA_ARGS__)))))
#define FCIMPL_RENAME_ARGSIZE(_rettype, _method, _argSize) \
    _Pragma(FCALL_XSTRINGIFY(comment (linker, FCALL_XSTRINGIFY(/alternatename:_method=@_method##_FCall@_argSize))))

#define FCIMPL1_F(_rettype, _method, a) \
    FCIMPL_RENAME_ARGSIZE(_rettype, _method, 4) \
    EXTERN_C _rettype F_CALL_CONV _method##_FCall (a) \
    {
#define FCIMPL1_D(_rettype, _method, a) \
    FCIMPL_RENAME_ARGSIZE(_rettype, _method, 8) \
    EXTERN_C _rettype F_CALL_CONV _method##_FCall (a) \
    {
#define FCIMPL1_L FCIMPL1_D
#define FCIMPL2_FF(_rettype, _method, a, b) \
    FCIMPL_RENAME_ARGSIZE(_rettype, _method, 8) \
    EXTERN_C _rettype F_CALL_CONV _method##_FCall (b, a) \
    {
#define FCIMPL2_DD(_rettype, _method, a, b) \
    FCIMPL_RENAME_ARGSIZE(_rettype, _method, 16) \
    EXTERN_C _rettype F_CALL_CONV _method##_FCall (b, a) \
    {
#define FCIMPL2_LL FCIMPL2_DD
#define FCIMPL2_FI(_rettype, _method, a, b) \
    FCIMPL_RENAME_ARGSIZE(_rettype, _method, 8) \
    EXTERN_C _rettype F_CALL_CONV _method##_FCall (a, b) \
    {
#define FCIMPL2_DI(_rettype, _method, a, b) \
    FCIMPL_RENAME_ARGSIZE(_rettype, _method, 12) \
    EXTERN_C _rettype F_CALL_CONV _method##_FCall (a, b) \
    {
#define FCIMPL3_FFF(_rettype, _method, a, b, c) \
    FCIMPL_RENAME_ARGSIZE(_rettype, _method, 12) \
    EXTERN_C _rettype F_CALL_CONV _method##_FCall (c, b, a) \
    {
#define FCIMPL3_DDD(_rettype, _method, a, b, c) \
    FCIMPL_RENAME_ARGSIZE(_rettype, _method, 24) \
    EXTERN_C _rettype F_CALL_CONV _method##_FCall (c, b, a) \
    {
#define FCIMPL3_FII(_rettype, _method, a, b, c) \
    FCIMPL_RENAME_ARGSIZE(_rettype, _method, 12) \
    EXTERN_C _rettype F_CALL_CONV _method##_FCall (a, b, c) \
    {
#define FCIMPL3_DII(_rettype, _method, a, b, c) \
    FCIMPL_RENAME_ARGSIZE(_rettype, _method, 16) \
    EXTERN_C _rettype F_CALL_CONV _method##_FCall (a, b, c) \
    {
#define FCIMPL3_ILL(_rettype, _method, a, b, c) \
    FCIMPL_RENAME_ARGSIZE(_rettype, _method, 20) \
    EXTERN_C _rettype F_CALL_CONV _method##_FCall (a, c, b) \
    {

#elif defined(HOST_WASM)

#define FCDECL_RENAME(_rettype, _method, ...) \
    EXTERN_C _rettype _method##_IMPL FCALL_METHOD_ARGS_NO_METHOD_NAME(__VA_ARGS__);

// Wasm's managed calling convention passes the shadow stack as the first argument.
#define FCALL_METHOD_ARGS_NO_METHOD_NAME(...) (uintptr_t callersStackPointer __VA_OPT__(,) __VA_ARGS__)

#define FCALL_METHOD_ARGS(dummy, ...) FCALL_METHOD_ARGS_NO_METHOD_NAME(__VA_ARGS__)
#define FCALL_METHOD_ARGS_(tuple) FCALL_METHOD_ARGS tuple

#define FCIMPL1_F FCIMPL1
#define FCIMPL1_D FCIMPL1
#define FCIMPL1_L FCIMPL1
#define FCIMPL2_FF FCIMPL2
#define FCIMPL2_DD FCIMPL2
#define FCIMPL2_LL FCIMPL2
#define FCIMPL2_FI FCIMPL2
#define FCIMPL2_DI FCIMPL2
#define FCIMPL3_FFF FCIMPL3
#define FCIMPL3_DDD FCIMPL3
#define FCIMPL3_FII FCIMPL3
#define FCIMPL3_DII FCIMPL3
#define FCIMPL3_ILL FCIMPL3

#define FCDECL2_RAW(_rettype, _method, a, b) _rettype F_CALL_CONV _method(a, b)

#else

#define FCDECL_RENAME(_rettype, ...)
#define FCIMPL_RENAME(_rettype, ...)

#define FCALL_METHOD_ARGS_NO_METHOD_NAME(...) (__VA_ARGS__)
#define FCALL_METHOD_ARGS(dummy, ...) FCALL_METHOD_ARGS_NO_METHOD_NAME(__VA_ARGS__)
#define FCALL_METHOD_ARGS_(tuple) FCALL_METHOD_ARGS tuple

#define FCIMPL1_F(_rettype, _method, a) \
    EXTERN_C _rettype F_CALL_CONV _method FCALL_METHOD_ARGS_NO_METHOD_NAME(a) \
    {
#define FCIMPL1_D(_rettype, _method, a) \
    EXTERN_C _rettype F_CALL_CONV _method FCALL_METHOD_ARGS_NO_METHOD_NAME(a) \
    {
#define FCIMPL1_L FCIMPL1_D
#define FCIMPL2_FF(_rettype, _method, a, b) \
    EXTERN_C _rettype F_CALL_CONV _method FCALL_METHOD_ARGS_NO_METHOD_NAME(a, b) \
    {
#define FCIMPL2_DD(_rettype, _method, a, b) \
    EXTERN_C _rettype F_CALL_CONV _method FCALL_METHOD_ARGS_NO_METHOD_NAME(a, b) \
    {
#define FCIMPL2_LL FCIMPL2_DD
#define FCIMPL2_FI(_rettype, _method, a, b) \
    EXTERN_C _rettype F_CALL_CONV _method FCALL_METHOD_ARGS_NO_METHOD_NAME(a, b) \
    {
#define FCIMPL2_DI(_rettype, _method, a, b) \
    EXTERN_C _rettype F_CALL_CONV _method FCALL_METHOD_ARGS_NO_METHOD_NAME(a, b) \
    {
#define FCIMPL3_FFF(_rettype, _method, a, b, c) \
    EXTERN_C _rettype F_CALL_CONV _method FCALL_METHOD_ARGS_NO_METHOD_NAME(a, b, c) \
    {
#define FCIMPL3_DDD(_rettype, _method, a, b, c) \
    EXTERN_C _rettype F_CALL_CONV _method FCALL_METHOD_ARGS_NO_METHOD_NAME(a, b, c) \
    {
#define FCIMPL3_FII(_rettype, _method, a, b, c) \
    EXTERN_C _rettype F_CALL_CONV _method FCALL_METHOD_ARGS_NO_METHOD_NAME(a, b, c) \
    {
#define FCIMPL3_DII(_rettype, _method, a, b, c) \
    EXTERN_C _rettype F_CALL_CONV _method FCALL_METHOD_ARGS_NO_METHOD_NAME(a, b, c) \
    {
#define FCIMPL3_ILL(_rettype, _method, a, b, c) \
    EXTERN_C _rettype F_CALL_CONV _method FCALL_METHOD_ARGS_NO_METHOD_NAME(a, b, c) \
    {

#endif

#define FCDECL_(_rettype, ...) \
    FCDECL_RENAME(_rettype, __VA_ARGS__) \
    EXTERN_C _rettype F_CALL_CONV FCALL_METHOD_NAME_((__VA_ARGS__)) FCALL_METHOD_ARGS_((__VA_ARGS__))
#define FCDECL0(_rettype, _method) FCDECL_(_rettype, _method)
#define FCDECL1(_rettype, _method, a) FCDECL_(_rettype, _method, a)
#define FCDECL2(_rettype, _method, a, b) FCDECL_(_rettype, _method, a, b)
#define FCDECL3(_rettype, _method, a, b, c) FCDECL_(_rettype, _method, a, b, c)
#define FCDECL4(_rettype, _method, a, b, c, d) FCDECL_(_rettype, _method, a, b, c, d)
#define FCDECL5(_rettype, _method, a, b, c, d, e) FCDECL_(_rettype, _method, a, b, c, d, e)

#ifndef FCDECL2_RAW
#define FCDECL2_RAW(_rettype, _method, a, b) FCDECL2(_rettype, _method, a, b)
#endif

#ifdef HOST_WASM

#define WASM_WRAPPER_FUNC_INITIAL \
{ \
    asm ("local.get 0\n" /* Capture callersStackPointer onto the stack for calling _IMPL function*/ \
         "local.get 0\n" /* Capture callersStackPointer onto the stack for setting the __stack_pointer */ \
         "global.get __stack_pointer\n" /* Get current value of stack global */ \
         "local.set 0\n"  /* Store current stack global into callersStackPointer local */ \
         "global.set __stack_pointer\n" /* Set stack global to the initial value of callersStackPointer, which is the current stack pointer for the managed caller */

#define WASM_WRAPPER_FUNC_EPILOG(_method) \
             "call %0\n" /* Call the actual implementation function*/ \
             "local.get 0\n" /* Load the original stack pointer from stack Arg local */ \
             "global.set __stack_pointer\n" /* Restore the original stack pointer to the stack global */ \
             "return" :: "i" (_method ## _IMPL)); \
}

#define WASM_WRAPPER_FUNC_0(_rettype, _method) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer) WASM_WRAPPER_FUNC_INITIAL WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_1(_rettype, _method, a) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a) WASM_WRAPPER_FUNC_INITIAL "local.get 1\n" WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_2(_rettype, _method, a, b) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a, b) WASM_WRAPPER_FUNC_INITIAL "local.get 1\nlocal.get 2\n" WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_3(_rettype, _method, a, b, c) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a, b, c) WASM_WRAPPER_FUNC_INITIAL "local.get 1\nlocal.get 2\nlocal.get 3\n" WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_4(_rettype, _method, a, b, c, d) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a, b, c, d) WASM_WRAPPER_FUNC_INITIAL "local.get 1\nlocal.get 2\nlocal.get 3\nlocal.get 4\n" WASM_WRAPPER_FUNC_EPILOG(_method)
#define WASM_WRAPPER_FUNC_5(_rettype, _method, a, b, c, d, e) __attribute__((naked)) _rettype _method(uintptr_t callersStackPointer, a, b, c, d, e) WASM_WRAPPER_FUNC_INITIAL "local.get 1\nlocal.get 2\nlocal.get 3\nlocal.get 4\nlocal.get 5\n" WASM_WRAPPER_FUNC_EPILOG(_method)

#define WASM_CALLABLE_FUNC_0(_rettype, _method) \
    FCDECL0(_rettype, _method); \
    EXTERN_C WASM_WRAPPER_FUNC_0(_rettype, _method) \
    EXTERN_C _rettype _method ## _IMPL (uintptr_t callersStackPointer)

#define WASM_CALLABLE_FUNC_1(_rettype, _method, a) \
    FCDECL1(_rettype, _method, a); \
    EXTERN_C WASM_WRAPPER_FUNC_1(_rettype, _method, a) \
    EXTERN_C _rettype _method ## _IMPL (uintptr_t callersStackPointer, a)

#define WASM_CALLABLE_FUNC_2(_rettype, _method, a, b) \
    FCDECL2(_rettype, _method, a, b); \
    EXTERN_C WASM_WRAPPER_FUNC_2(_rettype, _method, a, b) \
    EXTERN_C _rettype _method ## _IMPL (uintptr_t callersStackPointer, a, b)

#define WASM_CALLABLE_FUNC_3(_rettype, _method, a, b, c) \
    FCDECL3(_rettype, _method, a, b, c); \
    EXTERN_C WASM_WRAPPER_FUNC_3(_rettype, _method, a, b, c) \
    EXTERN_C _rettype _method ## _IMPL (uintptr_t callersStackPointer, a, b, c)

#define WASM_CALLABLE_FUNC_4(_rettype, _method, a, b, c, d) \
    FCDECL4(_rettype, _method, a, b, c, d); \
    EXTERN_C WASM_WRAPPER_FUNC_4(_rettype, _method, a, b, c, d) \
    EXTERN_C _rettype _method ## _IMPL (uintptr_t callersStackPointer, a, b, c, d)

#define WASM_CALLABLE_FUNC_5(_rettype, _method, a, b, c, d, e) \
    FCDECL5(_rettype, _method, a, b, c, d, e); \
    EXTERN_C WASM_WRAPPER_FUNC_5(_rettype, _method, a, b, c, d, e) \
    EXTERN_C _rettype _method ## _IMPL (uintptr_t callersStackPointer, a, b, c, d, e)

#define FCIMPL0(rettype, funcname) WASM_CALLABLE_FUNC_0(rettype, funcname) {
#define FCIMPL1(rettype, funcname, a1) WASM_CALLABLE_FUNC_1(rettype, funcname, a1) {
#define FCIMPL2(rettype, funcname, a1, a2) WASM_CALLABLE_FUNC_2(rettype, funcname, a1, a2) {
#define FCIMPL3(rettype, funcname, a1, a2, a3) WASM_CALLABLE_FUNC_3(rettype, funcname, a1, a2, a3) {
#define FCIMPL4(rettype, funcname, a1, a2, a3, a4) WASM_CALLABLE_FUNC_4(rettype, funcname, a1, a2, a3, a4) {
#define FCIMPL5(rettype, funcname, a1, a2, a3, a4, a5) WASM_CALLABLE_FUNC_5(rettype, funcname, a1, a2, a3, a4, a5) {

#define FCIMPL2_RAW(_rettype, _method, a, b) \
    EXTERN_C FCDECL2_RAW(_rettype, _method, a, b) \
    {

#else // HOST_WASM

#define FCIMPL_(_rettype, ...) \
    FCIMPL_RENAME(_rettype, __VA_ARGS__) \
    EXTERN_C _rettype F_CALL_CONV FCALL_METHOD_NAME_((__VA_ARGS__)) FCALL_METHOD_ARGS_((__VA_ARGS__)) \
    {
#define FCIMPL0(_rettype, _method) FCIMPL_(_rettype, _method)
#define FCIMPL1(_rettype, _method, a) FCIMPL_(_rettype, _method, a)
#define FCIMPL2(_rettype, _method, a, b) FCIMPL_(_rettype, _method, a, b)
#define FCIMPL3(_rettype, _method, a, b, c) FCIMPL_(_rettype, _method, a, b, c)
#define FCIMPL4(_rettype, _method, a, b, c, d) FCIMPL_(_rettype, _method, a, b, c, d)
#define FCIMPL5(_rettype, _method, a, b, c, d, e) FCIMPL_(_rettype, _method, a, b, c, d, e)

#endif // HOST_WASM

#ifndef FCIMPL2_RAW
#define FCIMPL2_RAW(_rettype, _method, a, b) FCIMPL2(_rettype, _method, a, b)
#endif

#define FCIMPLEND \
    }

typedef bool CLR_BOOL;

typedef int32_t FC_BOOL_RET;
typedef int32_t FC_BOOL_ARG;

#define FC_RETURN_BOOL(x)   do { return !!(x); } while(0)
#define FC_ACCESS_BOOL(x) ((uint8_t)x != 0)

#ifndef DACCESS_COMPILE
#define IN_DAC(x)
#define NOT_IN_DAC(x) x
#else
#define IN_DAC(x) x
#define NOT_IN_DAC(x)
#endif

#endif // __COMMONMACROS_H__
