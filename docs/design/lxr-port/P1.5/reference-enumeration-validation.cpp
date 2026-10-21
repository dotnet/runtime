// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/gcref.h"

#include <vector>

static_assert(GC_INTERFACE_MAJOR_VERSION == 5);
static_assert(GC_INTERFACE_MINOR_VERSION >= GC_OBJECT_REFERENCE_ENUMERATION_INTERFACE_MINOR_VERSION);
static_assert(GC_OBJECT_REFERENCE_ENUMERATION_INTERFACE_MINOR_VERSION == 14);
static_assert(GC_ALLOCATION_NOTIFICATION_INTERFACE_MINOR_VERSION == 13);
static_assert(GC_WRITE_BARRIER_BULK_SCAN_INTERFACE_MINOR_VERSION == 12);

static_assert(
    offsetof(ObjectReferenceEnumerationParameters, request_status) == 0);
static_assert(
    offsetof(ObjectReferenceEnumerationParameters, request) == sizeof(uint32_t));
static_assert(
    offsetof(
        ObjectReferenceEnumerationParameters,
        get_loader_allocator_object_slot) == 2 * sizeof(uint32_t));
static_assert(
    sizeof(ObjectReferenceEnumerationParameters) ==
    (sizeof(void*) == 8 ? 16 : 12));

static_assert(
    offsetof(AllocationNotificationParameters, callback) ==
    (sizeof(void*) == 8 ? 8 : 4));
static_assert(
    sizeof(AllocationNotificationParameters) ==
    (sizeof(void*) == 8 ? 24 : 12));
static_assert(
    offsetof(WriteBarrierParameters, write_barrier_bulk_scan) ==
    (sizeof(void*) == 8 ? 136 : 76));
static_assert(
    sizeof(WriteBarrierParameters) ==
    (sizeof(void*) == 8 ? 152 : 84));

namespace
{
int s_checks;
int s_failures;

void Expect(const char* name, bool condition)
{
    s_checks++;
    if (!condition)
    {
        s_failures++;
        printf("FAIL: %s\n", name);
    }
}

class TestMethodTable
{
public:
    explicit TestMethodTable(size_t gcDescSize)
        : m_storage(gcDescSize + sizeof(MethodTable))
        , m_methodTable(
            reinterpret_cast<MethodTable*>(m_storage.data() + gcDescSize))
    {
        memset(m_storage.data(), 0, m_storage.size());
    }

    MethodTable* Get() const
    {
        return m_methodTable;
    }

private:
    std::vector<uint8_t> m_storage;
    MethodTable* m_methodTable;
};

class TestObject
{
public:
    explicit TestObject(size_t objectSize)
        : m_storage(objectSize)
        , m_object(
            reinterpret_cast<Object*>(m_storage.data() + sizeof(ObjHeader)))
    {
        memset(m_storage.data(), 0, m_storage.size());
    }

    Object* Get() const
    {
        return m_object;
    }

private:
    std::vector<uint8_t> m_storage;
    Object* m_object;
};

void InitializeMethodTable(
    MethodTable* methodTable,
    uint32_t baseSize,
    uint32_t flags,
    uint16_t componentSize = 0)
{
    methodTable->m_flags = flags;
    methodTable->m_baseSize = baseSize;
    if ((flags & MTFlag_HasComponentSize) != 0)
    {
        methodTable->m_componentSize = componentSize;
    }
}

void SetComponentCount(Object* object, uint32_t count)
{
    uint8_t* address = reinterpret_cast<uint8_t*>(object);
    *reinterpret_cast<uint32_t*>(
        address + ArrayBase::GetOffsetOfNumComponents()) = count;
}

std::vector<Object**> Flatten(Object* object)
{
    std::vector<Object**> slots;
    GCReferenceRangeIterator iterator(object);
    GCReferenceRange range = {};
    while (iterator.Next(&range))
    {
        for (size_t index = 0; index < range.count; index++)
        {
            slots.push_back(range.start + index);
        }
    }

    Expect(
        "iterator clears range after completion",
        (range.start == nullptr) && (range.count == 0));
    return slots;
}

std::vector<Object**> FlattenWithVisitor(Object* object)
{
    std::vector<Object**> slots;
    bool completed = GCReferenceRanges::Enumerate(
        object,
        [&slots](Object** start, size_t count)
        {
            for (size_t index = 0; index < count; index++)
            {
                slots.push_back(start + index);
            }
            return true;
        });
    Expect("range visitor completed", completed);
    return slots;
}

void TestDefaultRequest()
{
    ObjectReferenceEnumerationParameters parameters = {};
    Expect(
        "zero request status",
        parameters.request_status ==
            ObjectReferenceEnumerationRequestStatus::NotProcessed);
    Expect(
        "zero request is disabled",
        parameters.request == ObjectReferenceEnumerationRequest::Disabled);
    Expect(
        "zero request resolver",
        parameters.get_loader_allocator_object_slot == nullptr);
}

void TestNoReferences()
{
    const uint32_t BaseSize =
        static_cast<uint32_t>(sizeof(ObjHeader) + 2 * sizeof(void*));
    TestMethodTable type(0);
    InitializeMethodTable(type.Get(), BaseSize, 0);

    TestObject instance(BaseSize);
    instance.Get()->RawSetMethodTable(type.Get());

    std::vector<Object**> slots = Flatten(instance.Get());
    Expect("no-reference object has no ranges", slots.empty());
}

void TestPositiveSeries()
{
    const size_t ObjectBytes = 6 * sizeof(void*);
    const uint32_t BaseSize =
        static_cast<uint32_t>(sizeof(ObjHeader) + ObjectBytes);
    TestMethodTable type(CGCDesc::ComputeSize(2));
    InitializeMethodTable(type.Get(), BaseSize, MTFlag_ContainsGCPointers);

    CGCDesc::Init(type.Get(), 2);
    CGCDescSeries* lowest = CGCDesc::GetCGCDescFromMT(type.Get())->GetLowestSeries();
    lowest[0].SetSeriesOffset(4 * sizeof(void*));
    lowest[0].SetSeriesSize(sizeof(void*) - BaseSize);
    lowest[1].SetSeriesOffset(sizeof(void*));
    lowest[1].SetSeriesSize(2 * sizeof(void*) - BaseSize);

    TestObject instance(BaseSize);
    Object* object = instance.Get();
    object->RawSetMethodTable(type.Get());

    Object** first = reinterpret_cast<Object**>(
        reinterpret_cast<uint8_t*>(object) + sizeof(void*));
    Object** second = first + 1;
    Object** third = reinterpret_cast<Object**>(
        reinterpret_cast<uint8_t*>(object) + 4 * sizeof(void*));
    *first = reinterpret_cast<Object*>(static_cast<uintptr_t>(0x1000));
    *second = nullptr;
    *third = reinterpret_cast<Object*>(static_cast<uintptr_t>(0x3000));

    std::vector<Object**> slots = Flatten(object);
#ifdef P15_FILTER_NULL_CONTROL
    slots.erase(slots.begin() + 1);
#endif // P15_FILTER_NULL_CONTROL
    std::vector<Object**> visitorSlots = FlattenWithVisitor(object);
    Expect("positive series slot count", slots.size() == 3);
    Expect("positive visitor parity", visitorSlots == slots);
    if (slots.size() == 3)
    {
        Expect("positive series first slot", slots[0] == first);
        Expect("positive series second slot", slots[1] == second);
        Expect("positive series third slot", slots[2] == third);
        Expect("positive series includes null", *slots[1] == nullptr);
    }
}

void TestReferenceArray(uint32_t componentCount)
{
    const size_t DataOffset = sizeof(ArrayBase);
    const uint32_t BaseSize =
        static_cast<uint32_t>(sizeof(ObjHeader) + DataOffset);
    const size_t ObjectSize =
        BaseSize + static_cast<size_t>(componentCount) * sizeof(Object*);
    TestMethodTable type(CGCDesc::ComputeSize(1));
    InitializeMethodTable(
        type.Get(),
        BaseSize,
        MTFlag_ContainsGCPointers |
            MTFlag_HasComponentSize |
            MTFlag_IsArray,
        static_cast<uint16_t>(sizeof(Object*)));

    CGCDesc::Init(type.Get(), 1);
    CGCDescSeries* series =
        CGCDesc::GetCGCDescFromMT(type.Get())->GetHighestSeries();
    series->SetSeriesOffset(DataOffset);
    series->SetSeriesSize(
        static_cast<size_t>(0) - (DataOffset + sizeof(ObjHeader)));

    TestObject instance(ObjectSize);
    Object* object = instance.Get();
    object->RawSetMethodTable(type.Get());
    SetComponentCount(object, componentCount);

    std::vector<Object**> slots = Flatten(object);
    std::vector<Object**> visitorSlots = FlattenWithVisitor(object);
    Expect(
        componentCount == 0
            ? "empty reference array"
            : "reference array slot count",
        slots.size() == componentCount);
    Expect("reference array visitor parity", visitorSlots == slots);
    if (componentCount != 0)
    {
        Object** expected = reinterpret_cast<Object**>(
            reinterpret_cast<uint8_t*>(object) + DataOffset);
        Expect("reference array first slot", slots.front() == expected);
        Expect(
            "reference array last slot",
            slots.back() == expected + componentCount - 1);
    }
}

void TestRepeatingSeries()
{
    const size_t DataOffset = sizeof(ArrayBase);
    const size_t ComponentSize = 4 * sizeof(void*);
    const uint32_t ComponentCount = 3;
    const uint32_t BaseSize =
        static_cast<uint32_t>(sizeof(ObjHeader) + DataOffset);
    const size_t ObjectSize =
        BaseSize + ComponentCount * ComponentSize;
    TestMethodTable type(CGCDesc::ComputeSizeRepeating(2));
    InitializeMethodTable(
        type.Get(),
        BaseSize,
        MTFlag_ContainsGCPointers |
            MTFlag_HasComponentSize |
            MTFlag_IsArray,
        static_cast<uint16_t>(ComponentSize));

    CGCDesc::InitValueClassSeries(type.Get(), 2);
    CGCDescSeries* series =
        CGCDesc::GetCGCDescFromMT(type.Get())->GetHighestSeries();
    series->SetSeriesOffset(DataOffset);
    (series->val_serie + 0)->set_val_serie_item(
        1,
        static_cast<HALF_SIZE_T>(sizeof(void*)));
    (series->val_serie - 1)->set_val_serie_item(
        1,
        static_cast<HALF_SIZE_T>(sizeof(void*)));

    TestObject instance(ObjectSize);
    Object* object = instance.Get();
    object->RawSetMethodTable(type.Get());
    SetComponentCount(object, ComponentCount);

    Object** data = reinterpret_cast<Object**>(
        reinterpret_cast<uint8_t*>(object) + DataOffset);
    data[0] = reinterpret_cast<Object*>(static_cast<uintptr_t>(0x1000));
    data[2] = nullptr;
    data[4] = reinterpret_cast<Object*>(static_cast<uintptr_t>(0x2000));
    data[6] = reinterpret_cast<Object*>(static_cast<uintptr_t>(0x3000));
    data[8] = nullptr;
    data[10] = reinterpret_cast<Object*>(static_cast<uintptr_t>(0x4000));

    std::vector<Object**> slots = Flatten(object);
    std::vector<Object**> visitorSlots = FlattenWithVisitor(object);
    Expect("repeating series slot count", slots.size() == 6);
    Expect("repeating visitor parity", visitorSlots == slots);
    for (size_t index = 0; index < slots.size(); index++)
    {
        size_t expectedIndex = (index / 2) * 4 + (index % 2) * 2;
        Expect("repeating series order", slots[index] == data + expectedIndex);
    }
    if (slots.size() == 6)
    {
        Expect("repeating series includes first null", *slots[1] == nullptr);
        Expect("repeating series includes second null", *slots[4] == nullptr);
    }
}

#ifdef TARGET_WINDOWS
void TestGuardPageBoundary()
{
    SYSTEM_INFO systemInfo = {};
    GetSystemInfo(&systemInfo);
    size_t pageSize = systemInfo.dwPageSize;
    uint8_t* pages = static_cast<uint8_t*>(VirtualAlloc(
        nullptr,
        2 * pageSize,
        MEM_RESERVE | MEM_COMMIT,
        PAGE_READWRITE));
    Expect("guard allocation", pages != nullptr);
    if (pages == nullptr)
    {
        return;
    }

    DWORD oldProtection;
    BOOL protectedPage = VirtualProtect(
        pages + pageSize,
        pageSize,
        PAGE_NOACCESS,
        &oldProtection);
    Expect("guard protection", protectedPage != FALSE);
    if (protectedPage == FALSE)
    {
        VirtualFree(pages, 0, MEM_RELEASE);
        return;
    }

    const size_t ObjectBytes = 2 * sizeof(void*);
    const uint32_t BaseSize =
        static_cast<uint32_t>(sizeof(ObjHeader) + ObjectBytes);
    TestMethodTable type(CGCDesc::ComputeSize(1));
    InitializeMethodTable(type.Get(), BaseSize, MTFlag_ContainsGCPointers);
    CGCDesc::Init(type.Get(), 1);
    CGCDescSeries* series =
        CGCDesc::GetCGCDescFromMT(type.Get())->GetHighestSeries();
    series->SetSeriesOffset(sizeof(void*));
    series->SetSeriesSize(sizeof(void*) - BaseSize);

    Object* object = reinterpret_cast<Object*>(
        pages + pageSize - ObjectBytes);
    object->RawSetMethodTable(type.Get());
    Object** slot = reinterpret_cast<Object**>(
        reinterpret_cast<uint8_t*>(object) + sizeof(void*));
    *slot = nullptr;

    std::vector<Object**> slots = Flatten(object);
    Expect("guard-page slot count", slots.size() == 1);
    if (slots.size() == 1)
    {
        Expect("guard-page slot address", slots[0] == slot);
    }

    VirtualFree(pages, 0, MEM_RELEASE);
}
#endif
}

int main()
{
    TestDefaultRequest();
    TestNoReferences();
    TestPositiveSeries();
    TestReferenceArray(0);
    TestReferenceArray(7);
    TestRepeatingSeries();
#ifdef TARGET_WINDOWS
    TestGuardPageBoundary();
#endif

    printf(
        "%d/%d object reference enumeration checks passed\n",
        s_checks - s_failures,
        s_checks);
    return s_failures == 0 ? 0 : 1;
}
