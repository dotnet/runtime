#if defined(_MSC_VER) && defined(_DEBUG)
#include "ep-tests-debug.h"
#endif

#include <eventpipe/ep.h>
#include <eventpipe/ep-config.h>
#include <eventpipe/ep-event.h>
#include <eventpipe/ep-session.h>
#include <eventpipe/ep-event-instance.h>
#include <eventpipe/ep-event-payload.h>
#include <eventpipe/ep-sample-profiler.h>
#include <eglib/test/test.h>

#define TEST_PROVIDER_NAME "MyTestProvider"
#define TEST_FILE "./ep_test_create_file.txt"
#define TEST_FILE_2 "./ep_test_create_file_2.txt"

//#define TEST_PERF

#ifdef _CRTDBG_MAP_ALLOC
static _CrtMemState eventpipe_memory_start_snapshot;
static _CrtMemState eventpipe_memory_end_snapshot;
static _CrtMemState eventpipe_memory_diff_snapshot;
#endif

static RESULT
test_eventpipe_setup (void)
{
	uint32_t test_location = 0;

	// Lazy initialized, force now to not show up as leak.
	ep_rt_os_command_line_get ();
	ep_rt_managed_command_line_get ();

	test_location = 1;

	// Init profiler, force now to not show up as leaks.
	// Set long sampling rate to reduce impact.
	EP_LOCK_ENTER (section1)
		ep_sample_profiler_init (NULL);
		ep_sample_profiler_set_sampling_rate (1000 * 1000 * 100);
	EP_LOCK_EXIT (section1)

	test_location = 2;

#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&eventpipe_memory_start_snapshot);
#endif
	ep_thread_get_or_create ();
	return NULL;

ep_on_error:
	return FAILED ("Failed at test location=%i", test_location);
}

static RESULT
test_create_delete_provider (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeProvider *test_provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
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
		test_providers [i] = ep_create_provider (provider_name, NULL, NULL, NULL);
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

	EventPipeProvider *test_provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
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
	EventPipeProvider *test_provider = NULL;
	EventPipeProvider *test_provider2 = NULL;
	EventPipeProvider *returned_test_provider = NULL;

	test_provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	if (!test_provider) {
		result = FAILED ("Failed to create provider %s, ep_create_provider returned NULL", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

	test_location = 1;

	returned_test_provider = ep_get_provider (TEST_PROVIDER_NAME);
	if (!returned_test_provider) {
		result = FAILED ("Failed to get provider %s, ep_get_provider returned NULL", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

	test_location = 2;

	test_provider2 = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	if (!test_provider2) {
		result = FAILED ("Creating to create an already existing provider %s", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

	test_location = 3;

	returned_test_provider = ep_get_provider (TEST_PROVIDER_NAME);
	if (!returned_test_provider) {
		result = FAILED ("Failed to get provider %s, ep_get_provider returned NULL", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

	test_location = 4;
	if (returned_test_provider != test_provider) {
		result = FAILED ("Failed to get provider %s, ep_get_provider returned unexpected provider instance", TEST_PROVIDER_NAME);
		ep_raise_error ();
	}

ep_on_exit:
	ep_delete_provider (test_provider2);
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
	EventPipeProviderConfiguration *current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
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
		NULL,
		NULL);

	if (!session_id) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	ep_start_streaming (session_id);

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

static RESULT
validate_default_provider_config (EventPipeSession *session)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeSessionProviderList *provider_list = ep_session_get_providers (session);
	EventPipeSessionProvider *session_provider = ep_rt_session_provider_list_find_by_name (ep_session_provider_list_get_providers_cref (provider_list), "Microsoft-Windows-DotNETRuntime");
	ep_raise_error_if_nok (session_provider != NULL);

	test_location = 1;

	ep_raise_error_if_nok (!ep_rt_utf8_string_compare (ep_session_provider_get_provider_name (session_provider), "Microsoft-Windows-DotNETRuntime"));

	test_location = 2;

	ep_raise_error_if_nok (ep_session_provider_get_keywords (session_provider) == 0x4c14fccbd);

	test_location = 3;

	ep_raise_error_if_nok (ep_session_provider_get_logging_level (session_provider) == EP_EVENT_LEVEL_VERBOSE);

	test_location = 4;

	ep_raise_error_if_nok (ep_session_provider_get_filter_data (session_provider) == NULL);

	test_location = 5;

	session_provider = ep_rt_session_provider_list_find_by_name (ep_session_provider_list_get_providers_cref (provider_list), "Microsoft-Windows-DotNETRuntimePrivate");
	ep_raise_error_if_nok (session_provider != NULL);

	test_location = 6;

	ep_raise_error_if_nok (!ep_rt_utf8_string_compare (ep_session_provider_get_provider_name (session_provider), "Microsoft-Windows-DotNETRuntimePrivate"));

	test_location = 7;

	ep_raise_error_if_nok (ep_session_provider_get_keywords (session_provider) == 0x4002000b);

	test_location = 8;

	ep_raise_error_if_nok (ep_session_provider_get_logging_level (session_provider) == EP_EVENT_LEVEL_VERBOSE);

	test_location = 9;

	ep_raise_error_if_nok (ep_session_provider_get_filter_data (session_provider) == NULL);

	test_location = 10;

	session_provider = ep_rt_session_provider_list_find_by_name (ep_session_provider_list_get_providers_cref (provider_list), "Microsoft-DotNETCore-SampleProfiler");
	ep_raise_error_if_nok (session_provider != NULL);

	test_location = 11;

	ep_raise_error_if_nok (!ep_rt_utf8_string_compare (ep_session_provider_get_provider_name (session_provider), "Microsoft-DotNETCore-SampleProfiler"));

	test_location = 12;

	ep_raise_error_if_nok (ep_session_provider_get_keywords (session_provider) == 0);

	test_location = 13;

	ep_raise_error_if_nok (ep_session_provider_get_logging_level (session_provider) == EP_EVENT_LEVEL_VERBOSE);

	test_location = 14;

	ep_raise_error_if_nok (ep_session_provider_get_filter_data (session_provider) == NULL);

	test_location = 15;

ep_on_exit:
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_enable_disable_default_provider_config (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeSessionID session_id = 0;

	session_id = ep_enable_2 (
		TEST_FILE,
		1,
		NULL,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		NULL,
		NULL);

	if (!session_id) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	result = validate_default_provider_config ((EventPipeSession *)session_id);
	ep_raise_error_if_nok (result == NULL);

	test_location = 3;

	ep_start_streaming (session_id);

	if (!ep_enabled ()) {
		result = FAILED ("event pipe disabled");
		ep_raise_error ();
	}

ep_on_exit:
	ep_disable (session_id);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_enable_disable_multiple_default_provider_config (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeSessionID session_id_1 = 0;
	EventPipeSessionID session_id_2 = 0;

	session_id_1 = ep_enable_2 (
		TEST_FILE,
		1,
		NULL,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		NULL,
		NULL);

	if (!session_id_1) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	result = validate_default_provider_config ((EventPipeSession *)session_id_1);
	ep_raise_error_if_nok (result == NULL);

	test_location = 3;

	ep_start_streaming (session_id_1);

	if (!ep_enabled ()) {
		result = FAILED ("event pipe disabled");
		ep_raise_error ();
	}

	test_location = 4;

	session_id_2 = ep_enable_2 (
		TEST_FILE_2,
		1,
		NULL,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		NULL,
		NULL);

	if (!session_id_2) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 5;

	result = validate_default_provider_config ((EventPipeSession *)session_id_2);
	ep_raise_error_if_nok (result == NULL);

	test_location = 6;

	ep_start_streaming (session_id_2);

	if (!ep_enabled ()) {
		result = FAILED ("event pipe disabled");
		ep_raise_error ();
	}

ep_on_exit:
	ep_disable (session_id_1);
	ep_disable (session_id_2);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_enable_disable_provider_config (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	const ep_char8_t *provider_config = TEST_PROVIDER_NAME ":1:0:";
	EventPipeSessionID session_id = 0;

	session_id = ep_enable_2 (
		TEST_FILE,
		1,
		provider_config,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		NULL,
		NULL);

	if (!session_id) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	EventPipeSessionProviderList *provider_list = ep_session_get_providers ((EventPipeSession *)session_id);
	EventPipeSessionProvider *session_provider = ep_rt_session_provider_list_find_by_name (ep_session_provider_list_get_providers_cref (provider_list), TEST_PROVIDER_NAME);
	ep_raise_error_if_nok (session_provider != NULL);

	test_location = 3;

	ep_raise_error_if_nok (!ep_rt_utf8_string_compare (ep_session_provider_get_provider_name (session_provider), TEST_PROVIDER_NAME));

	test_location = 4;

	ep_raise_error_if_nok (ep_session_provider_get_keywords (session_provider) == 1);

	test_location = 5;

	ep_raise_error_if_nok (ep_session_provider_get_logging_level (session_provider) == EP_EVENT_LEVEL_LOGALWAYS);

	test_location = 6;

	ep_raise_error_if_nok (ep_session_provider_get_filter_data (session_provider) == NULL);

	test_location = 7;

	ep_start_streaming (session_id);

	if (!ep_enabled ()) {
		result = FAILED ("event pipe disabled");
		ep_raise_error ();
	}

ep_on_exit:
	ep_disable (session_id);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_enable_disable_provider_parse_default_config (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	const ep_char8_t *provider_config =
		"Microsoft-Windows-DotNETRuntime"
		":0x4c14fccbd"
		":5"
		":"
		","
		"Microsoft-Windows-DotNETRuntimePrivate"
		":0x4002000b"
		":5"
		":"
		","
		"Microsoft-DotNETCore-SampleProfiler"
		":0"
		":5"
		":";

	EventPipeSessionID session_id = 0;

	session_id = ep_enable_2 (
		TEST_FILE,
		1,
		provider_config,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		NULL,
		NULL);

	if (!session_id) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	result = validate_default_provider_config ((EventPipeSession *)session_id);
	ep_raise_error_if_nok (result == NULL);

	test_location = 3;

	ep_start_streaming (session_id);

	if (!ep_enabled ()) {
		result = FAILED ("event pipe disabled");
		ep_raise_error ();
	}

ep_on_exit:
	ep_disable (session_id);
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

	EventPipeProviderConfiguration *current_provider_config =ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
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
		NULL,
		NULL);

	if (!session_id) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	ep_start_streaming (session_id);

	test_provider = ep_create_provider (TEST_PROVIDER_NAME, provider_callback, NULL, &provider_callback_data);
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

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
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
test_session_start_streaming (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeSessionID session_id = 0;
	EventPipeProviderConfiguration provider_config;

	EventPipeProviderConfiguration *current_provider_config =ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
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
		NULL,
		NULL);

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
test_session_write_event (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeSessionID session_id = 0;
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = NULL;
	bool write_result = false;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_enable (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4,false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_streaming (session_id);

	EventPipeEventPayload payload;;
	ep_event_payload_init (&payload, NULL, 0);
	write_result = ep_session_write_event ((EventPipeSession *)session_id, ep_rt_thread_get_handle (), ep_event, &payload, NULL, NULL, NULL, NULL);
	ep_event_payload_fini (&payload);

	ep_raise_error_if_nok (write_result == true);

ep_on_exit:
	ep_disable (session_id);
	ep_delete_provider (provider);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_session_write_event_seq_point (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeSessionID session_id = 0;
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = NULL;
	bool write_result = false;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_enable (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_streaming (session_id);

	EventPipeEventPayload payload;;
	ep_event_payload_init (&payload, NULL, 0);
	write_result = ep_session_write_event ((EventPipeSession *)session_id, ep_rt_thread_get_handle (), ep_event, &payload, NULL, NULL, NULL, NULL);
	ep_event_payload_fini (&payload);

	ep_raise_error_if_nok (write_result == true);

	test_location = 5;

	ep_session_write_sequence_point_unbuffered ((EventPipeSession *)session_id);

ep_on_exit:
	ep_disable (session_id);
	ep_delete_provider (provider);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_session_write_wait_get_next_event (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeSessionID session_id = 0;
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = NULL;
	bool write_result = false;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_enable (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_streaming (session_id);

	EventPipeEventPayload payload;;
	ep_event_payload_init (&payload, NULL, 0);
	write_result = ep_session_write_event ((EventPipeSession *)session_id, ep_rt_thread_get_handle (), ep_event, &payload, NULL, NULL, NULL, NULL);
	ep_event_payload_fini (&payload);

	ep_raise_error_if_nok (write_result == true);

	test_location = 5;

	EventPipeEventInstance *event_instance = ep_session_get_next_event ((EventPipeSession *)session_id);

	ep_raise_error_if_nok (event_instance != NULL);

	test_location = 6;

	event_instance = ep_session_get_next_event ((EventPipeSession *)session_id);

	ep_raise_error_if_nok (event_instance == NULL);

ep_on_exit:
	ep_disable (session_id);
	ep_delete_provider (provider);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_session_write_get_next_event (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeSessionID session_id = 0;
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = NULL;
	bool write_result = false;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_enable (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_streaming (session_id);

	// Starts as signaled.
	// TODO: Is this expected behavior, just a way to notify observer that we are up and running?
	uint32_t test = ep_rt_wait_event_wait ((ep_rt_wait_event_handle_t *)ep_session_get_wait_event ((EventPipeSession *)session_id), 0, false);
	ep_raise_error_if_nok (test == 0);

	test_location = 5;

	EventPipeEventPayload payload;;
	ep_event_payload_init (&payload, NULL, 0);
	write_result = ep_session_write_event ((EventPipeSession *)session_id, ep_rt_thread_get_handle (), ep_event, &payload, NULL, NULL, NULL, NULL);
	ep_event_payload_fini (&payload);

	ep_raise_error_if_nok (write_result == true);

	test_location = 6;

	// TODO: Is this really the correct behavior, first write signals event, meaning that buffer will converted to read only
	// with just one event in it.
	test = ep_rt_wait_event_wait ((ep_rt_wait_event_handle_t *)ep_session_get_wait_event ((EventPipeSession *)session_id), 0, false);
	ep_raise_error_if_nok (test == 0);

	test_location = 7;

	EventPipeEventInstance *event_instance = ep_session_get_next_event ((EventPipeSession *)session_id);
	ep_raise_error_if_nok (event_instance != NULL);

	test_location = 8;

	event_instance = ep_session_get_next_event ((EventPipeSession *)session_id);
	ep_raise_error_if_nok (event_instance == NULL);

ep_on_exit:
	ep_disable (session_id);
	ep_delete_provider (provider);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_session_write_suspend_event (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeSessionID session_id = 0;
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = NULL;
	bool write_result = false;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_enable (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_streaming (session_id);

	EventPipeEventPayload payload;;
	ep_event_payload_init (&payload, NULL, 0);
	write_result = ep_session_write_event ((EventPipeSession *)session_id, ep_rt_thread_get_handle (), ep_event, &payload, NULL, NULL, NULL, NULL);
	ep_event_payload_fini (&payload);

	ep_raise_error_if_nok (write_result == true);

	test_location = 5;

	// ep_session_suspend_write_event_happens in disable session.

ep_on_exit:
	ep_disable (session_id);
	ep_delete_provider (provider);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

// TODO: Add test setting rundown and write events.

// TODO: Suspend write and write events.

static RESULT
test_write_event (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeSessionID session_id = 0;
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = NULL;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_enable (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_streaming (session_id);

	EventData data[1];
	ep_event_data_init (&data[0], 0, 0, 0);
	ep_write_event_2 (ep_event, data, EP_ARRAY_SIZE (data), NULL, NULL);
	ep_event_data_fini (data);

ep_on_exit:
	ep_disable (session_id);
	ep_delete_provider (provider);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_write_get_next_event (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeSessionID session_id = 0;
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = NULL;
	EventPipeEventInstance *event_instance = NULL;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_enable (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_streaming (session_id);

	EventData data[1];
	ep_event_data_init (&data[0], 0, 0, 0);
	ep_write_event_2 (ep_event, data, EP_ARRAY_SIZE (data), NULL, NULL);
	ep_event_data_fini (data);

	event_instance = ep_get_next_event (session_id);
	ep_raise_error_if_nok (event_instance != NULL);

	test_location = 5;

	event_instance = ep_get_next_event (session_id);
	ep_raise_error_if_nok (event_instance == NULL);

ep_on_exit:
	ep_disable (session_id);
	ep_delete_provider (provider);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_write_wait_get_next_event (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeSessionID session_id = 0;
	EventPipeSession *session = NULL;
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = NULL;
	EventPipeEventInstance *event_instance = NULL;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_enable (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	session = ep_get_session (session_id);
	ep_raise_error_if_nok (session != NULL);

	test_location = 4;

	ep_start_streaming (session_id);

	// Starts as signaled.
	// TODO: Is this expected behavior, just a way to notify observer that we are up and running?
	uint32_t test = ep_rt_wait_event_wait (ep_session_get_wait_event (session), 0, false);
	ep_raise_error_if_nok (test == 0);

	test_location = 5;

	test = ep_rt_wait_event_wait (ep_session_get_wait_event (session), 0, false);
	ep_raise_error_if_nok (test != 0);

	test_location = 6;

	EventData data[1];
	ep_event_data_init (&data[0], 0, 0, 0);
	for (int i = 0; i < 100; i++)
		ep_write_event_2 (ep_event, data, EP_ARRAY_SIZE (data), NULL, NULL);
	ep_event_data_fini (data);

	//Should be signaled, since we should have buffers put in readonly by now.
	test = ep_rt_wait_event_wait (ep_session_get_wait_event (session), 0, false);
	ep_raise_error_if_nok (test == 0);

	test_location = 7;

	event_instance = ep_get_next_event (session_id);
	ep_raise_error_if_nok (event_instance != NULL);

	//Drain all events.
	while (ep_get_next_event (session_id));

ep_on_exit:
	ep_disable (session_id);
	ep_delete_provider (provider);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_write_event_perf (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeSessionID session_id = 0;
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = NULL;
	int64_t accumulted_write_time_ticks = 0;
	uint32_t events_written = 0;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_enable (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_streaming (session_id);

	EventData data[1];
	ep_event_data_init (&data[0], 0, 0, 0);

	// Write in chunks of 1000 events, all should fit into buffer manager.
	for (events_written = 0; events_written < 10 * 1000 * 1000; events_written += 1000) {
		int64_t start = ep_perf_timestamp_get ();
		for (uint32_t i = 0; i < 1000; i++)
			ep_write_event_2 (ep_event, data, EP_ARRAY_SIZE (data), NULL, NULL);
		int64_t stop = ep_perf_timestamp_get ();
		accumulted_write_time_ticks += stop - start;

		// Drain events to not end up in having buffer manager OOM.
		while (ep_get_next_event (session_id));
	}

	ep_event_data_fini (data);

	float accumulted_write_time_sec = ((float)accumulted_write_time_ticks / (float)ep_perf_frequency_query ());
	float events_written_per_sec = (float)events_written / (accumulted_write_time_sec ? accumulted_write_time_sec : 1.0);

	// Measured number of events/second for one thread.
	// TODO: Setup acceptable pass/failure metrics.
	printf ("\n\tPerformance stats:\n");
	printf ("\t\tTotal number of events: %i\n", events_written);
	printf ("\t\tTotal time in sec: %.2f\n\t\tTotal number of events written per sec/core: %.2f\n\t", accumulted_write_time_sec, events_written_per_sec);

ep_on_exit:
	ep_disable (session_id);
	ep_delete_provider (provider);
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

// TODO: Add multithreaded test writing into private/shared sessions.

// TODO: Add consumer thread test, flushing file buffers/session, acting on signal.

static RESULT
test_eventpipe_mem_checkpoint (void)
{
	RESULT result = NULL;
#ifdef _CRTDBG_MAP_ALLOC
	// Need to emulate a thread exit to make sure TLS gets cleaned up for current thread
	// or we will get memory leaks reported.
	extern void ep_rt_mono_thread_exited (void);
	ep_rt_mono_thread_exited ();

	_CrtMemCheckpoint (&eventpipe_memory_end_snapshot);
	if ( _CrtMemDifference(&eventpipe_memory_diff_snapshot, &eventpipe_memory_start_snapshot, &eventpipe_memory_end_snapshot) ) {
		_CrtMemDumpStatistics( &eventpipe_memory_diff_snapshot );
		result = FAILED ("Memory leak detected!");
	}
	_CrtMemCheckpoint (&eventpipe_memory_start_snapshot);
#endif
	return result;
}

static RESULT
test_eventpipe_reset_mem_checkpoint (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&eventpipe_memory_start_snapshot);
#endif
	return NULL;
}


static RESULT
test_eventpipe_teardown (void)
{
	uint32_t test_location = 0;

#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&eventpipe_memory_end_snapshot);
	if ( _CrtMemDifference(&eventpipe_memory_diff_snapshot, &eventpipe_memory_start_snapshot, &eventpipe_memory_end_snapshot) ) {
		_CrtMemDumpStatistics( &eventpipe_memory_diff_snapshot );
		return FAILED ("Memory leak detected!");
	}
#endif
	test_location = 1;

	EP_LOCK_ENTER (section1)
		ep_sample_profiler_shutdown ();
	EP_LOCK_EXIT (section1)

	return NULL;

ep_on_error:
	return FAILED ("Failed at test location=%i", test_location);
}

static Test ep_tests [] = {
	{"test_eventpipe_setup", test_eventpipe_setup},
	{"test_create_delete_provider", test_create_delete_provider},
	{"test_stress_create_delete_provider", test_stress_create_delete_provider},
	{"test_get_provider", test_get_provider},
	{"test_create_same_provider_twice", test_create_same_provider_twice},
	{"test_enable_disable", test_enable_disable},
	{"test_enable_disable_provider_config", test_enable_disable_provider_config},
	{"test_create_delete_provider_with_callback", test_create_delete_provider_with_callback},
	{"test_build_event_metadata", test_build_event_metadata},
	{"test_session_start_streaming", test_session_start_streaming},
	{"test_session_write_event", test_session_write_event_seq_point},
	{"test_session_write_event_seq_point", test_session_write_event_seq_point},
	{"test_session_write_get_next_event", test_session_write_get_next_event},
	{"test_session_write_wait_get_next_event", test_session_write_wait_get_next_event},
	{"test_session_write_suspend_event", test_session_write_suspend_event},
	{"test_write_event", test_write_event},
	{"test_write_get_next_event", test_write_get_next_event},
	{"test_write_wait_get_next_event", test_write_wait_get_next_event},
#ifdef TEST_PERF
	{"test_write_event_perf", test_write_event_perf},
#endif
	{"test_eventpipe_mem_checkpoint", test_eventpipe_mem_checkpoint},
	{"test_enable_disable_default_provider_config", test_enable_disable_default_provider_config},
	{"test_enable_disable_multiple_default_provider_config", test_enable_disable_multiple_default_provider_config},
	{"test_enable_disable_provider_parse_default_config", test_enable_disable_provider_parse_default_config},
	{"test_eventpipe_reset_mem_checkpoint", test_eventpipe_reset_mem_checkpoint},
	{"test_eventpipe_teardown", test_eventpipe_teardown},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_tests_init, ep_tests)
