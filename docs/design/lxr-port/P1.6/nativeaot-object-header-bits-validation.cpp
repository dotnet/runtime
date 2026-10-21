// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "rhassert.h"
#include "PalLimitedContext.h"
#include "Pal.h"
#include "TargetPtrs.h"
#include "MethodTable.h"
#include "ObjectLayout.h"

#include <atomic>
#include <stdio.h>
#include <string.h>
#include <thread>

UInt32_BOOL PalSwitchToThread()
{
    std::this_thread::yield();
    return 1;
}

namespace
{
constexpr uint32_t StateMask = 0x00000003;
constexpr uint32_t Sentinel = 0xa5a50000;

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
}

int main()
{
    alignas(sizeof(void*)) uint8_t storage[sizeof(ObjHeader)] = {};
    ObjHeader* header = reinterpret_cast<ObjHeader*>(storage);

    header->SetGCReservedBits(UINT32_MAX, 0, Sentinel);
    Expect("NativeAOT raw sentinel", header->GetGCReservedBits(UINT32_MAX, 0) == Sentinel);
    Expect(
        "NativeAOT claim",
        header->CompareExchangeGCReservedBits(StateMask, 0, 2, 0) == 0);
    Expect("NativeAOT transition", header->GetGCReservedBits(StateMask, 0) == 2);

    std::atomic<bool> ready = false;
    std::atomic<uint32_t> payload = 0;
    uint32_t observed = 0;
    uint32_t observedPayload = 0;
    std::thread waiter(
        [&]()
        {
            ready.store(true, std::memory_order_seq_cst);
            observed = header->WaitWhileGCReservedBits(StateMask, 0, 2);
            observedPayload = payload.load(std::memory_order_seq_cst);
        });

    while (!ready.load(std::memory_order_seq_cst))
    {
        std::this_thread::yield();
    }
    payload.store(42, std::memory_order_seq_cst);
    Expect(
        "NativeAOT publish",
        header->CompareExchangeGCReservedBits(StateMask, 0, 3, 2) == 2);
    waiter.join();

    Expect("NativeAOT wait state", observed == 3);
    Expect("NativeAOT wait payload", observedPayload == 42);
    Expect(
        "NativeAOT preserves upper bits",
        (header->GetGCReservedBits(UINT32_MAX, 0) & ~StateMask) == Sentinel);

    printf("%d/%d NativeAOT object-header bit checks passed\n", s_checks - s_failures, s_checks);
    return s_failures == 0 ? 0 : 1;
}
