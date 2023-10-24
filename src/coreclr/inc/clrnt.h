// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef CLRNT_H_
#define CLRNT_H_

#include "staticcontract.h"
#include "cfi.h"

//
// ALL PLATFORMS
//

#define STATUS_INVALID_PARAMETER_3       ((NTSTATUS)0xC00000F1L)
#define STATUS_INVALID_PARAMETER_4       ((NTSTATUS)0xC00000F2L)
#define STATUS_UNSUCCESSFUL              ((NTSTATUS)0xC0000001L)
#define STATUS_SUCCESS                   ((NTSTATUS)0x00000000L)

#ifndef STATUS_UNWIND
#define STATUS_UNWIND                    ((NTSTATUS)0x80000027L)
#endif

#ifndef DBG_PRINTEXCEPTION_C
#define DBG_PRINTEXCEPTION_C             ((DWORD)0x40010006L)
#endif

#ifndef STATUS_UNWIND_CONSOLIDATE
#define STATUS_UNWIND_CONSOLIDATE        ((NTSTATUS)0x80000029L)
#endif

#ifndef STATUS_LONGJUMP
#define STATUS_LONGJUMP        ((NTSTATUS)0x80000026L)
#endif

#ifndef LOCALE_NAME_MAX_LENGTH
#define LOCALE_NAME_MAX_LENGTH 85
#endif // !LOCALE_NAME_MAX_LENGTH

#ifndef IMAGE_FILE_MACHINE_RISCV64
#define IMAGE_FILE_MACHINE_RISCV64        0x5064  // RISCV64
#endif // !IMAGE_FILE_MACHINE_RISCV64

#ifndef __out_xcount_opt
#define __out_xcount_opt(var) __out
#endif

#ifndef __encoded_pointer
#define __encoded_pointer
#endif

#ifndef __range
#define __range(min, man)
#endif

#ifndef __field_bcount
#define __field_bcount(size)
#endif

#ifndef __field_ecount_opt
#define __field_ecount_opt(nFields)
#endif

#ifndef __field_ecount
#define __field_ecount(EHCount)
#endif

#undef _Ret_bytecap_
#define _Ret_bytecap_(_Size)

#ifndef NT_SUCCESS
#define NT_SUCCESS(Status) (((NTSTATUS)(Status)) >= 0)
#endif

#define ARGUMENT_PRESENT(ArgumentPointer)    (\
    (CHAR *)(ArgumentPointer) != (CHAR *)(NULL) )

#define EXCEPTION_CHAIN_END ((PEXCEPTION_REGISTRATION_RECORD)-1)

typedef signed char SCHAR;
typedef SCHAR *PSCHAR;
typedef LONG NTSTATUS;

#ifdef HOST_WINDOWS

#define TLS_EXPANSION_SLOTS   1024

// Included for TEB::ReservedForOle, TlsSlots, TlsExpansionSlots
#include <winternl.h>

// Alias for TEB::ThreadLocalStoragePointer
#define ThreadLocalStoragePointer Reserved1[11]

#endif

#if !defined(TARGET_X86)

typedef enum _FUNCTION_TABLE_TYPE {
    RF_SORTED,
    RF_UNSORTED,
    RF_CALLBACK
} FUNCTION_TABLE_TYPE;

typedef struct _DYNAMIC_FUNCTION_TABLE {
    LIST_ENTRY Links;
    PT_RUNTIME_FUNCTION FunctionTable;
    LARGE_INTEGER TimeStamp;

#ifdef TARGET_ARM
    ULONG MinimumAddress;
    ULONG MaximumAddress;
    ULONG BaseAddress;
#else
    ULONG64 MinimumAddress;
    ULONG64 MaximumAddress;
    ULONG64 BaseAddress;
#endif

    PGET_RUNTIME_FUNCTION_CALLBACK Callback;
    PVOID Context;
    PWSTR OutOfProcessCallbackDll;
    FUNCTION_TABLE_TYPE Type;
    ULONG EntryCount;
} DYNAMIC_FUNCTION_TABLE, *PDYNAMIC_FUNCTION_TABLE;

#endif // !TARGET_X86

//
//   AMD64
//
#ifdef TARGET_AMD64

#define RUNTIME_FUNCTION__BeginAddress(prf)             (prf)->BeginAddress
#define RUNTIME_FUNCTION__SetBeginAddress(prf,address)  ((prf)->BeginAddress = (address))

#define RUNTIME_FUNCTION__EndAddress(prf, ImageBase)    (prf)->EndAddress

#define RUNTIME_FUNCTION__GetUnwindInfoAddress(prf) (prf)->UnwindData
#define RUNTIME_FUNCTION__SetUnwindInfoAddress(prf,address) do { (prf)->UnwindData = (address); } while (0)
#define OFFSETOF__RUNTIME_FUNCTION__UnwindInfoAddress offsetof(T_RUNTIME_FUNCTION, UnwindData)

#include "win64unwind.h"

typedef
PEXCEPTION_ROUTINE
(RtlVirtualUnwindFn) (
    IN ULONG HandlerType,
    IN ULONG64 ImageBase,
    IN ULONG64 ControlPc,
    IN PT_RUNTIME_FUNCTION FunctionEntry,
    IN OUT PCONTEXT ContextRecord,
    OUT PVOID *HandlerData,
    OUT PULONG64 EstablisherFrame,
    IN OUT PKNONVOLATILE_CONTEXT_POINTERS ContextPointers OPTIONAL
    );

#ifndef HOST_UNIX
extern RtlVirtualUnwindFn* RtlVirtualUnwind_Unsafe;
#else // !HOST_UNIX
PEXCEPTION_ROUTINE
RtlVirtualUnwind_Unsafe(
    IN ULONG HandlerType,
    IN ULONG64 ImageBase,
    IN ULONG64 ControlPc,
    IN PT_RUNTIME_FUNCTION FunctionEntry,
    IN OUT PCONTEXT ContextRecord,
    OUT PVOID *HandlerData,
    OUT PULONG64 EstablisherFrame,
    IN OUT PKNONVOLATILE_CONTEXT_POINTERS ContextPointers OPTIONAL
    );
#endif // !HOST_UNIX

#endif // TARGET_AMD64

//
//  X86
//

#ifdef TARGET_X86
#ifndef HOST_UNIX
//
// x86 ABI does not define RUNTIME_FUNCTION. Define our own to allow unification between x86 and other platforms.
//
#ifdef HOST_X86
typedef struct _RUNTIME_FUNCTION {
    DWORD BeginAddress;
    DWORD UnwindData;
} RUNTIME_FUNCTION, *PRUNTIME_FUNCTION;

typedef struct _DISPATCHER_CONTEXT {
    _EXCEPTION_REGISTRATION_RECORD* RegistrationPointer;
} DISPATCHER_CONTEXT, *PDISPATCHER_CONTEXT;
#endif // HOST_X86
#endif // !HOST_UNIX

#define RUNTIME_FUNCTION__BeginAddress(prf)             (prf)->BeginAddress
#define RUNTIME_FUNCTION__SetBeginAddress(prf,addr)     ((prf)->BeginAddress = (addr))

#ifdef FEATURE_EH_FUNCLETS
#include "win64unwind.h"
#include "daccess.h"

FORCEINLINE
DWORD
RtlpGetFunctionEndAddress (
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    _In_ TADDR ImageBase
    )
{
    PTR_UNWIND_INFO pUnwindInfo = (PTR_UNWIND_INFO)(ImageBase + FunctionEntry->UnwindData);

    return FunctionEntry->BeginAddress + pUnwindInfo->FunctionLength;
}

#define RUNTIME_FUNCTION__EndAddress(prf, ImageBase)   RtlpGetFunctionEndAddress(prf, ImageBase)

#define RUNTIME_FUNCTION__GetUnwindInfoAddress(prf)    (prf)->UnwindData
#define RUNTIME_FUNCTION__SetUnwindInfoAddress(prf, addr) do { (prf)->UnwindData = (addr); } while(0)

#ifdef HOST_X86
EXTERN_C
NTSYSAPI
PEXCEPTION_ROUTINE
NTAPI
RtlVirtualUnwind (
    _In_ DWORD HandlerType,
    _In_ DWORD ImageBase,
    _In_ DWORD ControlPc,
    _In_ PRUNTIME_FUNCTION FunctionEntry,
    __inout PT_CONTEXT ContextRecord,
    _Out_ PVOID *HandlerData,
    _Out_ PDWORD EstablisherFrame,
    __inout_opt PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers
    );
#endif // HOST_X86
#endif // FEATURE_EH_FUNCLETS

#endif // TARGET_X86

#ifdef TARGET_ARM
#include "daccess.h"

//
// Define unwind information flags.
//

#define UNW_FLAG_NHANDLER               0x0             /* any handler */
#define UNW_FLAG_EHANDLER               0x1             /* filter handler */
#define UNW_FLAG_UHANDLER               0x2             /* unwind handler */

// This function returns the length of a function using the new unwind info on arm.
// Taken from minkernel\ntos\rtl\arm\ntrtlarm.h.
FORCEINLINE
ULONG
RtlpGetFunctionEndAddress (
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    _In_ TADDR ImageBase
    )
{
    ULONG FunctionLength;

    FunctionLength = FunctionEntry->UnwindData;
    if ((FunctionLength & 3) != 0) {
        FunctionLength = (FunctionLength >> 2) & 0x7ff;
    } else {
        FunctionLength = *(PTR_ULONG)(ImageBase + FunctionLength) & 0x3ffff;
    }

    return FunctionEntry->BeginAddress + 2 * FunctionLength;
}

#define RUNTIME_FUNCTION__BeginAddress(FunctionEntry)               ThumbCodeToDataPointer<DWORD,DWORD>((FunctionEntry)->BeginAddress)
#define RUNTIME_FUNCTION__SetBeginAddress(FunctionEntry,address)    ((FunctionEntry)->BeginAddress = DataPointerToThumbCode<DWORD,DWORD>(address))

#define RUNTIME_FUNCTION__EndAddress(FunctionEntry, ImageBase)      ThumbCodeToDataPointer<DWORD,DWORD>(RtlpGetFunctionEndAddress(FunctionEntry, ImageBase))

#define RUNTIME_FUNCTION__SetUnwindInfoAddress(prf,address) do { (prf)->UnwindData = (address); } while (0)

typedef struct _UNWIND_INFO {
    // dummy
} UNWIND_INFO, *PUNWIND_INFO;

#if defined(HOST_UNIX) || defined(HOST_X86)

EXTERN_C
NTSYSAPI
PEXCEPTION_ROUTINE
NTAPI
RtlVirtualUnwind (
    _In_ DWORD HandlerType,
    _In_ DWORD ImageBase,
    _In_ DWORD ControlPc,
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    __inout PT_CONTEXT ContextRecord,
    _Out_ PVOID *HandlerData,
    _Out_ PDWORD EstablisherFrame,
    __inout_opt PT_KNONVOLATILE_CONTEXT_POINTERS ContextPointers
    );
#endif // HOST_UNIX || HOST_X86

#define UNW_FLAG_NHANDLER 0x0

#endif // TARGET_ARM

#ifdef TARGET_ARM64
#include "daccess.h"

#define UNW_FLAG_NHANDLER               0x0             /* any handler */
#define UNW_FLAG_EHANDLER               0x1             /* filter handler */
#define UNW_FLAG_UHANDLER               0x2             /* unwind handler */

// This function returns the RVA of the end of the function (exclusive, so one byte after the actual end)
// using the unwind info on ARM64. (see ExternalAPIs\Win9CoreSystem\inc\winnt.h)
FORCEINLINE
ULONG64
RtlpGetFunctionEndAddress (
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    _In_ ULONG64 ImageBase
    )
{
    ULONG64 FunctionLength;

    FunctionLength = FunctionEntry->UnwindData;
    if ((FunctionLength & 3) != 0)
    {
        // Compact form pdata.
        if ((FunctionLength & 7) == 3)
        {
            // Long branch pdata, by standard this is 3 so size is 12.
            FunctionLength = 3;
        }
        else
        {
            FunctionLength = (FunctionLength >> 2) & 0x7ff;
        }
    }
    else
    {
        // Get from the xdata record.
        FunctionLength = *(PTR_ULONG64)(ImageBase + FunctionLength) & 0x3ffff;
    }

    return FunctionEntry->BeginAddress + 4 * FunctionLength;
}

#define RUNTIME_FUNCTION__BeginAddress(FunctionEntry)               ((FunctionEntry)->BeginAddress)
#define RUNTIME_FUNCTION__SetBeginAddress(FunctionEntry,address)    ((FunctionEntry)->BeginAddress = (address))

#define RUNTIME_FUNCTION__EndAddress(FunctionEntry, ImageBase)      (RtlpGetFunctionEndAddress(FunctionEntry, (ULONG64)(ImageBase)))

#define RUNTIME_FUNCTION__SetUnwindInfoAddress(prf,address)         do { (prf)->UnwindData = (address); } while (0)

typedef struct _UNWIND_INFO {
    // dummy
} UNWIND_INFO, *PUNWIND_INFO;

EXTERN_C
NTSYSAPI
PEXCEPTION_ROUTINE
NTAPI
RtlVirtualUnwind(
    IN ULONG HandlerType,
    IN ULONG64 ImageBase,
    IN ULONG64 ControlPc,
    IN PRUNTIME_FUNCTION FunctionEntry,
    IN OUT PCONTEXT ContextRecord,
    OUT PVOID *HandlerData,
    OUT PULONG64 EstablisherFrame,
    IN OUT PKNONVOLATILE_CONTEXT_POINTERS ContextPointers OPTIONAL
    );

#endif

#ifdef TARGET_LOONGARCH64
#include "daccess.h"

#define UNW_FLAG_NHANDLER               0x0             /* any handler */
#define UNW_FLAG_EHANDLER               0x1             /* filter handler */
#define UNW_FLAG_UHANDLER               0x2             /* unwind handler */

// This function returns the RVA of the end of the function (exclusive, so one byte after the actual end)
// using the unwind info on LOONGARCH64. (see ExternalAPIs\Win9CoreSystem\inc\winnt.h)
FORCEINLINE
ULONG64
RtlpGetFunctionEndAddress (
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    _In_ ULONG64 ImageBase
    )
{
    ULONG64 FunctionLength;

    FunctionLength = FunctionEntry->UnwindData;
    if ((FunctionLength & 3) != 0) {
        FunctionLength = (FunctionLength >> 2) & 0x7ff;
    } else {
        memcpy(&FunctionLength, (void*)(ImageBase + FunctionLength), sizeof(UINT32));
        FunctionLength &= 0x3ffff;
    }

    return FunctionEntry->BeginAddress + 4 * FunctionLength;
}

#define RUNTIME_FUNCTION__BeginAddress(FunctionEntry)               ((FunctionEntry)->BeginAddress)
#define RUNTIME_FUNCTION__SetBeginAddress(FunctionEntry,address)    ((FunctionEntry)->BeginAddress = (address))

#define RUNTIME_FUNCTION__EndAddress(FunctionEntry, ImageBase)      (RtlpGetFunctionEndAddress(FunctionEntry, (ULONG64)(ImageBase)))

#define RUNTIME_FUNCTION__SetUnwindInfoAddress(prf,address)         do { (prf)->UnwindData = (address); } while (0)

typedef struct _UNWIND_INFO {
    // dummy
} UNWIND_INFO, *PUNWIND_INFO;

EXTERN_C
NTSYSAPI
PEXCEPTION_ROUTINE
NTAPI
RtlVirtualUnwind(
    IN ULONG HandlerType,
    IN ULONG64 ImageBase,
    IN ULONG64 ControlPc,
    IN PRUNTIME_FUNCTION FunctionEntry,
    IN OUT PCONTEXT ContextRecord,
    OUT PVOID *HandlerData,
    OUT PULONG64 EstablisherFrame,
    IN OUT PKNONVOLATILE_CONTEXT_POINTERS ContextPointers OPTIONAL
    );

#endif // TARGET_LOONGARCH64

#ifdef TARGET_RISCV64
#include "daccess.h"

#define UNW_FLAG_NHANDLER               0x0             /* any handler */
#define UNW_FLAG_EHANDLER               0x1             /* filter handler */
#define UNW_FLAG_UHANDLER               0x2             /* unwind handler */

// This function returns the RVA of the end of the function (exclusive, so one byte after the actual end)
// using the unwind info on ARM64. (see ExternalAPIs\Win9CoreSystem\inc\winnt.h)
FORCEINLINE
ULONG64
RtlpGetFunctionEndAddress (
    _In_ PT_RUNTIME_FUNCTION FunctionEntry,
    _In_ ULONG64 ImageBase
    )
{
    ULONG64 FunctionLength;

    FunctionLength = FunctionEntry->UnwindData;
    if ((FunctionLength & 3) != 0) {
        FunctionLength = (FunctionLength >> 2) & 0x7ff;
    } else {
        FunctionLength = *(PTR_ULONG64)(ImageBase + FunctionLength) & 0x3ffff;
    }

    return FunctionEntry->BeginAddress + 4 * FunctionLength;
}

#define RUNTIME_FUNCTION__BeginAddress(FunctionEntry)               ((FunctionEntry)->BeginAddress)
#define RUNTIME_FUNCTION__SetBeginAddress(FunctionEntry,address)    ((FunctionEntry)->BeginAddress = (address))

#define RUNTIME_FUNCTION__EndAddress(FunctionEntry, ImageBase)      (RtlpGetFunctionEndAddress(FunctionEntry, (ULONG64)(ImageBase)))

#define RUNTIME_FUNCTION__SetUnwindInfoAddress(prf,address)         do { (prf)->UnwindData = (address); } while (0)

typedef struct _UNWIND_INFO {
    // dummy
} UNWIND_INFO, *PUNWIND_INFO;

EXTERN_C
NTSYSAPI
PEXCEPTION_ROUTINE
NTAPI
RtlVirtualUnwind(
    IN ULONG HandlerType,
    IN ULONG64 ImageBase,
    IN ULONG64 ControlPc,
    IN PRUNTIME_FUNCTION FunctionEntry,
    IN OUT PCONTEXT ContextRecord,
    OUT PVOID *HandlerData,
    OUT PULONG64 EstablisherFrame,
    IN OUT PKNONVOLATILE_CONTEXT_POINTERS ContextPointers OPTIONAL
    );

#endif // TARGET_RISCV64

#endif  // CLRNT_H_
