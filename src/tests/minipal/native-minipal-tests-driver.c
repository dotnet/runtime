// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "test.h"

DEFINE_TEST_GROUP_INIT_H(minipal_utf8_tests_init);
DEFINE_TEST_GROUP_INIT_H(minipal_u16_tests_init);

static const Group test_groups[] = {
    {"utf8", minipal_utf8_tests_init},
    {"u16", minipal_u16_tests_init},
    {NULL, NULL}
};

int DN_CALLBACK_CALLTYPE main(int argc, char **argv)
{
    return run_tests_main(test_groups, argc, argv);
}
