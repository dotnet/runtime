// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"

#include <stdio.h>
#include <type_traits>

static_assert(GC_INTERFACE_MAJOR_VERSION == 5);
static_assert(GC_INTERFACE_MINOR_VERSION >= GC_ALLOCATION_NOTIFICATION_INTERFACE_MINOR_VERSION);
static_assert(GC_ALLOCATION_NOTIFICATION_INTERFACE_MINOR_VERSION == 13);
static_assert(GC_WRITE_BARRIER_BULK_SCAN_INTERFACE_MINOR_VERSION == 12);

static_assert(static_cast<uint32_t>(AllocationNotificationRequestStatus::NotProcessed) == 0);
static_assert(static_cast<uint32_t>(AllocationNotificationRequestStatus::Accepted) == 1);
static_assert(static_cast<uint32_t>(AllocationNotificationRequestStatus::Unsupported) == 2);
static_assert(static_cast<uint32_t>(AllocationCompleteFlags::None) == 0);
static_assert(static_cast<uint32_t>(AllocationCompleteFlags::FrozenObjectHeap) == (1 << 9));

static_assert(std::is_standard_layout_v<AllocationNotificationParameters>);
static_assert(offsetof(AllocationNotificationParameters, request_status) == 0);
static_assert(
    offsetof(AllocationNotificationParameters, callback) ==
    (sizeof(void*) == 8 ? 8 : 4));
static_assert(
    offsetof(AllocationNotificationParameters, context) ==
    (sizeof(void*) == 8 ? 16 : 8));
static_assert(sizeof(AllocationNotificationParameters) == (sizeof(void*) == 8 ? 24 : 12));

static_assert(offsetof(WriteBarrierParameters, write_barrier_bulk_scan) == (sizeof(void*) == 8 ? 136 : 76));
static_assert(sizeof(WriteBarrierParameters) == (sizeof(void*) == 8 ? 152 : 84));

namespace
{
int checks;
int failures;

void Expect(const char* name, bool condition)
{
    checks++;
    if (!condition)
    {
        failures++;
        printf("FAIL: %s\n", name);
    }
}

void Callback(Object*, size_t, AllocationCompleteFlags, void*)
{
}
}

int main()
{
    AllocationNotificationParameters parameters = {};
    Expect(
        "zero initialization selects NotProcessed",
        parameters.request_status == AllocationNotificationRequestStatus::NotProcessed);
    Expect("zero initialization clears callback", parameters.callback == nullptr);
    Expect("zero initialization clears context", parameters.context == nullptr);

    int context = 0;
    parameters.callback = Callback;
    parameters.context = &context;
    parameters.request_status = AllocationNotificationRequestStatus::Accepted;
    Expect("callback round trips", parameters.callback == Callback);
    Expect("context round trips", parameters.context == &context);
    Expect(
        "Accepted round trips",
        parameters.request_status == AllocationNotificationRequestStatus::Accepted);

    printf("%d/%d allocation notification checks passed\n", checks - failures, checks);
    return failures == 0 ? 0 : 1;
}
