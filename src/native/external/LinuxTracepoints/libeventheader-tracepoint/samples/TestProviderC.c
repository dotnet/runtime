// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <eventheader/TraceLoggingProvider.h>
#include <stdbool.h>
#include <uchar.h>

#define TestProvider TestProviderC
#include "TestProviderCommon.h"

static bool TestTraceLoggingLangSpecific()
{
    return true;
}

TRACELOGGING_DEFINE_PROVIDER(
    TestProviderC,
    "TestProviderC",
    // {0da7a945-e9b1-510f-0ccf-ab1af0bc095b}
    (0x0da7a945, 0xe9b1, 0x510f, 0x0c, 0xcf, 0xab, 0x1a, 0xf0, 0xbc, 0x09, 0x5b));

TRACELOGGING_DEFINE_PROVIDER(
    TestProviderCG,
    "TestProviderC",
    // {0da7a945-e9b1-510f-0ccf-ab1af0bc095b}
    (0x0da7a945, 0xe9b1, 0x510f, 0x0c, 0xcf, 0xab, 0x1a, 0xf0, 0xbc, 0x09, 0x5b),
    TraceLoggingOptionGroupName("msft"));

#include <stdio.h>

bool TestC()
{
    printf("TestProvider Name: %s\n", TraceLoggingProviderName(TestProvider));

    int err = TraceLoggingRegister(TestProviderC);
    printf("TestProviderC register: %d\n", err);

    bool ok = TestCommon() && TestTraceLoggingLangSpecific();

    TraceLoggingUnregister(TestProviderC);
    return ok && err == 0;
}
