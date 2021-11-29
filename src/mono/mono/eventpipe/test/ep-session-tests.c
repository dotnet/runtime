#if defined(_MSC_VER) && defined(_DEBUG)
#include "ep-tests-debug.h"
#endif

#include <eventpipe/ep.h>
#include <eventpipe/ep-config.h>
#include <eventpipe/ep-event.h>
#include <eventpipe/ep-session.h>
#include <eglib/test/test.h>

#define TEST_PROVIDER_NAME "MyTestProvider"
#define TEST_FILE "./ep_test_create_file.txt"

#ifdef _CRTDBG_MAP_ALLOC
static _CrtMemState eventpipe_memory_start_snapshot;
static _CrtMemState eventpipe_memory_end_snapshot;
static _CrtMemState eventpipe_memory_diff_snapshot;
#endif

static RESULT
test_session_setup (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&eventpipe_memory_start_snapshot);
#endif
	return NULL;
}

static RESULT
test_create_delete_session (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeSession *test_session = NULL;

	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	EP_LOCK_ENTER (section1)
		test_session = ep_session_alloc (
			1,
			TEST_FILE,
			NULL,
			EP_SESSION_TYPE_FILE,
			EP_SERIALIZATION_FORMAT_NETTRACE_V4,
			false,
			1,
			current_provider_config,
			1,
			NULL,
			NULL);
	EP_LOCK_EXIT (section1)

	ep_raise_error_if_nok (test_session != NULL);

ep_on_exit:
	ep_session_free (test_session);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_add_session_providers (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeSession *test_session = NULL;
	EventPipeSessionProvider *test_session_provider = NULL;

	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	EP_LOCK_ENTER (section1)
		test_session = ep_session_alloc (
			1,
			TEST_FILE,
			NULL,
			EP_SESSION_TYPE_FILE,
			EP_SERIALIZATION_FORMAT_NETTRACE_V4,
			false,
			1,
			current_provider_config,
			1,
			NULL,
			NULL);

		ep_raise_error_if_nok_holding_lock (test_session != NULL, section1);

		ep_session_start_streaming (test_session);
	EP_LOCK_EXIT (section1)

	test_location = 2;

	EP_LOCK_ENTER (section2)
		if (!ep_session_is_valid (test_session)) {
			result = FAILED ("ep_session_is_valid returned false with session providers");
			ep_raise_error_holding_lock (section2);
		}
	EP_LOCK_EXIT (section2)

	test_location = 3;

	test_session_provider = ep_session_provider_alloc (TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (test_session_provider != NULL);

	test_location = 4;

	EP_LOCK_ENTER (section3)
		ep_session_add_session_provider (test_session, test_session_provider);
	EP_LOCK_EXIT (section3)

	test_session_provider = NULL;

	EP_LOCK_ENTER (section4)
		if (!ep_session_is_valid (test_session)) {
			result = FAILED ("ep_session_is_valid returned false with session providers");
			ep_raise_error_holding_lock (section4);
		}
	EP_LOCK_EXIT (section4)

	test_location = 5;

	ep_session_disable (test_session);

	EP_LOCK_ENTER (section5)
		if (ep_session_is_valid (test_session)) {
			result = FAILED ("ep_session_is_valid returned true without session providers");
			ep_raise_error_holding_lock (section5);
		}
	EP_LOCK_EXIT (section5)

ep_on_exit:
	ep_session_free (test_session);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_session_special_get_set (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeSession *test_session = NULL;

	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	EP_LOCK_ENTER (section1)
		test_session = ep_session_alloc (
			1,
			TEST_FILE,
			NULL,
			EP_SESSION_TYPE_FILE,
			EP_SERIALIZATION_FORMAT_NETTRACE_V4,
			false,
			1,
			current_provider_config,
			1,
			NULL,
			NULL);
	EP_LOCK_EXIT (section1)

	ep_raise_error_if_nok (test_session != NULL);

	test_location = 2;

	if (ep_session_get_rundown_enabled (test_session)) {
		result = FAILED ("ep_session_get_rundown_enabled returned true, should be false");
		ep_raise_error ();
	}

	test_location = 3;

	ep_session_set_rundown_enabled (test_session, true);

	if (!ep_session_get_rundown_enabled (test_session)) {
		result = FAILED ("ep_session_get_rundown_enabled returned false, should be true");
		ep_raise_error ();
	}

	test_location = 4;

	if (ep_session_get_streaming_enabled (test_session)) {
		result = FAILED ("ep_session_get_ipc_streaming_enabled returned true, should be false");
		ep_raise_error ();
	}

	test_location = 5;

	ep_session_set_streaming_enabled (test_session, true);

	if (!ep_session_get_streaming_enabled (test_session)) {
		result = FAILED ("ep_session_set_ipc_streaming_enabled returned false, should be true");
		ep_raise_error ();
	}

	ep_session_set_streaming_enabled (test_session, false);

	test_location = 6;

	if (!ep_session_get_wait_event (test_session)) {
		result = FAILED ("ep_session_get_wait_event failed");
		ep_raise_error ();
	}

	test_location = 7;

	if (!ep_session_get_mask (test_session)) {
		result = FAILED ("Unexpected session mask");
		ep_raise_error ();
	}

ep_on_exit:
	ep_session_free (test_session);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_session_teardown (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&eventpipe_memory_end_snapshot);
	if ( _CrtMemDifference( &eventpipe_memory_diff_snapshot, &eventpipe_memory_start_snapshot, &eventpipe_memory_end_snapshot) ) {
		_CrtMemDumpStatistics( &eventpipe_memory_diff_snapshot );
		return FAILED ("Memory leak detected!");
	}
#endif
	return NULL;
}

static Test ep_session_tests [] = {
	{"test_session_setup", test_session_setup},
	{"test_create_delete_session", test_create_delete_session},
	{"test_add_session_providers", test_add_session_providers},
	{"test_session_special_get_set", test_session_special_get_set},
	{"test_session_teardown", test_session_teardown},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_session_tests_init, ep_session_tests)
