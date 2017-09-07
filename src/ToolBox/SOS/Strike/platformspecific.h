// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==

// Include platform specific declarations based on the target platform rather than the host platform.

#ifndef __PLATFORM_SPECIFIC_INCLUDED
#define __PLATFORM_SPECIFIC_INCLUDED

// The main debugger code already has target platform definitions for CONTEXT.
#include "../../../debug/inc/dbgtargetcontext.h"

#ifndef FEATURE_PAL

// The various OS structure definitions below tend to differ based soley on the size of pointers. DT_POINTER
// is a type whose size matches that of the target platform. It's integral rather than point since it is never
// legal to dereference one of these on the host.
#ifdef _TARGET_WIN64_
typedef ULONG64 DT_POINTER;
#else
typedef ULONG32 DT_POINTER;
#endif

struct DT_LIST_ENTRY
{
    DT_POINTER Flink;
    DT_POINTER Blink;
};

struct DT_UNICODE_STRING
{
    USHORT Length;
    USHORT MaximumLength;
    DT_POINTER Buffer;
};

#define DT_GDI_HANDLE_BUFFER_SIZE32  34
#define DT_GDI_HANDLE_BUFFER_SIZE64  60

#ifndef IMAGE_FILE_MACHINE_ARMNT
#define IMAGE_FILE_MACHINE_ARMNT             0x01c4  // ARM Thumb-2 Little-Endian 
#endif

#ifndef IMAGE_FILE_MACHINE_ARM64
#define IMAGE_FILE_MACHINE_ARM64             0xAA64  // ARM64 Little-Endian
#endif

#ifdef _TARGET_WIN64_
typedef ULONG DT_GDI_HANDLE_BUFFER[DT_GDI_HANDLE_BUFFER_SIZE64];
#else
typedef ULONG DT_GDI_HANDLE_BUFFER[DT_GDI_HANDLE_BUFFER_SIZE32];
#endif

struct DT_PEB
{
    BOOLEAN InheritedAddressSpace;
    BOOLEAN ReadImageFileExecOptions;
    BOOLEAN BeingDebugged;
    BOOLEAN SpareBool;
    DT_POINTER Mutant;
    DT_POINTER ImageBaseAddress;
    DT_POINTER Ldr;
    DT_POINTER ProcessParameters;
    DT_POINTER SubSystemData;
    DT_POINTER ProcessHeap;
    DT_POINTER FastPebLock;
    DT_POINTER SparePtr1;
    DT_POINTER SparePtr2;
    ULONG EnvironmentUpdateCount;
    DT_POINTER KernelCallbackTable;
    ULONG SystemReserved[1];
    struct _dummy {
        ULONG ExecuteOptions : 2;
        ULONG SpareBits : 30;
    };    
    DT_POINTER FreeList;
    ULONG TlsExpansionCounter;
    DT_POINTER TlsBitmap;
    ULONG TlsBitmapBits[2];
    DT_POINTER ReadOnlySharedMemoryBase;
    DT_POINTER ReadOnlySharedMemoryHeap;
    DT_POINTER ReadOnlyStaticServerData;
    DT_POINTER AnsiCodePageData;
    DT_POINTER OemCodePageData;
    DT_POINTER UnicodeCaseTableData;
    ULONG NumberOfProcessors;
    ULONG NtGlobalFlag;
    LARGE_INTEGER CriticalSectionTimeout;
    DT_POINTER HeapSegmentReserve;
    DT_POINTER HeapSegmentCommit;
    DT_POINTER HeapDeCommitTotalFreeThreshold;
    DT_POINTER HeapDeCommitFreeBlockThreshold;
    ULONG NumberOfHeaps;
    ULONG MaximumNumberOfHeaps;
    DT_POINTER ProcessHeaps;
    DT_POINTER GdiSharedHandleTable;
    DT_POINTER ProcessStarterHelper;
    ULONG GdiDCAttributeList;
    DT_POINTER LoaderLock;
    ULONG OSMajorVersion;
    ULONG OSMinorVersion;
    USHORT OSBuildNumber;
    USHORT OSCSDVersion;
    ULONG OSPlatformId;
    ULONG ImageSubsystem;
    ULONG ImageSubsystemMajorVersion;
    ULONG ImageSubsystemMinorVersion;
    DT_POINTER ImageProcessAffinityMask;
    DT_GDI_HANDLE_BUFFER GdiHandleBuffer;
    DT_POINTER PostProcessInitRoutine;
    DT_POINTER TlsExpansionBitmap;
    ULONG TlsExpansionBitmapBits[32];
    ULONG SessionId;
    ULARGE_INTEGER AppCompatFlags;
    ULARGE_INTEGER AppCompatFlagsUser;
    DT_POINTER pShimData;
    DT_POINTER AppCompatInfo;
    DT_UNICODE_STRING CSDVersion;
    DT_POINTER ActivationContextData;
    DT_POINTER ProcessAssemblyStorageMap;
    DT_POINTER SystemDefaultActivationContextData;
    DT_POINTER SystemAssemblyStorageMap;
    DT_POINTER MinimumStackCommit;
    DT_POINTER FlsCallback;
    DT_LIST_ENTRY FlsListHead;
    DT_POINTER FlsBitmap;
    ULONG FlsBitmapBits[FLS_MAXIMUM_AVAILABLE / (sizeof(ULONG) * 8)];
    ULONG FlsHighIndex;
};

struct DT_PEB_LDR_DATA
{
    BYTE Reserved1[8];
    DT_POINTER Reserved2[3];
    DT_LIST_ENTRY InMemoryOrderModuleList;
};

struct DT_CURDIR
{
    DT_UNICODE_STRING DosPath;
    DT_POINTER Handle;
};

struct DT_RTL_DRIVE_LETTER_CURDIR {
    USHORT Flags;
    USHORT Length;
    ULONG TimeStamp;
    STRING DosPath;
};

#define DT_RTL_MAX_DRIVE_LETTERS 32

struct DT_RTL_USER_PROCESS_PARAMETERS
{
    ULONG MaximumLength;
    ULONG Length;
    ULONG Flags;
    ULONG DebugFlags;
    DT_POINTER ConsoleHandle;
    ULONG  ConsoleFlags;
    DT_POINTER StandardInput;
    DT_POINTER StandardOutput;
    DT_POINTER StandardError;
    DT_CURDIR CurrentDirectory;
    DT_UNICODE_STRING DllPath;
    DT_UNICODE_STRING ImagePathName;
    DT_UNICODE_STRING CommandLine;
    DT_POINTER Environment;
    ULONG StartingX;
    ULONG StartingY;
    ULONG CountX;
    ULONG CountY;
    ULONG CountCharsX;
    ULONG CountCharsY;
    ULONG FillAttribute;
    ULONG WindowFlags;
    ULONG ShowWindowFlags;
    DT_UNICODE_STRING WindowTitle;
    DT_UNICODE_STRING DesktopInfo;
    DT_UNICODE_STRING ShellInfo;
    DT_UNICODE_STRING RuntimeData;
    DT_RTL_DRIVE_LETTER_CURDIR CurrentDirectores[ DT_RTL_MAX_DRIVE_LETTERS ];
};

#endif // !FEATURE_PAL

// TODO-ARM64-NYI Support for SOS on target with 64K pages
//
// This is probably as simple as redefining DT_OS_PAGE_SIZE to be a function
// which returns the page size of the connected target
#define DT_OS_PAGE_SIZE   4096
#define DT_GC_PAGE_SIZE   0x1000

#endif // !__PLATFORM_SPECIFIC_INCLUDED
