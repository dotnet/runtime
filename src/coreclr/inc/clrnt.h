// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef CLRNT_H_
#define CLRNT_H_

#include "staticcontract.h"
#include "cfi.h"

//
// This file is the result of some changes to the SDK header files.
// In particular, nt.h and some of its dependencies are no longer
// available except as "nonship" files.  As a result, this file
// was created as a simple cut and past of structures and functions
// from NT that are either not yet documented or have been overlooked
// as being part of the platform SDK.
//

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

#ifndef SUBLANG_CUSTOM_DEFAULT
#define SUBLANG_CUSTOM_DEFAULT                      0x03    // default custom language/locale
#define SUBLANG_CUSTOM_UNSPECIFIED                  0x04    // custom language/locale
#define LOCALE_CUSTOM_DEFAULT                                                 \
              (MAKELCID(MAKELANGID(LANG_NEUTRAL, SUBLANG_CUSTOM_DEFAULT), SORT_DEFAULT))
#define LOCALE_CUSTOM_UNSPECIFIED                                             \
              (MAKELCID(MAKELANGID(LANG_NEUTRAL, SUBLANG_CUSTOM_UNSPECIFIED), SORT_DEFAULT))
#endif // !SUBLANG_CUSTOM_DEFAULT

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

#ifndef HOST_UNIX

#define TLS_MINIMUM_AVAILABLE 64    // winnt
#define TLS_EXPANSION_SLOTS   1024

typedef enum _THREADINFOCLASS {
    ThreadBasicInformation,
    ThreadTimes,
    ThreadPriority,
    ThreadBasePriority,
    ThreadAffinityMask,
    ThreadImpersonationToken,
    ThreadDescriptorTableEntry,
    ThreadEnableAlignmentFaultFixup,
    ThreadEventPair_Reusable,
    ThreadQuerySetWin32StartAddress,
    ThreadZeroTlsCell,
    ThreadPerformanceCount,
    ThreadAmILastThread,
    ThreadIdealProcessor,
    ThreadPriorityBoost,
    ThreadSetTlsArrayAddress,
    ThreadIsIoPending,
    ThreadHideFromDebugger,
    ThreadBreakOnTermination,
    MaxThreadInfoClass
    } THREADINFOCLASS;

typedef enum _SYSTEM_INFORMATION_CLASS {
    SystemBasicInformation,
    SystemProcessorInformation,             // obsolete...delete
    SystemPerformanceInformation,
    SystemTimeOfDayInformation,
    SystemPathInformation,
    SystemProcessInformation,
    SystemCallCountInformation,
    SystemDeviceInformation,
    SystemProcessorPerformanceInformation,
    SystemFlagsInformation,
    SystemCallTimeInformation,
    SystemModuleInformation,
    SystemLocksInformation,
    SystemStackTraceInformation,
    SystemPagedPoolInformation,
    SystemNonPagedPoolInformation,
    SystemHandleInformation,
    SystemObjectInformation,
    SystemPageFileInformation,
    SystemVdmInstemulInformation,
    SystemVdmBopInformation,
    SystemFileCacheInformation,
    SystemPoolTagInformation,
    SystemInterruptInformation,
    SystemDpcBehaviorInformation,
    SystemFullMemoryInformation,
    SystemLoadGdiDriverInformation,
    SystemUnloadGdiDriverInformation,
    SystemTimeAdjustmentInformation,
    SystemSummaryMemoryInformation,
    SystemMirrorMemoryInformation,
    SystemPerformanceTraceInformation,
    SystemObsolete0,
    SystemExceptionInformation,
    SystemCrashDumpStateInformation,
    SystemKernelDebuggerInformation,
    SystemContextSwitchInformation,
    SystemRegistryQuotaInformation,
    SystemExtendServiceTableInformation,
    SystemPrioritySeparation,
    SystemVerifierAddDriverInformation,
    SystemVerifierRemoveDriverInformation,
    SystemProcessorIdleInformation,
    SystemLegacyDriverInformation,
    SystemCurrentTimeZoneInformation,
    SystemLookasideInformation,
    SystemTimeSlipNotification,
    SystemSessionCreate,
    SystemSessionDetach,
    SystemSessionInformation,
    SystemRangeStartInformation,
    SystemVerifierInformation,
    SystemVerifierThunkExtend,
    SystemSessionProcessInformation,
    SystemLoadGdiDriverInSystemSpace,
    SystemNumaProcessorMap,
    SystemPrefetcherInformation,
    SystemExtendedProcessInformation,
    SystemRecommendedSharedDataAlignment,
    SystemComPlusPackage,
    SystemNumaAvailableMemory,
    SystemProcessorPowerInformation,
    SystemEmulationBasicInformation,
    SystemEmulationProcessorInformation,
    SystemExtendedHandleInformation,
    SystemLostDelayedWriteInformation
} SYSTEM_INFORMATION_CLASS;

typedef enum _EVENT_INFORMATION_CLASS {
    EventBasicInformation
    } EVENT_INFORMATION_CLASS;

typedef struct _SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION {
    LARGE_INTEGER IdleTime;
    LARGE_INTEGER KernelTime;
    LARGE_INTEGER UserTime;
    LARGE_INTEGER DpcTime;          // DEVL only
    LARGE_INTEGER InterruptTime;    // DEVL only
    ULONG InterruptCount;
} SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION, *PSYSTEM_PROCESSOR_PERFORMANCE_INFORMATION;

typedef enum _EVENT_TYPE {
    NotificationEvent,
    SynchronizationEvent
    } EVENT_TYPE;

typedef struct _EVENT_BASIC_INFORMATION {
    EVENT_TYPE EventType;
    LONG EventState;
} EVENT_BASIC_INFORMATION, *PEVENT_BASIC_INFORMATION;

#define RTL_MEG                   (1024UL * 1024UL)
#define RTLP_IMAGE_MAX_DOS_HEADER ( 256UL * RTL_MEG)

typedef struct _SYSTEM_KERNEL_DEBUGGER_INFORMATION {
    BOOLEAN KernelDebuggerEnabled;
    BOOLEAN KernelDebuggerNotPresent;
} SYSTEM_KERNEL_DEBUGGER_INFORMATION, *PSYSTEM_KERNEL_DEBUGGER_INFORMATION;

typedef struct _STRING {
    USHORT Length;
    USHORT MaximumLength;
#ifdef MIDL_PASS
    [size_is(MaximumLength), length_is(Length) ]
#endif // MIDL_PASS
    PCHAR Buffer;
} STRING;
typedef STRING *PSTRING;

typedef STRING ANSI_STRING;
typedef PSTRING PANSI_STRING;

typedef STRING OEM_STRING;
typedef PSTRING POEM_STRING;
typedef CONST STRING* PCOEM_STRING;

typedef struct _UNICODE_STRING {
    USHORT Length;
    USHORT MaximumLength;
#ifdef MIDL_PASS
    [size_is(MaximumLength / 2), length_is((Length) / 2) ] USHORT * Buffer;
#else // MIDL_PASS
    PWSTR  Buffer;
#endif // MIDL_PASS
} UNICODE_STRING;
typedef UNICODE_STRING *PUNICODE_STRING;
typedef const UNICODE_STRING *PCUNICODE_STRING;
#define UNICODE_NULL ((WCHAR)0) // winnt

typedef struct _STRING32 {
    USHORT   Length;
    USHORT   MaximumLength;
    ULONG  Buffer;
} STRING32;
typedef STRING32 *PSTRING32;

typedef STRING32 UNICODE_STRING32;
typedef UNICODE_STRING32 *PUNICODE_STRING32;

typedef STRING32 ANSI_STRING32;
typedef ANSI_STRING32 *PANSI_STRING32;


typedef struct _STRING64 {
    USHORT   Length;
    USHORT   MaximumLength;
    ULONGLONG  Buffer;
} STRING64;
typedef STRING64 *PSTRING64;

typedef STRING64 UNICODE_STRING64;
typedef UNICODE_STRING64 *PUNICODE_STRING64;

typedef STRING64 ANSI_STRING64;
typedef ANSI_STRING64 *PANSI_STRING64;

#define GDI_HANDLE_BUFFER_SIZE32  34
#define GDI_HANDLE_BUFFER_SIZE64  60

#if !defined(TARGET_AMD64)
#define GDI_HANDLE_BUFFER_SIZE      GDI_HANDLE_BUFFER_SIZE32
#else
#define GDI_HANDLE_BUFFER_SIZE      GDI_HANDLE_BUFFER_SIZE64
#endif

typedef ULONG GDI_HANDLE_BUFFER32[GDI_HANDLE_BUFFER_SIZE32];
typedef ULONG GDI_HANDLE_BUFFER64[GDI_HANDLE_BUFFER_SIZE64];
typedef ULONG GDI_HANDLE_BUFFER  [GDI_HANDLE_BUFFER_SIZE  ];


typedef struct _PEB_LDR_DATA {
    ULONG Length;
    BOOLEAN Initialized;
    HANDLE SsHandle;
    LIST_ENTRY InLoadOrderModuleList;
    LIST_ENTRY InMemoryOrderModuleList;
    LIST_ENTRY InInitializationOrderModuleList;
    PVOID EntryInProgress;
} PEB_LDR_DATA, *PPEB_LDR_DATA;

typedef struct _PEB_FREE_BLOCK {
    struct _PEB_FREE_BLOCK *Next;
    ULONG Size;
} PEB_FREE_BLOCK, *PPEB_FREE_BLOCK;

typedef PVOID* PPVOID;

typedef
VOID
(*PPS_POST_PROCESS_INIT_ROUTINE) (
    VOID
    );

typedef struct _LDR_DATA_TABLE_ENTRY {
    LIST_ENTRY InLoadOrderLinks;
    LIST_ENTRY InMemoryOrderLinks;
    LIST_ENTRY InInitializationOrderLinks;
    PVOID DllBase;
    PVOID EntryPoint;
    ULONG SizeOfImage;
    UNICODE_STRING FullDllName;
    UNICODE_STRING BaseDllName;
    ULONG Flags;
    USHORT LoadCount;
    USHORT TlsIndex;
    union _foo {
        LIST_ENTRY HashLinks;
        struct _bar {
            PVOID SectionPointer;
            ULONG CheckSum;
        };
    };
    union _foo2 {
        struct _bar2 {
            ULONG TimeDateStamp;
        };
        struct _bar3 {
            PVOID LoadedImports;
        };
    };
    PVOID EntryPointActivationContext;
} LDR_DATA_TABLE_ENTRY, *PLDR_DATA_TABLE_ENTRY;

#define TYPE3(arg) arg

typedef struct _PEB {
    BOOLEAN InheritedAddressSpace;      // These four fields cannot change unless the
    BOOLEAN ReadImageFileExecOptions;   //
    BOOLEAN BeingDebugged;              //
    BOOLEAN SpareBool;                  //
    HANDLE Mutant;                      // INITIAL_PEB structure is also updated.

    PVOID ImageBaseAddress;
    PPEB_LDR_DATA Ldr;
    TYPE3(struct _RTL_USER_PROCESS_PARAMETERS*) ProcessParameters;
    PVOID SubSystemData;
    PVOID ProcessHeap;
    TYPE3(struct _RTL_CRITICAL_SECTION*) FastPebLock;
    PVOID FastPebLockRoutine;
    PVOID FastPebUnlockRoutine;
    ULONG EnvironmentUpdateCount;
    PVOID KernelCallbackTable;
    ULONG SystemReserved[1];

    struct _foo {
        ULONG ExecuteOptions : 2;
        ULONG SpareBits : 30;
    };


    PPEB_FREE_BLOCK FreeList;
    ULONG TlsExpansionCounter;
    PVOID TlsBitmap;
    ULONG TlsBitmapBits[2];         // TLS_MINIMUM_AVAILABLE bits
    PVOID ReadOnlySharedMemoryBase;
    PVOID ReadOnlySharedMemoryHeap;
    PPVOID ReadOnlyStaticServerData;
    PVOID AnsiCodePageData;
    PVOID OemCodePageData;
    PVOID UnicodeCaseTableData;

    //
    // Useful information for LdrpInitialize
    ULONG NumberOfProcessors;
    ULONG NtGlobalFlag;

    //
    // Passed up from MmCreatePeb from Session Manager registry key
    //

    LARGE_INTEGER CriticalSectionTimeout;
    SIZE_T HeapSegmentReserve;
    SIZE_T HeapSegmentCommit;
    SIZE_T HeapDeCommitTotalFreeThreshold;
    SIZE_T HeapDeCommitFreeBlockThreshold;

    //
    // Where heap manager keeps track of all heaps created for a process
    // Fields initialized by MmCreatePeb.  ProcessHeaps is initialized
    // to point to the first free byte after the PEB and MaximumNumberOfHeaps
    // is computed from the page size used to hold the PEB, less the fixed
    // size of this data structure.
    //

    ULONG NumberOfHeaps;
    ULONG MaximumNumberOfHeaps;
    PPVOID ProcessHeaps;

    //
    //
    PVOID GdiSharedHandleTable;
    PVOID ProcessStarterHelper;
    ULONG GdiDCAttributeList;
    PVOID LoaderLock;

    //
    // Following fields filled in by MmCreatePeb from system values and/or
    // image header.
    //

    ULONG OSMajorVersion;
    ULONG OSMinorVersion;
    USHORT OSBuildNumber;
    USHORT OSCSDVersion;
    ULONG OSPlatformId;
    ULONG ImageSubsystem;
    ULONG ImageSubsystemMajorVersion;
    ULONG ImageSubsystemMinorVersion;
    ULONG_PTR ImageProcessAffinityMask;
    GDI_HANDLE_BUFFER GdiHandleBuffer;
    PPS_POST_PROCESS_INIT_ROUTINE PostProcessInitRoutine;

    PVOID TlsExpansionBitmap;
    ULONG TlsExpansionBitmapBits[32];   // TLS_EXPANSION_SLOTS bits

    //
    // Id of the Hydra session in which this process is running
    //
    ULONG SessionId;

    //
    // Filled in by LdrpInstallAppcompatBackend
    //
    ULARGE_INTEGER AppCompatFlags;

    //
    // ntuser appcompat flags
    //
    ULARGE_INTEGER AppCompatFlagsUser;

    //
    // Filled in by LdrpInstallAppcompatBackend
    //
    PVOID pShimData;

    //
    // Filled in by LdrQueryImageFileExecutionOptions
    //
    PVOID AppCompatInfo;

    //
    // Used by GetVersionExW as the szCSDVersion string
    //
    UNICODE_STRING CSDVersion;

    //
    // Fusion stuff
    //
    PVOID ActivationContextData;
    PVOID ProcessAssemblyStorageMap;
    PVOID SystemDefaultActivationContextData;
    PVOID SystemAssemblyStorageMap;

    //
    // Enforced minimum initial commit stack
    //
    SIZE_T MinimumStackCommit;

} PEB, *PPEB;

#define ACTIVATION_CONTEXT_STACK_FLAG_QUERIES_DISABLED (0x00000001)

typedef struct _ACTIVATION_CONTEXT_STACK {
    ULONG Flags;
    ULONG NextCookieSequenceNumber;
    PVOID ActiveFrame;
    LIST_ENTRY FrameListCache;

#if NT_SXS_PERF_COUNTERS_ENABLED
    struct _ACTIVATION_CONTEXT_STACK_PERF_COUNTERS {
        ULONGLONG Activations;
        ULONGLONG ActivationCycles;
        ULONGLONG Deactivations;
        ULONGLONG DeactivationCycles;
    } Counters;
#endif // NT_SXS_PERF_COUNTERS_ENABLED
} ACTIVATION_CONTEXT_STACK, *PACTIVATION_CONTEXT_STACK;

typedef const ACTIVATION_CONTEXT_STACK *PCACTIVATION_CONTEXT_STACK;

#define TEB_ACTIVE_FRAME_CONTEXT_FLAG_EXTENDED (0x00000001)

typedef struct _TEB_ACTIVE_FRAME_CONTEXT {
    ULONG Flags;
    PCSTR FrameName;
} TEB_ACTIVE_FRAME_CONTEXT, *PTEB_ACTIVE_FRAME_CONTEXT;

typedef const struct _TEB_ACTIVE_FRAME_CONTEXT *PCTEB_ACTIVE_FRAME_CONTEXT;

typedef struct _TEB_ACTIVE_FRAME_CONTEXT_EX {
    TEB_ACTIVE_FRAME_CONTEXT BasicContext;
    PCSTR SourceLocation; // e.g. "Z:\foo\bar\baz.c"
} TEB_ACTIVE_FRAME_CONTEXT_EX, *PTEB_ACTIVE_FRAME_CONTEXT_EX;

typedef const struct _TEB_ACTIVE_FRAME_CONTEXT_EX *PCTEB_ACTIVE_FRAME_CONTEXT_EX;

#define TEB_ACTIVE_FRAME_FLAG_EXTENDED (0x00000001)

typedef struct _TEB_ACTIVE_FRAME {
    ULONG Flags;
    TYPE3(struct _TEB_ACTIVE_FRAME*) Previous;
    PCTEB_ACTIVE_FRAME_CONTEXT Context;
} TEB_ACTIVE_FRAME, *PTEB_ACTIVE_FRAME;

typedef const struct _TEB_ACTIVE_FRAME *PCTEB_ACTIVE_FRAME;

typedef struct _TEB_ACTIVE_FRAME_EX {
    TEB_ACTIVE_FRAME BasicFrame;
    PVOID ExtensionIdentifier; // use address of your DLL Main or something unique to your mapping in the address space
} TEB_ACTIVE_FRAME_EX, *PTEB_ACTIVE_FRAME_EX;

typedef const struct _TEB_ACTIVE_FRAME_EX *PCTEB_ACTIVE_FRAME_EX;

typedef struct _CLIENT_ID {
    HANDLE UniqueProcess;
    HANDLE UniqueThread;
} CLIENT_ID;
typedef CLIENT_ID *PCLIENT_ID;

#define GDI_BATCH_BUFFER_SIZE 310

typedef struct _GDI_TEB_BATCH {
    ULONG    Offset;
    ULONG_PTR HDC;
    ULONG    Buffer[GDI_BATCH_BUFFER_SIZE];
} GDI_TEB_BATCH,*PGDI_TEB_BATCH;

typedef struct _Wx86ThreadState {
    PULONG  CallBx86Eip;
    PVOID   DeallocationCpu;
    BOOLEAN UseKnownWx86Dll;
    char    OleStubInvoked;
} WX86THREAD, *PWX86THREAD;

#define STATIC_UNICODE_BUFFER_LENGTH 261
#define WIN32_CLIENT_INFO_LENGTH 62

typedef struct _PEB* PPEB;

typedef struct _TEB {
    NT_TIB NtTib;
    PVOID  EnvironmentPointer;
    CLIENT_ID ClientId;
    PVOID ActiveRpcHandle;
    PVOID ThreadLocalStoragePointer;
#if defined(PEBTEB_BITS)
    PVOID ProcessEnvironmentBlock;
#else
    PPEB ProcessEnvironmentBlock;
#endif
    ULONG LastErrorValue;
    ULONG CountOfOwnedCriticalSections;
    PVOID CsrClientThread;
    PVOID Win32ThreadInfo;          // PtiCurrent
    ULONG User32Reserved[26];       // user32.dll items
    ULONG UserReserved[5];          // Winsrv SwitchStack
    PVOID WOW32Reserved;            // used by WOW
    LCID CurrentLocale;
    ULONG FpSoftwareStatusRegister; // offset known by outsiders!
    PVOID SystemReserved1[54];      // Used by FP emulator
    NTSTATUS ExceptionCode;         // for RaiseUserException
    ACTIVATION_CONTEXT_STACK ActivationContextStack;   // Fusion activation stack
    // sizeof(PVOID) is a way to express processor-dependence, more generally than #ifdef HOST_64BIT
    UCHAR SpareBytes1[48 - sizeof(PVOID) - sizeof(ACTIVATION_CONTEXT_STACK)];
    GDI_TEB_BATCH GdiTebBatch;      // Gdi batching
    CLIENT_ID RealClientId;
    HANDLE GdiCachedProcessHandle;
    ULONG GdiClientPID;
    ULONG GdiClientTID;
    PVOID GdiThreadLocalInfo;
    ULONG_PTR Win32ClientInfo[WIN32_CLIENT_INFO_LENGTH]; // User32 Client Info
    PVOID glDispatchTable[233];     // OpenGL
    ULONG_PTR glReserved1[29];      // OpenGL
    PVOID glReserved2;              // OpenGL
    PVOID glSectionInfo;            // OpenGL
    PVOID glSection;                // OpenGL
    PVOID glTable;                  // OpenGL
    PVOID glCurrentRC;              // OpenGL
    PVOID glContext;                // OpenGL
    ULONG LastStatusValue;
    UNICODE_STRING StaticUnicodeString;
    WCHAR StaticUnicodeBuffer[STATIC_UNICODE_BUFFER_LENGTH];
    PVOID DeallocationStack;
    PVOID TlsSlots[TLS_MINIMUM_AVAILABLE];
    LIST_ENTRY TlsLinks;
    PVOID Vdm;
    PVOID ReservedForNtRpc;
    PVOID DbgSsReserved[2];
    ULONG HardErrorsAreDisabled;
    PVOID Instrumentation[16];
    PVOID WinSockData;              // WinSock
    ULONG GdiBatchCount;
    BOOLEAN InDbgPrint;
    BOOLEAN FreeStackOnTermination;
    BOOLEAN HasFiberData;
    BOOLEAN IdealProcessor;
    ULONG Spare3;
    PVOID ReservedForPerf;
    PVOID ReservedForOle;
    ULONG WaitingOnLoaderLock;
    WX86THREAD Wx86Thread;
    PPVOID TlsExpansionSlots;
    LCID ImpersonationLocale;       // Current locale of impersonated user
    ULONG IsImpersonating;          // Thread impersonation status
    PVOID NlsCache;                 // NLS thread cache
    PVOID pShimData;                // Per thread data used in the shim
    ULONG HeapVirtualAffinity;
    HANDLE CurrentTransactionHandle;// reserved for TxF transaction context
    PTEB_ACTIVE_FRAME ActiveFrame;
} TEB;
typedef TEB *PTEB;

typedef struct _CURDIR {
    UNICODE_STRING DosPath;
    HANDLE Handle;
} CURDIR, *PCURDIR;

#define RTL_USER_PROC_CURDIR_CLOSE      0x00000002
#define RTL_USER_PROC_CURDIR_INHERIT    0x00000003

typedef struct _RTL_DRIVE_LETTER_CURDIR {
    USHORT Flags;
    USHORT Length;
    ULONG TimeStamp;
    STRING DosPath;
} RTL_DRIVE_LETTER_CURDIR, *PRTL_DRIVE_LETTER_CURDIR;


#define RTL_MAX_DRIVE_LETTERS 32
#define RTL_DRIVE_LETTER_VALID (USHORT)0x0001

typedef struct _RTL_USER_PROCESS_PARAMETERS {
    ULONG MaximumLength;
    ULONG Length;

    ULONG Flags;
    ULONG DebugFlags;

    HANDLE ConsoleHandle;
    ULONG  ConsoleFlags;
    HANDLE StandardInput;
    HANDLE StandardOutput;
    HANDLE StandardError;

    CURDIR CurrentDirectory;        // ProcessParameters
    UNICODE_STRING DllPath;         // ProcessParameters
    UNICODE_STRING ImagePathName;   // ProcessParameters
    UNICODE_STRING CommandLine;     // ProcessParameters
    PVOID Environment;              // NtAllocateVirtualMemory

    ULONG StartingX;
    ULONG StartingY;
    ULONG CountX;
    ULONG CountY;
    ULONG CountCharsX;
    ULONG CountCharsY;
    ULONG FillAttribute;

    ULONG WindowFlags;
    ULONG ShowWindowFlags;
    UNICODE_STRING WindowTitle;     // ProcessParameters
    UNICODE_STRING DesktopInfo;     // ProcessParameters
    UNICODE_STRING ShellInfo;       // ProcessParameters
    UNICODE_STRING RuntimeData;     // ProcessParameters
    RTL_DRIVE_LETTER_CURDIR CurrentDirectores[ RTL_MAX_DRIVE_LETTERS ];
} RTL_USER_PROCESS_PARAMETERS, *PRTL_USER_PROCESS_PARAMETERS;


typedef enum _PROCESSINFOCLASS {
    ProcessBasicInformation,
    ProcessQuotaLimits,
    ProcessIoCounters,
    ProcessVmCounters,
    ProcessTimes,
    ProcessBasePriority,
    ProcessRaisePriority,
    ProcessDebugPort,
    ProcessExceptionPort,
    ProcessAccessToken,
    ProcessLdtInformation,
    ProcessLdtSize,
    ProcessDefaultHardErrorMode,
    ProcessIoPortHandlers,          // Note: this is kernel mode only
    ProcessPooledUsageAndLimits,
    ProcessWorkingSetWatch,
    ProcessUserModeIOPL,
    ProcessEnableAlignmentFaultFixup,
    ProcessPriorityClass,
    ProcessWx86Information,
    ProcessHandleCount,
    ProcessAffinityMask,
    ProcessPriorityBoost,
    ProcessDeviceMap,
    ProcessSessionInformation,
    ProcessForegroundInformation,
    ProcessWow64Information,
    ProcessImageFileName,
    ProcessLUIDDeviceMapsEnabled,
    ProcessBreakOnTermination,
    ProcessDebugObjectHandle,
    ProcessDebugFlags,
    ProcessHandleTracing,
    MaxProcessInfoClass             // MaxProcessInfoClass should always be the last enum
    } PROCESSINFOCLASS;


typedef struct _VM_COUNTERS {
    SIZE_T PeakVirtualSize;
    SIZE_T VirtualSize;
    ULONG PageFaultCount;
    SIZE_T PeakWorkingSetSize;
    SIZE_T WorkingSetSize;
    SIZE_T QuotaPeakPagedPoolUsage;
    SIZE_T QuotaPagedPoolUsage;
    SIZE_T QuotaPeakNonPagedPoolUsage;
    SIZE_T QuotaNonPagedPoolUsage;
    SIZE_T PagefileUsage;
    SIZE_T PeakPagefileUsage;
} VM_COUNTERS;
typedef VM_COUNTERS *PVM_COUNTERS;

#undef TYPE3

#endif // !defined(HOST_UNIX)

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

#endif  // CLRNT_H_
