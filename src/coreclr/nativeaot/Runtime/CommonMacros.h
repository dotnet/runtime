// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __COMMONMACROS_H__
#define __COMMONMACROS_H__

#include "rhassert.h"

#define EXTERN_C extern "C"

#if defined(HOST_X86) && !defined(HOST_UNIX)
#define FASTCALL __fastcall
#define STDCALL __stdcall
#else
#define FASTCALL
#define STDCALL
#endif

#define REDHAWK_API
#define REDHAWK_CALLCONV FASTCALL

#ifdef _MSC_VER

#define MSVC_SAVE_WARNING_STATE() __pragma(warning(push))
#define MSVC_DISABLE_WARNING(warn_num) __pragma(warning(disable: warn_num))
#define MSVC_RESTORE_WARNING_STATE() __pragma(warning(pop))

#else

#define MSVC_SAVE_WARNING_STATE()
#define MSVC_DISABLE_WARNING(warn_num)
#define MSVC_RESTORE_WARNING_STATE()

#endif // _MSC_VER

#ifndef COUNTOF
template <typename _CountofType, size_t _SizeOfArray>
char (*COUNTOF_helper(_CountofType (&_Array)[_SizeOfArray]))[_SizeOfArray];
#define COUNTOF(_Array) sizeof(*COUNTOF_helper(_Array))
#endif // COUNTOF

#ifndef offsetof
#define offsetof(s,m)   (uintptr_t)( (intptr_t)&reinterpret_cast<const volatile char&>((((s *)0)->m)) )
#endif // offsetof

#ifndef FORCEINLINE
#define FORCEINLINE __forceinline
#endif

#ifndef NOINLINE
#ifdef _MSC_VER
#define NOINLINE __declspec(noinline)
#else
#define NOINLINE __attribute__((noinline))
#endif
#endif

#ifndef __GCENV_BASE_INCLUDED__

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

//-------------------------------------------------------------------------------------------------
// min/max

#ifndef min
#define min(_a, _b) ((_a) < (_b) ? (_a) : (_b))
#endif
#ifndef max
#define max(_a, _b) ((_a) < (_b) ? (_b) : (_a))
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
#if defined(HOST_AMD64)

#define DATA_ALIGNMENT  8
#define OS_PAGE_SIZE    0x1000

#elif defined(HOST_X86)

#define DATA_ALIGNMENT  4
#ifndef OS_PAGE_SIZE
#define OS_PAGE_SIZE    0x1000
#endif

#elif defined(HOST_ARM)

#define DATA_ALIGNMENT  4
#ifndef OS_PAGE_SIZE
#define OS_PAGE_SIZE    0x1000
#endif

#elif defined(HOST_ARM64)

#define DATA_ALIGNMENT  8
#ifndef OS_PAGE_SIZE
#define OS_PAGE_SIZE    0x1000
#endif

#elif defined(HOST_WASM)

#define DATA_ALIGNMENT  4
#ifndef OS_PAGE_SIZE
#define OS_PAGE_SIZE    0x4
#endif

#else
#error Unsupported target architecture
#endif
#endif // __GCENV_BASE_INCLUDED__

#if defined(TARGET_ARM)
#define THUMB_CODE 1
#endif

//
// Define an unmanaged function called from managed code that needs to execute in co-operative GC mode. (There
// should be very few of these, most such functions will be simply p/invoked).
//
#define COOP_PINVOKE_HELPER(_rettype, _method, _args) EXTERN_C REDHAWK_API _rettype REDHAWK_CALLCONV _method _args
#ifdef HOST_X86
// We have helpers that act like memcpy and memset from the CRT, so they need to be __cdecl.
#define COOP_PINVOKE_CDECL_HELPER(_rettype, _method, _args) EXTERN_C REDHAWK_API _rettype __cdecl _method _args
#else
#define COOP_PINVOKE_CDECL_HELPER COOP_PINVOKE_HELPER
#endif

typedef bool CLR_BOOL;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
// The return value is artifically widened on x86 and amd64
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
extern unsigned __int64 g_startupTimelineEvents[NUM_STARTUP_TIMELINE_EVENTS];
#define STARTUP_TIMELINE_EVENT(eventid) PalQueryPerformanceCounter((LARGE_INTEGER*)&g_startupTimelineEvents[eventid]);
#else // PROFILE_STARTUP
#define STARTUP_TIMELINE_EVENT(eventid)
#endif // PROFILE_STARTUP

#ifndef C_ASSERT
#define C_ASSERT(e) static_assert(e, #e)
#endif // C_ASSERT

#ifdef __llvm__
#define DECLSPEC_THREAD __thread
#else // __llvm__
#define DECLSPEC_THREAD __declspec(thread)
#endif // !__llvm__

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

#endif // __COMMONMACROS_H__
