#ifndef _EVENTPIPE_TESTS_H
#define _EVENTPIPE_TESTS_H

#include <eglib/test/test.h>

DEFINE_TEST_GROUP_INIT_H(ep_setup_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_rt_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_fastserializer_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_provider_callback_data_queue_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_file_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_session_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_thread_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_buffer_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_buffer_manager_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_tests_init);
DEFINE_TEST_GROUP_INIT_H(fake_tests_init);
DEFINE_TEST_GROUP_INIT_H(ep_teardown_tests_init);

const
static Group test_groups [] = {
	{"setup", ep_setup_tests_init},
	{"rt", ep_rt_tests_init},
	{"fastserializer", ep_fastserializer_tests_init},
	{"provider-callback-dataqueue", ep_provider_callback_data_queue_tests_init},
	{"file", ep_file_tests_init},
	{"session", ep_session_tests_init},
	{"thread", ep_thread_tests_init},
	{"buffer", ep_buffer_tests_init},
	{"buffer-manager", ep_buffer_manager_tests_init},
	{"eventpipe", ep_tests_init},
	{"fake", fake_tests_init},
	{"teardown", ep_teardown_tests_init},
	{NULL, NULL}
};

#endif /* _EVENTPIPE_TESTS_H */
