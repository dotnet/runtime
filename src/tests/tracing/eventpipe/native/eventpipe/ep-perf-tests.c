// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "test.h"

RESULT test_check_buffer_perf(void);
RESULT test_buffer_manager_perf(void);
RESULT test_write_event_perf(void);

static const Test ep_perf_tests[] = {
    {"test_check_buffer_perf", test_check_buffer_perf},
    {"test_buffer_manager_perf", test_buffer_manager_perf},
    {"test_write_event_perf", test_write_event_perf},
    {NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_perf_tests_init, ep_perf_tests)
