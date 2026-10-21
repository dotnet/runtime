// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../../src/coreclr/gc/env/common.h"
#include "../../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../../src/coreclr/gc/gcref.h"

#include <vector>

#ifdef _MSC_VER
#define P15_EXPORT extern "C" __declspec(dllexport)
#define P15_NOINLINE __declspec(noinline)
#else
#define P15_EXPORT extern "C" __attribute__((visibility("default")))
#define P15_NOINLINE __attribute__((noinline))
#endif

namespace
{
constexpr size_t ObjectCount = 2048;

std::vector<uint8_t> s_methodTableStorage;
MethodTable* s_methodTable;
std::vector<std::vector<uint8_t>> s_objectStorage;
std::vector<Object*> s_objects;

void InitializeMethodTable(
    size_t gcDescSize,
    uint32_t baseSize,
    uint32_t flags,
    uint16_t componentSize)
{
    s_methodTableStorage.assign(gcDescSize + sizeof(MethodTable), 0);
    s_methodTable = reinterpret_cast<MethodTable*>(
        s_methodTableStorage.data() + gcDescSize);
    s_methodTable->m_flags = flags;
    s_methodTable->m_baseSize = baseSize;
    if ((flags & MTFlag_HasComponentSize) != 0)
    {
        s_methodTable->m_componentSize = componentSize;
    }
}

void AllocateObjects(size_t objectSize)
{
    s_objectStorage.clear();
    s_objectStorage.resize(ObjectCount);
    s_objects.clear();
    s_objects.reserve(ObjectCount);

    for (size_t index = 0; index < ObjectCount; index++)
    {
        std::vector<uint8_t>& storage = s_objectStorage[index];
        storage.assign(objectSize, 0);
        Object* object = reinterpret_cast<Object*>(
            storage.data() + sizeof(ObjHeader));
        object->RawSetMethodTable(s_methodTable);
        s_objects.push_back(object);
    }
}

Object* ReferenceFor(size_t objectIndex, size_t slotIndex)
{
    if (((objectIndex + slotIndex) % 5) == 0)
    {
        return nullptr;
    }

    return s_objects[(objectIndex + slotIndex + 1) % ObjectCount];
}

void InitializeSparse()
{
    const size_t ObjectBytes = 8 * sizeof(void*);
    const uint32_t BaseSize =
        static_cast<uint32_t>(sizeof(ObjHeader) + ObjectBytes);
    InitializeMethodTable(
        CGCDesc::ComputeSize(3),
        BaseSize,
        MTFlag_ContainsGCPointers,
        0);

    CGCDesc::Init(s_methodTable, 3);
    CGCDescSeries* lowest =
        CGCDesc::GetCGCDescFromMT(s_methodTable)->GetLowestSeries();
    lowest[0].SetSeriesOffset(6 * sizeof(void*));
    lowest[0].SetSeriesSize(sizeof(void*) - BaseSize);
    lowest[1].SetSeriesOffset(3 * sizeof(void*));
    lowest[1].SetSeriesSize(sizeof(void*) - BaseSize);
    lowest[2].SetSeriesOffset(sizeof(void*));
    lowest[2].SetSeriesSize(sizeof(void*) - BaseSize);

    AllocateObjects(BaseSize);
    for (size_t objectIndex = 0; objectIndex < ObjectCount; objectIndex++)
    {
        Object** objectWords =
            reinterpret_cast<Object**>(s_objects[objectIndex]);
        objectWords[1] = ReferenceFor(objectIndex, 0);
        objectWords[3] = ReferenceFor(objectIndex, 1);
        objectWords[6] = ReferenceFor(objectIndex, 2);
    }
}

void InitializeDenseArray()
{
    const uint32_t ComponentCount = 256;
    const size_t DataOffset = sizeof(ArrayBase);
    const uint32_t BaseSize =
        static_cast<uint32_t>(sizeof(ObjHeader) + DataOffset);
    const size_t ObjectSize =
        BaseSize + ComponentCount * sizeof(Object*);
    InitializeMethodTable(
        CGCDesc::ComputeSize(1),
        BaseSize,
        MTFlag_ContainsGCPointers |
            MTFlag_HasComponentSize |
            MTFlag_IsArray,
        static_cast<uint16_t>(sizeof(Object*)));

    CGCDesc::Init(s_methodTable, 1);
    CGCDescSeries* series =
        CGCDesc::GetCGCDescFromMT(s_methodTable)->GetHighestSeries();
    series->SetSeriesOffset(DataOffset);
    series->SetSeriesSize(
        static_cast<size_t>(0) - (DataOffset + sizeof(ObjHeader)));

    AllocateObjects(ObjectSize);
    for (size_t objectIndex = 0; objectIndex < ObjectCount; objectIndex++)
    {
        Object* object = s_objects[objectIndex];
        *reinterpret_cast<uint32_t*>(
            reinterpret_cast<uint8_t*>(object) +
            ArrayBase::GetOffsetOfNumComponents()) = ComponentCount;
        Object** data = reinterpret_cast<Object**>(
            reinterpret_cast<uint8_t*>(object) + DataOffset);
        for (size_t slotIndex = 0; slotIndex < ComponentCount; slotIndex++)
        {
            data[slotIndex] = ReferenceFor(objectIndex, slotIndex);
        }
    }
}

void InitializeRepeatingArray()
{
    const uint32_t ComponentCount = 32;
    const size_t DataOffset = sizeof(ArrayBase);
    const size_t ComponentSize = 4 * sizeof(void*);
    const uint32_t BaseSize =
        static_cast<uint32_t>(sizeof(ObjHeader) + DataOffset);
    const size_t ObjectSize =
        BaseSize + ComponentCount * ComponentSize;
    InitializeMethodTable(
        CGCDesc::ComputeSizeRepeating(2),
        BaseSize,
        MTFlag_ContainsGCPointers |
            MTFlag_HasComponentSize |
            MTFlag_IsArray,
        static_cast<uint16_t>(ComponentSize));

    CGCDesc::InitValueClassSeries(s_methodTable, 2);
    CGCDescSeries* series =
        CGCDesc::GetCGCDescFromMT(s_methodTable)->GetHighestSeries();
    series->SetSeriesOffset(DataOffset);
    (series->val_serie + 0)->set_val_serie_item(
        1,
        static_cast<HALF_SIZE_T>(sizeof(void*)));
    (series->val_serie - 1)->set_val_serie_item(
        1,
        static_cast<HALF_SIZE_T>(sizeof(void*)));

    AllocateObjects(ObjectSize);
    for (size_t objectIndex = 0; objectIndex < ObjectCount; objectIndex++)
    {
        Object* object = s_objects[objectIndex];
        *reinterpret_cast<uint32_t*>(
            reinterpret_cast<uint8_t*>(object) +
            ArrayBase::GetOffsetOfNumComponents()) = ComponentCount;
        Object** data = reinterpret_cast<Object**>(
            reinterpret_cast<uint8_t*>(object) + DataOffset);
        for (size_t component = 0; component < ComponentCount; component++)
        {
            data[component * 4] =
                ReferenceFor(objectIndex, component * 2);
            data[component * 4 + 2] =
                ReferenceFor(objectIndex, component * 2 + 1);
        }
    }
}

P15_NOINLINE void ConsumeSlot(Object** slot, uintptr_t* checksum)
{
    Object* target = *slot;
    if (target != nullptr)
    {
        *checksum += reinterpret_cast<uintptr_t>(
            target->GetGCSafeMethodTable());
    }
}

void EnumerateWithCallback(Object* object, uintptr_t* checksum)
{
    MethodTable* methodTable = object->GetGCSafeMethodTable();
    size_t objectSize = object->GetSize();
    CGCDesc* map = CGCDesc::GetCGCDescFromMT(methodTable);
    CGCDescSeries* current = map->GetHighestSeries();
    ptrdiff_t seriesCount = static_cast<ptrdiff_t>(map->GetNumSeries());

    if (seriesCount > 0)
    {
        CGCDescSeries* last = map->GetLowestSeries();
        while (true)
        {
            Object** slot = reinterpret_cast<Object**>(
                reinterpret_cast<uint8_t*>(object) +
                current->GetSeriesOffset());
            Object** end = reinterpret_cast<Object**>(
                reinterpret_cast<uint8_t*>(slot) +
                current->GetSeriesSize() +
                objectSize);
            while (slot < end)
            {
                ConsumeSlot(slot, checksum);
                slot++;
            }

            if (current == last)
            {
                break;
            }
            current--;
        }
        return;
    }

    Object** slot = reinterpret_cast<Object**>(
        reinterpret_cast<uint8_t*>(object) + current->GetSeriesOffset());
    uint32_t componentCount =
        static_cast<ArrayBase*>(object)->GetNumComponents();
    for (uint32_t component = 0; component < componentCount; component++)
    {
        for (ptrdiff_t seriesIndex = 0;
             seriesIndex > seriesCount;
             seriesIndex--)
        {
            val_serie_item* item = current->val_serie + seriesIndex;
            Object** end = slot + item->nptrs;
            while (slot < end)
            {
                ConsumeSlot(slot, checksum);
                slot++;
            }
            slot = reinterpret_cast<Object**>(
                reinterpret_cast<uint8_t*>(end) + item->skip);
        }
    }
}

struct RangeVisitor
{
    uintptr_t checksum;

    FORCEINLINE bool operator()(Object** start, size_t count)
    {
        for (size_t index = 0; index < count; index++)
        {
            Object* target = start[index];
            if (target != nullptr)
            {
                checksum += reinterpret_cast<uintptr_t>(
                    target->GetGCSafeMethodTable());
            }
        }
        return true;
    }
};
}

P15_EXPORT int P15_Initialize(int scenario)
{
    switch (scenario)
    {
        case 0:
            InitializeSparse();
            break;
        case 1:
            InitializeDenseArray();
            break;
        case 2:
            InitializeRepeatingArray();
            break;
        default:
            return 0;
    }

    return static_cast<int>(s_objects.size());
}

P15_EXPORT uintptr_t P15_ScanCallbacks()
{
    uintptr_t checksum = 0;
    for (Object* object : s_objects)
    {
        EnumerateWithCallback(object, &checksum);
    }
    return checksum;
}

P15_EXPORT uintptr_t P15_ScanRanges()
{
    uintptr_t checksum = 0;
    for (Object* object : s_objects)
    {
        GCReferenceRangeIterator iterator(object);
        GCReferenceRange range = {};
        while (iterator.Next(&range))
        {
            for (size_t index = 0; index < range.count; index++)
            {
                Object* target = range.start[index];
                if (target != nullptr)
                {
                    checksum += reinterpret_cast<uintptr_t>(
                        target->GetGCSafeMethodTable());
                }
            }
        }
    }
    return checksum;
}

P15_EXPORT uintptr_t P15_ScanRangeVisitor()
{
    RangeVisitor visitor = {};
    for (Object* object : s_objects)
    {
        bool completed = GCReferenceRanges::Enumerate(object, visitor);
        if (!completed)
        {
            return 0;
        }
    }
    return visitor.checksum;
}
