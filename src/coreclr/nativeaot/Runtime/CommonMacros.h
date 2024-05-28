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

#ifndef offsetof
#define offsetof(s,m)   (uintptr_t)( (intptr_t)&reinterpret_cast<const volatile char&>((((s *)0)->m)) )
#endif // offsetof

#ifdef __GNUC__
#ifdef HOST_64BIT
#define __int64     long
#else // HOST_64BIT
#define __int64     long long
#endif // HOST_64BIT
#endif // __GNUC__

#ifndef FORCEINLINE
#define FORCEINLINE __forceinline
#endif

#ifdef __GNUC__
#define __forceinline __attribute__((always_inline)) inline
#endif // __GNUC__

#ifndef NOINLINE
#ifdef _MSC_VER
#define NOINLINE __declspec(noinline)
#else
#define NOINLINE __attribute__((noinline))
#endif
#endif

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
#define ZeroMemory(_dst, _size) memset((_dst), 0, (_size))
#endif

#endif // !DACCESS_COMPILE

//-------------------------------------------------------------------------------------------------
// Platform-specific defines

#if defined(HOST_AMD64)

#define LOG2_PTRSIZE 3
#define POINTER_SIZE 8

#elif defined(HOST_X86)

#define LOG2_PTRSIZE 2
#define POINTER_SIZE 4

#elif defined(HOST_ARM)

#define LOG2_PTRSIZE 2
#define POINTER_SIZE 4

#elif defined(HOST_ARM64)

#define LOG2_PTRSIZE 3
#define POINTER_SIZE 8

#elif defined (HOST_WASM)

#define LOG2_PTRSIZE 2
#define POINTER_SIZE 4

#else
#error Unsupported target architecture
#endif

#ifndef __GCENV_BASE_INCLUDED__

#define OS_PAGE_SIZE    PalOsPageSize()

#endif // __GCENV_BASE_INCLUDED__

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
#define FCIMPL3_ILL(_rettype, _method, a, b, c) \
    FCIMPL_RENAME_ARGSIZE(_rettype, _method, 20) \
    EXTERN_C _rettype F_CALL_CONV _method##_FCall (a, c, b) \
    {

#else

#define FCDECL_RENAME(_rettype, ...)
#define FCIMPL_RENAME(_rettype, ...)

#define FCALL_METHOD_ARGS(dummy, ...) (__VA_ARGS__)
#define FCALL_METHOD_ARGS_(tuple) FCALL_METHOD_ARGS tuple

#define FCIMPL1_F(_rettype, _method, a) \
    EXTERN_C _rettype F_CALL_CONV _method (a) \
    {
#define FCIMPL1_D(_rettype, _method, a) \
    EXTERN_C _rettype F_CALL_CONV _method (a) \
    {
#define FCIMPL1_L FCIMPL1_D
#define FCIMPL2_FF(_rettype, _method, a, b) \
    EXTERN_C _rettype F_CALL_CONV _method (a, b) \
    {
#define FCIMPL2_DD(_rettype, _method, a, b) \
    EXTERN_C _rettype F_CALL_CONV _method (a, b) \
    {
#define FCIMPL2_FI(_rettype, _method, a, b) \
    EXTERN_C _rettype F_CALL_CONV _method (a, b) \
    {
#define FCIMPL2_DI(_rettype, _method, a, b) \
    EXTERN_C _rettype F_CALL_CONV _method (a, b) \
    {
#define FCIMPL3_FFF(_rettype, _method, a, b, c) \
    EXTERN_C _rettype F_CALL_CONV _method (a, b, c) \
    {
#define FCIMPL3_DDD(_rettype, _method, a, b, c) \
    EXTERN_C _rettype F_CALL_CONV _method (a, b, c) \
    {
#define FCIMPL3_ILL(_rettype, _method, a, b, c) \
    EXTERN_C _rettype F_CALL_CONV _method (a, b, c) \
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

#define FCIMPLEND \
    }

typedef bool CLR_BOOL;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
// The return value is artificially widened on x86 and amd64
typedef int32_t FC_BOOL_RET;
#else
typedef bool FC_BOOL_RET;
#endif

#define FC_RETURN_BOOL(x)   do { return !!(x); } while(0)

#ifndef DACCESS_COMPILE
#define IN_DAC(x)
#define NOT_IN_DAC(x) x
#else
#define IN_DAC(x) x
#define NOT_IN_DAC(x)
#endif

#define INLINE inline

enum STARTUP_TIMELINE_EVENT_ID
{
    PROCESS_ATTACH_BEGIN = 0,
    NONGC_INIT_COMPLETE,
    GC_INIT_COMPLETE,
    PROCESS_ATTACH_COMPLETE,

    NUM_STARTUP_TIMELINE_EVENTS
};

#ifdef PROFILE_STARTUP
extern uint64_t g_startupTimelineEvents[NUM_STARTUP_TIMELINE_EVENTS];
#define STARTUP_TIMELINE_EVENT(eventid) g_startupTimelineEvents[eventid] = PalQueryPerformanceCounter();
#else // PROFILE_STARTUP
#define STARTUP_TIMELINE_EVENT(eventid)
#endif // PROFILE_STARTUP

#ifndef C_ASSERT
#define C_ASSERT(e) static_assert(e, #e)
#endif // C_ASSERT

#ifdef _MSC_VER
#define DECLSPEC_THREAD __declspec(thread)
#else // _MSC_VER
#define DECLSPEC_THREAD __thread
#endif // !_MSC_VER

#ifndef __GCENV_BASE_INCLUDED__
#if !defined(_INC_WINDOWS)
#ifdef _WIN32
// this must exactly match the typedef used by windows.h
typedef long HRESULT;
#else
typedef int32_t HRESULT;
#endif

#define S_OK  0x0
#define E_FAIL 0x80004005

#define UNREFERENCED_PARAMETER(P)          (void)(P)
#endif // !defined(_INC_WINDOWS)
#endif // __GCENV_BASE_INCLUDED__

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

#else

// GCC's way of declaring large integer constants
// If you define these in one step, without the _HELPER macros, you
// get extra whitespace when composing these with other concatenating macros.
#ifndef I64
#define I64_HELPER(x) x ## LL
#define I64(x)        I64_HELPER(x)
#endif

#endif

#endif // __COMMONMACROS_H__
