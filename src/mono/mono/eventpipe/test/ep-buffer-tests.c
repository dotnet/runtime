#if defined(_MSC_VER) && defined(_DEBUG)
#include "ep-tests-debug.h"
#endif

#include <eventpipe/ep.h>
#include <eventpipe/ep-config.h>
#include <eventpipe/ep-buffer.h>
#include <eventpipe/ep-event.h>
#include <eventpipe/ep-event-payload.h>
#include <eventpipe/ep-session.h>
#include <eglib/test/test.h>

#define TEST_PROVIDER_NAME "MyTestProvider"
#define TEST_FILE "./ep_test_create_file.txt"
#define TEST_EVENT_DATA "Dummy data for perf test."

//#define TEST_PERF

#ifdef _CRTDBG_MAP_ALLOC
static _CrtMemState eventpipe_memory_start_snapshot;
static _CrtMemState eventpipe_memory_end_snapshot;
static _CrtMemState eventpipe_memory_diff_snapshot;
#endif

static const gchar event_instance_data[] = "Dummy event test data %u.";

static
void
buffer_free (
	EventPipeBuffer *buffer,
	EventPipeThread *thread)
{
	ep_return_void_if_nok (buffer);

	// Buffer must be read only when freed.
	if (ep_buffer_get_volatile_state (buffer) == EP_BUFFER_STATE_WRITABLE) {
		EP_SPIN_LOCK_ENTER (ep_thread_get_rt_lock_ref (thread), section1)
		ep_buffer_convert_to_read_only (buffer);
		EP_SPIN_LOCK_EXIT (ep_thread_get_rt_lock_ref (thread), section1)
	}

	ep_buffer_free (buffer);
	return;

ep_on_error:
	return;
}

static
void
load_buffer_with_events_fini (
	EventPipeSession *session,
	EventPipeProvider *provider,
	EventPipeEvent *ep_event)
{
	ep_event_free (ep_event);
	ep_delete_provider (provider);
	ep_session_free (session);
}

static
RESULT
load_buffer_with_events_init (
	EventPipeSession **session,
	EventPipeProvider **provider,
	EventPipeEvent **ep_event)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	*session = NULL;
	*ep_event = NULL;
	*provider = NULL;

	EventPipeProviderConfiguration provider_config;
	EventPipeProviderConfiguration *current_provider_config;
	current_provider_config = ep_provider_config_init (&provider_config, TEST_PROVIDER_NAME, 1, EP_EVENT_LEVEL_LOGALWAYS, "");
	ep_raise_error_if_nok (current_provider_config != NULL);

	test_location = 1;

	EP_LOCK_ENTER (section1)
		*session = ep_session_alloc (
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

	ep_raise_error_if_nok (*session != NULL);

	test_location = 2;

	*provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	ep_raise_error_if_nok (*provider != NULL);

	test_location = 3;

	*ep_event = ep_event_alloc (*provider, 1, 1, 1, EP_EVENT_LEVEL_VERBOSE, false, NULL, 0);
	ep_raise_error_if_nok (*ep_event != NULL);

ep_on_exit:
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	load_buffer_with_events_fini (*session, *provider, *ep_event);
	*session = NULL;
	*provider = NULL;
	*ep_event = NULL;
	if (!result)
		result = FAILED ("Failed writing events into buffer at location=%i", test_location);
	ep_exit_error_handler ();
}

static
bool
load_buffer (
	EventPipeBuffer *buffer,
	EventPipeSession *session,
	EventPipeEvent *ep_event,
	uint32_t event_count,
	bool perf_test,
	uint32_t *events_written)
{
	bool result = true;
	uint32_t i = 0;
	for (; i < event_count; ++i) {
		EventPipeEventPayload payload;
		gchar *event_data = NULL;
		size_t event_data_len = 0;
		if (!perf_test) {
			event_data = g_strdup_printf (event_instance_data, i);
			event_data_len = strlen (event_data) + 1;
		}else {
			event_data = (gchar *)TEST_EVENT_DATA;
			event_data_len = ARRAY_SIZE (TEST_EVENT_DATA);
		}
		if (event_data) {
			ep_event_payload_init (&payload, (uint8_t *)event_data, (uint32_t)event_data_len);
			result = ep_buffer_write_event (buffer, ep_rt_thread_get_handle (), session, ep_event, &payload, NULL, NULL, NULL);
			ep_event_payload_fini (&payload);

			if (!perf_test)
				g_free (event_data);

			if (!result)
				break;
		}
	}

	if (events_written)
		*events_written = i;
	return result;
}

static
RESULT
load_buffer_with_events (
	EventPipeBuffer *buffer,
	uint32_t event_count)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeSession *session = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeProvider *provider = NULL;

	result = load_buffer_with_events_init (&session, &provider, &ep_event);
	ep_raise_error_if_nok (result == NULL);

	test_location = 2;
	ep_raise_error_if_nok (load_buffer (buffer, session, ep_event, event_count, false, NULL) == true);

ep_on_exit:
	load_buffer_with_events_fini (session, provider, ep_event);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed writing events into buffer at location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_buffer_setup (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&eventpipe_memory_start_snapshot);
#endif
	return NULL;
}

static RESULT
test_create_free_buffer (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeThread *thread = NULL;
	EventPipeBuffer *buffer = NULL;

	thread = ep_thread_alloc ();
	ep_raise_error_if_nok (thread != NULL);

	test_location = 1;

	buffer = ep_buffer_alloc (1024 *1024, thread, 0);
	ep_raise_error_if_nok (buffer != NULL);

	test_location = 2;

	EP_SPIN_LOCK_ENTER (ep_thread_get_rt_lock_ref (thread), section1)
	ep_buffer_convert_to_read_only (buffer);
	EP_SPIN_LOCK_EXIT (ep_thread_get_rt_lock_ref (thread), section1)

ep_on_exit:
	buffer_free (buffer, thread);
	ep_thread_free (thread);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_write_event_to_buffer (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeThread *thread = NULL;
	EventPipeBuffer *buffer = NULL;

	thread = ep_thread_alloc ();
	ep_raise_error_if_nok (thread != NULL);

	test_location = 1;

	buffer = ep_buffer_alloc (1024 *1024, thread, 0);
	ep_raise_error_if_nok (buffer != NULL);

	test_location = 2;

	result = load_buffer_with_events (buffer, 1);
	ep_raise_error_if_nok (result == NULL);

	test_location = 3;

	EP_SPIN_LOCK_ENTER (ep_thread_get_rt_lock_ref (thread), section1)
	ep_buffer_convert_to_read_only (buffer);
	EP_SPIN_LOCK_EXIT (ep_thread_get_rt_lock_ref (thread), section1)

ep_on_exit:
	buffer_free (buffer, thread);
	ep_thread_free (thread);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_read_event_from_buffer (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeThread *thread = NULL;
	EventPipeBuffer *buffer = NULL;

	thread = ep_thread_alloc ();
	ep_raise_error_if_nok (thread != NULL);

	test_location = 1;

	buffer = ep_buffer_alloc (1024 *1024, thread, 0);
	ep_raise_error_if_nok (buffer != NULL);

	test_location = 2;

	result = load_buffer_with_events (buffer, 1);
	ep_raise_error_if_nok (result == NULL);

	test_location = 3;

	EP_SPIN_LOCK_ENTER (ep_thread_get_rt_lock_ref (thread), section1)
	ep_buffer_convert_to_read_only (buffer);
	EP_SPIN_LOCK_EXIT (ep_thread_get_rt_lock_ref (thread), section1)

	test_location = 4;
	EventPipeEventInstance *current_event = ep_buffer_get_current_read_event (buffer);
	ep_raise_error_if_nok (current_event != NULL);

ep_on_exit:
	buffer_free (buffer, thread);
	ep_thread_free (thread);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_read_events_from_buffer (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	const int max_events = 100;

	EventPipeThread *thread = NULL;
	EventPipeBuffer *buffer = NULL;

	thread = ep_thread_alloc ();
	ep_raise_error_if_nok (thread != NULL);

	test_location = 1;

	buffer = ep_buffer_alloc (1024 *1024, thread, 0);
	ep_raise_error_if_nok (buffer != NULL);

	test_location = 2;

	result = load_buffer_with_events (buffer, max_events);
	ep_raise_error_if_nok (result == NULL);

	test_location = 3;

	EP_SPIN_LOCK_ENTER (ep_thread_get_rt_lock_ref (thread), section1)
	ep_buffer_convert_to_read_only (buffer);
	EP_SPIN_LOCK_EXIT (ep_thread_get_rt_lock_ref (thread), section1)

	test_location = 4;

	EventPipeEventInstance *instance = ep_buffer_get_current_read_event (buffer);
	ep_raise_error_if_nok (instance != NULL);

	test_location = 5;

	int event_count = 0;
	while (instance) {
		ep_buffer_move_next_read_event (buffer);
		instance = ep_buffer_get_current_read_event (buffer);
		event_count++;
		ep_raise_error_if_nok (ep_buffer_get_current_sequence_number (buffer) == event_count);
	}

	test_location = 6;
	ep_raise_error_if_nok (max_events == event_count);

	test_location = 7;
	ep_raise_error_if_nok (ep_buffer_get_current_sequence_number (buffer) == max_events);

ep_on_exit:
	buffer_free (buffer, thread);
	ep_thread_free (thread);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_check_buffer_state (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeThread *thread = NULL;
	EventPipeBuffer *buffer = NULL;

	thread = ep_thread_alloc ();
	ep_raise_error_if_nok (thread != NULL);

	test_location = 1;

	buffer = ep_buffer_alloc (1024 *1024, thread, 0);
	ep_raise_error_if_nok (buffer != NULL);

	test_location = 2;

	ep_raise_error_if_nok (ep_buffer_get_volatile_state (buffer) == EP_BUFFER_STATE_WRITABLE);

	test_location = 3;

	result = load_buffer_with_events (buffer, 1);
	ep_raise_error_if_nok (result == NULL);

	test_location = 4;

	ep_raise_error_if_nok (ep_buffer_get_volatile_state (buffer) == EP_BUFFER_STATE_WRITABLE);

	EP_SPIN_LOCK_ENTER (ep_thread_get_rt_lock_ref (thread), section1)
	ep_buffer_convert_to_read_only (buffer);
	EP_SPIN_LOCK_EXIT (ep_thread_get_rt_lock_ref (thread), section1)

	test_location = 5;
	ep_raise_error_if_nok (ep_buffer_get_volatile_state (buffer) == EP_BUFFER_STATE_READ_ONLY);

ep_on_exit:
	buffer_free (buffer, thread);
	ep_thread_free (thread);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_check_buffer_event_instances (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeThread *thread = NULL;
	EventPipeBuffer *buffer = NULL;
	gchar * template_data = NULL;

	thread = ep_thread_alloc ();
	ep_raise_error_if_nok (thread != NULL);

	test_location = 1;

	buffer = ep_buffer_alloc (1024 *1024, thread, 0);
	ep_raise_error_if_nok (buffer != NULL);

	test_location = 2;

	ep_raise_error_if_nok (ep_buffer_get_volatile_state (buffer) == EP_BUFFER_STATE_WRITABLE);

	test_location = 3;

	result = load_buffer_with_events (buffer, 100);
	ep_raise_error_if_nok (result == NULL);

	EP_SPIN_LOCK_ENTER (ep_thread_get_rt_lock_ref (thread), section1)
	ep_buffer_convert_to_read_only (buffer);
	EP_SPIN_LOCK_EXIT (ep_thread_get_rt_lock_ref (thread), section1)

	test_location = 4;

	int event_count = 0;
	const uint8_t *data = NULL;
	uint32_t data_len = 0;

	EventPipeEventInstance *instance = ep_buffer_get_current_read_event (buffer);
	ep_raise_error_if_nok (instance != NULL);

	while (instance) {
		test_location = 5;

		data = ep_event_instance_get_data (instance);
		data_len = ep_event_instance_get_data_len (instance);

		g_free (template_data);
		template_data = g_strdup_printf (event_instance_data, event_count);
		ep_raise_error_if_nok (template_data != NULL);

		test_location = 6;

		ep_raise_error_if_nok (strcmp (template_data, (gchar *)data) == 0);

		test_location = 7;

		ep_raise_error_if_nok (data_len == (strlen (template_data) + 1));

		test_location = 8;

		ep_buffer_move_next_read_event (buffer);
		instance = ep_buffer_get_current_read_event (buffer);
		event_count++;
		ep_raise_error_if_nok (ep_buffer_get_current_sequence_number (buffer) == event_count);
	}

ep_on_exit:
	g_free (template_data);
	buffer_free (buffer, thread);
	ep_thread_free (thread);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_check_buffer_oom (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeThread *thread = NULL;
	EventPipeBuffer *buffer = NULL;

	thread = ep_thread_alloc ();
	ep_raise_error_if_nok (thread != NULL);

	test_location = 1;

	buffer = ep_buffer_alloc (1024 *1024, thread, 0);
	ep_raise_error_if_nok (buffer != NULL);

	test_location = 2;

	ep_raise_error_if_nok (ep_buffer_get_volatile_state (buffer) == EP_BUFFER_STATE_WRITABLE);

	test_location = 3;

	// Should trigger an OOM and failure.
	result = load_buffer_with_events (buffer, 1000 * 1000);
	ep_raise_error_if_nok (result != NULL);

	g_free (result);
	result = NULL;

	test_location = 4;

	ep_raise_error_if_nok (ep_buffer_get_volatile_state (buffer) == EP_BUFFER_STATE_WRITABLE);

	EP_SPIN_LOCK_ENTER (ep_thread_get_rt_lock_ref (thread), section1)
	ep_buffer_convert_to_read_only (buffer);
	EP_SPIN_LOCK_EXIT (ep_thread_get_rt_lock_ref (thread), section1)

	test_location = 5;

	ep_raise_error_if_nok (ep_buffer_get_volatile_state (buffer) == EP_BUFFER_STATE_READ_ONLY);

	EventPipeEventInstance *instance = ep_buffer_get_current_read_event (buffer);
	ep_raise_error_if_nok (instance != NULL);

	test_location = 6;

	int event_count = 0;
	while (instance) {
		ep_buffer_move_next_read_event (buffer);
		instance = ep_buffer_get_current_read_event (buffer);
		event_count++;
		ep_raise_error_if_nok (ep_buffer_get_current_sequence_number (buffer) == event_count);
	}

ep_on_exit:
	buffer_free (buffer, thread);
	ep_thread_free (thread);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}


static RESULT
test_check_buffer_perf (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeThread *thread = NULL;
	EventPipeBuffer *buffer = NULL;

	EventPipeSession *session = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeProvider *provider = NULL;
	bool load_result = false;

	result = load_buffer_with_events_init (&session, &provider, &ep_event);
	ep_raise_error_if_nok (result == NULL);

	test_location = 1;

	thread = ep_thread_alloc ();
	ep_raise_error_if_nok (thread != NULL);

	test_location = 2;

	buffer = ep_buffer_alloc (1024 *1024, thread, 0);
	ep_raise_error_if_nok (buffer != NULL);

	test_location = 3;

	uint32_t events_written = 0;
	uint32_t number_of_buffers = 1;
	uint32_t total_events_written = 0;
	int64_t accumulated_time_ticks = 0;
	bool done = false;

	while (!done) {
		int64_t start = ep_perf_timestamp_get ();
		load_result = load_buffer (buffer, session, ep_event, 10 * 1000 * 1000, true, &events_written);
		int64_t stop = ep_perf_timestamp_get ();

		accumulated_time_ticks += stop - start;
		total_events_written += events_written;
		if (load_result || (total_events_written > 10 * 1000 * 1000)) {
			done = true;
		} else {
			buffer_free (buffer, thread);
			buffer = ep_buffer_alloc (1024 *1024, thread, 0);
			number_of_buffers++;
		}
	}

	test_location = 4;

	float accumulated_time_sec = ((float)accumulated_time_ticks / (float)ep_perf_frequency_query ());
	float events_per_sec = (float)total_events_written / (accumulated_time_sec ? accumulated_time_sec : 1.0);

	// Measured number of events/second for one thread.
	// Only measure loading data into pre-allocated buffer.
	// TODO: Setup acceptable pass/failure metrics.
	printf ("\n\tPerformance stats:\n");
	printf ("\t\tTotal number of events: %i\n", total_events_written);
	printf ("\t\tTotal time in sec: %.2f\n", accumulated_time_sec);
	printf ("\t\tTotal number of events written per sec/core: %.2f\n", events_per_sec);
	printf ("\t\tTotal number of used buffers: %i\n\t", number_of_buffers);

ep_on_exit:
	load_buffer_with_events_fini (session, provider, ep_event);
	buffer_free (buffer, thread);
	ep_thread_free (thread);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}


#ifdef EP_CHECKED_BUILD
static RESULT
test_check_buffer_consistency (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeThread *thread = NULL;
	EventPipeBuffer *buffer = NULL;

	thread = ep_thread_alloc ();
	ep_raise_error_if_nok (thread != NULL);

	test_location = 1;

	buffer = ep_buffer_alloc (1024 *1024, thread, 0);
	ep_raise_error_if_nok (buffer != NULL);

	test_location = 2;

	ep_raise_error_if_nok (ep_buffer_get_volatile_state (buffer) == EP_BUFFER_STATE_WRITABLE);

	test_location = 3;

	result = load_buffer_with_events (buffer, 100);
	ep_raise_error_if_nok (result == NULL);

	EP_SPIN_LOCK_ENTER (ep_thread_get_rt_lock_ref (thread), section1)
	ep_buffer_convert_to_read_only (buffer);
	EP_SPIN_LOCK_EXIT (ep_thread_get_rt_lock_ref (thread), section1)

	test_location = 4;

	ep_raise_error_if_nok (ep_buffer_ensure_consistency (buffer) == true);

ep_on_exit:
	buffer_free (buffer, thread);
	ep_thread_free (thread);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}
#endif

static RESULT
test_buffer_teardown (void)
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

static Test ep_buffer_tests [] = {
	{"test_buffer_setup", test_buffer_setup},
	{"test_create_free_buffer", test_create_free_buffer},
	{"test_write_event_to_buffer", test_write_event_to_buffer},
	{"test_read_event_from_buffer", test_read_event_from_buffer},
	{"test_read_events_from_buffer", test_read_events_from_buffer},
	{"test_check_buffer_state", test_check_buffer_state},
	{"test_check_buffer_event_instances", test_check_buffer_event_instances},
	{"test_check_buffer_oom", test_check_buffer_oom},
#ifdef TEST_PERF
	{"test_check_buffer_perf", test_check_buffer_perf},
#endif
#ifdef EP_CHECKED_BUILD
	{"test_check_buffer_consistency", test_check_buffer_consistency},
#endif
	{"test_buffer_teardown", test_buffer_teardown},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_buffer_tests_init, ep_buffer_tests)
