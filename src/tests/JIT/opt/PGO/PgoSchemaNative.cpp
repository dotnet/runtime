// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>
#include <assert.h>
#include <memory>
#include <vector>

#ifndef WINDOWS
using INT_PTR = intptr_t;
using UINT = unsigned int;
#endif

#ifndef _ASSERTE
#define _ASSERTE assert
#endif

static constexpr size_t TARGET_POINTER_SIZE = sizeof(void*);

static size_t AlignUp(size_t value, size_t alignment)
{
    return (value + alignment - 1) & ~(alignment - 1);
}

#include "corjit.h"
#include "pgo_formatprocessing.h"

class TestSchemaArray
{
    std::vector<ICorJitInfo::PgoInstrumentationSchema> m_schemas;

public:
    void Append(const ICorJitInfo::PgoInstrumentationSchema& schema)
    {
        m_schemas.push_back(schema);
    }

    size_t GetCount() const
    {
        return m_schemas.size();
    }

    const ICorJitInfo::PgoInstrumentationSchema* GetElements() const
    {
        return m_schemas.data();
    }

    const ICorJitInfo::PgoInstrumentationSchema& operator[](size_t index) const
    {
        return m_schemas[index];
    }
};

#ifdef WINDOWS
// Returns a buffer that is immediately followed by an inaccessible page, the way a loader heap block can end at the
// end of its committed memory. Reading past the buffer faults.
static uint8_t* AllocateBufferBeforeGuardPage(size_t size, void** region)
{
    SYSTEM_INFO info;
    GetSystemInfo(&info);
    size_t pageSize = info.dwPageSize;
    size_t committedSize = AlignUp(size, pageSize);
    uint8_t* allocation = static_cast<uint8_t*>(VirtualAlloc(nullptr, committedSize + pageSize, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE));
    if (allocation == nullptr)
    {
        return nullptr;
    }

    DWORD oldProtect;
    if (!VirtualProtect(allocation + committedSize, pageSize, PAGE_NOACCESS, &oldProtect))
    {
        VirtualFree(allocation, 0, MEM_RELEASE);
        return nullptr;
    }

    *region = allocation;
    return allocation + committedSize - size;
}

static void FreeBuffer(void* region)
{
    VirtualFree(region, 0, MEM_RELEASE);
}

static int FilterAccessViolation(EXCEPTION_POINTERS* exceptionPointers, uintptr_t* faultAddress)
{
    const EXCEPTION_RECORD* record = exceptionPointers->ExceptionRecord;
    if (record->ExceptionCode != EXCEPTION_ACCESS_VIOLATION)
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    *faultAddress = static_cast<uintptr_t>(record->ExceptionInformation[1]);
    return EXCEPTION_EXECUTE_HANDLER;
}
#else
// There is no guard page here; an over-read is still caught because it shifts the data compared below.
static uint8_t* AllocateBufferBeforeGuardPage(size_t size, void** region)
{
    *region = malloc(size);
    return static_cast<uint8_t*>(*region);
}

static void FreeBuffer(void* region)
{
    free(region);
}
#endif

struct BufferHolder
{
    void* region = nullptr;

    ~BufferHolder()
    {
        if (region != nullptr)
        {
            FreeBuffer(region);
        }
    }
};

// Reports a fault while reading the data as a failure that names the address, instead of crashing the test process.
static bool TrySnapshot(const uint8_t* data,
                        size_t countsOffset,
                        TestSchemaArray* schemas,
                        uint8_t** allocatedData,
                        ICorJitInfo::PgoInstrumentationSchema** snapshotSchema,
                        uint32_t* schemaCount,
                        uint8_t** snapshotData,
                        uintptr_t* faultAddress)
{
    *faultAddress = 0;
#ifdef WINDOWS
    __try
    {
        return SnapshotPgoInstrumentationData(data, countsOffset, schemas, allocatedData, snapshotSchema, schemaCount, snapshotData);
    }
    __except (FilterAccessViolation(GetExceptionInformation(), faultAddress))
    {
        return false;
    }
#else
    return SnapshotPgoInstrumentationData(data, countsOffset, schemas, allocatedData, snapshotSchema, schemaCount, snapshotData);
#endif
}

// On 32 bit targets countsOffset is only 4 byte aligned (schemaAlignment 4). Restarting the layout at the counters then
// moved the value histogram by four bytes: earlier with one leading block counter, later with two, which read past the
// end of the data.
extern "C" DLL_EXPORT int __cdecl ValidatePgoSnapshot(int schemaAlignment, int leadingCounters)
{
    if ((schemaAlignment != 0 && schemaAlignment != 4) || leadingCounters < 1)
    {
        printf("Unexpected arguments: schemaAlignment=%d, leadingCounters=%d\n", schemaAlignment, leadingCounters);
        return 1;
    }

    std::vector<ICorJitInfo::PgoInstrumentationSchema> schemas;
    auto addSchema = [&schemas](ICorJitInfo::PgoInstrumentationKind kind, int32_t count)
    {
        ICorJitInfo::PgoInstrumentationSchema schema = {};
        schema.InstrumentationKind = kind;
        schema.ILOffset = static_cast<int32_t>(schemas.size());
        schema.Count = count;
        schemas.push_back(schema);
    };

    for (int i = 0; i < leadingCounters; i++)
    {
        addSchema(ICorJitInfo::PgoInstrumentationKind::BasicBlockIntCount, 1);
    }
    addSchema(ICorJitInfo::PgoInstrumentationKind::ValueHistogramIntCount, 1);
    addSchema(ICorJitInfo::PgoInstrumentationKind::ValueHistogram, ICorJitInfo::HandleHistogram32::SIZE);
    addSchema(ICorJitInfo::PgoInstrumentationKind::BasicBlockIntCount, 1);

    std::vector<uint8_t> compressedSchema;
    if (!WriteInstrumentationSchemaToBytes(schemas.data(), schemas.size(), [&compressedSchema](uint8_t value)
    {
        compressedSchema.push_back(value);
        return true;
    }))
    {
        printf("Unable to write PGO schema\n");
        return 1;
    }

    size_t countsOffset = AlignUp(compressedSchema.size(), 8) + schemaAlignment;
    ICorJitInfo::PgoInstrumentationSchema previous = {};
    previous.Offset = static_cast<uint32_t>(countsOffset);
    for (ICorJitInfo::PgoInstrumentationSchema& schema : schemas)
    {
        LayoutPgoInstrumentationSchema(previous, &schema);
        previous = schema;
    }

    // The allocator rounds the data region up to whole size_t units, which is what the snapshot copies.
    const ICorJitInfo::PgoInstrumentationSchema& last = schemas.back();
    size_t dataSize = AlignUp(last.Offset + last.Count * InstrumentationKindToSize(last.InstrumentationKind), sizeof(size_t));

    BufferHolder buffer;
    uint8_t* data = AllocateBufferBeforeGuardPage(dataSize, &buffer.region);
    if (data == nullptr)
    {
        printf("Unable to allocate the PGO data\n");
        return 1;
    }

    memset(data, 0xCC, dataSize);
    memcpy(data, compressedSchema.data(), compressedSchema.size());
    for (size_t i = 0; i < schemas.size(); i++)
    {
        uint32_t entrySize = InstrumentationKindToSize(schemas[i].InstrumentationKind);
        for (int32_t j = 0; j < schemas[i].Count; j++)
        {
            // Distinct for every entry, so data read from the wrong place cannot match.
            uint64_t value = 0x9E3779B97F4A7C15ULL * (i + 1) + 0x0123456789ABCDEFULL * (j + 1);
            memcpy(data + schemas[i].Offset + j * entrySize, &value, entrySize);
        }
    }

    TestSchemaArray schemaArray;
    uint8_t* allocatedData = nullptr;
    ICorJitInfo::PgoInstrumentationSchema* snapshotSchema = nullptr;
    uint32_t schemaCount = 0;
    uint8_t* snapshotData = nullptr;
    uintptr_t faultAddress = 0;
    if (!TrySnapshot(data, countsOffset, &schemaArray, &allocatedData, &snapshotSchema, &schemaCount, &snapshotData, &faultAddress))
    {
        if (faultAddress != 0)
        {
            printf("Access violation reading %p; the PGO data ends at %p (countsOffset %% 8 = %zu, %d leading counters)\n",
                   reinterpret_cast<void*>(faultAddress), static_cast<void*>(data + dataSize), countsOffset % 8, leadingCounters);
        }
        else
        {
            printf("Unable to snapshot PGO data\n");
        }

        return 1;
    }

    std::unique_ptr<uint8_t[]> snapshot(allocatedData);

    if (schemaCount != schemas.size())
    {
        printf("Expected %zu schema entries, got %u\n", schemas.size(), schemaCount);
        return 1;
    }

    for (uint32_t i = 0; i < schemaCount; i++)
    {
        const ICorJitInfo::PgoInstrumentationSchema& expected = schemas[i];
        const ICorJitInfo::PgoInstrumentationSchema& actual = snapshotSchema[i];
        // The comparison below reads each entry through the snapshot's offsets, so a misplaced entry shows up as a mismatch.
        if (actual.InstrumentationKind != expected.InstrumentationKind ||
            actual.ILOffset != expected.ILOffset || actual.Count != expected.Count || actual.Other != expected.Other)
        {
            printf("Schema entry %u changed during snapshot\n", i);
            return 1;
        }

        size_t entrySize = expected.Count * InstrumentationKindToSize(expected.InstrumentationKind);
        if (memcmp(snapshotData + actual.Offset, data + expected.Offset, entrySize) != 0)
        {
            printf("Schema entry %u has shifted data with countsOffset %% 8 = %zu\n", i, countsOffset % 8);
            return 1;
        }
    }

    printf("Snapshot validated: countsOffset %% 8 = %zu, %d leading counters\n", countsOffset % 8, leadingCounters);
    return 0;
}
