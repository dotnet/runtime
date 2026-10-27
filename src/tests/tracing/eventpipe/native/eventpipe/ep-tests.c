// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(_MSC_VER) && defined(_DEBUG)
#include "ep-tests-debug.h"
#endif

#include <eventpipe/ep.h>
#include <eventpipe/ep-config.h>
#include <eventpipe/ep-event.h>
#include <eventpipe/ep-session.h>
#include <eventpipe/ep-event-instance.h>
#include <eventpipe/ep-event-payload.h>
#include <eventpipe/ep-buffer-manager.h>
#include <eventpipe/ep-ipc-stream.h>
#include <eventpipe/ep-sample-profiler.h>
#include "test.h"

#define TEST_PROVIDER_NAME "MyTestProvider"
#define TEST_FILE "./ep_test_create_file.txt"
#define TEST_FILE_2 "./ep_test_create_file_2.txt"

#ifdef _CRTDBG_MAP_ALLOC
static _CrtMemState eventpipe_memory_start_snapshot;
static _CrtMemState eventpipe_memory_end_snapshot;
static _CrtMemState eventpipe_memory_diff_snapshot;
#endif

static EventPipeWriteEventResult
write_session_event (
	EventPipeSessionID session_id,
	EventPipeEvent *ep_event,
	EventPipeEventPayload *payload)
{
	EventPipeThread *thread = ep_thread_get_or_create ();
	if (!thread)
		return EP_WRITE_EVENT_RESULT_NOT_WRITTEN;

	EventPipeSession *session = (EventPipeSession *)(uintptr_t)session_id;
	// Direct session writes bypass the session-use marker normally published by ep_write_event.
	ep_thread_set_session_use_in_progress (thread, ep_session_get_index (session) | EP_SESSION_USE_WRITE_BUFFER_IN_USE);
	EventPipeWriteEventResult result = ep_session_write_event (
		session, ep_rt_thread_get_handle (), ep_event, payload, NULL, NULL, NULL, NULL);
	ep_thread_set_session_use_in_progress (thread, UINT32_MAX);
	return result;
}

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
		char *provider_name = ep_rt_utf8_string_printf_alloc (TEST_PROVIDER_NAME "_%i", i);
		test_providers [i] = ep_create_provider (provider_name, NULL, NULL);
		free (provider_name);

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
	EventPipeProvider *test_provider = NULL;
	EventPipeProvider *test_provider2 = NULL;
	EventPipeProvider *returned_test_provider = NULL;

	test_provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
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

	test_provider2 = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
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

	session_id = ep_init_session (
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

	ep_start_session (session_id);

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
	EventPipeSessionProvider *session_provider = ep_session_provider_list_find_by_name (ep_session_provider_list_get_providers (provider_list), "Microsoft-Windows-DotNETRuntime");
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

	session_provider = ep_session_provider_list_find_by_name (ep_session_provider_list_get_providers (provider_list), "Microsoft-Windows-DotNETRuntimePrivate");
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

	session_provider = ep_session_provider_list_find_by_name (ep_session_provider_list_get_providers (provider_list), "Microsoft-DotNETCore-SampleProfiler");
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

	session_id = ep_init_session_2 (
		TEST_FILE,
		1,
		NULL,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		NULL,
		NULL,
		EP_BUFFERING_MODE_DROP);

	if (!session_id) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	result = validate_default_provider_config ((EventPipeSession *)(uintptr_t)session_id);
	ep_raise_error_if_nok (result == NULL);

	test_location = 3;

	ep_start_session (session_id);

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

	session_id_1 = ep_init_session_2 (
		TEST_FILE,
		1,
		NULL,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		NULL,
		NULL,
		EP_BUFFERING_MODE_DROP);

	if (!session_id_1) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	result = validate_default_provider_config ((EventPipeSession *)(uintptr_t)session_id_1);
	ep_raise_error_if_nok (result == NULL);

	test_location = 3;

	ep_start_session (session_id_1);

	if (!ep_enabled ()) {
		result = FAILED ("event pipe disabled");
		ep_raise_error ();
	}

	test_location = 4;

	session_id_2 = ep_init_session_2 (
		TEST_FILE_2,
		1,
		NULL,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		NULL,
		NULL,
		EP_BUFFERING_MODE_DROP);

	if (!session_id_2) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 5;

	result = validate_default_provider_config ((EventPipeSession *)(uintptr_t)session_id_2);
	ep_raise_error_if_nok (result == NULL);

	test_location = 6;

	ep_start_session (session_id_2);

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

	session_id = ep_init_session_2 (
		TEST_FILE,
		1,
		provider_config,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		NULL,
		NULL,
		EP_BUFFERING_MODE_DROP);

	if (!session_id) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	EventPipeSessionProviderList *provider_list = ep_session_get_providers ((EventPipeSession *)(uintptr_t)session_id);
	EventPipeSessionProvider *session_provider = ep_session_provider_list_find_by_name (ep_session_provider_list_get_providers (provider_list), TEST_PROVIDER_NAME);
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

	ep_start_session (session_id);

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

	session_id = ep_init_session_2 (
		TEST_FILE,
		1,
		provider_config,
		EP_SESSION_TYPE_FILE,
		EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false,
		NULL,
		NULL,
		NULL,
		EP_BUFFERING_MODE_DROP);

	if (!session_id) {
		result = FAILED ("Failed to enable session");
		ep_raise_error ();
	}

	test_location = 2;

	result = validate_default_provider_config ((EventPipeSession *)(uintptr_t)session_id);
	ep_raise_error_if_nok (result == NULL);

	test_location = 3;

	ep_start_session (session_id);

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
EP_CALLBACK_CALLTYPE
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

	EventPipeProviderConfiguration *current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	session_id = ep_init_session (
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

	ep_start_session (session_id);

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
	// This isolated session has stopped, so its deferred providers can now be reclaimed.
	ep_config_delete_deferred_providers (ep_config_get ());
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

typedef struct {
	EventPipeEvent *event;
	EventPipeSessionID stopping_session;
	EventPipeSessionID capture_session;
	uint32_t captures;
	uint32_t events_before_stop;
	uint32_t notifications;
	uint32_t notifications_at_capture;
} StopCaptureStateData;

static
bool
stop_capture_state_read_events (
	EventPipeSessionID session_id,
	EventPipeEvent *event,
	uint32_t marker,
	uint32_t expected_count)
{
	uint32_t count = 0;
	bool valid = true;
	EventPipeEventInstance *instance;
	while ((instance = ep_get_next_event (session_id)) != NULL) {
		if (ep_event_instance_get_ep_event (instance) == event) {
			count++;
			valid = valid && ep_event_instance_get_data_len (instance) == sizeof (marker) &&
				memcmp (ep_event_instance_get_data (instance), &marker, sizeof (marker)) == 0;
		}
	}
	return valid && count == expected_count;
}

static
void
EP_CALLBACK_CALLTYPE
stop_capture_state_callback (
	const uint8_t *source_id,
	unsigned long control_code,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_context)
{
	StopCaptureStateData *data = (StopCaptureStateData *)callback_context;
	if (control_code == 2 /* EVENT_CONTROL_CODE_CAPTURE_STATE */) {
		data->captures++;
		data->capture_session = 0;
		if (source_id != NULL)
			memcpy (&data->capture_session, source_id, sizeof (data->capture_session));
		data->notifications_at_capture = data->notifications;
		uint32_t marker = data->captures;
		EventData event_data;
		ep_event_data_init (&event_data, (uint64_t)(uintptr_t)&marker, sizeof (marker), 0);
		ep_write_event_2 (data->event, &event_data, 1, NULL, NULL);
		ep_event_data_fini (&event_data);

		if (data->stopping_session != 0 && stop_capture_state_read_events (data->stopping_session, data->event, marker, 1))
			data->events_before_stop++;
	} else {
		data->notifications++;
	}
}

static RESULT
test_stop_capture_provider_state (
	const ep_char8_t *provider_name,
	bool expect_capture)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeProvider *provider = NULL;
	EventPipeSessionID sessions [3] = {0, 0, 0};
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration unrelated_provider_config;
	EventPipeProviderConfiguration *config = NULL;
	EventPipeProviderConfiguration *unrelated_config = NULL;
	StopCaptureStateData data = {0};

	config = ep_provider_config_init (&provider_config, provider_name, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (config != NULL);
	unrelated_config = ep_provider_config_init (&unrelated_provider_config, "UnrelatedStopCaptureStateProvider", 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (unrelated_config != NULL);
	provider = ep_create_provider (provider_name, stop_capture_state_callback, &data);
	ep_raise_error_if_nok (provider != NULL);
	data.event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (data.event != NULL);

	test_location = 1;
	for (uint32_t i = 0; i < ARRAY_SIZE (sessions); ++i) {
		sessions [i] = ep_init_session (
			NULL, 1, i == 2 ? unrelated_config : config, 1, EP_SESSION_TYPE_LISTENER, EP_SERIALIZATION_FORMAT_NETTRACE_V4,
			false, NULL, NULL, NULL);
		ep_raise_error_if_nok (sessions [i] != 0);
		ep_start_session (sessions [i]);
	}

	test_location = 2;
	ep_raise_error_if_nok (data.captures == 0 && data.notifications == 2);
	ep_raise_error_if_nok (stop_capture_state_read_events (sessions [0], data.event, 0, 0));
	ep_raise_error_if_nok (stop_capture_state_read_events (sessions [1], data.event, 0, 0));

	data.stopping_session = sessions [2];
	ep_disable (sessions [2]);
	sessions [2] = 0;
	data.stopping_session = 0;
	ep_raise_error_if_nok (data.captures == 0 && data.notifications == 2);

	test_location = 3;
	data.stopping_session = sessions [0];
	ep_disable (sessions [0]);
	sessions [0] = 0;
	if (data.captures != (expect_capture ? 1u : 0u)) {
		result = FAILED ("Expected %u pre-stop capture callbacks, observed %u", expect_capture ? 1u : 0u, data.captures);
		ep_raise_error ();
	}
	ep_raise_error_if_nok (data.capture_session == (expect_capture ? data.stopping_session : 0));
	data.stopping_session = 0;
	ep_raise_error_if_nok (data.events_before_stop == (expect_capture ? 1u : 0u));
	ep_raise_error_if_nok (data.notifications_at_capture == (expect_capture ? 2u : 0u));
	ep_raise_error_if_nok (data.notifications == 3);

	test_location = 4;
	ep_raise_error_if_nok (stop_capture_state_read_events (sessions [1], data.event, 1, expect_capture ? 1u : 0u));

	test_location = 5;
	data.stopping_session = sessions [1];
	ep_disable (sessions [1]);
	sessions [1] = 0;
	ep_raise_error_if_nok (data.capture_session == (expect_capture ? data.stopping_session : 0));
	data.stopping_session = 0;
	ep_raise_error_if_nok (data.captures == (expect_capture ? 2u : 0u));
	ep_raise_error_if_nok (data.events_before_stop == (expect_capture ? 2u : 0u));
	ep_raise_error_if_nok (data.notifications_at_capture == (expect_capture ? 3u : 0u));
	ep_raise_error_if_nok (data.notifications == 4);

ep_on_exit:
	data.stopping_session = 0;
	for (uint32_t i = 0; i < ARRAY_SIZE (sessions); ++i)
		ep_disable (sessions [i]);
	ep_delete_provider (provider);
	ep_provider_config_fini (config);
	ep_provider_config_fini (unrelated_config);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_stop_capture_known_provider_state (void)
{
	return test_stop_capture_provider_state ("System.Runtime.CompilerServices.AsyncProfilerEventSource", true);
}

static RESULT
test_stop_does_not_capture_other_provider_state (void)
{
	return test_stop_capture_provider_state (TEST_PROVIDER_NAME, false);
}

#define STOP_CAPTURE_STATE_WAIT_TIMEOUT_MS 30000
#define STOP_CAPTURE_STATE_WRITE_COUNT 128

typedef struct {
	IpcStream stream;
	minipal_mutex capture_lock;
	EventPipeEvent *event;
	EventPipeEvent *marker_event;
	EventPipeThread *writer_thread;
	EventPipeSessionID stopping_session;
	ep_rt_wait_event_handle_t reader_paused;
	ep_rt_wait_event_handle_t allow_reader;
	ep_rt_wait_event_handle_t writer_started;
	ep_rt_wait_event_handle_t writer_done;
	ep_rt_wait_event_handle_t capture_entered;
	ep_rt_wait_event_handle_t stop_done;
	volatile uint32_t pause_reader;
	uint32_t writes;
	uint32_t captures;
	bool event_before_stop;
	bool wait_for_writer_lock;
} StopCaptureStateBlockingData;

static
void
stop_capture_state_stream_free (void *stream)
{
	ep_ipc_stream_fini ((IpcStream *)stream);
}

static
bool
stop_capture_state_stream_write (
	void *stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written,
	uint32_t timeout_ms)
{
	StopCaptureStateBlockingData *data = (StopCaptureStateBlockingData *)stream;
	if (ep_rt_volatile_load_uint32_t (&data->pause_reader)) {
		if (!ep_rt_wait_event_set (&data->reader_paused) ||
			ep_rt_wait_event_wait (&data->allow_reader, EP_INFINITE_WAIT, false) != 0)
			EP_UNREACHABLE ("Stop capture-state test reader gate wait failed");
	}
	*bytes_written = bytes_to_write;
	return true;
}

static
bool
stop_capture_state_stream_flush_or_close (void *stream)
{
	return true;
}

static IpcStreamVtable stop_capture_state_stream_vtable = {
	stop_capture_state_stream_free,
	NULL,
	stop_capture_state_stream_write,
	stop_capture_state_stream_flush_or_close,
	stop_capture_state_stream_flush_or_close,
	NULL
};

static
void
EP_CALLBACK_CALLTYPE
stop_capture_state_blocking_callback (
	const uint8_t *source_id,
	unsigned long control_code,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_context)
{
	StopCaptureStateBlockingData *data = (StopCaptureStateBlockingData *)callback_context;
	if (control_code != 2 /* EVENT_CONTROL_CODE_CAPTURE_STATE */ || data->stopping_session == 0)
		return;

	if (!ep_rt_wait_event_set (&data->capture_entered))
		EP_UNREACHABLE ("Failed to signal stop capture-state test callback");
	if (data->wait_for_writer_lock)
		minipal_mutex_enter (&data->capture_lock);
	data->captures++;
	uint32_t marker = data->captures;
	EventData event_data;
	ep_event_data_init (&event_data, (uint64_t)(uintptr_t)&marker, sizeof (marker), 0);
	ep_write_event_2 (data->marker_event, &event_data, 1, NULL, NULL);
	ep_event_data_fini (&event_data);
	data->event_before_stop = stop_capture_state_read_events (data->stopping_session, data->marker_event, marker, 1);
	if (data->wait_for_writer_lock)
		minipal_mutex_leave (&data->capture_lock);
}

EP_RT_DEFINE_THREAD_FUNC (stop_capture_state_blocked_writer)
{
	ep_rt_thread_params_t *params = (ep_rt_thread_params_t *)data;
	StopCaptureStateBlockingData *state = (StopCaptureStateBlockingData *)params->thread_params;
	EventPipeThread *thread = ep_thread_get_or_create ();
	if (thread == NULL)
		EP_UNREACHABLE ("Failed to create stop capture-state test writer thread state");
	ep_thread_addref (thread);
	state->writer_thread = thread;

	uint8_t payload [32 * 1024] = {0};
	EventData event_data;
	ep_event_data_init (&event_data, (uint64_t)(uintptr_t)payload, sizeof (payload), 0);
	minipal_mutex_enter (&state->capture_lock);
	if (!ep_rt_wait_event_set (&state->writer_started))
		EP_UNREACHABLE ("Failed to signal stop capture-state test writer startup");
	for (uint32_t i = 0; i < STOP_CAPTURE_STATE_WRITE_COUNT; ++i) {
		ep_write_event_2 (state->event, &event_data, 1, NULL, NULL);
		state->writes++;
	}
	minipal_mutex_leave (&state->capture_lock);
	ep_event_data_fini (&event_data);
	if (!ep_rt_wait_event_set (&state->writer_done))
		EP_UNREACHABLE ("Failed to signal stop capture-state test writer completion");
	return 0;
}

EP_RT_DEFINE_THREAD_FUNC (stop_capture_state_blocked_stop)
{
	ep_rt_thread_params_t *params = (ep_rt_thread_params_t *)data;
	StopCaptureStateBlockingData *state = (StopCaptureStateBlockingData *)params->thread_params;
	ep_disable (state->stopping_session);
	if (!ep_rt_wait_event_set (&state->stop_done))
		EP_UNREACHABLE ("Failed to signal stop capture-state test session stop");
	return 0;
}

static
bool
stop_capture_state_wait_for_blocked_writer (
	StopCaptureStateBlockingData *data,
	EventPipeSessionID session_id)
{
	uint32_t session_index = ep_session_get_index ((EventPipeSession *)(uintptr_t)session_id);
	ep_timestamp_t deadline = ep_perf_timestamp_get () + ep_perf_frequency_query () * (STOP_CAPTURE_STATE_WAIT_TIMEOUT_MS / 1000);
	uint32_t yields = 0;
	while (ep_perf_timestamp_get () < deadline) {
		// ep_write_event clears WRITE_BUFFER_IN_USE, retaining the session index, only when parking.
		if (ep_thread_get_session_use_in_progress (data->writer_thread) == session_index)
			return true;
		if (ep_rt_wait_event_wait (&data->writer_done, 0, false) == 0)
			return false;
		minipal_switch_to_thread (++yields);
	}
	return false;
}

static
bool
stop_capture_state_wait_for_capture (StopCaptureStateBlockingData *data)
{
	ep_timestamp_t deadline = ep_perf_timestamp_get () + ep_perf_frequency_query () * (STOP_CAPTURE_STATE_WAIT_TIMEOUT_MS / 1000);
	uint32_t yields = 0;
	while (ep_perf_timestamp_get () < deadline) {
		if (ep_rt_wait_event_wait (&data->capture_entered, 0, false) == 0)
			return true;
		if (ep_rt_wait_event_wait (&data->stop_done, 0, false) == 0)
			return ep_rt_wait_event_wait (&data->capture_entered, 0, false) == 0;
		minipal_switch_to_thread (++yields);
	}
	return false;
}

static RESULT
test_stop_capture_state_with_other_session_blocked (bool wait_for_writer_lock)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	StopCaptureStateBlockingData data = {0};
	ep_rt_wait_event_handle_t *waits [] = {
		&data.reader_paused, &data.allow_reader, &data.writer_started,
		&data.writer_done, &data.capture_entered, &data.stop_done
	};
	bool lock_initialized = false;
	bool writer_created = false;
	bool stopper_created = false;
	EventPipeProvider *provider = NULL;
	EventPipeSessionID sessions [2] = {0, 0};
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *config = NULL;
	EventPipeSessionOptions options;
	EventPipeBufferManager *buffer_manager = NULL;
	uint64_t thread_id = 0;

	data.wait_for_writer_lock = wait_for_writer_lock;
	for (uint32_t i = 0; i < ARRAY_SIZE (waits); ++i) {
		ep_rt_wait_event_alloc (waits [i], true, false);
		ep_raise_error_if_nok (ep_rt_wait_event_is_valid (waits [i]));
	}
	lock_initialized = minipal_mutex_init (&data.capture_lock);
	ep_raise_error_if_nok (lock_initialized);
	ep_ipc_stream_init (&data.stream, &stop_capture_state_stream_vtable);
	config = ep_provider_config_init (&provider_config, "System.Runtime.CompilerServices.AsyncProfilerEventSource", 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (config != NULL);
	provider = ep_create_provider ("System.Runtime.CompilerServices.AsyncProfilerEventSource", stop_capture_state_blocking_callback, &data);
	ep_raise_error_if_nok (provider != NULL);
	data.event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	data.marker_event = ep_provider_add_event (provider, 2, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (data.event != NULL && data.marker_event != NULL);

	test_location = 1;
	// Only B should exhaust its budget; A must retain room for the capture marker.
	sessions [0] = ep_init_session (
		NULL, 8, config, 1, EP_SESSION_TYPE_LISTENER, EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		false, NULL, NULL, NULL);
	ep_raise_error_if_nok (sessions [0] != 0);
	ep_start_session (sessions [0]);
	ep_session_options_init (
		&options, NULL, 1, config, 1, EP_SESSION_TYPE_IPCSTREAM, EP_SERIALIZATION_FORMAT_NETTRACE_V4,
		0, false, &data.stream, NULL, NULL, -1, EP_BUFFERING_MODE_BLOCK);
	sessions [1] = ep_init_session_3 (&options);
	ep_session_options_fini (&options);
	ep_raise_error_if_nok (sessions [1] != 0);
	ep_start_session (sessions [1]);
	buffer_manager = ep_session_get_buffer_manager ((EventPipeSession *)(uintptr_t)sessions [1]);
	ep_raise_error_if_nok (buffer_manager != NULL && ep_buffer_manager_get_buffering_mode (buffer_manager) == EP_BUFFERING_MODE_BLOCK);

	test_location = 2;
	ep_rt_volatile_store_uint32_t (&data.pause_reader, 1);
	writer_created = ep_rt_thread_create ((void *)stop_capture_state_blocked_writer, &data, EP_THREAD_TYPE_SESSION, &thread_id);
	ep_raise_error_if_nok (writer_created);
	ep_raise_error_if_nok (ep_rt_wait_event_wait (&data.writer_started, STOP_CAPTURE_STATE_WAIT_TIMEOUT_MS, false) == 0);
	ep_raise_error_if_nok (ep_rt_wait_event_wait (&data.reader_paused, STOP_CAPTURE_STATE_WAIT_TIMEOUT_MS, false) == 0);
	ep_raise_error_if_nok (stop_capture_state_wait_for_blocked_writer (&data, sessions [1]));

	test_location = 3;
	data.stopping_session = sessions [0];
	stopper_created = ep_rt_thread_create ((void *)stop_capture_state_blocked_stop, &data, EP_THREAD_TYPE_SESSION, &thread_id);
	ep_raise_error_if_nok (stopper_created);
	if (!stop_capture_state_wait_for_capture (&data)) {
		result = FAILED ("Session stop did not enter the capture callback while the other session's writer was blocked");
		ep_raise_error ();
	}
	ep_raise_error_if_nok (ep_rt_wait_event_wait (&data.writer_done, 0, false) == 258 /* WAIT_TIMEOUT */);
	if (wait_for_writer_lock) {
		ep_raise_error_if_nok (ep_rt_wait_event_wait (&data.stop_done, 0, false) == 258 /* WAIT_TIMEOUT */);
	} else {
		// Keep B's reader gated: capture must bypass its parked writer rather than join the wait queue.
		if (ep_rt_wait_event_wait (&data.stop_done, STOP_CAPTURE_STATE_WAIT_TIMEOUT_MS, false) != 0) {
			result = FAILED ("Capture-state write did not finish while the other session's reader remained gated");
			ep_raise_error ();
		}
		ep_raise_error_if_nok (data.captures == 1 && data.event_before_stop);
		ep_raise_error_if_nok (ep_rt_wait_event_wait (&data.writer_done, 0, false) == 258 /* WAIT_TIMEOUT */);
		ep_raise_error_if_nok (ep_thread_get_session_use_in_progress (data.writer_thread) ==
			ep_session_get_index ((EventPipeSession *)(uintptr_t)sessions [1]));
	}
	ep_raise_error_if_nok (!ep_buffer_manager_is_aborting (buffer_manager));

	test_location = 4;
	ep_raise_error_if_nok (ep_rt_wait_event_set (&data.allow_reader));
	ep_raise_error_if_nok (ep_rt_wait_event_wait (&data.writer_done, STOP_CAPTURE_STATE_WAIT_TIMEOUT_MS, false) == 0);
	ep_raise_error_if_nok (ep_rt_wait_event_wait (&data.stop_done, STOP_CAPTURE_STATE_WAIT_TIMEOUT_MS, false) == 0);
	if (data.writes != STOP_CAPTURE_STATE_WRITE_COUNT || data.captures != 1 || !data.event_before_stop) {
		result = FAILED ("Expected %u writes, one capture and a pre-stop marker; observed %u writes, %u captures, marker=%u",
			STOP_CAPTURE_STATE_WRITE_COUNT, data.writes, data.captures, (uint32_t)data.event_before_stop);
		ep_raise_error ();
	}
	ep_raise_error_if_nok (!ep_buffer_manager_is_aborting (buffer_manager));

ep_on_exit:
	if (ep_rt_wait_event_is_valid (&data.allow_reader) && !ep_rt_wait_event_set (&data.allow_reader))
		EP_UNREACHABLE ("Failed to release stop capture-state test reader");
	// Do not return or free the fixture while a worker can still access it, even on a failed assertion.
	if (writer_created && ep_rt_wait_event_wait (&data.writer_done, STOP_CAPTURE_STATE_WAIT_TIMEOUT_MS, false) != 0)
		EP_UNREACHABLE ("Stop capture-state test writer did not finish");
	if (stopper_created) {
		if (ep_rt_wait_event_wait (&data.stop_done, STOP_CAPTURE_STATE_WAIT_TIMEOUT_MS, false) != 0)
			EP_UNREACHABLE ("Stop capture-state test session stop did not finish");
		sessions [0] = 0;
	}
	data.stopping_session = 0;
	for (uint32_t i = 0; i < ARRAY_SIZE (sessions); ++i)
		ep_disable (sessions [i]);
	ep_rt_test_threads_wait ();
	if (data.writer_thread != NULL)
		ep_thread_release (data.writer_thread);
	ep_delete_provider (provider);
	ep_provider_config_fini (config);
	if (lock_initialized)
		minipal_mutex_destroy (&data.capture_lock);
	for (uint32_t i = 0; i < ARRAY_SIZE (waits); ++i)
		ep_rt_wait_event_free (waits [i]);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_stop_capture_state_waits_for_other_session_drain (void)
{
	return test_stop_capture_state_with_other_session_blocked (true);
}

static RESULT
test_stop_capture_state_does_not_block_on_other_session (void)
{
	return test_stop_capture_state_with_other_session_blocked (false);
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
	ep_event_metadata_event_free (metadata_event);

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

	session_id = ep_init_session (
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

	ep_start_session (session_id);

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
	EventPipeWriteEventResult write_result = EP_WRITE_EVENT_RESULT_NOT_WRITTEN;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_init_session (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4,false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_session (session_id);

	EventPipeEventPayload payload;;
	ep_event_payload_init (&payload, NULL, 0);
	write_result = write_session_event (session_id, ep_event, &payload);
	ep_event_payload_fini (&payload);

	ep_raise_error_if_nok (write_result == EP_WRITE_EVENT_RESULT_WRITTEN);

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
	EventPipeWriteEventResult write_result = EP_WRITE_EVENT_RESULT_NOT_WRITTEN;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_init_session (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_session (session_id);

	EventPipeEventPayload payload;;
	ep_event_payload_init (&payload, NULL, 0);
	write_result = write_session_event (session_id, ep_event, &payload);
	ep_event_payload_fini (&payload);

	ep_raise_error_if_nok (write_result == EP_WRITE_EVENT_RESULT_WRITTEN);

	test_location = 5;

	ep_session_write_sequence_point_unbuffered ((EventPipeSession *)(uintptr_t)session_id);

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
	EventPipeWriteEventResult write_result = EP_WRITE_EVENT_RESULT_NOT_WRITTEN;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_init_session (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_session (session_id);

	EventPipeEventPayload payload;;
	ep_event_payload_init (&payload, NULL, 0);
	write_result = write_session_event (session_id, ep_event, &payload);
	ep_event_payload_fini (&payload);

	ep_raise_error_if_nok (write_result == EP_WRITE_EVENT_RESULT_WRITTEN);

	test_location = 5;

	EventPipeEventInstance *event_instance = ep_session_get_next_event ((EventPipeSession *)(uintptr_t)session_id);

	ep_raise_error_if_nok (event_instance != NULL);

	test_location = 6;

	event_instance = ep_session_get_next_event ((EventPipeSession *)(uintptr_t)session_id);

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
	EventPipeWriteEventResult write_result = EP_WRITE_EVENT_RESULT_NOT_WRITTEN;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_init_session (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_session (session_id);

	// Starts as signaled.
	// TODO: Is this expected behavior, just a way to notify observer that we are up and running?
	uint32_t test = ep_rt_wait_event_wait ((ep_rt_wait_event_handle_t *)ep_session_get_wait_event ((EventPipeSession *)(uintptr_t)session_id), 0, false);
	ep_raise_error_if_nok (test == 0);

	test_location = 5;

	EventPipeEventPayload payload;;
	ep_event_payload_init (&payload, NULL, 0);
	write_result = write_session_event (session_id, ep_event, &payload);
	ep_event_payload_fini (&payload);

	ep_raise_error_if_nok (write_result == EP_WRITE_EVENT_RESULT_WRITTEN);

	test_location = 6;

	// TODO: Is this really the correct behavior, first write signals event, meaning that buffer will converted to read only
	// with just one event in it.
	test = ep_rt_wait_event_wait ((ep_rt_wait_event_handle_t *)ep_session_get_wait_event ((EventPipeSession *)(uintptr_t)session_id), 0, false);
	ep_raise_error_if_nok (test == 0);

	test_location = 7;

	EventPipeEventInstance *event_instance = ep_session_get_next_event ((EventPipeSession *)(uintptr_t)session_id);
	ep_raise_error_if_nok (event_instance != NULL);

	test_location = 8;

	event_instance = ep_session_get_next_event ((EventPipeSession *)(uintptr_t)session_id);
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
	EventPipeWriteEventResult write_result = EP_WRITE_EVENT_RESULT_NOT_WRITTEN;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_init_session (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_session (session_id);

	EventPipeEventPayload payload;;
	ep_event_payload_init (&payload, NULL, 0);
	write_result = write_session_event (session_id, ep_event, &payload);
	ep_event_payload_fini (&payload);

	ep_raise_error_if_nok (write_result == EP_WRITE_EVENT_RESULT_WRITTEN);

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

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_init_session (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_session (session_id);

	EventData data[1];
	ep_event_data_init (&data[0], 0, 0, 0);
	ep_write_event_2 (ep_event, data, ARRAY_SIZE (data), NULL, NULL);
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

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_init_session (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_session (session_id);

	EventData data[1];
	ep_event_data_init (&data[0], 0, 0, 0);
	ep_write_event_2 (ep_event, data, ARRAY_SIZE (data), NULL, NULL);
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

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_init_session (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	session = ep_get_session (session_id);
	ep_raise_error_if_nok (session != NULL);

	test_location = 4;

	ep_start_session (session_id);

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
		ep_write_event_2 (ep_event, data, ARRAY_SIZE (data), NULL, NULL);
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

#if TEST_PERF
RESULT
test_write_event_perf (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeSessionID session_id = 0;
	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config = NULL;
	int64_t accumulated_write_time_ticks = 0;
	uint32_t events_written = 0;

	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 2;

	ep_event = ep_provider_add_event (provider, 1, 1, 1, EP_EVENT_LEVEL_LOGALWAYS, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 3;

	session_id = ep_init_session (TEST_FILE, 1, current_provider_config, 1, EP_SESSION_TYPE_FILE, EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, NULL, NULL, NULL);
	ep_raise_error_if_nok (session_id != 0);

	test_location = 4;

	ep_start_session (session_id);

	EventData data[1];
	ep_event_data_init (&data[0], 0, 0, 0);

	// Write in chunks of 1000 events, all should fit into buffer manager.
	for (events_written = 0; events_written < 10 * 1000 * 1000; events_written += 1000) {
		int64_t start = ep_perf_timestamp_get ();
		for (uint32_t i = 0; i < 1000; i++)
			ep_write_event_2 (ep_event, data, ARRAY_SIZE (data), NULL, NULL);
		int64_t stop = ep_perf_timestamp_get ();
		accumulated_write_time_ticks += stop - start;

		// Drain events to not end up in having buffer manager OOM.
		while (ep_get_next_event (session_id));
	}

	ep_event_data_fini (data);

	float accumulated_write_time_sec = ((float)accumulated_write_time_ticks / (float)ep_perf_frequency_query ());
	float events_written_per_sec = (float)events_written / (accumulated_write_time_sec ? accumulated_write_time_sec : 1.0);

	// Measured number of events/second for one thread.
	// TODO: Setup acceptable pass/failure metrics.
	printf ("\n\tPerformance stats:\n");
	printf ("\t\tTotal number of events: %i\n", events_written);
	printf ("\t\tTotal time in sec: %.2f\n\t\tTotal number of events written per sec/core: %.2f\n\t", accumulated_write_time_sec, events_written_per_sec);

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
#endif

// TODO: Add multithreaded test writing into private/shared sessions.

// TODO: Add consumer thread test, flushing file buffers/session, acting on signal.

static RESULT
test_eventpipe_mem_checkpoint (void)
{
	RESULT result = NULL;
#ifdef _CRTDBG_MAP_ALLOC
	// Need to emulate a thread exit to make sure TLS gets cleaned up for current thread
	// or we will get memory leaks reported.
	ep_rt_test_thread_exited ();

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
	{"test_stop_capture_known_provider_state", test_stop_capture_known_provider_state},
	{"test_stop_does_not_capture_other_provider_state", test_stop_does_not_capture_other_provider_state},
	{"test_stop_capture_state_waits_for_other_session_drain", test_stop_capture_state_waits_for_other_session_drain},
	{"test_stop_capture_state_does_not_block_on_other_session", test_stop_capture_state_does_not_block_on_other_session},
	{"test_build_event_metadata", test_build_event_metadata},
	{"test_session_start_streaming", test_session_start_streaming},
	{"test_session_write_event", test_session_write_event},
	{"test_session_write_event_seq_point", test_session_write_event_seq_point},
	{"test_session_write_get_next_event", test_session_write_get_next_event},
	{"test_session_write_wait_get_next_event", test_session_write_wait_get_next_event},
	{"test_session_write_suspend_event", test_session_write_suspend_event},
	{"test_write_event", test_write_event},
	{"test_write_get_next_event", test_write_get_next_event},
	{"test_write_wait_get_next_event", test_write_wait_get_next_event},
	{"test_eventpipe_mem_checkpoint", test_eventpipe_mem_checkpoint},
	{"test_enable_disable_default_provider_config", test_enable_disable_default_provider_config},
	{"test_enable_disable_multiple_default_provider_config", test_enable_disable_multiple_default_provider_config},
	{"test_enable_disable_provider_parse_default_config", test_enable_disable_provider_parse_default_config},
	{"test_eventpipe_reset_mem_checkpoint", test_eventpipe_reset_mem_checkpoint},
	{"test_eventpipe_teardown", test_eventpipe_teardown},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_tests_init, ep_tests)
