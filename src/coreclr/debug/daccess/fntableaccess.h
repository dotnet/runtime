// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#ifdef _MSC_VER
#pragma once
#endif

#ifndef _FN_TABLE_ACCESS_H
#define _FN_TABLE_ACCESS_H


#if !defined(TARGET_X86)

#ifndef TARGET_UNIX
#define DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO
#endif // !TARGET_UNIX

#ifndef USE_INDIRECT_CODEHEADER
#define USE_INDIRECT_CODEHEADER
#endif  // USE_INDIRECT_CODEHEADER
#endif


struct FakeEEJitManager
{
    LPVOID      __VFN_table;
    LPVOID      m_runtimeSupport;
    LPVOID      m_pCodeHeap;
    // Nothing after this point matters: we only need the correct offset of m_pCodeHeap.
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
    size_t              reserveForJumpStubs;
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

#ifdef DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO

struct FakeStubUnwindInfoHeaderSuffix
{
    UCHAR nUnwindInfoSize;
};

// Variable-sized struct that preceeds a Stub when the stub requires unwind
// information.  Followed by a StubUnwindInfoHeaderSuffix.
struct FakeStubUnwindInfoHeader
{
    FakeStubUnwindInfoHeader *pNext;
    T_RUNTIME_FUNCTION FunctionEntry;
    UNWIND_INFO UnwindInfo;  // variable length
};

// List of stub address ranges, in increasing address order.
struct FakeStubUnwindInfoHeapSegment
{
    PBYTE pbBaseAddress;
    SIZE_T cbSegment;
    FakeStubUnwindInfoHeader *pUnwindHeaderList;
    FakeStubUnwindInfoHeapSegment *pNext;
};

#define FAKE_STUB_EXTERNAL_ENTRY_BIT 0x40000000
#define FAKE_STUB_UNWIND_INFO_BIT   0x08000000

#ifdef _DEBUG
#define FAKE_STUB_SIGNATURE         0x42555453
#endif

struct FakeStub
{
    ULONG   m_refcount;
    ULONG   m_patchOffset;

    UINT    m_numCodeBytes;
#ifdef _DEBUG
    UINT32  m_signature;
#else
#ifdef HOST_64BIT
    //README ALIGNMENT: in retail mode UINT m_numCodeBytes does not align to 16byte for the code
    //                   after the Stub struct. This is to pad properly
    UINT    m_pad_code_bytes;
#endif // HOST_64BIT
#endif // _DEBUG
};

#endif // DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO


enum FakeEEDynamicFunctionTableType
{
    FAKEDYNFNTABLE_JIT = 0,
    FAKEDYNFNTABLE_STUB = 1,
};


#ifdef CHECK_DUPLICATED_STRUCT_LAYOUTS

//
// These are the fields of the above structs that we use.
// We need to assert that their layout matches the layout
// in the EE.
//
class CheckDuplicatedStructLayouts
{
#define CHECK_OFFSET(cls, fld) CPP_ASSERT(cls##fld, offsetof(Fake##cls, fld) == offsetof(cls, fld))

    CHECK_OFFSET(EEJitManager, m_pCodeHeap);

    CHECK_OFFSET(HeapList, hpNext);
    CHECK_OFFSET(HeapList, startAddress);
    CHECK_OFFSET(HeapList, endAddress);
    CHECK_OFFSET(HeapList, mapBase);
    CHECK_OFFSET(HeapList, pHdrMap);

#if !defined(TARGET_X86)
    CHECK_OFFSET(RealCodeHeader,    nUnwindInfos);
    CHECK_OFFSET(RealCodeHeader,    unwindInfos);
#endif  // !TARGET_X86

#ifdef DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO
    CHECK_OFFSET(StubUnwindInfoHeader, pNext);

    CHECK_OFFSET(StubUnwindInfoHeapSegment, pbBaseAddress);
    CHECK_OFFSET(StubUnwindInfoHeapSegment, cbSegment);
    CHECK_OFFSET(StubUnwindInfoHeapSegment, pUnwindHeaderList);
    CHECK_OFFSET(StubUnwindInfoHeapSegment, pNext);


    CHECK_OFFSET(Stub, m_refcount);
    CHECK_OFFSET(Stub, m_patchOffset);
    CHECK_OFFSET(Stub, m_numCodeBytes);
#ifdef _DEBUG
    CHECK_OFFSET(Stub, m_signature);
#endif // _DEBUG

#endif // DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO

#undef CHECK_OFFSET

#ifdef DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO

    static_assert_no_msg(       Stub::EXTERNAL_ENTRY_BIT
             == FAKE_STUB_EXTERNAL_ENTRY_BIT);

    static_assert_no_msg(       Stub::UNWIND_INFO_BIT
             == FAKE_STUB_UNWIND_INFO_BIT);

#ifdef _DEBUG
    static_assert_no_msg(   FAKE_STUB_SIGNATURE
             == Stub::kUsedStub);
#endif

#endif // DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO
};

#ifdef DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO

static_assert_no_msg(   FAKEDYNFNTABLE_JIT
         ==     DYNFNTABLE_JIT);

static_assert_no_msg(   FAKEDYNFNTABLE_STUB
         ==     DYNFNTABLE_STUB);

#endif // DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO

#else // CHECK_DUPLICATED_STRUCT_LAYOUTS

BOOL WINAPI             DllMain(HINSTANCE hDLL, DWORD dwReason, LPVOID pReserved);
//NTSTATUS                OutOfProcessFindHeader(HANDLE hProcess, DWORD_PTR pMapIn, DWORD_PTR addr, DWORD_PTR &codeHead);
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
