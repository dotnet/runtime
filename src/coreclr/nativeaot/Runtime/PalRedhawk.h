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
#include "gcenv.structs.h"
#include "IntrinsicConstants.h"

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

#ifndef _INC_WINDOWS
//#ifndef DACCESS_COMPILE

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
#endif

#ifndef __GCENV_BASE_INCLUDED__
#define CALLBACK            __stdcall
#define WINAPI              __stdcall
#define WINBASEAPI          __declspec(dllimport)
#endif //!__GCENV_BASE_INCLUDED__

#ifdef TARGET_UNIX
#define DIRECTORY_SEPARATOR_CHAR '/'
#else // TARGET_UNIX
#define DIRECTORY_SEPARATOR_CHAR '\\'
#endif // TARGET_UNIX

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

typedef struct _GUID {
    uint32_t Data1;
    uint16_t Data2;
    uint16_t Data3;
    uint8_t Data4[8];
} GUID;

#define DECLARE_HANDLE(_name) typedef HANDLE _name

// defined in gcrhenv.cpp
bool __SwitchToThread(uint32_t dwSleepMSec, uint32_t dwSwitchCount);

struct FILETIME
{
    uint32_t dwLowDateTime;
    uint32_t dwHighDateTime;
};

#ifdef HOST_AMD64

#define CONTEXT_AMD64   0x00100000L

#define CONTEXT_CONTROL         (CONTEXT_AMD64 | 0x00000001L)
#define CONTEXT_INTEGER         (CONTEXT_AMD64 | 0x00000002L)

typedef struct DECLSPEC_ALIGN(16) _XSAVE_FORMAT {
    uint16_t  ControlWord;
    uint16_t  StatusWord;
    uint8_t   TagWord;
    uint8_t   Reserved1;
    uint16_t  ErrorOpcode;
    uint32_t  ErrorOffset;
    uint16_t  ErrorSelector;
    uint16_t  Reserved2;
    uint32_t  DataOffset;
    uint16_t  DataSelector;
    uint16_t  Reserved3;
    uint32_t  MxCsr;
    uint32_t  MxCsr_Mask;
    Fp128   FloatRegisters[8];
#if defined(HOST_64BIT)
    Fp128   XmmRegisters[16];
    uint8_t   Reserved4[96];
#else
    Fp128   XmmRegisters[8];
    uint8_t   Reserved4[220];
    uint32_t  Cr0NpxState;
#endif
} XSAVE_FORMAT, *PXSAVE_FORMAT;


typedef XSAVE_FORMAT XMM_SAVE_AREA32, *PXMM_SAVE_AREA32;

typedef struct DECLSPEC_ALIGN(16) _CONTEXT {
    uint64_t P1Home;
    uint64_t P2Home;
    uint64_t P3Home;
    uint64_t P4Home;
    uint64_t P5Home;
    uint64_t P6Home;
    uint32_t ContextFlags;
    uint32_t MxCsr;
    uint16_t SegCs;
    uint16_t SegDs;
    uint16_t SegEs;
    uint16_t SegFs;
    uint16_t SegGs;
    uint16_t SegSs;
    uint32_t EFlags;
    uint64_t Dr0;
    uint64_t Dr1;
    uint64_t Dr2;
    uint64_t Dr3;
    uint64_t Dr6;
    uint64_t Dr7;
    uint64_t Rax;
    uint64_t Rcx;
    uint64_t Rdx;
    uint64_t Rbx;
    uint64_t Rsp;
    uint64_t Rbp;
    uint64_t Rsi;
    uint64_t Rdi;
    uint64_t R8;
    uint64_t R9;
    uint64_t R10;
    uint64_t R11;
    uint64_t R12;
    uint64_t R13;
    uint64_t R14;
    uint64_t R15;
    uint64_t Rip;
    union {
        XMM_SAVE_AREA32 FltSave;
        struct {
            Fp128 Header[2];
            Fp128 Legacy[8];
            Fp128 Xmm0;
            Fp128 Xmm1;
            Fp128 Xmm2;
            Fp128 Xmm3;
            Fp128 Xmm4;
            Fp128 Xmm5;
            Fp128 Xmm6;
            Fp128 Xmm7;
            Fp128 Xmm8;
            Fp128 Xmm9;
            Fp128 Xmm10;
            Fp128 Xmm11;
            Fp128 Xmm12;
            Fp128 Xmm13;
            Fp128 Xmm14;
            Fp128 Xmm15;
        } DUMMYSTRUCTNAME;
    } DUMMYUNIONNAME;
    Fp128 VectorRegister[26];
    uint64_t VectorControl;
    uint64_t DebugControl;
    uint64_t LastBranchToRip;
    uint64_t LastBranchFromRip;
    uint64_t LastExceptionToRip;
    uint64_t LastExceptionFromRip;

    void SetIp(uintptr_t ip) { Rip = ip; }
    void SetSp(uintptr_t sp) { Rsp = sp; }
#ifdef UNIX_AMD64_ABI
    void SetArg0Reg(uintptr_t val) { Rdi = val; }
    void SetArg1Reg(uintptr_t val) { Rsi = val; }
#else // UNIX_AMD64_ABI
    void SetArg0Reg(uintptr_t val) { Rcx = val; }
    void SetArg1Reg(uintptr_t val) { Rdx = val; }
#endif // UNIX_AMD64_ABI
    uintptr_t GetIp() { return Rip; }
    uintptr_t GetSp() { return Rsp; }

    template <typename F>
    void ForEachPossibleObjectRef(F lambda)
    {
        for (uint64_t* pReg = &Rax; pReg < &Rip; pReg++)
            lambda((size_t*)pReg);
    }

} CONTEXT, *PCONTEXT;
#elif defined(HOST_ARM)

#define CONTEXT_ARM   0x00200000L

#define CONTEXT_CONTROL (CONTEXT_ARM | 0x1L)
#define CONTEXT_INTEGER (CONTEXT_ARM | 0x2L)

#define ARM_MAX_BREAKPOINTS     8
#define ARM_MAX_WATCHPOINTS     1

typedef struct DECLSPEC_ALIGN(8) _CONTEXT {
    uint32_t ContextFlags;
    uint32_t R0;
    uint32_t R1;
    uint32_t R2;
    uint32_t R3;
    uint32_t R4;
    uint32_t R5;
    uint32_t R6;
    uint32_t R7;
    uint32_t R8;
    uint32_t R9;
    uint32_t R10;
    uint32_t R11;
    uint32_t R12;
    uint32_t Sp; // R13
    uint32_t Lr; // R14
    uint32_t Pc; // R15
    uint32_t Cpsr;
    uint32_t Fpscr;
    uint32_t Padding;
    union {
        Fp128  Q[16];
        uint64_t D[32];
        uint32_t S[32];
    } DUMMYUNIONNAME;
    uint32_t Bvr[ARM_MAX_BREAKPOINTS];
    uint32_t Bcr[ARM_MAX_BREAKPOINTS];
    uint32_t Wvr[ARM_MAX_WATCHPOINTS];
    uint32_t Wcr[ARM_MAX_WATCHPOINTS];
    uint32_t Padding2[2];

    void SetIp(uintptr_t ip) { Pc = ip; }
    void SetArg0Reg(uintptr_t val) { R0 = val; }
    void SetArg1Reg(uintptr_t val) { R1 = val; }
    uintptr_t GetIp() { return Pc; }
    uintptr_t GetLr() { return Lr; }
} CONTEXT, *PCONTEXT;

#elif defined(HOST_X86)

#define CONTEXT_i386    0x00010000L

#define CONTEXT_CONTROL         (CONTEXT_i386 | 0x00000001L) // SS:SP, CS:IP, FLAGS, BP
#define CONTEXT_INTEGER         (CONTEXT_i386 | 0x00000002L) // AX, BX, CX, DX, SI, DI

#define SIZE_OF_80387_REGISTERS      80
#define MAXIMUM_SUPPORTED_EXTENSION  512

typedef struct _FLOATING_SAVE_AREA {
    uint32_t ControlWord;
    uint32_t StatusWord;
    uint32_t TagWord;
    uint32_t ErrorOffset;
    uint32_t ErrorSelector;
    uint32_t DataOffset;
    uint32_t DataSelector;
    uint8_t  RegisterArea[SIZE_OF_80387_REGISTERS];
    uint32_t Cr0NpxState;
} FLOATING_SAVE_AREA;

#include "pshpack4.h"
typedef struct _CONTEXT {
    uint32_t ContextFlags;
    uint32_t Dr0;
    uint32_t Dr1;
    uint32_t Dr2;
    uint32_t Dr3;
    uint32_t Dr6;
    uint32_t Dr7;
    FLOATING_SAVE_AREA FloatSave;
    uint32_t SegGs;
    uint32_t SegFs;
    uint32_t SegEs;
    uint32_t SegDs;
    uint32_t Edi;
    uint32_t Esi;
    uint32_t Ebx;
    uint32_t Edx;
    uint32_t Ecx;
    uint32_t Eax;
    uint32_t Ebp;
    uint32_t Eip;
    uint32_t SegCs;
    uint32_t EFlags;
    uint32_t Esp;
    uint32_t SegSs;
    uint8_t  ExtendedRegisters[MAXIMUM_SUPPORTED_EXTENSION];

    void SetIp(uintptr_t ip) { Eip = ip; }
    void SetSp(uintptr_t sp) { Esp = sp; }
    void SetArg0Reg(uintptr_t val) { Ecx = val; }
    void SetArg1Reg(uintptr_t val) { Edx = val; }
    uintptr_t GetIp() { return Eip; }
    uintptr_t GetSp() { return Esp; }
} CONTEXT, *PCONTEXT;
#include "poppack.h"

#elif defined(HOST_ARM64)

#define CONTEXT_ARM64   0x00400000L

#define CONTEXT_CONTROL (CONTEXT_ARM64 | 0x1L)
#define CONTEXT_INTEGER (CONTEXT_ARM64 | 0x2L)

// Specify the number of breakpoints and watchpoints that the OS
// will track. Architecturally, ARM64 supports up to 16. In practice,
// however, almost no one implements more than 4 of each.

#define ARM64_MAX_BREAKPOINTS     8
#define ARM64_MAX_WATCHPOINTS     2

typedef struct _NEON128 {
    uint64_t Low;
    int64_t High;
} NEON128, *PNEON128;

typedef struct DECLSPEC_ALIGN(16) _CONTEXT {
    //
    // Control flags.
    //
    uint32_t ContextFlags;

    //
    // Integer registers
    //
    uint32_t Cpsr;       // NZVF + DAIF + CurrentEL + SPSel
    union {
        struct {
            uint64_t X0;
            uint64_t X1;
            uint64_t X2;
            uint64_t X3;
            uint64_t X4;
            uint64_t X5;
            uint64_t X6;
            uint64_t X7;
            uint64_t X8;
            uint64_t X9;
            uint64_t X10;
            uint64_t X11;
            uint64_t X12;
            uint64_t X13;
            uint64_t X14;
            uint64_t X15;
            uint64_t X16;
            uint64_t X17;
            uint64_t X18;
            uint64_t X19;
            uint64_t X20;
            uint64_t X21;
            uint64_t X22;
            uint64_t X23;
            uint64_t X24;
            uint64_t X25;
            uint64_t X26;
            uint64_t X27;
            uint64_t X28;
#pragma warning(push)
#pragma warning(disable:4201) // nameless struct
        };
        uint64_t X[29];
    };
#pragma warning(pop)
    uint64_t Fp; // X29
    uint64_t Lr; // X30
    uint64_t Sp;
    uint64_t Pc;

    //
    // Floating Point/NEON Registers
    //
    NEON128 V[32];
    uint32_t Fpcr;
    uint32_t Fpsr;

    //
    // Debug registers
    //
    uint32_t Bcr[ARM64_MAX_BREAKPOINTS];
    uint64_t Bvr[ARM64_MAX_BREAKPOINTS];
    uint32_t Wcr[ARM64_MAX_WATCHPOINTS];
    uint64_t Wvr[ARM64_MAX_WATCHPOINTS];

    void SetIp(uintptr_t ip) { Pc = ip; }
    void SetArg0Reg(uintptr_t val) { X0 = val; }
    void SetArg1Reg(uintptr_t val) { X1 = val; }
    uintptr_t GetIp() { return Pc; }
    uintptr_t GetLr() { return Lr; }
    uintptr_t GetSp() { return Sp; }

    template <typename F>
    void ForEachPossibleObjectRef(F lambda)
    {
        for (uint64_t* pReg = &X0; pReg <= &X28; pReg++)
            lambda((size_t*)pReg);

        // Lr can be used as a scratch register
        lambda((size_t*)&Lr);
    }
} CONTEXT, *PCONTEXT;

#elif defined(HOST_WASM)

typedef struct DECLSPEC_ALIGN(8) _CONTEXT {
    // TODO: Figure out if WebAssembly has a meaningful context available
    void SetIp(uintptr_t ip) {  }
    void SetArg0Reg(uintptr_t val) {  }
    void SetArg1Reg(uintptr_t val) {  }
    uintptr_t GetIp() { return 0; }
} CONTEXT, *PCONTEXT;
#endif

#define EXCEPTION_MAXIMUM_PARAMETERS 15 // maximum number of exception parameters

typedef struct _EXCEPTION_RECORD32 {
    uint32_t      ExceptionCode;
    uint32_t      ExceptionFlags;
    uintptr_t  ExceptionRecord;
    uintptr_t  ExceptionAddress;
    uint32_t      NumberParameters;
    uintptr_t  ExceptionInformation[EXCEPTION_MAXIMUM_PARAMETERS];
} EXCEPTION_RECORD, *PEXCEPTION_RECORD;

typedef struct _EXCEPTION_POINTERS {
    PEXCEPTION_RECORD   ExceptionRecord;
    PCONTEXT            ContextRecord;
} EXCEPTION_POINTERS, *PEXCEPTION_POINTERS;

typedef int32_t (__stdcall *PVECTORED_EXCEPTION_HANDLER)(
    PEXCEPTION_POINTERS ExceptionInfo
    );

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
#define STATUS_REDHAWK_NULL_REFERENCE                  ((uint32_t   )0x00000000L)
#define STATUS_REDHAWK_UNMANAGED_HELPER_NULL_REFERENCE ((uint32_t   )0x00000042L)

#ifdef TARGET_UNIX
#define NULL_AREA_SIZE                   (4*1024)
#else
#define NULL_AREA_SIZE                   (64*1024)
#endif

//#endif // !DACCESS_COMPILE
#endif // !_INC_WINDOWS



#ifndef DACCESS_COMPILE
#ifndef _INC_WINDOWS

#ifndef __GCENV_BASE_INCLUDED__
#define TRUE                    1
#define FALSE                   0
#endif // !__GCENV_BASE_INCLUDED__

#define INVALID_HANDLE_VALUE    ((HANDLE)(intptr_t)-1)

#define DLL_PROCESS_ATTACH      1
#define DLL_THREAD_ATTACH       2
#define DLL_THREAD_DETACH       3
#define DLL_PROCESS_DETACH      0

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
#define MEM_COMMIT              0x1000
#define MEM_RESERVE             0x2000
#define MEM_DECOMMIT            0x4000
#define MEM_RELEASE             0x8000
#define MEM_FREE                0x10000
#define MEM_PRIVATE             0x20000
#define MEM_MAPPED              0x40000
#define MEM_RESET               0x80000
#define MEM_TOP_DOWN            0x100000
#define MEM_WRITE_WATCH         0x200000
#define MEM_PHYSICAL            0x400000
#define MEM_LARGE_PAGES         0x20000000
#define MEM_4MB_PAGES           0x80000000

#define WAIT_OBJECT_0           0
#define WAIT_TIMEOUT            258
#define WAIT_FAILED             0xFFFFFFFF

#endif // !_INC_WINDOWS
#endif // !DACCESS_COMPILE

typedef uint64_t REGHANDLE;
typedef uint64_t TRACEHANDLE;

#ifndef _EVNTPROV_H_
struct EVENT_DATA_DESCRIPTOR
{
    uint64_t  Ptr;
    uint32_t  Size;
    uint32_t  Reserved;
};

struct EVENT_DESCRIPTOR
{
    uint16_t  Id;
    uint8_t   Version;
    uint8_t   Channel;
    uint8_t   Level;
    uint8_t   Opcode;
    uint16_t  Task;
    uint64_t  Keyword;

};

struct EVENT_FILTER_DESCRIPTOR
{
    uint64_t  Ptr;
    uint32_t  Size;
    uint32_t  Type;
};

__forceinline
void
EventDataDescCreate(_Out_ EVENT_DATA_DESCRIPTOR * EventDataDescriptor, _In_opt_ const void * DataPtr, uint32_t DataSize)
{
    EventDataDescriptor->Ptr = (uint64_t)DataPtr;
    EventDataDescriptor->Size = DataSize;
    EventDataDescriptor->Reserved = 0;
}
#endif // _EVNTPROV_H_

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

REDHAWK_PALIMPORT CONTEXT* PalAllocateCompleteOSContext(_Out_ uint8_t** contextBuffer);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalGetCompleteThreadContext(HANDLE hThread, _Out_ CONTEXT * pCtx);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalSetThreadContext(HANDLE hThread, _Out_ CONTEXT * pCtx);
REDHAWK_PALIMPORT void REDHAWK_PALAPI PalRestoreContext(CONTEXT * pCtx);

// For platforms that have segment registers in the CONTEXT_CONTROL set that
// are not saved in PAL_LIMITED_CONTEXT, this captures them from the current
// thread and saves them in `pContext`.
REDHAWK_PALIMPORT void REDHAWK_PALAPI PopulateControlSegmentRegisters(CONTEXT* pContext);

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
#elif defined(HOST_ARM)
EXTERN_C unsigned int _MoveFromCoprocessor(unsigned int, unsigned int, unsigned int, unsigned int, unsigned int);
#pragma intrinsic(_MoveFromCoprocessor)
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
#elif defined(HOST_ARM)
    return (uint8_t*)_MoveFromCoprocessor(15, 0, 13,  0, 2);
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

#else // _WIN32

inline uint8_t * PalNtCurrentTeb()
{
    // UNIXTODO: Implement PalNtCurrentTeb
    return NULL;
}

#define OFFSETOF__TEB__ThreadLocalStoragePointer 0

#endif // _WIN32

//
// Compiler intrinsic definitions. In the interest of performance the PAL doesn't provide exports of these
// (that would defeat the purpose of having an intrinsic in the first place). Instead we place the necessary
// compiler linkage directly inline in this header. As a result this section may have platform specific
// conditional compilation (upto and including defining an export of functionality that isn't a supported
// intrinsic on that platform).
//

EXTERN_C void * __cdecl _alloca(size_t);
#pragma intrinsic(_alloca)

REDHAWK_PALIMPORT _Ret_maybenull_ _Post_writable_byte_size_(size) void* REDHAWK_PALAPI PalVirtualAlloc(_In_opt_ void* pAddress, uintptr_t size, uint32_t allocationType, uint32_t protect);
REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalVirtualFree(_In_ void* pAddress, uintptr_t size, uint32_t freeType);
REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalVirtualProtect(_In_ void* pAddress, uintptr_t size, uint32_t protect);
REDHAWK_PALIMPORT void PalFlushInstructionCache(_In_ void* pAddress, size_t size);
REDHAWK_PALIMPORT void REDHAWK_PALAPI PalSleep(uint32_t milliseconds);
REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalSwitchToThread();
REDHAWK_PALIMPORT HANDLE REDHAWK_PALAPI PalCreateEventW(_In_opt_ LPSECURITY_ATTRIBUTES pEventAttributes, UInt32_BOOL manualReset, UInt32_BOOL initialState, _In_opt_z_ LPCWSTR pName);
REDHAWK_PALIMPORT uint64_t REDHAWK_PALAPI PalGetTickCount64();
REDHAWK_PALIMPORT void REDHAWK_PALAPI PalTerminateCurrentProcess(uint32_t exitCode);
REDHAWK_PALIMPORT HANDLE REDHAWK_PALAPI PalGetModuleHandleFromPointer(_In_ void* pointer);

#ifdef TARGET_UNIX
struct UNIX_CONTEXT;
#define NATIVE_CONTEXT UNIX_CONTEXT
#else
#define NATIVE_CONTEXT CONTEXT
#endif

#ifdef TARGET_UNIX
REDHAWK_PALIMPORT void REDHAWK_PALAPI PalSetHardwareExceptionHandler(PHARDWARE_EXCEPTION_HANDLER handler);
#else
REDHAWK_PALIMPORT void* REDHAWK_PALAPI PalAddVectoredExceptionHandler(uint32_t firstHandler, _In_ PVECTORED_EXCEPTION_HANDLER vectoredHandler);
#endif

typedef uint32_t (__stdcall *BackgroundCallback)(_In_opt_ void* pCallbackContext);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalStartBackgroundGCThread(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalStartFinalizerThread(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalStartEventPipeHelperThread(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext);

typedef void (*PalHijackCallback)(_In_ NATIVE_CONTEXT* pThreadContext, _In_opt_ void* pThreadToHijack);
REDHAWK_PALIMPORT void REDHAWK_PALAPI PalHijack(HANDLE hThread, _In_opt_ void* pThreadToHijack);
REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalRegisterHijackCallback(_In_ PalHijackCallback callback);

#ifdef FEATURE_ETW
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalEventEnabled(REGHANDLE regHandle, _In_ const EVENT_DESCRIPTOR* eventDescriptor);
#endif

REDHAWK_PALIMPORT _Ret_maybenull_ void* REDHAWK_PALAPI PalSetWerDataBuffer(_In_ void* pNewBuffer);

REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalAllocateThunksFromTemplate(_In_ HANDLE hTemplateModule, uint32_t templateRva, size_t templateSize, _Outptr_result_bytebuffer_(templateSize) void** newThunksOut);
REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalFreeThunksFromTemplate(_In_ void *pBaseAddress);

REDHAWK_PALIMPORT UInt32_BOOL REDHAWK_PALAPI PalMarkThunksAsValidCallTargets(
    void *virtualAddress,
    int thunkSize,
    int thunksPerBlock,
    int thunkBlockSize,
    int thunkBlocksPerMapping);

REDHAWK_PALIMPORT uint32_t REDHAWK_PALAPI PalCompatibleWaitAny(UInt32_BOOL alertable, uint32_t timeout, uint32_t count, HANDLE* pHandles, UInt32_BOOL allowReentrantWait);

REDHAWK_PALIMPORT void REDHAWK_PALAPI PalAttachThread(void* thread);
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalDetachThread(void* thread);

REDHAWK_PALIMPORT uint64_t PalGetCurrentThreadIdForLogging();

REDHAWK_PALIMPORT uint64_t PalQueryPerformanceCounter();
REDHAWK_PALIMPORT uint64_t PalQueryPerformanceFrequency();

REDHAWK_PALIMPORT void PalPrintFatalError(const char* message);

#ifdef TARGET_UNIX
REDHAWK_PALIMPORT int32_t __cdecl _stricmp(const char *string1, const char *string2);
#endif // TARGET_UNIX

#if defined(HOST_X86) || defined(HOST_AMD64)

#ifdef TARGET_UNIX
// MSVC directly defines intrinsics for __cpuid and __cpuidex matching the below signatures
// We define matching signatures for use on Unix platforms.
//
// IMPORTANT: Unlike MSVC, Unix does not explicitly zero ECX for __cpuid

REDHAWK_PALIMPORT void __cpuid(int cpuInfo[4], int function_id);
REDHAWK_PALIMPORT void __cpuidex(int cpuInfo[4], int function_id, int subFunction_id);
#else
#include <intrin.h>
#endif

REDHAWK_PALIMPORT uint32_t REDHAWK_PALAPI xmmYmmStateSupport();
REDHAWK_PALIMPORT uint32_t REDHAWK_PALAPI avx512StateSupport();
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalIsAvxEnabled();
REDHAWK_PALIMPORT bool REDHAWK_PALAPI PalIsAvx512Enabled();

#endif // defined(HOST_X86) || defined(HOST_AMD64)

#if defined(HOST_ARM64)
REDHAWK_PALIMPORT void REDHAWK_PALAPI PAL_GetCpuCapabilityFlags(int* flags);
#endif //defined(HOST_ARM64)

#include "PalRedhawkInline.h"

#endif // !PAL_REDHAWK_INCLUDED
