// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/nativeaot/Runtime/gcenv.h"
#include "../../../../src/coreclr/gc/gcinterface.h"
#include "../../../../src/coreclr/gc/gcref.h"

#include <vector>

namespace
{
constexpr uint32_t HasComponentSizeFlag = 0x80000000;
constexpr uint32_t HasPointersFlag = 0x01000000;
constexpr uintptr_t MarkedBit = 0x1;

int s_checks;
int s_failures;
int s_getSizeCalls;
bool s_getSizeSawMarkedMethodTable;

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

    void Initialize(
        uint32_t flags,
        uint32_t baseSize,
        uint16_t componentSize = 0)
    {
        uint8_t* bytes = reinterpret_cast<uint8_t*>(m_methodTable);
        *reinterpret_cast<uint32_t*>(
            bytes + cdac_data<MethodTable>::Flags) = flags;
        *reinterpret_cast<uint32_t*>(
            bytes + cdac_data<MethodTable>::BaseSize) = baseSize;
        if ((flags & HasComponentSizeFlag) != 0)
        {
            *reinterpret_cast<uint16_t*>(
                bytes + cdac_data<MethodTable>::Flags) = componentSize;
        }
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

    void SetMarkedMethodTable(MethodTable* methodTable)
    {
        m_object->RawSetMethodTable(reinterpret_cast<MethodTable*>(
            reinterpret_cast<uintptr_t>(methodTable) | MarkedBit));
    }

private:
    std::vector<uint8_t> m_storage;
    Object* m_object;
};

void SetComponentCount(Object* object, uint32_t componentCount)
{
    *reinterpret_cast<uint32_t*>(
        reinterpret_cast<uint8_t*>(object) +
        ArrayBase::GetOffsetOfNumComponents()) = componentCount;
}

std::vector<Object**> EnumerateWithVisitor(Object* object)
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
    Expect("NativeAOT visitor completed", completed);
    return slots;
}

std::vector<Object**> EnumerateWithIterator(Object* object)
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
    return slots;
}

void TestPointerFreeMarkedObject()
{
    const uint32_t BaseSize =
        static_cast<uint32_t>(sizeof(ObjHeader) + 2 * sizeof(void*));
    TestMethodTable type(0);
    type.Initialize(0, BaseSize);
    TestObject instance(BaseSize);
    instance.SetMarkedMethodTable(type.Get());

    s_getSizeCalls = 0;
    s_getSizeSawMarkedMethodTable = false;
    std::vector<Object**> visitorSlots = EnumerateWithVisitor(instance.Get());
    std::vector<Object**> iteratorSlots = EnumerateWithIterator(instance.Get());
    Expect("NativeAOT pointer-free visitor is empty", visitorSlots.empty());
    Expect("NativeAOT pointer-free iterator is empty", iteratorSlots.empty());
    Expect("NativeAOT pointer-free scan avoids Object::GetSize", s_getSizeCalls == 0);
    Expect(
        "NativeAOT pointer-free scan avoids tagged Object::GetSize",
        !s_getSizeSawMarkedMethodTable);
}

void TestPositiveMarkedObject()
{
    const size_t ObjectBytes = 5 * sizeof(void*);
    const uint32_t BaseSize =
        static_cast<uint32_t>(sizeof(ObjHeader) + ObjectBytes);
    TestMethodTable type(CGCDesc::ComputeSize(2));
    type.Initialize(HasPointersFlag, BaseSize);

    CGCDesc::Init(type.Get(), 2);
    CGCDescSeries* lowest =
        CGCDesc::GetCGCDescFromMT(type.Get())->GetLowestSeries();
    lowest[0].SetSeriesOffset(3 * sizeof(void*));
    lowest[0].SetSeriesSize(sizeof(void*) - BaseSize);
    lowest[1].SetSeriesOffset(sizeof(void*));
    lowest[1].SetSeriesSize(sizeof(void*) - BaseSize);

    TestObject instance(BaseSize);
    Object* object = instance.Get();
    instance.SetMarkedMethodTable(type.Get());
    Object** first = reinterpret_cast<Object**>(
        reinterpret_cast<uint8_t*>(object) + sizeof(void*));
    Object** second = reinterpret_cast<Object**>(
        reinterpret_cast<uint8_t*>(object) + 3 * sizeof(void*));

    s_getSizeCalls = 0;
    s_getSizeSawMarkedMethodTable = false;
    std::vector<Object**> visitorSlots = EnumerateWithVisitor(object);
    std::vector<Object**> iteratorSlots = EnumerateWithIterator(object);
    Expect(
        "NativeAOT positive marked visitor slots",
        (visitorSlots.size() == 2) &&
        (visitorSlots[0] == first) &&
        (visitorSlots[1] == second));
    Expect("NativeAOT positive marked iterator parity", iteratorSlots == visitorSlots);
    Expect("NativeAOT positive marked scan avoids Object::GetSize", s_getSizeCalls == 0);
    Expect(
        "NativeAOT positive marked scan avoids tagged Object::GetSize",
        !s_getSizeSawMarkedMethodTable);
}

void TestRepeatingMarkedObject()
{
    const size_t DataOffset = sizeof(Array);
    const size_t ComponentSize = 4 * sizeof(void*);
    const uint32_t ComponentCount = 3;
    const uint32_t BaseSize =
        static_cast<uint32_t>(sizeof(ObjHeader) + DataOffset);
    const size_t ObjectSize =
        BaseSize + ComponentCount * ComponentSize;
    TestMethodTable type(CGCDesc::ComputeSizeRepeating(2));
    type.Initialize(
        HasPointersFlag | HasComponentSizeFlag,
        BaseSize,
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
    instance.SetMarkedMethodTable(type.Get());
    SetComponentCount(object, ComponentCount);
    Object** data = reinterpret_cast<Object**>(
        reinterpret_cast<uint8_t*>(object) + DataOffset);

    s_getSizeCalls = 0;
    s_getSizeSawMarkedMethodTable = false;
    std::vector<Object**> visitorSlots = EnumerateWithVisitor(object);
    std::vector<Object**> iteratorSlots = EnumerateWithIterator(object);
    Expect("NativeAOT repeating marked slot count", visitorSlots.size() == 6);
    for (size_t index = 0; index < visitorSlots.size(); index++)
    {
        size_t expectedIndex = (index / 2) * 4 + (index % 2) * 2;
        Expect(
            "NativeAOT repeating marked slot order",
            visitorSlots[index] == data + expectedIndex);
    }
    Expect("NativeAOT repeating marked iterator parity", iteratorSlots == visitorSlots);
    Expect("NativeAOT repeating marked scan avoids Object::GetSize", s_getSizeCalls == 0);
    Expect(
        "NativeAOT repeating marked scan avoids tagged Object::GetSize",
        !s_getSizeSawMarkedMethodTable);
}
}

size_t Object::GetSize()
{
    s_getSizeCalls++;
    MethodTable* methodTable = GetMethodTable();
    if ((reinterpret_cast<uintptr_t>(methodTable) & MarkedBit) != 0)
    {
        s_getSizeSawMarkedMethodTable = true;
        methodTable = GetGCSafeMethodTable();
    }
    size_t objectSize = methodTable->GetBaseSize();
    if (methodTable->HasComponentSize())
    {
        objectSize +=
            static_cast<size_t>(
                reinterpret_cast<ArrayBase*>(this)->GetNumComponents()) *
            methodTable->RawGetComponentSize();
    }
    return objectSize;
}

int main()
{
    TestPointerFreeMarkedObject();
    TestPositiveMarkedObject();
    TestRepeatingMarkedObject();

    printf(
        "%d/%d NativeAOT reference enumeration checks passed\n",
        s_checks - s_failures,
        s_checks);
    return s_failures == 0 ? 0 : 1;
}
