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

extern "C" DLL_EXPORT int __cdecl ValidatePgoSnapshot(int schemaAlignment)
{
    ICorJitInfo::PgoInstrumentationSchema schemas[4] = {};
    schemas[0].InstrumentationKind = ICorJitInfo::PgoInstrumentationKind::BasicBlockIntCount;
    schemas[0].Count = 1;
    schemas[1].InstrumentationKind = ICorJitInfo::PgoInstrumentationKind::ValueHistogramIntCount;
    schemas[1].ILOffset = 1;
    schemas[1].Count = 1;
    schemas[2].InstrumentationKind = ICorJitInfo::PgoInstrumentationKind::ValueHistogram;
    schemas[2].ILOffset = 1;
    schemas[2].Count = ICorJitInfo::HandleHistogram32::SIZE;
    schemas[3].InstrumentationKind = ICorJitInfo::PgoInstrumentationKind::BasicBlockIntCount;
    schemas[3].ILOffset = 2;
    schemas[3].Count = 1;

    std::vector<uint8_t> compressedSchema;
    if (!WriteInstrumentationSchemaToBytes(schemas, ARRAY_SIZE(schemas), [&compressedSchema](uint8_t value)
    {
        compressedSchema.push_back(value);
        return true;
    }))
    {
        printf("Unable to write PGO schema\n");
        return 1;
    }

    if (schemaAlignment != 0 && schemaAlignment != 4)
    {
        printf("Unexpected schema alignment: %d\n", schemaAlignment);
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

    // Leave guard bytes after the payload so the unfixed overread produces a value
    // mismatch instead of depending on whether the next page happens to be mapped.
    alignas(8) uint8_t data[512];
    size_t dataSize = AlignUp(schemas[3].Offset + sizeof(uint32_t), sizeof(size_t));
    if (dataSize + sizeof(size_t) > sizeof(data))
    {
        printf("PGO fixture exceeds its data buffer\n");
        return 1;
    }
    memset(data, 0xCC, sizeof(data));
    memcpy(data, compressedSchema.data(), compressedSchema.size());

    uint32_t blockCount = 0x12345678;
    uint32_t histogramCount = 0x27182818;
    uint32_t trailingCount = 0x89ABCDEF;
    memcpy(data + schemas[0].Offset, &blockCount, sizeof(blockCount));
    memcpy(data + schemas[1].Offset, &histogramCount, sizeof(histogramCount));
    for (int32_t i = 0; i < schemas[2].Count; i++)
    {
        uint64_t value = 0x1122334455667788ULL + i;
        memcpy(data + schemas[2].Offset + i * sizeof(value), &value, sizeof(value));
    }
    memcpy(data + schemas[3].Offset, &trailingCount, sizeof(trailingCount));

    TestSchemaArray schemaArray;
    uint8_t* allocatedData;
    ICorJitInfo::PgoInstrumentationSchema* snapshotSchema;
    uint32_t schemaCount;
    uint8_t* snapshotData;
    if (!SnapshotPgoInstrumentationData(data, countsOffset, &schemaArray, &allocatedData,
                                       &snapshotSchema, &schemaCount, &snapshotData))
    {
        printf("Unable to snapshot PGO data\n");
        return 1;
    }
    std::unique_ptr<uint8_t[]> snapshot(allocatedData);

    if (schemaCount != ARRAY_SIZE(schemas))
    {
        printf("Expected %zu schema entries, got %u\n", ARRAY_SIZE(schemas), schemaCount);
        return 1;
    }

    for (uint32_t i = 0; i < schemaCount; i++)
    {
        const ICorJitInfo::PgoInstrumentationSchema& expected = schemas[i];
        const ICorJitInfo::PgoInstrumentationSchema& actual = snapshotSchema[i];
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

    printf("Snapshot validated: countsOffset %% 8 = %zu, %u schema entries\n", countsOffset % 8, schemaCount);
    return 0;
}
