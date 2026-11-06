// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "test.h"

RESULT test_simdhash_perf(void);

static const Test dn_container_perf_tests[] = {
    {"test_simdhash_perf", test_simdhash_perf},
    {NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(dn_container_perf_tests_init, dn_container_perf_tests)
