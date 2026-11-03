// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <eventpipe/ep.h>
#include "test.h"

DEFINE_TEST_GROUP_INIT_H(ep_rt_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_fastserializer_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_provider_callback_data_queue_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_file_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_session_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_thread_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_buffer_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_buffer_manager_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_tests_init);

#if TEST_PERF
RESULT test_check_buffer_perf(void);
RESULT test_buffer_manager_perf(void);
RESULT test_write_event_perf(void);

static const Test perf_tests[] = {
    {"test_check_buffer_perf", test_check_buffer_perf},
    {"test_buffer_manager_perf", test_buffer_manager_perf},
    {"test_write_event_perf", test_write_event_perf},
    {NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_perf_tests_init, perf_tests)
#endif

static const Group test_groups[] = {
    {"rt", ep_rt_tests_init},
    {"fastserializer", ep_fastserializer_tests_init},
    {"provider-callback-dataqueue", ep_provider_callback_data_queue_tests_init},
    {"file", ep_file_tests_init},
    {"session", ep_session_tests_init},
    {"thread", ep_thread_tests_init},
    {"buffer", ep_buffer_tests_init},
    {"buffer-manager", ep_buffer_manager_tests_init},
    {"eventpipe", ep_tests_init},
#if TEST_PERF
    {"perf", ep_perf_tests_init},
#endif
    {NULL, NULL}
};

int DN_CALLBACK_CALLTYPE main(int argc, char **argv)
{
    if (argc == 2 && (strcmp(argv[1], "--list") == 0 || strcmp(argv[1], "--help") == 0))
        return run_tests_main(test_groups, argc, argv);

    ep_init();
    ep_finish_init();
    int result = run_tests_main(test_groups, argc, argv);
    ep_shutdown();
    ep_rt_test_thread_exited();

    const char *files[] = {"ep_test_create_file.txt", "ep_test_create_file_2.txt"};
    for (size_t i = 0; i < ARRAY_SIZE(files); ++i)
    {
        if (remove(files[i]) != 0 && errno != ENOENT)
        {
            fprintf(stderr, "Failed to remove '%s' (errno %d)\n", files[i], errno);
            result = EXIT_FAILURE;
        }
    }
    return result;
}
