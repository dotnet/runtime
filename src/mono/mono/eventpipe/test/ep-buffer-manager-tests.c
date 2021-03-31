#if defined(_MSC_VER) && defined(_DEBUG)
#include "ep-tests-debug.h"
#endif

#include <eventpipe/ep.h>
#include <eventpipe/ep-config.h>
#include <eventpipe/ep-buffer.h>
#include <eventpipe/ep-event.h>
#include <eventpipe/ep-event-payload.h>
#include <eventpipe/ep-session.h>
#include <eventpipe/ep-buffer-manager.h>
#include <eventpipe/ep-file.h>
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

static
void
null_stream_writer_free_func (void *stream)
{
	;
}

static
bool
null_stream_writer_write_func (
	void *stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	*bytes_written = bytes_to_write;
	return true;
}

static StreamWriterVtable null_stream_writer_vtable = {
	null_stream_writer_free_func,
	null_stream_writer_write_func };

static
void
buffer_manager_fini (
	EventPipeBufferManager *buffer_manager,
	EventPipeThread *thread,
	EventPipeSession *session,
	EventPipeProvider *provider,
	EventPipeEvent *ep_event)
{
	ep_event_free (ep_event);
	ep_delete_provider (provider);

	// buffer_manager owned by session.
	EP_ASSERT (buffer_manager == NULL || buffer_manager == ep_session_get_buffer_manager (session));
	ep_session_free (session);
}

static
RESULT
buffer_manager_init (
	EventPipeSerializationFormat format,
	EventPipeBufferManager **buffer_manager,
	ep_rt_thread_handle_t *thread_handle,
	EventPipeThread **thread,
	EventPipeSession **session,
	EventPipeProvider **provider,
	EventPipeEvent **ep_event)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	*buffer_manager = NULL;
	*thread = NULL;
	*session = NULL;
	*provider = NULL;
	*ep_event = NULL;

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
			format,
			false,
			1,
			current_provider_config,
			1,
			false);
	EP_LOCK_EXIT (section1)

	ep_raise_error_if_nok (*session != NULL);

	test_location = 2;

	*buffer_manager = ep_session_get_buffer_manager (*session);

	ep_raise_error_if_nok (*buffer_manager != NULL);

	test_location = 3;

	*provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	ep_raise_error_if_nok (*provider != NULL);

	test_location = 4;

	*ep_event = ep_event_alloc (*provider, 1, 1, 1, EP_EVENT_LEVEL_VERBOSE, false, NULL, 0);
	ep_raise_error_if_nok (*ep_event != NULL);

	test_location = 5;

	ep_event_set_enabled_mask (*ep_event, 1);

	*thread = ep_thread_get_or_create ();
	ep_raise_error_if_nok (*thread != NULL);

	test_location = 6;

	*thread_handle = ep_rt_thread_get_handle ();

ep_on_exit:
	ep_provider_config_fini (current_provider_config);
	return result;

ep_on_error:
	buffer_manager_fini (*buffer_manager, *thread, *session, *provider, *ep_event);
	*buffer_manager = NULL;
	*session = NULL;
	if (!result)
		result = FAILED ("Failed writing events into buffer at location=%i", test_location);
	ep_exit_error_handler ();
}

static
bool
write_events (
	EventPipeBufferManager *buffer_manager,
	ep_rt_thread_handle_t thread,
	EventPipeSession *session,
	EventPipeEvent *ep_event,
	uint32_t event_count,
	uint32_t *events_written)
{
	bool result = true;
	uint32_t i = 0;
	for (; i < event_count; ++i) {
		EventPipeEventPayload payload;
		ep_event_payload_init (&payload, (uint8_t *)TEST_EVENT_DATA, EP_ARRAY_SIZE (TEST_EVENT_DATA));
		result = ep_buffer_manager_write_event (buffer_manager, thread, session, ep_event, &payload, NULL, NULL, thread, NULL);
		ep_event_payload_fini (&payload);

		if (!result)
			break;
	}

	if (events_written)
		*events_written = i;
	return result;
}

static RESULT
test_buffer_manager_setup (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&eventpipe_memory_start_snapshot);
#endif
	return NULL;
}

static RESULT
test_create_free_buffer_manager (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeBufferManager *buffer_manager = NULL;
	ep_rt_thread_handle_t thread_handle;
	EventPipeThread *thread = NULL;
	EventPipeSession *session = NULL;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;

	result = buffer_manager_init (EP_SERIALIZATION_FORMAT_NETTRACE_V4, &buffer_manager, &thread_handle, &thread, &session, &provider, &ep_event);

	ep_raise_error_if_nok (result == NULL);

	test_location = 1;

	ep_raise_error_if_nok (buffer_manager != NULL && session != NULL);

ep_on_exit:
	buffer_manager_fini (buffer_manager, thread, session, provider, ep_event);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_buffer_manager_init_sequence_point (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeBufferManager *buffer_manager = NULL;
	ep_rt_thread_handle_t thread_handle;
	EventPipeThread *thread = NULL;
	EventPipeSession *session = NULL;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeSequencePoint sequence_point;
	EventPipeSequencePoint *current_sequence_point = NULL;

	result = buffer_manager_init (EP_SERIALIZATION_FORMAT_NETTRACE_V4, &buffer_manager, &thread_handle, &thread, &session, &provider, &ep_event);

	ep_raise_error_if_nok (result == NULL);

	test_location = 1;

	ep_raise_error_if_nok (buffer_manager != NULL && session != NULL);

	test_location = 2;

	current_sequence_point = ep_sequence_point_init (&sequence_point);
	ep_buffer_manager_init_sequence_point_thread_list (buffer_manager, current_sequence_point);

	ep_raise_error_if_nok (ep_sequence_point_get_timestamp (current_sequence_point) != 0);

ep_on_exit:
	ep_sequence_point_fini (current_sequence_point);
	buffer_manager_fini (buffer_manager,thread, session, provider, ep_event);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_buffer_manager_write_event (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeBufferManager *buffer_manager = NULL;
	ep_rt_thread_handle_t thread_handle;
	EventPipeThread *thread = NULL;
	EventPipeSession *session = NULL;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;

	result = buffer_manager_init (EP_SERIALIZATION_FORMAT_NETTRACE_V4, &buffer_manager,  &thread_handle, &thread, &session, &provider, &ep_event);

	ep_raise_error_if_nok (result == NULL);

	test_location = 1;

	ep_raise_error_if_nok (buffer_manager != NULL && session != NULL);

	test_location = 2;

	ep_raise_error_if_nok (write_events (buffer_manager, thread_handle, session, ep_event, 1, NULL) == true);

	EP_LOCK_ENTER (section1)
		ep_buffer_manager_suspend_write_event (buffer_manager, ep_session_get_index (session));
	EP_LOCK_EXIT (section1)

ep_on_exit:
	buffer_manager_fini (buffer_manager,thread, session, provider, ep_event);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_buffer_manager_read_event (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeBufferManager *buffer_manager = NULL;
	ep_rt_thread_handle_t thread_handle;
	EventPipeThread *thread = NULL;
	EventPipeSession *session = NULL;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeEventInstance *ep_event_instance = NULL;

	result = buffer_manager_init (EP_SERIALIZATION_FORMAT_NETTRACE_V4, &buffer_manager, &thread_handle, &thread, &session, &provider, &ep_event);

	ep_raise_error_if_nok (result == NULL);

	test_location = 1;

	ep_raise_error_if_nok (buffer_manager != NULL && session != NULL);

	test_location = 2;

	ep_raise_error_if_nok (write_events (buffer_manager, thread_handle, session, ep_event, 1, NULL) == true);

	EP_LOCK_ENTER (section1)
		ep_buffer_manager_suspend_write_event (buffer_manager, ep_session_get_index (session));
	EP_LOCK_EXIT (section1)

	ep_event_instance = ep_buffer_manager_get_next_event (buffer_manager);
	ep_raise_error_if_nok (ep_event_instance != NULL);

	test_location = 3;

	ep_raise_error_if_nok (ep_event_instance_get_data_len (ep_event_instance) != 0);

	test_location = 4;

	ep_raise_error_if_nok (ep_event_instance_get_timestamp (ep_event_instance) != 0);

	test_location = 5;

	ep_raise_error_if_nok (ep_event_instance_get_thread_id (ep_event_instance) == ep_rt_thread_id_t_to_uint64_t (ep_rt_current_thread_get_id ()));

ep_on_exit:
	buffer_manager_fini (buffer_manager,thread, session, provider, ep_event);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_buffer_manager_deallocate_buffers (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeBufferManager *buffer_manager = NULL;
	ep_rt_thread_handle_t thread_handle;
	EventPipeThread *thread = NULL;
	EventPipeSession *session = NULL;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;

	result = buffer_manager_init (EP_SERIALIZATION_FORMAT_NETTRACE_V4, &buffer_manager, &thread_handle, &thread, &session, &provider, &ep_event);

	ep_raise_error_if_nok (result == NULL);

	test_location = 1;

	ep_raise_error_if_nok (buffer_manager != NULL && session != NULL);

	test_location = 2;

	ep_raise_error_if_nok (write_events (buffer_manager, thread_handle, session, ep_event, 1, NULL) == true);

	EP_LOCK_ENTER (section1)
		ep_buffer_manager_suspend_write_event (buffer_manager, ep_session_get_index (session));
	EP_LOCK_EXIT (section1)

	ep_buffer_manager_deallocate_buffers (buffer_manager);

ep_on_exit:
	buffer_manager_fini (buffer_manager,thread, session, provider, ep_event);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_buffer_manager_write_events_to_file (EventPipeSerializationFormat format)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeBufferManager *buffer_manager = NULL;
	ep_rt_thread_handle_t thread_handle;
	EventPipeThread *thread = NULL;
	EventPipeSession *session = NULL;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	bool events_written = false;

	result = buffer_manager_init (format, &buffer_manager, &thread_handle, &thread, &session, &provider, &ep_event);

	ep_raise_error_if_nok (result == NULL);

	test_location = 1;

	ep_raise_error_if_nok (buffer_manager != NULL && session != NULL);

	test_location = 2;

	ep_raise_error_if_nok (write_events (buffer_manager, thread_handle, session, ep_event, 10, NULL) == true);

	test_location = 3;

	ep_raise_error_if_nok (ep_file_initialize_file (ep_session_get_file (session)) == true);

	test_location = 4;

	ep_buffer_manager_write_all_buffers_to_file (buffer_manager, ep_session_get_file (session), ep_perf_timestamp_get (), &events_written);

	ep_raise_error_if_nok (events_written == true);

	test_location = 5;

	ep_file_flush (ep_session_get_file (session), EP_FILE_FLUSH_FLAGS_ALL_BLOCKS);

ep_on_exit:
	buffer_manager_fini (buffer_manager,thread, session, provider, ep_event);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_buffer_manager_write_events_to_file_v3 (void)
{
	return test_buffer_manager_write_events_to_file (EP_SERIALIZATION_FORMAT_NETPERF_V3);
}

static RESULT
test_buffer_manager_write_events_to_file_v4 (void)
{
	return test_buffer_manager_write_events_to_file (EP_SERIALIZATION_FORMAT_NETTRACE_V4);
}

static RESULT
test_buffer_manager_oom (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeBufferManager *buffer_manager = NULL;
	ep_rt_thread_handle_t thread_handle;
	EventPipeThread *thread = NULL;
	EventPipeSession *session = NULL;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;

	result = buffer_manager_init (EP_SERIALIZATION_FORMAT_NETTRACE_V4, &buffer_manager, &thread_handle, &thread, &session, &provider, &ep_event);

	ep_raise_error_if_nok (result == NULL);

	test_location = 1;

	ep_raise_error_if_nok (buffer_manager != NULL && session != NULL);

	test_location = 2;

	ep_raise_error_if_nok (write_events (buffer_manager, thread_handle, session, ep_event, 1000 * 1000, NULL) == false);

	EP_LOCK_ENTER (section1)
		ep_buffer_manager_suspend_write_event (buffer_manager, ep_session_get_index (session));
	EP_LOCK_EXIT (section1)

ep_on_exit:
	buffer_manager_fini (buffer_manager,thread, session, provider, ep_event);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_buffer_manager_perf (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;
	EventPipeBufferManager *buffer_manager = NULL;
	ep_rt_thread_handle_t thread_handle;
	EventPipeThread *thread = NULL;
	EventPipeSession *session = NULL;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	bool write_result = false;
	uint32_t events_written = 0;
	uint32_t total_events_written = 0;
	int64_t accumulted_buffer_manager_write_time_ticks = 0;
	int64_t accumulted_buffer_to_null_file_time_ticks = 0;
	StreamWriter null_stream_writer;
	StreamWriter *current_null_stream_writer = NULL;
	EventPipeFile *null_file = NULL;
	bool done = false;

	result = buffer_manager_init (EP_SERIALIZATION_FORMAT_NETTRACE_V4, &buffer_manager, &thread_handle, &thread, &session, &provider, &ep_event);

	ep_raise_error_if_nok (result == NULL);

	test_location = 1;

	ep_raise_error_if_nok (buffer_manager != NULL && session != NULL);

	test_location = 2;

	current_null_stream_writer = ep_stream_writer_init (&null_stream_writer, &null_stream_writer_vtable);
	null_file = ep_file_alloc (&null_stream_writer, EP_SERIALIZATION_FORMAT_NETTRACE_V4);

	ep_raise_error_if_nok (ep_file_initialize_file (null_file) == true);

	test_location = 3;

	while (!done) {
		int64_t start = ep_perf_timestamp_get ();
		write_result = write_events (buffer_manager, thread_handle, session, ep_event, 10 * 1000 * 1000, &events_written);
		int64_t stop = ep_perf_timestamp_get ();

		accumulted_buffer_manager_write_time_ticks += stop - start;
		total_events_written += events_written;
		if (write_result || (total_events_written > 10 * 1000 * 1000)) {
			done = true;
		} else {
			bool ignore_events_written;
			int64_t start = ep_perf_timestamp_get ();
			ep_buffer_manager_write_all_buffers_to_file (buffer_manager, null_file, ep_perf_timestamp_get (), &ignore_events_written);
			int64_t stop = ep_perf_timestamp_get ();

			accumulted_buffer_to_null_file_time_ticks += stop - start;
		}
	}

	EP_LOCK_ENTER (section1)
		ep_buffer_manager_suspend_write_event (buffer_manager, ep_session_get_index (session));
	EP_LOCK_EXIT (section1)

	test_location = 4;

	float accumulted_buffer_manager_write_time_sec = ((float)accumulted_buffer_manager_write_time_ticks / (float)ep_perf_frequency_query ());
	float buffer_manager_events_written_per_sec = (float)total_events_written / (accumulted_buffer_manager_write_time_sec ? accumulted_buffer_manager_write_time_sec : 1.0);

	float accumulted_buffer_to_null_file_time_sec = ((float)accumulted_buffer_to_null_file_time_ticks / (float)ep_perf_frequency_query ());
	float null_file_events_written_per_sec = (float)total_events_written / (accumulted_buffer_to_null_file_time_sec ? accumulted_buffer_to_null_file_time_sec : 1.0);

	float total_accumulted_time_sec = accumulted_buffer_manager_write_time_sec + accumulted_buffer_to_null_file_time_sec;
	float total_events_written_per_sec = (float)total_events_written / (total_accumulted_time_sec ? total_accumulted_time_sec : 1.0);

	// Measured number of events/second for one thread.
	// TODO: Setup acceptable pass/failure metrics.
	printf ("\n\tPerformance stats:\n");
	printf ("\t\tTotal number of events: %i\n", total_events_written);
	printf ("\t\tTotal time in sec: %.2f\n\t\tTotal number of events written per sec/core: %.2f\n", total_accumulted_time_sec, total_events_written_per_sec);
	printf ("\t\tep_buffer_manager_write_event:\n");
	printf ("\t\t\tTotal time in sec: %.2f\n\t\t\tEvents written per sec/core: %.2f\n", accumulted_buffer_manager_write_time_sec, buffer_manager_events_written_per_sec);
	printf ("\t\tep_buffer_manager_write_all_buffers_to_file:\n");
	printf ("\t\t\tTotal time in sec: %.2f\n\t\t\tEvents written per sec/core: %.2f\n\t", accumulted_buffer_to_null_file_time_sec, null_file_events_written_per_sec);

ep_on_exit:
	ep_file_free (null_file);
	ep_stream_writer_fini (current_null_stream_writer);
	buffer_manager_fini (buffer_manager,thread, session, provider, ep_event);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_buffer_manager_teardown (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	// Need to emulate a thread exit to make sure TLS gets cleaned up for current thread
	// or we will get memory leaks reported.
	extern void ep_rt_mono_thread_exited (void);
	ep_rt_mono_thread_exited ();

	_CrtMemCheckpoint (&eventpipe_memory_end_snapshot);
	if ( _CrtMemDifference( &eventpipe_memory_diff_snapshot, &eventpipe_memory_start_snapshot, &eventpipe_memory_end_snapshot) ) {
		_CrtMemDumpStatistics( &eventpipe_memory_diff_snapshot );
		return FAILED ("Memory leak detected!");
	}
#endif
	return NULL;
}

static Test ep_buffer_manager_tests [] = {
	{"test_buffer_manager_setup", test_buffer_manager_setup},
	{"test_create_free_buffer_manager", test_create_free_buffer_manager},
	{"test_buffer_manager_init_sequence_point", test_buffer_manager_init_sequence_point},
	{"test_buffer_manager_write_event", test_buffer_manager_write_event},
	{"test_buffer_manager_read_event", test_buffer_manager_read_event},
	{"test_buffer_manager_deallocate_buffers", test_buffer_manager_deallocate_buffers},
	{"test_buffer_manager_write_events_to_file_v3", test_buffer_manager_write_events_to_file_v3},
	{"test_buffer_manager_write_events_to_file_v4", test_buffer_manager_write_events_to_file_v4},
	{"test_buffer_manager_oom", test_buffer_manager_oom},
#ifdef TEST_PERF
	{"test_buffer_manager_perf", test_buffer_manager_perf},
#endif
	{"test_buffer_manager_teardown", test_buffer_manager_teardown},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_buffer_manager_tests_init, ep_buffer_manager_tests)
