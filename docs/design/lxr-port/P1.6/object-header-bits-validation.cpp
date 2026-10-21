// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"

#include <atomic>
#include <stdio.h>
#include <string.h>
#include <thread>
#include <vector>

void GCToOSInterface::YieldThread(uint32_t)
{
    std::this_thread::yield();
}

static_assert(GC_INTERFACE_MAJOR_VERSION == 5);
static_assert(GC_INTERFACE_MINOR_VERSION == 15);
static_assert(GC_OBJECT_HEADER_BITS_INTERFACE_MINOR_VERSION == 15);
static_assert(GC_OBJECT_REFERENCE_ENUMERATION_INTERFACE_MINOR_VERSION == 14);
static_assert(GC_ALLOCATION_NOTIFICATION_INTERFACE_MINOR_VERSION == 13);
static_assert(GC_WRITE_BARRIER_BULK_SCAN_INTERFACE_MINOR_VERSION == 12);

static_assert(static_cast<uint32_t>(ObjectHeaderBitsRequestStatus::NotProcessed) == 0);
static_assert(static_cast<uint32_t>(ObjectHeaderBitsRequestStatus::Accepted) == 1);
static_assert(static_cast<uint32_t>(ObjectHeaderBitsRequestStatus::Unsupported) == 2);
static_assert(static_cast<uint32_t>(ObjectHeaderBitsRequest::Disabled) == 0);
static_assert(static_cast<uint32_t>(ObjectHeaderBitsProtocol::None) == 0);
static_assert(
    static_cast<uint32_t>(ObjectHeaderBitsMemoryOrder::SequentiallyConsistent) == 1);
static_assert(sizeof(ObjectHeaderBitsParameters) == 80);
static_assert(offsetof(ObjectHeaderBitsParameters, request_status) == 0);
static_assert(offsetof(ObjectHeaderBitsParameters, object_byte_offset) == 36);
static_assert(offsetof(ObjectHeaderBitsParameters, published_state) == 76);

static_assert(sizeof(AllocationNotificationParameters) == (sizeof(void*) == 8 ? 24 : 12));
static_assert(sizeof(ObjectReferenceEnumerationParameters) == (sizeof(void*) == 8 ? 16 : 12));
static_assert(sizeof(WriteBarrierParameters) == (sizeof(void*) == 8 ? 152 : 84));

namespace
{
constexpr uint32_t StateMask = 0x00000003;
constexpr uint32_t StateShift = 0;
constexpr uint32_t Sentinel = 0xa5a50000;
constexpr uint32_t ThreadCount = 32;

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

class TestObject
{
public:
    TestObject()
        : m_object(reinterpret_cast<Object*>(m_storage + sizeof(ObjHeader)))
    {
        memset(m_storage, 0, sizeof(m_storage));
    }

    Object* Get() const
    {
        return m_object;
    }

private:
    alignas(sizeof(void*)) uint8_t m_storage[sizeof(ObjHeader) + sizeof(Object)];
    Object* m_object;
};

void TestDefaultDescriptor()
{
    ObjectHeaderBitsParameters parameters = {};
    Expect(
        "zero status is NotProcessed",
        parameters.request_status == ObjectHeaderBitsRequestStatus::NotProcessed);
    Expect(
        "zero request is disabled",
        parameters.request == ObjectHeaderBitsRequest::Disabled);
    Expect("zero descriptor has no requested bits", parameters.requested_bit_count == 0);
    Expect("zero descriptor has zero outputs", parameters.object_byte_offset == 0);
}

void TestLayout()
{
    Expect(
        "object header is pointer sized",
        sizeof(ObjHeader) == sizeof(void*));
#ifdef HOST_64BIT
    Expect("64-bit object header is 8 bytes", sizeof(ObjHeader) == 8);
#else
    Expect("32-bit object header is 4 bytes", sizeof(ObjHeader) == 4);
#endif
}

#ifdef HOST_64BIT
void TestStatesAndPreservation()
{
    TestObject instance;
    ObjHeader* header = instance.Get()->GetHeader();

    Expect("new state word is clear", header->GetGCReservedBits(StateMask, StateShift) == 0);
    header->SetGCReservedBits(UINT32_MAX, 0, Sentinel);
    Expect("raw sentinel round trips", header->GetGCReservedBits(UINT32_MAX, 0) == Sentinel);

    for (uint32_t state = 0; state < 4; state++)
    {
        header->SetGCReservedBits(StateMask, StateShift, state);
        Expect("all raw states round trip", header->GetGCReservedBits(StateMask, StateShift) == state);
        Expect(
            "masked state update preserves upper bits",
            (header->GetGCReservedBits(UINT32_MAX, 0) & ~StateMask) == Sentinel);
    }

    header->SetGCReservedBits(StateMask, StateShift, 0);
    Expect(
        "clear to transition CAS observes clear",
        header->CompareExchangeGCReservedBits(StateMask, StateShift, 2, 0) == 0);
    Expect("transition state installed", header->GetGCReservedBits(StateMask, StateShift) == 2);
    Expect(
        "losing CAS observes transition",
        header->CompareExchangeGCReservedBits(StateMask, StateShift, 2, 0) == 2);
    Expect(
        "transition to published CAS observes transition",
        header->CompareExchangeGCReservedBits(StateMask, StateShift, 3, 2) == 2);
    Expect("published state installed", header->GetGCReservedBits(StateMask, StateShift) == 3);
    Expect(
        "protocol CAS preserves upper bits",
        (header->GetGCReservedBits(UINT32_MAX, 0) & ~StateMask) == Sentinel);
}

void TestSingleClaimWinner()
{
    TestObject instance;
    ObjHeader* header = instance.Get()->GetHeader();
    header->SetGCReservedBits(UINT32_MAX, 0, Sentinel);

    std::atomic<uint32_t> ready = 0;
    std::atomic<bool> start = false;
    std::atomic<uint32_t> winners = 0;
    std::vector<std::thread> threads;
    threads.reserve(ThreadCount);

    for (uint32_t index = 0; index < ThreadCount; index++)
    {
        threads.emplace_back(
            [&]()
            {
                ready.fetch_add(1, std::memory_order_seq_cst);
                while (!start.load(std::memory_order_seq_cst))
                {
                    std::this_thread::yield();
                }

                if (header->CompareExchangeGCReservedBits(StateMask, StateShift, 2, 0) == 0)
                {
                    winners.fetch_add(1, std::memory_order_seq_cst);
                }
            });
    }

    while (ready.load(std::memory_order_seq_cst) != ThreadCount)
    {
        std::this_thread::yield();
    }
    start.store(true, std::memory_order_seq_cst);

    for (std::thread& thread : threads)
    {
        thread.join();
    }

    Expect("exactly one claim winner", winners.load(std::memory_order_seq_cst) == 1);
    Expect("race leaves transition state", header->GetGCReservedBits(StateMask, StateShift) == 2);
    Expect(
        "race preserves upper bits",
        (header->GetGCReservedBits(UINT32_MAX, 0) & ~StateMask) == Sentinel);
}

void TestWaitAndPublication()
{
    TestObject instance;
    ObjHeader* header = instance.Get()->GetHeader();
    header->SetGCReservedBits(StateMask, StateShift, 2);

    std::atomic<bool> ready = false;
    std::atomic<uint32_t> payload = 0;
    uint32_t observed = 0;
    uint32_t observedPayload = 0;
    std::thread waiter(
        [&]()
        {
            ready.store(true, std::memory_order_seq_cst);
            observed = header->WaitWhileGCReservedBits(StateMask, StateShift, 2);
            observedPayload = payload.load(std::memory_order_seq_cst);
        });

    while (!ready.load(std::memory_order_seq_cst))
    {
        std::this_thread::yield();
    }
    payload.store(42, std::memory_order_seq_cst);
    Expect(
        "publication CAS observes transition",
        header->CompareExchangeGCReservedBits(StateMask, StateShift, 3, 2) == 2);
    waiter.join();

    Expect("wait observes published state", observed == 3);
    Expect("wait observes published payload", observedPayload == 42);
}
#endif
}

int main()
{
    TestDefaultDescriptor();
    TestLayout();
#ifdef HOST_64BIT
    TestStatesAndPreservation();
    TestSingleClaimWinner();
    TestWaitAndPublication();
#endif

    printf("%d/%d object-header bit checks passed\n", s_checks - s_failures, s_checks);
    return s_failures == 0 ? 0 : 1;
}
