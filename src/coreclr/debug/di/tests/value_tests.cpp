// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Runs the real DBI value code against a mock DAC and an in-memory data target. Each test runs
// with both 4-byte and 8-byte target pointers to verify that DBI uses the target's sizes, not its own.

#include "stdafx.h"
#include "primitives.h"
#include <clrdata.h>
#include <crtdbg.h>
#include <stdio.h>
#include <stdlib.h>
#include "mockdac.generated.h"

static void Check(bool condition, const char * expression, int line)
{
    if (!condition)
    {
        fprintf(stderr, "Line %d: %s\n", line, expression);
        ThrowHR(E_FAIL);
    }
}

#define TEST_CHECK(expression) Check((expression), #expression, __LINE__)

template <typename F>
static HRESULT CaptureHR(F action)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        action();
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

template <typename T>
static T TargetHandle(CORDB_ADDRESS address)
{
    static_assert(sizeof(T) == sizeof(address));
    T handle;
    memcpy(&handle, &address, sizeof(handle));
    return handle;
}

class MockDataTarget : public ICorDebugMutableDataTarget
{
public:
    static const CORDB_ADDRESS Base = 0x10000;
    BYTE memory[256] = {};
    const BYTE * metadata = nullptr;
    DWORD metadataSize = 0;
    static const CORDB_ADDRESS MetadataBase = 0x200000;
    ULONG32 pointerSize;
    ULONG32 lastReadSize = 0;
    ULONG32 lastWriteSize = 0;
    HRESULT readResult = S_OK;
    HRESULT writeResult = S_OK;

    explicit MockDataTarget(ULONG32 size) : pointerSize(size) {}

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, void ** result) override
    {
        *result = nullptr;
        if (id != IID_IUnknown && id != IID_ICorDebugDataTarget && id != IID_ICorDebugMutableDataTarget)
            return E_NOINTERFACE;
        *result = static_cast<ICorDebugMutableDataTarget *>(this);
        AddRef();
        return S_OK;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++m_refs; }
    ULONG STDMETHODCALLTYPE Release() override { return --m_refs; }
    HRESULT STDMETHODCALLTYPE GetPlatform(CorDebugPlatform * platform) override
    {
        *platform = pointerSize == 4 ? CORDB_PLATFORM_WINDOWS_X86 : CORDB_PLATFORM_WINDOWS_AMD64;
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE ReadVirtual(CORDB_ADDRESS address, BYTE * buffer, ULONG32 count, ULONG32 * read) override
    {
        *read = 0;
        lastReadSize = count;
        if (FAILED(readResult))
            return readResult;
        if (metadata != nullptr && address >= MetadataBase && address - MetadataBase <= metadataSize &&
            count <= metadataSize - (address - MetadataBase))
        {
            memcpy(buffer, metadata + (address - MetadataBase), count);
            *read = count;
            return S_OK;
        }
        if (address < Base || address - Base > sizeof(memory) || count > sizeof(memory) - (address - Base))
            return E_FAIL;
        memcpy(buffer, memory + (address - Base), count);
        *read = count;
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetThreadContext(DWORD, ULONG32, ULONG32, BYTE *) override { return E_NOTIMPL; }
    HRESULT STDMETHODCALLTYPE WriteVirtual(CORDB_ADDRESS address, const BYTE * buffer, ULONG32 count) override
    {
        lastWriteSize = count;
        if (FAILED(writeResult))
            return writeResult;
        if (address < Base || address - Base > sizeof(memory) || count > sizeof(memory) - (address - Base))
            return E_FAIL;
        memcpy(memory + (address - Base), buffer, count);
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE SetThreadContext(DWORD, ULONG32, const BYTE *) override { return E_NOTIMPL; }
    HRESULT STDMETHODCALLTYPE ContinueStatusChanged(DWORD, CORDB_CONTINUE_STATUS) override { return S_OK; }

    void Store(CORDB_ADDRESS value)
    {
        memset(memory, 0xA5, sizeof(memory));
        memcpy(memory, &value, pointerSize);
    }

private:
    ULONG m_refs = 1;
};

// Implements only the DAC methods these tests need; MockDacBase (generated) provides the rest.
class MockDac : public MockDacBase
{
public:
    ULONG32 pointerSize;
    HRESULT targetInfoResult = S_OK;
    UINT targetInfoCalls = 0;
    HRESULT registerWriteResult = S_OK;
    CORDB_ADDRESS lastObjectAddress = 0;
    CorElementType objectType = ELEMENT_TYPE_OBJECT;
    CORDB_REGISTER registerValue = 0xA5A5A5A5;
    CORDB_REGISTER secondRegisterValue = 0x5A5A5A5A;
    CORDB_REGISTER writtenValues[2] = {};
    CorDebugRegister writtenRegisters[2] = {};
    ULONG32 registerWrites = 0;
    UINT stringLength = 7;
    SIZE_T objectSize = 0;
    ULONG32 metadataSize = 0;

    explicit MockDac(ULONG32 size) : pointerSize(size) {}

    HRESULT STDMETHODCALLTYPE FlushCache() override { return S_OK; }
    HRESULT STDMETHODCALLTYPE Destroy() override { return S_OK; }
    HRESULT STDMETHODCALLTYPE DacSetTargetConsistencyChecks(BOOL) override { return S_OK; }
    HRESULT STDMETHODCALLTYPE IsLeftSideInitialized(BOOL * result) override { *result = TRUE; return S_OK; }
    HRESULT STDMETHODCALLTYPE GetCurrentAppDomain(VMPTR_AppDomain * result) override
    {
        *result = TargetHandle<VMPTR_AppDomain>(0x20000);
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetAppDomainId(VMPTR_AppDomain, ULONG * result) override { *result = 1; return S_OK; }
    HRESULT STDMETHODCALLTYPE GetTargetInfo(TargetInfo * info) override
    {
        targetInfoCalls++;
        if (FAILED(targetInfoResult))
            return targetInfoResult;
        *info = {pointerSize == 4 ? kArchX86 : kArchAMD64, kOSWindows, pointerSize};
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetModuleData(VMPTR_Module, ModuleInfo * info) override
    {
        *info = {};
        info->vmAssembly = TargetHandle<VMPTR_Assembly>(0x30000);
        info->fIsDynamic = TRUE;
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetMetadata(VMPTR_Module, TargetBuffer * buffer) override
    {
        *buffer = TargetBuffer(MockDataTarget::MetadataBase, metadataSize);
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetTypeHandle(VMPTR_Module, mdTypeDef, VMPTR_TypeHandle * handle) override
    {
        *handle = TargetHandle<VMPTR_TypeHandle>(0x50000);
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE IsValueType(VMPTR_TypeHandle, BOOL * result) override { *result = TRUE; return S_OK; }
    HRESULT STDMETHODCALLTYPE HasTypeParams(VMPTR_TypeHandle, BOOL * result) override { *result = FALSE; return S_OK; }
    HRESULT STDMETHODCALLTYPE EnumerateClassFields(VMPTR_TypeHandle, SIZE_T * size,
        FP_FIELDDATA_CALLBACK, CALLBACK_DATA) override
    {
        *size = objectSize;
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetBasicObjectInfo(CORDB_ADDRESS address, BOOL * valid, UINT * size,
        UINT * offset, DebuggerIPCE_ExpandedTypeData * type) override
    {
        lastObjectAddress = address;
        *valid = address != 0;
        *size = 32;
        *offset = pointerSize;
        *type = {};
        type->elementType = objectType;
        if (objectType == ELEMENT_TYPE_SZARRAY)
        {
            type->ArrayTypeData.arrayRank = 1;
            type->ArrayTypeData.arrayTypeArg.elementType = ELEMENT_TYPE_I4;
        }
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetStringData(CORDB_ADDRESS address, UINT * length, UINT * offset) override
    {
        lastObjectAddress = address;
        *length = stringLength;
        *offset = pointerSize + sizeof(UINT);
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetArrayData(CORDB_ADDRESS, BOOL * valid, DacDbiArrayInfo * info) override
    {
        *valid = TRUE;
        *info = {};
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetHandleAddressFromVmHandle(VMPTR_OBJECTHANDLE, CORDB_ADDRESS * address) override
    {
        *address = MockDataTarget::Base;
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetUniqueThreadID(VMPTR_Thread, DWORD * id) override { *id = 1; return S_OK; }
    HRESULT STDMETHODCALLTYPE GetPartialUserState(VMPTR_Thread, CorDebugUserState * state) override
    {
        *state = static_cast<CorDebugUserState>(0);
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetTargetContextSize(ULONG32, ULONG32 * size) override { *size = 64; return S_OK; }
    HRESULT STDMETHODCALLTYPE IsLeafFrame(VMPTR_Thread, ContextBuffer, BOOL * result) override
    {
        *result = TRUE;
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE ConvertJitRegNumToCorDebugRegister(ULONG32, CorDebugRegister * reg, BOOL * ambient) override
    {
        *reg = REGISTER_X86_EAX;
        if (ambient != nullptr)
            *ambient = FALSE;
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE ReadRegistersFromContext(ContextBuffer, const CorDebugRegister * regs, ULONG32 count,
        CORDB_REGISTER * values) override
    {
        for (ULONG32 i = 0; i < count; i++)
            values[i] = regs[i] == REGISTER_X86_ECX ? secondRegisterValue : registerValue;
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE WriteRegistersToContext(ContextBuffer, const CorDebugRegister * regs, ULONG32 count,
        const CORDB_REGISTER * values) override
    {
        if (FAILED(registerWriteResult))
            return registerWriteResult;
        for (ULONG32 i = 0; i < count; i++)
        {
            TEST_CHECK(registerWrites < ARRAY_SIZE(writtenValues));
            writtenRegisters[registerWrites] = regs[i];
            writtenValues[registerWrites++] = values[i];
        }
        return S_OK;
    }
};

static MockDac * s_dac;

// The executable doubles as a DAC module so process creation uses the normal DBI initialization path.
extern "C" __declspec(dllexport) HRESULT STDMETHODCALLTYPE DacDbiInterfaceInstance(
    ICorDebugDataTarget *, CORDB_ADDRESS, CLRDATA_ADDRESS, IDacDbiInterface::IAllocator *,
    IDacDbiInterface::IMetaDataLookup *, IDacDbiInterface ** result, IUnknown ** legacyDac)
{
    *result = s_dac;
    s_dac->AddRef();
    *legacyDac = nullptr;
    return S_OK;
}

class Fixture
{
public:
    MockDataTarget target;
    MockDac dac;
    RSExtSmartPtr<CordbProcess> process;

    explicit Fixture(ULONG32 size) : target(size), dac(size)
    {
        PUBLIC_API_ENTRY(nullptr);
        s_dac = &dac;
        HMODULE module = nullptr;
        TEST_CHECK(GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS,
            reinterpret_cast<LPCWSTR>(&DacDbiInterfaceInstance), &module) != FALSE);
        ProcessDescriptor descriptor = ProcessDescriptor::CreateUninitialized();
        TEST_CHECK(SUCCEEDED(CordbProcess::OpenVirtualProcess(
            0x1000, &target, module, nullptr, &descriptor, nullptr, &process)));
    }

    ~Fixture()
    {
        {
            PUBLIC_API_ENTRY(nullptr);
            process->Neuter();
        }
        process.Clear();
        s_dac = nullptr;
    }

    template <typename F>
    auto Inside(F action) -> decltype(action())
    {
        PUBLIC_REENTRANT_API_ENTRY(nullptr);
        RSLockHolder stopGoLock(process->GetStopGoLock());
        RSLockHolder lock(process->GetProcessLock());
        return action();
    }

    CordbAppDomain * AppDomain() { return Inside([&] { return process->GetAppDomain(); }); }

    CordbType * Type(CorElementType elementType)
    {
        return Inside([&] {
            CordbType * type = nullptr;
            if (elementType == ELEMENT_TYPE_PTR || elementType == ELEMENT_TYPE_BYREF ||
                elementType == ELEMENT_TYPE_ARRAY || elementType == ELEMENT_TYPE_SZARRAY)
            {
                ULONG rank = elementType == ELEMENT_TYPE_ARRAY || elementType == ELEMENT_TYPE_SZARRAY ? 1 : 0;
                TEST_CHECK(CordbType::MkType(AppDomain(), elementType, rank, Type(ELEMENT_TYPE_I4), &type) == S_OK);
            }
            else if (elementType == ELEMENT_TYPE_FNPTR)
            {
                CordbType * returnType = Type(ELEMENT_TYPE_I4);
                Instantiation signature(1, &returnType);
                TEST_CHECK(CordbType::MkType(AppDomain(), elementType, &signature, &type) == S_OK);
            }
            else
            {
                TEST_CHECK(CordbType::MkType(AppDomain(), elementType, &type) == S_OK);
            }
            return type;
        });
    }

    CordbReferenceValue * Reference(CorElementType elementType)
    {
        return Inside([&] {
            return new CordbReferenceValue(AppDomain(), Type(elementType), MemoryRange(nullptr, 0),
                TargetBuffer(MockDataTarget::Base, target.pointerSize), nullptr, VMPTR_OBJECTHANDLE::NullPtr(),
                target.pointerSize);
        });
    }
};

class RecordingValueHome : public RemoteValueHome
{
public:
    ULONG32 size = 0;
    CORDB_ADDRESS value = 0;
    UINT calls = 0;
    HRESULT result = S_OK;

    explicit RecordingValueHome(CordbProcess * process)
        : RemoteValueHome(process, TargetBuffer(MockDataTarget::Base, 8)) {}

    void SetValue(MemoryRange source, CordbType *) override
    {
        calls++;
        IfFailThrow(result);
        size = static_cast<ULONG32>(source.Size());
        value = 0;
        memcpy(&value, source.StartAddress(), source.Size());
    }
};

// Places the data at the end of a page followed by an inaccessible page, so reading even one byte
// past the requested size faults immediately.
class GuardedBuffer
{
public:
    BYTE * data;

    explicit GuardedBuffer(SIZE_T size)
    {
        SYSTEM_INFO info;
        GetSystemInfo(&info);
        m_allocation = static_cast<BYTE *>(VirtualAlloc(nullptr, 2 * info.dwPageSize,
            MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE));
        TEST_CHECK(m_allocation != nullptr);
        DWORD oldProtection;
        TEST_CHECK(VirtualProtect(m_allocation + info.dwPageSize, info.dwPageSize, PAGE_NOACCESS, &oldProtection) != FALSE);
        data = m_allocation + info.dwPageSize - size;
    }

    ~GuardedBuffer() { VirtualFree(m_allocation, 0, MEM_RELEASE); }

private:
    BYTE * m_allocation;
};

static void ReferenceSizes(Fixture & fixture)
{
    const CorElementType types[] = {ELEMENT_TYPE_OBJECT, ELEMENT_TYPE_STRING, ELEMENT_TYPE_PTR,
        ELEMENT_TYPE_BYREF, ELEMENT_TYPE_TYPEDBYREF, ELEMENT_TYPE_FNPTR, ELEMENT_TYPE_ARRAY, ELEMENT_TYPE_SZARRAY};
    for (CorElementType elementType : types)
    {
        CordbType * type = fixture.Type(elementType);
        CordbReferenceValue * literal = fixture.Inside([&] {
            TEST_CHECK(CordbValue::GetSizeForType(type, kUnboxed) == fixture.target.pointerSize);
            return new CordbReferenceValue(type, fixture.target.pointerSize);
        });
        ULONG32 size = 0;
        TEST_CHECK(literal->GetSize(&size) == S_OK);
        TEST_CHECK(size == fixture.target.pointerSize);
        // Sentinel bytes on both sides verify that exactly pointerSize bytes are written.
        BYTE bytes[16];
        memset(bytes, 0xA5, sizeof(bytes));
        TEST_CHECK(literal->CopyLiteralData(bytes + 1));
        TEST_CHECK(bytes[0] == 0xA5);
        for (ULONG32 i = 1; i <= size; i++)
            TEST_CHECK(bytes[i] == 0);
        TEST_CHECK(bytes[size + 1] == 0xA5);
        TEST_CHECK(literal->SetValue(0) == E_INVALIDARG);
    }
}

static void ReferenceReads(Fixture & fixture)
{
    const CORDB_ADDRESS values[] = {0, 0x12345678, 0x80000000, UINT32_MAX,
        fixture.target.pointerSize == 8 ? 0x1234567887654321ULL : 0x76543210};
    const CorElementType types[] = {ELEMENT_TYPE_OBJECT, ELEMENT_TYPE_STRING, ELEMENT_TYPE_SZARRAY};
    for (CorElementType elementType : types)
    {
        fixture.dac.objectType = elementType;
        CordbReferenceValue * reference = fixture.Reference(
            elementType == ELEMENT_TYPE_SZARRAY ? ELEMENT_TYPE_OBJECT : elementType);
        for (CORDB_ADDRESS expected : values)
        {
            fixture.target.Store(expected);
            GuardedBuffer cached(fixture.target.pointerSize);
            memcpy(cached.data, &expected, fixture.target.pointerSize);
            TEST_CHECK(fixture.Inside([&] {
                return reference->InitRef(MemoryRange(cached.data, fixture.target.pointerSize));
            }) == S_OK);
            TEST_CHECK(fixture.dac.lastObjectAddress == expected);
            CORDB_ADDRESS actual = UINT64_MAX;
            TEST_CHECK(reference->GetValue(&actual) == S_OK);
            TEST_CHECK(actual == expected);
            TEST_CHECK(fixture.Inside([&] { return reference->InitRef(MemoryRange(nullptr, 0)); }) == S_OK);
            TEST_CHECK(fixture.target.lastReadSize == fixture.target.pointerSize);
            TEST_CHECK(fixture.dac.lastObjectAddress == expected);
            BOOL isNull = FALSE;
            TEST_CHECK(reference->IsNull(&isNull) == S_OK);
            TEST_CHECK(isNull == (expected == 0));
        }
    }
}

static void PointerReadsAndWrites(Fixture & fixture)
{
    CordbReferenceValue * reference = fixture.Reference(ELEMENT_TYPE_FNPTR);
    const CORDB_ADDRESS values[] = {UINT32_MAX, 0, 0x80000000,
        fixture.target.pointerSize == 8 ? 0x1234567887654321ULL : 0x12345678};
    for (CORDB_ADDRESS expected : values)
    {
        fixture.target.Store(expected);
        TEST_CHECK(fixture.Inside([&] { return reference->InitRef(MemoryRange(nullptr, 0)); }) == S_OK);
        TEST_CHECK(reference->m_info.objRef == expected);
        GuardedBuffer cached(fixture.target.pointerSize);
        memcpy(cached.data, &expected, fixture.target.pointerSize);
        TEST_CHECK(fixture.Inside([&] {
            return reference->InitRef(MemoryRange(cached.data, fixture.target.pointerSize));
        }) == S_OK);
        TEST_CHECK(reference->m_info.objRef == expected);
        TEST_CHECK(reference->SetValue(expected) == S_OK);
        TEST_CHECK(fixture.target.lastWriteSize == fixture.target.pointerSize);
        CORDB_ADDRESS actual = 0;
        memcpy(&actual, fixture.target.memory, fixture.target.pointerSize);
        TEST_CHECK(actual == expected);
        TEST_CHECK(fixture.target.memory[fixture.target.pointerSize] == 0xA5);
    }
    fixture.target.writeResult = E_ACCESSDENIED;
    TEST_CHECK(reference->SetValue(1) == E_ACCESSDENIED);
    TEST_CHECK(reference->m_info.objRef == values[ARRAY_SIZE(values) - 1]);
}

static void HeapReferences(Fixture & fixture)
{
    const CORDB_ADDRESS values[] = {0, 0x80000000, UINT32_MAX,
        fixture.target.pointerSize == 8 ? 0x1234567887654321ULL : 0x12345678};
    for (CORDB_ADDRESS expected : values)
    {
        CordbReferenceValue * reference = fixture.Inside([&] {
            return CordbValue::CreateHeapReferenceValue(fixture.AppDomain(), TargetHandle<VMPTR_Object>(expected));
        });
        CORDB_ADDRESS actual = UINT64_MAX;
        TEST_CHECK(reference->GetValue(&actual) == S_OK);
        TEST_CHECK(actual == expected);
        ULONG32 size = 0;
        TEST_CHECK(reference->GetSize(&size) == S_OK);
        TEST_CHECK(size == fixture.target.pointerSize);
    }
}

static void HandleReferences(Fixture & fixture)
{
    const CORDB_ADDRESS values[] = {0x80000000, UINT32_MAX,
        fixture.target.pointerSize == 8 ? 0x1234567887654321ULL : 0x12345678, 0};
    VMPTR_OBJECTHANDLE token = TargetHandle<VMPTR_OBJECTHANDLE>(0x30000);
    for (CORDB_ADDRESS expected : values)
    {
        fixture.target.Store(expected);
        RSExtSmartPtr<ICorDebugReferenceValue> reference;
        TEST_CHECK(fixture.Inside([&] {
            return CordbReferenceValue::BuildFromGCHandle(fixture.AppDomain(), token, &reference);
        }) == S_OK);
        CORDB_ADDRESS actual = UINT64_MAX;
        TEST_CHECK(reference->GetValue(&actual) == S_OK);
        TEST_CHECK(actual == expected);
        TEST_CHECK(fixture.target.lastReadSize == fixture.target.pointerSize);
        ULONG32 size = 0;
        TEST_CHECK(reference->GetSize(&size) == S_OK);
        TEST_CHECK(size == fixture.target.pointerSize);
    }
}

static void HandleRefresh(Fixture & fixture)
{
    fixture.target.Store(0x80000000);
    CordbHandleValue * handle = fixture.Inside([&] {
        CordbHandleValue * result = new CordbHandleValue(fixture.AppDomain(), fixture.Type(ELEMENT_TYPE_OBJECT),
            HANDLE_STRONG, fixture.target.pointerSize);
        TEST_CHECK(result->Init(TargetHandle<VMPTR_OBJECTHANDLE>(0x30000)) == S_OK);
        return result;
    });
    ULONG32 size = 0;
    TEST_CHECK(handle->GetSize(&size) == S_OK);
    TEST_CHECK(size == fixture.target.pointerSize);
    const CORDB_ADDRESS values[] = {UINT32_MAX,
        fixture.target.pointerSize == 8 ? 0x1234567887654321ULL : 0x80000000, 0};
    for (CORDB_ADDRESS expected : values)
    {
        fixture.target.Store(expected);
        CORDB_ADDRESS actual = UINT64_MAX;
        TEST_CHECK(handle->GetValue(&actual) == S_OK);
        TEST_CHECK(actual == expected);
        TEST_CHECK(fixture.target.lastReadSize == fixture.target.pointerSize);
    }
    // The synthetic V3 target has no IPC channel for releasing a real GC handle.
    TEST_CHECK(handle->Dispose() == E_NOTIMPL);
}

static void ReferenceAssignment(Fixture & fixture)
{
    CordbReferenceValue * reference = fixture.Reference(ELEMENT_TYPE_OBJECT);
    delete reference->m_valueHome.m_pHome;
    RecordingValueHome * home = new RecordingValueHome(fixture.process);
    reference->m_valueHome.m_pHome = home;
    const CORDB_ADDRESS values[] = {0x12345678, 0, UINT32_MAX,
        fixture.target.pointerSize == 8 ? 0x1234567887654321ULL : 0x80000000};
    for (CORDB_ADDRESS expected : values)
    {
        UINT counter = fixture.process->m_continueCounter;
        TEST_CHECK(reference->SetValue(expected) == S_OK);
        TEST_CHECK(home->size == fixture.target.pointerSize);
        TEST_CHECK(home->value == expected);
        TEST_CHECK(reference->m_info.objRef == expected);
        TEST_CHECK(fixture.process->m_continueCounter == counter + 1);
    }
    UINT calls = home->calls;
    UINT counter = fixture.process->m_continueCounter;
    if (fixture.target.pointerSize == 4)
    {
        TEST_CHECK(reference->SetValue(0x100000000ULL) == E_INVALIDARG);
        TEST_CHECK(home->calls == calls);
        TEST_CHECK(reference->m_info.objRef == values[ARRAY_SIZE(values) - 1]);
        TEST_CHECK(fixture.process->m_continueCounter == counter);
    }
    home->result = E_ACCESSDENIED;
    TEST_CHECK(reference->SetValue(1) == E_ACCESSDENIED);
    TEST_CHECK(reference->m_info.objRef == values[ARRAY_SIZE(values) - 1]);
    TEST_CHECK(fixture.process->m_continueCounter == counter);
}

static void StringAssignment(Fixture & fixture)
{
    fixture.dac.objectType = ELEMENT_TYPE_STRING;
    CordbReferenceValue * reference = fixture.Reference(ELEMENT_TYPE_STRING);
    delete reference->m_valueHome.m_pHome;
    RecordingValueHome * home = new RecordingValueHome(fixture.process);
    reference->m_valueHome.m_pHome = home;
    reference->m_info.objTypeData.elementType = ELEMENT_TYPE_STRING;
    TEST_CHECK(reference->SetValue(0x80001234) == S_OK);
    TEST_CHECK(fixture.dac.lastObjectAddress == 0x80001234);
    TEST_CHECK(reference->m_info.stringInfo.length == fixture.dac.stringLength);
    fixture.dac.stringLength = 19;
    TEST_CHECK(reference->SetValue(0x12345678) == S_OK);
    TEST_CHECK(fixture.dac.lastObjectAddress == 0x12345678);
    TEST_CHECK(reference->m_info.stringInfo.length == 19);
    TEST_CHECK(reference->SetValue(0) == S_OK);
    TEST_CHECK(reference->m_info.objRef == 0);
}

static void HandleReadsAndWrites(Fixture & fixture)
{
    PUBLIC_API_ENTRY(nullptr);
    RSLockHolder stopGoLock(fixture.process->GetStopGoLock());
    RSLockHolder lock(fixture.process->GetProcessLock());
    VMPTR_OBJECTHANDLE handle = TargetHandle<VMPTR_OBJECTHANDLE>(0x30000);
    HandleValueHome home(fixture.process, handle);
    const CORDB_ADDRESS values[] = {0x80001234, 0,
        fixture.target.pointerSize == 8 ? 0x1234567887654321ULL : UINT32_MAX};
    for (CORDB_ADDRESS expected : values)
    {
        fixture.target.Store(expected);
        BYTE bytes[16];
        memset(bytes, 0xA5, sizeof(bytes));
        home.GetValue(MemoryRange(bytes + 1, fixture.target.pointerSize));
        TEST_CHECK(fixture.target.lastReadSize == fixture.target.pointerSize);
        CORDB_ADDRESS actual = 0;
        memcpy(&actual, bytes + 1, fixture.target.pointerSize);
        TEST_CHECK(actual == expected);
        TEST_CHECK(bytes[0] == 0xA5);
        TEST_CHECK(bytes[fixture.target.pointerSize + 1] == 0xA5);

        GuardedBuffer source(fixture.target.pointerSize);
        memcpy(source.data, &expected, fixture.target.pointerSize);
        // V3 has no IPC channel. Reaching it must not overread the target-sized input first.
        TEST_CHECK(CaptureHR([&] {
            home.SetValue(MemoryRange(source.data, fixture.target.pointerSize), nullptr);
        }) == E_NOTIMPL);

        RefRemoteValueHome remoteHome(fixture.process,
            TargetBuffer(MockDataTarget::Base, fixture.target.pointerSize));
        TEST_CHECK(CaptureHR([&] {
            remoteHome.SetValue(MemoryRange(source.data, fixture.target.pointerSize), fixture.Type(ELEMENT_TYPE_OBJECT));
        }) == E_NOTIMPL);
    }
    fixture.target.readResult = E_ACCESSDENIED;
    TEST_CHECK(CaptureHR([&] {
        CORDB_ADDRESS value = 0;
        home.GetValue(MemoryRange(&value, fixture.target.pointerSize));
    }) == CORDBG_E_READVIRTUAL_FAILURE);
}

// Owns a native frame on a placeholder thread; register reads go through the mock DAC.
class NativeFrameScope
{
public:
    explicit NativeFrameScope(Fixture & fixture) : m_fixture(fixture)
    {
        HRESULT hr = CaptureHR([&] {
            m_fixture.Inside([&] {
                m_thread = new CordbThread(m_fixture.process, VMPTR_Thread::NullPtr());
                m_thread->InternalAddRef();
                m_frame = new CordbNativeFrame(m_thread, FramePointer::MakeFramePointer(nullptr),
                    nullptr, 0, 0, m_fixture.AppDomain(), &m_misc, Context());
                m_frame->InternalAddRef();
            });
        });
        if (FAILED(hr))
        {
            Release();
            ThrowHR(hr);
        }
    }

    ~NativeFrameScope()
    {
        Release();
    }

    NativeFrameScope(const NativeFrameScope &) = delete;
    NativeFrameScope & operator=(const NativeFrameScope &) = delete;

    CordbNativeFrame * Frame() const { return m_frame; }
    ContextBuffer Context() { return {m_context, sizeof(m_context)}; }

private:
    void Release()
    {
        m_fixture.Inside([&] {
            if (m_frame != nullptr)
            {
                m_frame->Neuter();
                m_frame->InternalRelease();
                m_frame = nullptr;
            }
            if (m_thread != nullptr)
            {
                m_thread->Neuter();
                m_thread->InternalRelease();
                m_thread = nullptr;
            }
        });
    }

    Fixture & m_fixture;
    BYTE m_context[64] = {};
    CordbMiscFrame m_misc;
    CordbThread * m_thread = nullptr;
    CordbNativeFrame * m_frame = nullptr;
};

static void RegisterLocals(Fixture & fixture)
{
    NativeFrameScope scope(fixture);
    CordbNativeFrame * frame = scope.Frame();
    const CorElementType types[] = {ELEMENT_TYPE_I1, ELEMENT_TYPE_I2, ELEMENT_TYPE_I4};
    const ULONG32 sizes[] = {1, 2, 4};
    for (UINT i = 0; i < ARRAY_SIZE(types); i++)
    {
        RSExtSmartPtr<ICorDebugValue> value;
        CorDebugRegister reg = fixture.target.pointerSize == 4 ? REGISTER_X86_EAX : REGISTER_AMD64_RAX;
        TEST_CHECK(fixture.Inside([&] {
            return frame->GetLocalRegisterValue(reg, fixture.Type(types[i]), &value);
        }) == S_OK);
        ULONG32 size = 0;
        TEST_CHECK(value->GetSize(&size) == S_OK);
        TEST_CHECK(size == sizes[i]);
        RSExtSmartPtr<ICorDebugGenericValue> generic;
        TEST_CHECK(value->QueryInterface(IID_ICorDebugGenericValue, reinterpret_cast<void **>(&generic)) == S_OK);
        BYTE bytes[16];
        memset(bytes, 0xCC, sizeof(bytes));
        TEST_CHECK(generic->GetValue(bytes + 1) == S_OK);
        TEST_CHECK(memcmp(bytes + 1, &fixture.dac.registerValue, size) == 0);
        TEST_CHECK(bytes[0] == 0xCC && bytes[size + 1] == 0xCC);

        fixture.Inside([&] {
            RegValueHome home(frame, reg);
            home.GetEnregisteredValue(MemoryRange(bytes + 1, size));
        });
        TEST_CHECK(bytes[size + 1] == 0xCC);
    }
}

static void TypedReferenceDereference(Fixture & fixture)
{
    const CorElementType types[] = {ELEMENT_TYPE_I1, ELEMENT_TYPE_I4, ELEMENT_TYPE_I8};
    const ULONG32 sizes[] = {1, 4, 8};
    fixture.target.Store(0x12345678);
    for (UINT i = 0; i < ARRAY_SIZE(types); i++)
    {
        DacDbiObjectData info = {};
        info.objRef = MockDataTarget::Base;
        RSExtSmartPtr<ICorDebugValue> value;
        TEST_CHECK(fixture.Inside([&] {
            return CordbReferenceValue::DereferenceCommon(fixture.AppDomain(), fixture.Type(ELEMENT_TYPE_TYPEDBYREF),
                fixture.Type(types[i]), &info, &value);
        }) == S_OK);
        TEST_CHECK(fixture.target.lastReadSize == sizes[i]);
        ULONG32 size = 0;
        TEST_CHECK(value->GetSize(&size) == S_OK);
        TEST_CHECK(size == sizes[i]);
    }
}

static void NativeIntegers(Fixture & fixture)
{
    fixture.target.Store(fixture.target.pointerSize == 4 ? 0x87654321 : 0x1234567887654321ULL);
    fixture.Inside([&] {
        RSInitHolder<CordbThread> thread(new CordbThread(fixture.process, VMPTR_Thread::NullPtr()));
        RSInitHolder<CordbEval> eval(new CordbEval(thread));
        eval->m_resultAddr = MockDataTarget::Base;

        for (CorElementType elementType : {ELEMENT_TYPE_I, ELEMENT_TYPE_U})
        {
            CordbType * type = fixture.Type(elementType);
            ULONG32 size = 0;
            TEST_CHECK(type->GetUnboxedObjectSize(&size) == S_OK);
            TEST_CHECK(size == fixture.target.pointerSize);
            TEST_CHECK(CordbValue::GetSizeForType(type, kUnboxed) == size);
            BOOL align8 = FALSE;
            TEST_CHECK(type->RequiresAlign8(&align8) == S_OK);
            TEST_CHECK(align8 == (size == 8 ? TRUE : FALSE));

            for (bool literal : {false, true})
            {
                RSExtSmartPtr<ICorDebugValue> value;
                if (literal)
                {
                    TEST_CHECK(eval->CreatePrimitiveLiteral(type, &value) == S_OK);
                }
                else
                {
                    CordbValue::CreateValueByType(fixture.AppDomain(), type, false,
                        TargetBuffer(MockDataTarget::Base, size), MemoryRange(nullptr, 0), nullptr,
                        VMPTR_DebuggerExternalMemoryOwner::NullPtr(), &value);
                }
                ULONG32 valueSize = 0;
                TEST_CHECK(value->GetSize(&valueSize) == S_OK);
                TEST_CHECK(valueSize == size);
                RSExtSmartPtr<ICorDebugGenericValue> generic;
                TEST_CHECK(value->QueryInterface(IID_ICorDebugGenericValue, reinterpret_cast<void **>(&generic)) == S_OK);
                GuardedBuffer buffer(size);
                TEST_CHECK(generic->GetValue(buffer.data) == S_OK);
                TEST_CHECK(memcmp(buffer.data, fixture.target.memory, size) == 0);
                TEST_CHECK(fixture.target.lastReadSize == size);
            }
        }
    });
}

static void NativeIntegerSizeFailure(Fixture & fixture)
{
    fixture.dac.targetInfoResult = E_ACCESSDENIED;
    for (CorElementType elementType : {ELEMENT_TYPE_I, ELEMENT_TYPE_U})
    {
        TEST_CHECK(CaptureHR([&] {
            fixture.Inside([&] { CordbValue::GetSizeForType(fixture.Type(elementType), kUnboxed); });
        }) == E_ACCESSDENIED);
    }
}

static void NativeIntegerConstructionFailure(Fixture & fixture)
{
    fixture.dac.targetInfoResult = E_ACCESSDENIED;
    fixture.target.lastReadSize = 0;
    for (CorElementType elementType : {ELEMENT_TYPE_I, ELEMENT_TYPE_U})
    {
        RSExtSmartPtr<ICorDebugValue> value;
        TEST_CHECK(CaptureHR([&] {
            fixture.Inside([&] {
                CordbValue::CreateValueByType(fixture.AppDomain(), fixture.Type(elementType), false,
                    TargetBuffer(MockDataTarget::Base, fixture.target.pointerSize), MemoryRange(nullptr, 0),
                    nullptr, VMPTR_DebuggerExternalMemoryOwner::NullPtr(), &value);
            });
        }) == E_ACCESSDENIED);
        TEST_CHECK(value == nullptr);
        TEST_CHECK(fixture.target.lastReadSize == 0);
    }
}

static void NativeIntegerLiteralFailure(Fixture & fixture)
{
    fixture.dac.targetInfoResult = E_ACCESSDENIED;
    for (CorElementType elementType : {ELEMENT_TYPE_I, ELEMENT_TYPE_U})
    {
        fixture.Inside([&] {
            RSInitHolder<CordbThread> thread(new CordbThread(fixture.process, VMPTR_Thread::NullPtr()));
            RSInitHolder<CordbEval> eval(new CordbEval(thread));
            RSExtSmartPtr<ICorDebugValue> value;
            fixture.target.lastReadSize = 0;
            TEST_CHECK(eval->CreatePrimitiveLiteral(fixture.Type(elementType), &value) == E_ACCESSDENIED);
            TEST_CHECK(value == nullptr);
            TEST_CHECK(fixture.target.lastReadSize == 0);
        });
    }
}

static void NativeIntegerAlignmentFailure(Fixture & fixture)
{
    fixture.dac.targetInfoResult = E_ACCESSDENIED;
    fixture.Inside([&] {
        for (CorElementType elementType : {ELEMENT_TYPE_I, ELEMENT_TYPE_U})
        {
            BOOL align8 = TRUE;
            TEST_CHECK(fixture.Type(elementType)->RequiresAlign8(&align8) == E_ACCESSDENIED);
            TEST_CHECK(align8 == FALSE);
        }
    });
}

static void BoxSizeFailure(Fixture & fixture)
{
    fixture.dac.targetInfoResult = E_ACCESSDENIED;
    fixture.Inside([&] {
        for (CorElementType elementType : {ELEMENT_TYPE_I, ELEMENT_TYPE_U})
        {
            RSInitHolder<CordbBoxValue> box(new CordbBoxValue(fixture.AppDomain(), fixture.Type(elementType),
                TargetBuffer(MockDataTarget::Base, fixture.target.pointerSize * 2),
                fixture.target.pointerSize * 2, fixture.target.pointerSize));
            RSExtSmartPtr<ICorDebugObjectValue> value;
            UINT calls = fixture.dac.targetInfoCalls;
            TEST_CHECK(box->GetObject(&value) == E_ACCESSDENIED);
            TEST_CHECK(value == nullptr);
            TEST_CHECK(fixture.dac.targetInfoCalls == calls + 1);
        }
    });
}

static void InvalidHomeSize(EnregisteredValueHome & home, SIZE_T size, ContextBuffer context)
{
    BYTE bytes[32];
    memset(bytes, 0xCC, sizeof(bytes));
    TEST_CHECK(CaptureHR([&] { home.GetEnregisteredValue(MemoryRange(bytes, size)); }) == E_INVALIDARG);
    for (BYTE value : bytes)
        TEST_CHECK(value == 0xCC);
    TEST_CHECK(CaptureHR([&] {
        home.SetEnregisteredValue(MemoryRange(bytes, size), context, false);
    }) == E_INVALIDARG);
}

// Fills both mock registers with values that use every byte of a target register,
// and returns them laid out low register first, as the two-part homes see them.
static void UseWideRegisterValues(Fixture & fixture, BYTE (&expected)[16])
{
    ULONG32 width = fixture.target.pointerSize;
    fixture.dac.registerValue = width == 4 ? 0x87654321 : 0x1234567887654321ULL;
    fixture.dac.secondRegisterValue = width == 4 ? 0xFEDCBA98 : 0xABCDEF01FEDCBA98ULL;
    memset(expected, 0, sizeof(expected));
    memcpy(expected, &fixture.dac.registerValue, width);
    memcpy(expected + width, &fixture.dac.secondRegisterValue, width);
}

static void SingleRegisterHome(Fixture & fixture)
{
    NativeFrameScope scope(fixture);
    fixture.Inside([&] {
        ULONG32 width = fixture.target.pointerSize;
        BYTE expected[16];
        UseWideRegisterValues(fixture, expected);
        RegValueHome single(scope.Frame(), REGISTER_X86_EAX);
        GuardedBuffer buffer(width);
        single.GetEnregisteredValue(MemoryRange(buffer.data, width));
        TEST_CHECK(memcmp(buffer.data, expected, width) == 0);
        fixture.dac.registerWrites = 0;
        single.SetEnregisteredValue(MemoryRange(buffer.data, width), scope.Context(), false);
        TEST_CHECK(fixture.dac.registerWrites == 1);
        TEST_CHECK(fixture.dac.writtenValues[0] == fixture.dac.registerValue);

        BYTE negative = 0x80;
        fixture.dac.registerWrites = 0;
        single.SetEnregisteredValue(MemoryRange(&negative, 1), scope.Context(), true);
        TEST_CHECK(fixture.dac.writtenValues[0] == static_cast<CORDB_REGISTER>(static_cast<INT64>(-128)));
        InvalidHomeSize(single, width + 1, scope.Context());

        fixture.dac.registerWriteResult = E_ACCESSDENIED;
        TEST_CHECK(CaptureHR([&] {
            single.SetEnregisteredValue(MemoryRange(&negative, 1), scope.Context(), false);
        }) == E_ACCESSDENIED);
        fixture.dac.registerWriteResult = S_OK;
    });
}

static void RegisterPairHome(Fixture & fixture)
{
    NativeFrameScope scope(fixture);
    fixture.Inside([&] {
        ULONG32 width = fixture.target.pointerSize;
        BYTE expected[16];
        UseWideRegisterValues(fixture, expected);
        RegRegValueHome pair(scope.Frame(), REGISTER_X86_ECX, REGISTER_X86_EAX);
        InvalidHomeSize(pair, 2 * width + 1, scope.Context());
        for (ULONG32 size : {width + 1, 2 * width})
        {
            GuardedBuffer output(size);
            pair.GetEnregisteredValue(MemoryRange(output.data, size));
            TEST_CHECK(memcmp(output.data, expected, size) == 0);
            fixture.dac.registerWrites = 0;
            pair.SetEnregisteredValue(MemoryRange(output.data, size), scope.Context(), false);
            TEST_CHECK(fixture.dac.registerWrites == 2);
            TEST_CHECK(fixture.dac.writtenRegisters[0] == REGISTER_X86_ECX);
            TEST_CHECK(fixture.dac.writtenRegisters[1] == REGISTER_X86_EAX);
            CORDB_REGISTER high = 0;
            memcpy(&high, expected + width, size - width);
            TEST_CHECK(fixture.dac.writtenValues[0] == high);
            TEST_CHECK(fixture.dac.writtenValues[1] == fixture.dac.registerValue);
        }
    });
}

static void RegisterMemoryHomes(Fixture & fixture)
{
    NativeFrameScope scope(fixture);
    fixture.Inside([&] {
        ULONG32 width = fixture.target.pointerSize;
        BYTE expected[16];
        UseWideRegisterValues(fixture, expected);
        GuardedBuffer output(2 * width);

        fixture.target.Store(fixture.dac.registerValue);
        RegMemValueHome regMem(scope.Frame(), REGISTER_X86_ECX, MockDataTarget::Base);
        InvalidHomeSize(regMem, 2 * width - 1, scope.Context());
        regMem.GetEnregisteredValue(MemoryRange(output.data, 2 * width));
        TEST_CHECK(fixture.target.lastReadSize == width);
        TEST_CHECK(memcmp(output.data, expected, 2 * width) == 0);
        fixture.target.Store(0);
        fixture.dac.registerWrites = 0;
        regMem.SetEnregisteredValue(MemoryRange(output.data, 2 * width), scope.Context(), false);
        TEST_CHECK(fixture.dac.writtenValues[0] == fixture.dac.secondRegisterValue);
        TEST_CHECK(fixture.target.lastWriteSize == width);
        TEST_CHECK(memcmp(fixture.target.memory, expected, width) == 0);
        TEST_CHECK(fixture.target.memory[width] == 0xA5);

        fixture.target.Store(fixture.dac.secondRegisterValue);
        MemRegValueHome memReg(scope.Frame(), REGISTER_X86_EAX, MockDataTarget::Base);
        InvalidHomeSize(memReg, 2 * width - 1, scope.Context());
        memReg.GetEnregisteredValue(MemoryRange(output.data, 2 * width));
        TEST_CHECK(fixture.target.lastReadSize == width);
        TEST_CHECK(memcmp(output.data, expected, 2 * width) == 0);
        fixture.target.Store(0);
        fixture.dac.registerWrites = 0;
        memReg.SetEnregisteredValue(MemoryRange(output.data, 2 * width), scope.Context(), false);
        TEST_CHECK(fixture.dac.writtenValues[0] == fixture.dac.registerValue);
        TEST_CHECK(fixture.target.lastWriteSize == width);
        TEST_CHECK(memcmp(fixture.target.memory, expected + width, width) == 0);
        TEST_CHECK(fixture.target.memory[width] == 0xA5);
    });
}

static void FrameRegisterReads(Fixture & fixture)
{
    NativeFrameScope scope(fixture);
    CordbNativeFrame * frame = scope.Frame();
    fixture.Inside([&] {
        ULONG32 width = fixture.target.pointerSize;
        BYTE expected[16];
        UseWideRegisterValues(fixture, expected);
        TEST_CHECK(frame->GetStackPointer() == fixture.dac.registerValue);
        TEST_CHECK(frame->GetLSStackAddress(static_cast<ICorDebugInfo::RegNum>(0), -8) ==
            fixture.dac.registerValue - 8);

        fixture.target.Store(MockDataTarget::Base + 32);
        RSExtSmartPtr<ICorDebugValue> indirect;
        fixture.target.readResult = E_ACCESSDENIED;
        TEST_CHECK(frame->GetLocalByRefMemoryValue(MockDataTarget::Base, fixture.Type(ELEMENT_TYPE_I4), &indirect) ==
            CORDBG_E_READVIRTUAL_FAILURE);
        TEST_CHECK(fixture.target.lastReadSize == width);
        fixture.target.readResult = S_OK;

        ICorDebugInfo::NativeVarInfo variable = {};
        variable.loc.vlType = ICorDebugInfo::VLT_REG;
        variable.loc.vlReg.vlrReg = static_cast<ICorDebugInfo::RegNum>(0);
        TEST_CHECK(frame->GetRegisterOrStackValue(&variable) == fixture.dac.registerValue);
        variable.loc.vlType = ICorDebugInfo::VLT_STK;
        variable.loc.vlStk.vlsBaseReg = static_cast<ICorDebugInfo::RegNum>(0);
        variable.loc.vlStk.vlsOffset = 0;
        fixture.dac.registerValue = MockDataTarget::Base;
        fixture.target.Store(fixture.dac.secondRegisterValue);
        TEST_CHECK(frame->GetRegisterOrStackValue(&variable) == fixture.dac.secondRegisterValue);
        TEST_CHECK(fixture.target.lastReadSize == width);
    });
}

static void ReturnValueTypes(Fixture & fixture)
{
    fixture.Inside([&] {
        CordbModule * module = fixture.AppDomain()->LookupOrCreateModule(
            TargetHandle<VMPTR_Assembly>(0x30000), TargetHandle<VMPTR_Module>(0x40000));
        fixture.target.metadataSize = sizeof(s_returnTypeMetadata);
        fixture.target.metadata = s_returnTypeMetadata;
        fixture.dac.metadataSize = fixture.target.metadataSize;
        for (UINT i = 0; i < ARRAY_SIZE(s_returnTypes); i++)
        {
            fixture.dac.objectSize = s_returnTypes[i].size;
            CordbClass * klass = nullptr;
            TEST_CHECK(module->LookupOrCreateClass(TokenFromRid(i + 2, mdtTypeDef), &klass) == S_OK);
            CordbType * type = nullptr;
            TEST_CHECK(CordbType::MkUnparameterizedType(fixture.AppDomain(), ELEMENT_TYPE_VALUETYPE, klass, &type) == S_OK);
            bool supported = fixture.target.pointerSize == 4 ? s_returnTypes[i].supported32 :
                sizeof(void *) == 4 ? s_returnTypes[i].supported64On32BitHost : s_returnTypes[i].supported64;
            TEST_CHECK(type->ReturnedByValue() == (supported ? S_OK : S_FALSE));
        }
        fixture.target.metadata = nullptr;
    });
}

static void HandleConstructionFailure(Fixture & fixture)
{
    PUBLIC_API_ENTRY(nullptr);
    RSLockHolder stopGoLock(fixture.process->GetStopGoLock());
    RSLockHolder lock(fixture.process->GetProcessLock());
    CordbGenericValue * value = new CordbGenericValue(fixture.AppDomain(), fixture.Type(ELEMENT_TYPE_I4),
        sizeof(INT32), TargetBuffer(MockDataTarget::Base, sizeof(INT32)), nullptr);
    fixture.dac.targetInfoResult = E_ACCESSDENIED;
    ICorDebugHandleValue * handle = reinterpret_cast<ICorDebugHandleValue *>(static_cast<UINT_PTR>(1));
    TEST_CHECK(value->InternalCreateHandle(HANDLE_STRONG, &handle) == E_ACCESSDENIED);
    TEST_CHECK(handle == nullptr);
}

static void ReferenceConstructionFailure(Fixture & fixture)
{
    fixture.dac.targetInfoResult = E_ACCESSDENIED;
    CordbReferenceValue * reference = nullptr;
    TEST_CHECK(fixture.Inside([&] {
        return CordbReferenceValue::Build(fixture.AppDomain(), fixture.Type(ELEMENT_TYPE_OBJECT),
            TargetBuffer(MockDataTarget::Base, fixture.target.pointerSize), MemoryRange(nullptr, 0),
            VMPTR_OBJECTHANDLE::NullPtr(), nullptr, &reference);
    }) == E_ACCESSDENIED);
    TEST_CHECK(reference == nullptr);
}

struct TestCase
{
    const char * name;
    void (*run)(Fixture &);
};

int __cdecl main(int argc, char ** argv)
{
    setvbuf(stdout, nullptr, _IONBF, 0);
    _set_error_mode(_OUT_TO_STDERR);
#ifdef _DEBUG
    // Turn debug asserts into an immediate failing exit instead of a dialog that would hang CI.
    _CrtSetReportHook([](int, char * message, int *) -> int {
        fprintf(stderr, "%s", message);
        _Exit(1);
    });
#endif
    if (argc != 1 && (argc != 3 || (strcmp(argv[2], "4") != 0 && strcmp(argv[2], "8") != 0)))
    {
        fprintf(stderr, "Usage: dbi_value_tests [test-name 4|8]\n");
        return 2;
    }
    const TestCase tests[] = {
        {"ReferenceSizes", ReferenceSizes},
        {"ReferenceReads", ReferenceReads},
        {"PointerReadsAndWrites", PointerReadsAndWrites},
        {"HeapReferences", HeapReferences},
        {"HandleReferences", HandleReferences},
        {"HandleRefresh", HandleRefresh},
        {"ReferenceAssignment", ReferenceAssignment},
        {"StringAssignment", StringAssignment},
        {"HandleReadsAndWrites", HandleReadsAndWrites},
        {"RegisterLocals", RegisterLocals},
        {"NativeIntegers", NativeIntegers},
        {"NativeIntegerSizeFailure", NativeIntegerSizeFailure},
        {"NativeIntegerConstructionFailure", NativeIntegerConstructionFailure},
        {"NativeIntegerLiteralFailure", NativeIntegerLiteralFailure},
        {"NativeIntegerAlignmentFailure", NativeIntegerAlignmentFailure},
        {"BoxSizeFailure", BoxSizeFailure},
        {"SingleRegisterHome", SingleRegisterHome},
        {"RegisterPairHome", RegisterPairHome},
        {"RegisterMemoryHomes", RegisterMemoryHomes},
        {"FrameRegisterReads", FrameRegisterReads},
        {"ReturnValueTypes", ReturnValueTypes},
        {"TypedReferenceDereference", TypedReferenceDereference},
        {"HandleConstructionFailure", HandleConstructionFailure},
        {"ReferenceConstructionFailure", ReferenceConstructionFailure},
    };
    UINT failures = 0;
    UINT count = 0;
    for (ULONG32 pointerSize : {4u, 8u})
    {
        for (const TestCase & test : tests)
        {
            if (argc == 3 && (strcmp(argv[1], test.name) != 0 || pointerSize != static_cast<ULONG32>(argv[2][0] - '0')))
                continue;
            printf("Running %s (%u-bit target)\n", test.name, pointerSize * 8);
            HRESULT hr = S_OK;
            EX_TRY
            {
                Fixture fixture(pointerSize);
                test.run(fixture);
            }
            EX_CATCH_HRESULT(hr);
            printf("%s (%u-bit target): %s (0x%08x)\n", test.name, pointerSize * 8,
                SUCCEEDED(hr) ? "PASS" : "FAIL", hr);
            count++;
            if (FAILED(hr))
                failures++;
        }
    }
    printf("%u tests, %u failures\n", count, failures);
    return count > 0 && failures == 0 ? 0 : 1;
}
