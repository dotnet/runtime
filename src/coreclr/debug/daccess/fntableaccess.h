// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#ifdef _MSC_VER
#pragma once
#endif

#ifndef _FN_TABLE_ACCESS_H
#define _FN_TABLE_ACCESS_H


struct FakeEEJitManager
{
    LPVOID      __VFN_table;
    LPVOID      m_runtimeSupport;
    LPVOID      m_pAllCodeHeaps;
    // Nothing after this point matters: we only need the correct offset of m_pAllCodeHeaps.
};

struct FakeHeapList
{
    FakeHeapList*       hpNext;
    LPVOID              pHeap;          // changed type from LoaderHeap*
    DWORD_PTR           startAddress;   // changed from PBYTE
    DWORD_PTR           endAddress;     // changed from PBYTE
    DWORD_PTR           mapBase;        // changed from PBYTE
    DWORD_PTR           pHdrMap;        // changed from DWORD*
    size_t              maxCodeHeapSize;
    DWORD_PTR           pLoaderAllocator;
#if defined(TARGET_64BIT)
    DWORD_PTR           CLRPersonalityRoutine;
#endif

    DWORD_PTR GetModuleBase()
    {
#if defined(TARGET_64BIT)
        return CLRPersonalityRoutine;
#else
        return mapBase;
#endif
    }
};

typedef struct _FakeHpRealCodeHdr
{
    LPVOID              phdrDebugInfo;
    LPVOID              phdrJitEHInfo;  // changed from EE_ILEXCEPTION*
    LPVOID              phdrJitGCInfo;  // changed from BYTE*
#if defined (FEATURE_GDBJIT)
    LPVOID              pCalledMethods;
#endif
    LPVOID              hdrMDesc;       // changed from MethodDesc*
    DWORD               nUnwindInfos;
    T_RUNTIME_FUNCTION  unwindInfos[0];
} FakeRealCodeHeader;

typedef struct _FakeHpCodeHdr
{
    LPVOID              pRealCodeHeader;
} FakeCodeHeader;

#define FAKE_STUB_CODE_BLOCK_LAST 0xF


#ifdef CHECK_DUPLICATED_STRUCT_LAYOUTS

//
// These are the fields of the above structs that we use.
// We need to assert that their layout matches the layout
// in the EE.
//
class CheckDuplicatedStructLayouts
{
#define CHECK_OFFSET(cls, fld) static_assert(offsetof(Fake##cls, fld) == offsetof(cls, fld))

    CHECK_OFFSET(EEJitManager, m_pAllCodeHeaps);

    CHECK_OFFSET(HeapList, hpNext);
    CHECK_OFFSET(HeapList, startAddress);
    CHECK_OFFSET(HeapList, endAddress);
    CHECK_OFFSET(HeapList, mapBase);
    CHECK_OFFSET(HeapList, pHdrMap);

#if !defined(TARGET_X86)
    CHECK_OFFSET(RealCodeHeader,    nUnwindInfos);
    CHECK_OFFSET(RealCodeHeader,    unwindInfos);
#endif  // !TARGET_X86

#if defined(TARGET_WINDOWS) && defined(TARGET_64BIT)
    // Keep the descriptor-free reader in mscordaccore_enummemory/FunctionTableCallbacks.cs in sync.
    static_assert(offsetof(DYNAMIC_FUNCTION_TABLE, MinimumAddress) == 0x20);
    static_assert(offsetof(DYNAMIC_FUNCTION_TABLE, Context) == 0x40);
    static_assert(offsetof(EEJitManager, m_pAllCodeHeaps) == 0x10);
    static_assert(offsetof(HeapList, hpNext) == 0x00);
    static_assert(offsetof(HeapList, startAddress) == 0x10);
    static_assert(offsetof(HeapList, endAddress) == 0x18);
    static_assert(offsetof(HeapList, mapBase) == 0x20);
    static_assert(offsetof(HeapList, pHdrMap) == 0x28);
    static_assert(offsetof(HeapList, CLRPersonalityRoutine) == 0x40);
    static_assert(offsetof(RealCodeHeader, nUnwindInfos) == 0x20);
    static_assert(offsetof(RealCodeHeader, unwindInfos) == 0x24);
    static_assert(sizeof(CodeHeader) == sizeof(uint64_t));
    static_assert(STUB_CODE_BLOCK_LAST == 0xf);
    static_assert(VALUE_MASK == 0x7FFFFFFFFFFFFFFFLL);
    static_assert(BYTES_PER_BUCKET == 32);
    static_assert(CODE_ALIGN == 4);
    static_assert(NIBBLES_PER_DWORD == 8);
    static_assert(NIBBLE_SIZE == 4);
    static_assert(NIBBLE_MASK == 0xf);
#if defined(TARGET_AMD64)
    static_assert(sizeof(T_RUNTIME_FUNCTION) == 12);
#elif defined(TARGET_ARM64)
    static_assert(sizeof(T_RUNTIME_FUNCTION) == 8);
#endif // TARGET_AMD64
#endif // TARGET_WINDOWS && TARGET_64BIT

#undef CHECK_OFFSET
};

#else // CHECK_DUPLICATED_STRUCT_LAYOUTS

extern "C" NTSTATUS     OutOfProcessFunctionTableCallback(IN HANDLE hProcess, IN PVOID TableAddress, OUT PULONG pnEntries, OUT PT_RUNTIME_FUNCTION* ppFunctions);


// OutOfProcessFunctionTableCallbackEx is like the standard OS-defined OutOfProcessFunctionTableCallback, but rather
// than take a handle to a process, it takes a callback function which can read from the target.  This allows the API to work on
// targets other than live processes (such as TTT trace files).
// pUserContext is passed directly to fpReadMemory, and the semantics of all other ReadMemoryFunction arguments (and return value) are
// the same as those for kernel32!ReadProcessMemory.
typedef BOOL (ReadMemoryFunction)(PVOID pUserContext, LPCVOID lpBaseAddress, PVOID lpBuffer, SIZE_T nSize, SIZE_T* lpNumberOfBytesRead);
extern "C" NTSTATUS     OutOfProcessFunctionTableCallbackEx(IN ReadMemoryFunction fpReadMemory, IN PVOID pUserContext, IN PVOID TableAddress, OUT PULONG pnEntries, OUT PT_RUNTIME_FUNCTION* ppFunctions);

#endif // CHECK_DUPLICATED_STRUCT_LAYOUTS

#endif //_FN_TABLE_ACCESS_H
