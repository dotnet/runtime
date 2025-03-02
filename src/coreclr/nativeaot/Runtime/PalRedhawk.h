// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Provides declarations for external resources consumed by Redhawk. This comprises functionality
// normally exported from Win32 libraries such as KERNEL32 and MSVCRT. When hosted on Win32 calls to these
// functions become simple pass throughs to the native implementation via export forwarding entries in a PAL
// (Platform Abstraction Layer) library. On other platforms the PAL library has actual code to emulate the
// functionality of these same APIs.
//
// In order to make it both obvious and intentional where Redhawk consumes an external API, such functions are
// decorated with an 'Pal' prefix. Ideally the associated supporting types, constants etc. would be
// similarly isolated from their concrete Win32 definitions, making the extent of platform dependence within
// the core explicit. For now that is too big a work item and we'll settle for manually restricting the use of
// external header files to within this header.
//

#include <sal.h>
#include <stdarg.h>
#ifdef TARGET_UNIX
#include <pthread.h>
#endif

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "gcenv.structs.h" // CRITICAL_SECTION
#include "PalRedhawkCommon.h"

#ifndef PAL_REDHAWK_INCLUDED
#define PAL_REDHAWK_INCLUDED

/* Adapted from intrin.h - For compatibility with <winnt.h>, some intrinsics are __cdecl except on x64 */
#if defined (_M_X64)
#define __PN__MACHINECALL_CDECL_OR_DEFAULT
#else
#define __PN__MACHINECALL_CDECL_OR_DEFAULT __cdecl
#endif

#ifndef _MSC_VER

// Note:  Win32-hosted GCC predefines __stdcall and __cdecl, but Unix-
// hosted GCC does not.

#ifdef __i386__

#if !defined(__cdecl)
#define __cdecl        __attribute__((cdecl))
#endif

#else   // !defined(__i386__)

#define __cdecl

#endif  // !defined(__i386__)

#endif // !_MSC_VER

#ifdef TARGET_UNIX
#define DIRECTORY_SEPARATOR_CHAR '/'
#else // TARGET_UNIX
#define DIRECTORY_SEPARATOR_CHAR '\\'
#endif // TARGET_UNIX

#ifndef _INC_WINDOWS

// There are some fairly primitive type definitions below but don't pull them into the rest of Redhawk unless
// we have to (in which case these definitions will move to CommonTypes.h).
typedef WCHAR *             LPWSTR;
typedef const WCHAR *       LPCWSTR;
typedef char *              LPSTR;
typedef const char *        LPCSTR;
typedef void *              HINSTANCE;

typedef void *              LPSECURITY_ATTRIBUTES;
typedef void *              LPOVERLAPPED;

#ifdef TARGET_UNIX
#define __stdcall
typedef char TCHAR;
#define _T(s) s
#else
typedef wchar_t TCHAR;
#define _T(s) L##s
#endif

typedef union _LARGE_INTEGER {
    struct {
#if BIGENDIAN
        int32_t HighPart;
        uint32_t LowPart;
#else
        uint32_t LowPart;
        int32_t HighPart;
#endif
    } u;
    int64_t QuadPart;
} LARGE_INTEGER, *PLARGE_INTEGER;

#define DECLARE_HANDLE(_name) typedef HANDLE _name

struct FILETIME
{
    uint32_t dwLowDateTime;
    uint32_t dwHighDateTime;
};

typedef struct _CONTEXT CONTEXT, *PCONTEXT;

#define EXCEPTION_MAXIMUM_PARAMETERS 15 // maximum number of exception parameters

typedef struct _EXCEPTION_RECORD32 {
    uint32_t      ExceptionCode;
    uint32_t      ExceptionFlags;
    uintptr_t  ExceptionRecord;
    uintptr_t  ExceptionAddress;
    uint32_t      NumberParameters;
    uintptr_t  ExceptionInformation[EXCEPTION_MAXIMUM_PARAMETERS];
} EXCEPTION_RECORD, *PEXCEPTION_RECORD;

#define EXCEPTION_CONTINUE_EXECUTION (-1)
#define EXCEPTION_CONTINUE_SEARCH (0)
#define EXCEPTION_EXECUTE_HANDLER (1)

typedef enum _EXCEPTION_DISPOSITION {
    ExceptionContinueExecution,
    ExceptionContinueSearch,
    ExceptionNestedException,
    ExceptionCollidedUnwind
} EXCEPTION_DISPOSITION;

#define STATUS_BREAKPOINT                              ((uint32_t   )0x80000003L)
#define STATUS_SINGLE_STEP                             ((uint32_t   )0x80000004L)
#define STATUS_ACCESS_VIOLATION                        ((uint32_t   )0xC0000005L)
#define STATUS_STACK_OVERFLOW                          ((uint32_t   )0xC00000FDL)

#endif // !_INC_WINDOWS

#define STATUS_REDHAWK_NULL_REFERENCE                  ((uint32_t   )0x00000000L)
#define STATUS_REDHAWK_UNMANAGED_HELPER_NULL_REFERENCE ((uint32_t   )0x00000042L)

#ifdef TARGET_UNIX
#define NULL_AREA_SIZE                   (4*1024)
#else
#define NULL_AREA_SIZE                   (64*1024)
#endif


#ifndef DACCESS_COMPILE
#ifndef _INC_WINDOWS

#ifndef TRUE
#define TRUE                    1
#endif
#ifndef FALSE
#define FALSE                   0
#endif

#define INVALID_HANDLE_VALUE    ((HANDLE)(intptr_t)-1)

#define INFINITE                0xFFFFFFFF

#define DUPLICATE_CLOSE_SOURCE  0x00000001
#define DUPLICATE_SAME_ACCESS   0x00000002

#define PAGE_NOACCESS           0x01
#define PAGE_READONLY           0x02
#define PAGE_READWRITE          0x04
#define PAGE_WRITECOPY          0x08
#define PAGE_EXECUTE            0x10
#define PAGE_EXECUTE_READ       0x20
#define PAGE_EXECUTE_READWRITE  0x40
#define PAGE_EXECUTE_WRITECOPY  0x80
#define PAGE_GUARD              0x100
#define PAGE_NOCACHE            0x200
#define PAGE_WRITECOMBINE       0x400

#define WAIT_OBJECT_0           0
#define WAIT_TIMEOUT            258
#define WAIT_FAILED             0xFFFFFFFF

#endif // !_INC_WINDOWS
#endif // !DACCESS_COMPILE

extern uint32_t g_RhNumberOfProcessors;

#ifdef TARGET_UNIX
#define REDHAWK_PALIMPORT extern "C"
#define REDHAWK_PALEXPORT extern "C"
#define REDHAWK_PALAPI
#else
#define REDHAWK_PALIMPORT EXTERN_C
#define REDHAWK_PALAPI __stdcall
#endif // TARGET_UNIX

#ifndef DACCESS_COMPILE

#ifdef _DEBUG
#define CaptureStackBackTrace RtlCaptureStackBackTrace
#endif

#ifndef _INC_WINDOWS
// Include the list of external functions we wish to access. If we do our job 100% then it will be
// possible to link without any direct reference to any Win32 library.
#include "PalRedhawkFunctions.h"
#endif // !_INC_WINDOWS
#endif // !DACCESS_COMPILE

// The Redhawk PAL must be initialized before any of its exports can be called. Returns true for a successful
// initialization and false on failure.
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalInit();

// Given the OS handle of a loaded module, compute the upper and lower virtual address bounds (inclusive).
REDHAWK_PALIMPORT void REDHAWK_PALAPI PalGetModuleBounds(HANDLE hOsHandle, _Out_ uint8_t ** ppLowerBound, _Out_ uint8_t ** ppUpperBound);

struct NATIVE_CONTEXT;

#if _WIN32
REDHAWK_PALIMPORT NATIVE_CONTEXT* PalAllocateCompleteOSContext(_Out_ uint8_t** contextBuffer);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalGetCompleteThreadContext(HANDLE hThread, _Out_ NATIVE_CONTEXT * pCtx);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalSetThreadContext(HANDLE hThread, _Out_ NATIVE_CONTEXT * pCtx);
REDHAWK_PALIMPORT void REDHAWK_PALAPI PalRestoreContext(NATIVE_CONTEXT * pCtx);

// For platforms that have segment registers in the CONTEXT_CONTROL set that
// are not saved in PAL_LIMITED_CONTEXT, this captures them from the current
// thread and saves them in `pContext`.
REDHAWK_PALIMPORT void REDHAWK_PALAPI PopulateControlSegmentRegisters(CONTEXT * pContext);
#endif

REDHAWK_PALIMPORT int32_t REDHAWK_PALAPI PalGetProcessCpuCount();

// Retrieves the entire range of memory dedicated to the calling thread's stack.  This does
// not get the current dynamic bounds of the stack, which can be significantly smaller than
// the maximum bounds.
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalGetMaximumStackBounds(_Out_ void** ppStackLowOut, _Out_ void** ppStackHighOut);

// Return value:  number of characters in name string
REDHAWK_PALIMPORT int32_t PalGetModuleFileName(_Out_ const TCHAR** pModuleNameOut, HANDLE moduleBase);

#if _WIN32

// Various intrinsic declarations needed for the PalGetCurrentTEB implementation below.
#if defined(HOST_X86)
EXTERN_C unsigned long __readfsdword(unsigned long Offset);
#pragma intrinsic(__readfsdword)
#elif defined(HOST_AMD64)
EXTERN_C unsigned __int64  __readgsqword(unsigned long Offset);
#pragma intrinsic(__readgsqword)
#elif defined(HOST_ARM64)
EXTERN_C unsigned __int64 __getReg(int);
#pragma intrinsic(__getReg)
#else
#error Unsupported architecture
#endif

// Retrieves the OS TEB for the current thread.
inline uint8_t * PalNtCurrentTeb()
{
#if defined(HOST_X86)
    return (uint8_t*)__readfsdword(0x18);
#elif defined(HOST_AMD64)
    return (uint8_t*)__readgsqword(0x30);
#elif defined(HOST_ARM64)
    return (uint8_t*)__getReg(18);
#else
#error Unsupported architecture
#endif
}

// Offsets of ThreadLocalStoragePointer in the TEB.
#if defined(HOST_64BIT)
#define OFFSETOF__TEB__ThreadLocalStoragePointer 0x58
#else
#define OFFSETOF__TEB__ThreadLocalStoragePointer 0x2c
#endif

#endif // _WIN32

REDHAWK_PALIMPORT _Ret_maybenull_ _Post_writable_byte_size_(size) void* REDHAWK_PALAPI PalVirtualAlloc(uintptr_t size, uint32_t protect);
REDHAWK_PALIMPORT void REDHAWK_PALAPI PalVirtualFree(_In_ void* pAddress, uintptr_t size);
REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalVirtualProtect(_In_ void* pAddress, uintptr_t size, uint32_t protect);
REDHAWK_PALIMPORT void PalFlushInstructionCache(_In_ void* pAddress, size_t size);
REDHAWK_PALIMPORT void REDHAWK_PALAPI PalSleep(uint32_t milliseconds);
REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalSwitchToThread();
REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalAreShadowStacksEnabled();
REDHAWK_PALIMPORT HANDLE REDHAWK_PALAPI PalCreateEventW(_In_opt_ LPSECURITY_ATTRIBUTES pEventAttributes, UInt32_BOOL manualReset, UInt32_BOOL initialState, _In_opt_z_ LPCWSTR pName);
REDHAWK_PALIMPORT uint64_t REDHAWK_PALAPI PalGetTickCount64();
REDHAWK_PALIMPORT HANDLE REDHAWK_PALAPI PalGetModuleHandleFromPointer(_In_ void* pointer);

#ifdef TARGET_UNIX
REDHAWK_PALIMPORT uint32_t REDHAWK_PALAPI PalGetOsPageSize();
REDHAWK_PALIMPORT void REDHAWK_PALAPI PalSetHardwareExceptionHandler(PHARDWARE_EXCEPTION_HANDLER handler);
#endif

typedef uint32_t (__stdcall *BackgroundCallback)(_In_opt_ void* pCallbackContext);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalSetCurrentThreadName(const char* name);
#ifdef TARGET_WINDOWS
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalSetCurrentThreadNameW(const WCHAR* name);
#endif
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalStartBackgroundGCThread(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalStartFinalizerThread(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalStartEventPipeHelperThread(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext);

#ifdef FEATURE_HIJACK
class Thread;
REDHAWK_PALIMPORT void REDHAWK_PALAPI PalHijack(Thread* pThreadToHijack);
REDHAWK_PALIMPORT HijackFunc* REDHAWK_PALAPI PalGetHijackTarget(_In_ HijackFunc* defaultHijackTarget);
#endif

REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalAllocateThunksFromTemplate(_In_ HANDLE hTemplateModule, uint32_t templateRva, size_t templateSize, _Outptr_result_bytebuffer_(templateSize) void** newThunksOut);
REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalFreeThunksFromTemplate(_In_ void *pBaseAddress, size_t templateSize);

REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalMarkThunksAsValidCallTargets(
    void *virtualAddress,
    int thunkSize,
    int thunksPerBlock,
    int thunkBlockSize,
    int thunkBlocksPerMapping);

REDHAWK_PALIMPORT uint32_t REDHAWK_PALAPI PalCompatibleWaitAny(UInt32_BOOL alertable, uint32_t timeout, uint32_t count, HANDLE* pHandles, UInt32_BOOL allowReentrantWait);

REDHAWK_PALIMPORT HANDLE PalCreateLowMemoryResourceNotification();

REDHAWK_PALIMPORT void REDHAWK_PALAPI PalAttachThread(void* thread);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalDetachThread(void* thread);

REDHAWK_PALIMPORT uint64_t PalGetCurrentOSThreadId();

REDHAWK_PALIMPORT uint64_t PalQueryPerformanceCounter();
REDHAWK_PALIMPORT uint64_t PalQueryPerformanceFrequency();

REDHAWK_PALIMPORT void PalPrintFatalError(const char* message);

REDHAWK_PALIMPORT char* PalCopyTCharAsChar(const TCHAR* toCopy);

REDHAWK_PALIMPORT HANDLE PalLoadLibrary(const char* moduleName);

REDHAWK_PALIMPORT void* PalGetProcAddress(HANDLE module, const char* functionName);

#ifdef TARGET_UNIX
REDHAWK_PALIMPORT int32_t __cdecl _stricmp(const char *string1, const char *string2);
#endif // TARGET_UNIX

#include "PalRedhawkInline.h"

#endif // !PAL_REDHAWK_INCLUDED
