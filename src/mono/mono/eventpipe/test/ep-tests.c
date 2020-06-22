#include "mono/eventpipe/ep.h"
#include "eglib/test/test.h"

#define TEST_PROVIDER_NAME "MyTestProvider"
#define TEST_FILE "./ep_test_create_file.txt"

#ifdef _CRTDBG_MAP_ALLOC
static _CrtMemState eventpipe_memory_start_snapshot;
static _CrtMemState eventpipe_memory_end_snapshot;
static _CrtMemState eventpipe_memory_diff_snapshot;
#endif

static RESULT
test_eventpipe_setup (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&eventpipe_memory_start_snapshot);
#endif
	return NULL;
}

static RESULT
test_create_delete_provider (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeProvider *test_provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	if (!test_provider) {
		result = FAILED ("Failed to create provider %s, ep_create_provider returned NULL", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

ep_on_exit:
	ep_delete_provider (test_provider);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_stress_create_delete_provider (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeProvider *test_providers [1000] = {0};

	for (uint32_t i = 0; i < 1000; ++i) {
		char *provider_name = g_strdup_printf (TEST_PROVIDER_NAME "_%i", i);
		test_providers [i] = ep_create_provider (provider_name, NULL, NULL);
		g_free (provider_name);

		if (!test_providers [i]) {
			result = FAILED ("Failed to create provider %s_%i, ep_create_provider returned NULL", TEST_PROVIDER_NAME, i);
			ep_raise_error ();
		}
	}

ep_on_exit:
	for (uint32_t i = 0; i < 1000; ++i) {
		if (test_providers [i])
			ep_delete_provider (test_providers [i]);
	}
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_get_provider (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeProvider *test_provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	if (!test_provider) {
		result = FAILED ("Failed to create provider %s, ep_create_provider returned NULL", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

	test_location = 1;

	EventPipeProvider *returned_test_provider = ep_get_provider (TEST_PROVIDER_NAME);
	if (!returned_test_provider) {
		result = FAILED ("Failed to get provider %s, ep_get_provider returned NULL", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

	test_location = 2;

	ep_delete_provider (test_provider);
	test_provider = NULL;

	returned_test_provider = ep_get_provider (TEST_PROVIDER_NAME);
	if (returned_test_provider) {
		result = FAILED ("Provider %s, still returned from ep_get_provider after deleted", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

ep_on_exit:
	ep_delete_provider (test_provider);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_create_same_provider_twice (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeProvider *test_provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	if (!test_provider) {
		result = FAILED ("Failed to create provider %s, ep_create_provider returned NULL", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

	test_location = 1;

	EventPipeProvider *returned_test_provider = ep_get_provider (TEST_PROVIDER_NAME);
	if (!returned_test_provider) {
		result = FAILED ("Failed to get provider %s, ep_get_provider returned NULL", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

	test_location = 2;

	EventPipeProvider *test_provider2 = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	if (test_provider2) {
		result = FAILED ("Creating an already existing provider %s, succeeded", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

	test_location = 3;

	returned_test_provider = ep_get_provider (TEST_PROVIDER_NAME);
	if (!returned_test_provider) {
		result = FAILED ("Failed to get provider %s, ep_get_provider returned NULL", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

ep_on_exit:
	ep_delete_provider (test_provider);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}


static RESULT
test_enable_disable (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeSessionID session_id = 0;
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config =ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOG_ALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	session_id = ep_enable (
		TEST_FILE,
		1,
		current_provider_config,
		1,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		false);

	if (!session_id) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	if (!ep_enabled ()) {
		result = FAILED ("event pipe disabled");
		ep_raise_error ();
	}

ep_on_exit:
	ep_disable (session_id);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static bool provider_callback_data;

static
void
provider_callback (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_context)
{
	*(bool *)callback_context = true;
}

static RESULT
test_create_delete_provider_with_callback (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeSessionID session_id = 0;
	EventPipeProvider *test_provider = NULL;
	EventPipeProviderConfiguration provider_config;

	EventPipeProviderConfiguration *current_provider_config =ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOG_ALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	session_id = ep_enable (
		TEST_FILE,
		1,
		current_provider_config,
		1,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		false);

	if (!session_id) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	test_provider = ep_create_provider (TEST_PROVIDER_NAME, provider_callback, &provider_callback_data);
	ep_raise_error_if_nok (test_provider != NULL);

	test_location = 3;

	if (!provider_callback_data) {
		result = FAILED ("Provider callback not called");
		ep_raise_error ();
	}

ep_on_exit:
	ep_delete_provider (test_provider);
	ep_disable (session_id);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_build_event_metadata (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeEventInstance *ep_event_instance = NULL;
	EventPipeEventMetadataEvent *metadata_event = NULL;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 1;

	ep_event = ep_event_alloc (provider, 1, 1, 1, EP_EVENT_LEVEL_VERBOSE, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 2;

	ep_event_instance = ep_event_instance_alloc (ep_event, 0, 0, NULL, 0, NULL, NULL);
	ep_raise_error_if_nok (ep_event_instance != NULL);

	test_location = 3;

	metadata_event = ep_build_event_metadata_event (ep_event_instance, 1);
	ep_raise_error_if_nok (metadata_event != NULL);

	test_location = 4;

ep_on_exit:
	ep_delete_provider (provider);
	ep_event_free (ep_event);
	ep_event_instance_free (ep_event_instance);
	ep_event_metdata_event_free (metadata_event);

	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_start_session_streaming (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeSessionID session_id = 0;
	EventPipeProviderConfiguration provider_config;

	EventPipeProviderConfiguration *current_provider_config =ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOG_ALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	session_id = ep_enable (
		TEST_FILE,
		1,
		current_provider_config,
		1,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		false);

	if (!session_id) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	ep_start_streaming (session_id);

ep_on_exit:
	ep_disable (session_id);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_eventpipe_teardown (void)
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

static Test ep_tests [] = {
	{"test_eventpipe_setup", test_eventpipe_setup},
	{"test_create_delete_provider", test_create_delete_provider},
	{"test_stress_create_delete_provider", test_stress_create_delete_provider},
	{"test_get_provider", test_get_provider},
	{"test_create_same_provider_twice", test_create_same_provider_twice},
	{"test_enable_disable", test_enable_disable},
	{"test_create_delete_provider_with_callback", test_create_delete_provider_with_callback},
	{"test_build_event_metadata", test_build_event_metadata},
	{"test_start_session_streaming", test_start_session_streaming},
	{"test_eventpipe_teardown", test_eventpipe_teardown},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_tests_init, ep_tests)
