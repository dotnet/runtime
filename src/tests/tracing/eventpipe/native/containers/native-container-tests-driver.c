// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "test.h"

DEFINE_TEST_GROUP_INIT_H(dn_fwd_list_tests_init);
DEFINE_TEST_GROUP_INIT_H(dn_list_tests_init);
DEFINE_TEST_GROUP_INIT_H(dn_queue_tests_init);
DEFINE_TEST_GROUP_INIT_H(dn_umap_tests_init);
DEFINE_TEST_GROUP_INIT_H(dn_vector_ptr_tests_init);
DEFINE_TEST_GROUP_INIT_H(dn_vector_tests_init);

static const Group test_groups[] = {
    {"vector", dn_vector_tests_init},
    {"ptr-vector", dn_vector_ptr_tests_init},
    {"fwd_list", dn_fwd_list_tests_init},
    {"list", dn_list_tests_init},
    {"queue", dn_queue_tests_init},
    {"umap", dn_umap_tests_init},
    {NULL, NULL}
};

int main(int argc, char **argv)
{
    return run_tests_main(test_groups, argc, argv);
}
