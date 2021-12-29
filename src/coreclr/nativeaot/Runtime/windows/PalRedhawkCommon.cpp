// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Implementation of the portions of the Redhawk Platform Abstraction Layer (PAL) library that are common among
// multiple PAL variants.
//
// Note that in general we don't want to assume that Windows and Redhawk global definitions can co-exist.
// Since this code must include Windows headers to do its job we can't therefore safely include general
// Redhawk header files.
//

#include <windows.h>
#include <stdio.h>
#include <errno.h>
#include <evntprov.h>
#include "CommonTypes.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include <winternl.h>
#include "CommonMacros.h"
#include "rhassert.h"


#define REDHAWK_PALEXPORT extern "C"
#define REDHAWK_PALAPI __stdcall


// Given the OS handle of a loaded module, compute the upper and lower virtual address bounds (inclusive).
REDHAWK_PALEXPORT void REDHAWK_PALAPI PalGetModuleBounds(HANDLE hOsHandle, _Out_ uint8_t ** ppLowerBound, _Out_ uint8_t ** ppUpperBound)
{
    BYTE *pbModule = (BYTE*)hOsHandle;
    DWORD cbModule;

    IMAGE_NT_HEADERS *pNtHeaders = (IMAGE_NT_HEADERS*)(pbModule + ((IMAGE_DOS_HEADER*)hOsHandle)->e_lfanew);
    if (pNtHeaders->OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC)
        cbModule = ((IMAGE_OPTIONAL_HEADER32*)&pNtHeaders->OptionalHeader)->SizeOfImage;
    else
        cbModule = ((IMAGE_OPTIONAL_HEADER64*)&pNtHeaders->OptionalHeader)->SizeOfImage;

    *ppLowerBound = pbModule;
    *ppUpperBound = pbModule + cbModule - 1;
}

uint32_t g_RhNumberOfProcessors;

REDHAWK_PALEXPORT int32_t REDHAWK_PALAPI PalGetProcessCpuCount()
{
    ASSERT(g_RhNumberOfProcessors > 0);
    return g_RhNumberOfProcessors;
}

// Retrieves the entire range of memory dedicated to the calling thread's stack.  This does
// not get the current dynamic bounds of the stack, which can be significantly smaller than
// the maximum bounds.
REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalGetMaximumStackBounds(_Out_ void** ppStackLowOut, _Out_ void** ppStackHighOut)
{
    // VirtualQuery on the address of a local variable to get the allocation
    // base of the stack.  Then use the StackBase field in the TEB to give
    // the highest address of the stack region.
    MEMORY_BASIC_INFORMATION mbi = { 0 };
    SIZE_T cb = VirtualQuery(&mbi, &mbi, sizeof(mbi));
    if (cb != sizeof(mbi))
        return false;

    NT_TIB* pTib = (NT_TIB*)NtCurrentTeb();
    *ppStackHighOut = pTib->StackBase;      // stack base is the highest address
    *ppStackLowOut = mbi.AllocationBase;    // allocation base is the lowest address
    return true;
}

#if !defined(_INC_WINDOWS)

typedef struct _UNICODE_STRING {
    USHORT Length;
    USHORT MaximumLength;
    PWSTR  Buffer;
} UNICODE_STRING;
typedef UNICODE_STRING *PUNICODE_STRING;
typedef const UNICODE_STRING *PCUNICODE_STRING;

typedef struct _PEB_LDR_DATA {
    BYTE Reserved1[8];
    PVOID Reserved2[3];
    LIST_ENTRY InMemoryOrderModuleList;
} PEB_LDR_DATA, *PPEB_LDR_DATA;

typedef struct _LDR_DATA_TABLE_ENTRY {
    PVOID Reserved1[2];
    LIST_ENTRY InMemoryOrderLinks;
    PVOID Reserved2[2];
    PVOID DllBase;
    PVOID Reserved3[2];
    UNICODE_STRING FullDllName;
    BYTE Reserved4[8];
    PVOID Reserved5[3];
    union {
        ULONG CheckSum;
        PVOID Reserved6;
    } DUMMYUNIONNAME;
    ULONG TimeDateStamp;
} LDR_DATA_TABLE_ENTRY, *PLDR_DATA_TABLE_ENTRY;

typedef struct _PEB {
    BYTE Reserved1[2];
    BYTE BeingDebugged;
    BYTE Reserved2[1];
    PVOID Reserved3[2];
    PPEB_LDR_DATA Ldr;
    PVOID /*PRTL_USER_PROCESS_PARAMETERS*/ ProcessParameters;
    PVOID Reserved4[3];
    PVOID AtlThunkSListPtr;
    PVOID Reserved5;
    ULONG Reserved6;
    PVOID Reserved7;
    ULONG Reserved8;
    ULONG AtlThunkSListPtr32;
    PVOID Reserved9[45];
    BYTE Reserved10[96];
    PVOID /*PPS_POST_PROCESS_INIT_ROUTINE*/ PostProcessInitRoutine;
    BYTE Reserved11[128];
    PVOID Reserved12[1];
    ULONG SessionId;
} PEB, *PPEB;

typedef struct _TEB {
    PVOID Reserved1[12];
    PPEB ProcessEnvironmentBlock;
    PVOID Reserved2[399];
    BYTE Reserved3[1952];
    PVOID TlsSlots[64];
    BYTE Reserved4[8];
    PVOID Reserved5[26];
    PVOID ReservedForOle;  // Windows 2000 only
    PVOID Reserved6[4];
    PVOID TlsExpansionSlots;
} TEB, *PTEB;

#endif // !defined(_INC_WINDOWS)

// retrieves the full path to the specified module, if moduleBase is NULL retreieves the full path to the
// executable module of the current process.
//
// Return value:  number of characters in name string
//
//NOTE:  This implementation exists because calling GetModuleFileName is not wack compliant.  if we later decide
//       that the framework package containing mrt100_app no longer needs to be wack compliant, this should be
//       removed and the windows implementation of GetModuleFileName should be substitued on windows.
REDHAWK_PALEXPORT int32_t PalGetModuleFileName(_Out_ const TCHAR** pModuleNameOut, HANDLE moduleBase)
{
    TEB* pTEB = NtCurrentTeb();
    LIST_ENTRY* pStartLink = &(pTEB->ProcessEnvironmentBlock->Ldr->InMemoryOrderModuleList);
    LIST_ENTRY* pCurLink = pStartLink->Flink;

    do
    {
        LDR_DATA_TABLE_ENTRY* pEntry = CONTAINING_RECORD(pCurLink, LDR_DATA_TABLE_ENTRY, InMemoryOrderLinks);

        //null moduleBase will result in the first module being returned
        //since the module list is ordered this is the executable module of the current process
        if ((pEntry->DllBase == moduleBase) || (moduleBase == NULL))
        {
            *pModuleNameOut = pEntry->FullDllName.Buffer;
            return pEntry->FullDllName.Length / 2;
        }
        pCurLink = pCurLink->Flink;
    }
    while (pCurLink != pStartLink);

    *pModuleNameOut = NULL;
    return 0;
}

REDHAWK_PALEXPORT uint64_t REDHAWK_PALAPI PalGetTickCount64()
{
    return GetTickCount64();
}
