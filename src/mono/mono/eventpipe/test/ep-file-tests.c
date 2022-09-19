#include <eventpipe/ep.h>
#include <eventpipe/ep-config.h>
#include <eventpipe/ep-event.h>
#include <eventpipe/ep-event-instance.h>
#include <eventpipe/ep-file.h>
#include <eglib/test/test.h>

#define TEST_PROVIDER_NAME "MyTestProvider"
#define TEST_FILE "./ep_test_create_file.txt"

static RESULT
test_create_file (EventPipeSerializationFormat format)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeFile *file = NULL;
	FileStreamWriter *file_stream_writer = NULL;

	file_stream_writer = ep_file_stream_writer_alloc (TEST_FILE);
	ep_raise_error_if_nok (file_stream_writer != NULL);

	test_location = 1;

	file = ep_file_alloc ((StreamWriter *)file_stream_writer, format);
	ep_raise_error_if_nok (file != NULL);

	file_stream_writer = NULL;
	if (!ep_file_initialize_file (file)) {
		result = FAILED ("ep_file_initialize_file failed");
		ep_raise_error ();
	}

	test_location = 2;

	ep_file_flush (file, EP_FILE_FLUSH_FLAGS_ALL_BLOCKS);

	test_location = 3;

ep_on_exit:
	ep_file_free (file);
	ep_file_stream_writer_free (file_stream_writer);

	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_file_write_event (EventPipeSerializationFormat format, bool write_event, bool write_sequence_point)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeFile *file = NULL;
	FileStreamWriter *file_stream_writer = NULL;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeEventInstance *ep_event_instance = NULL;
	EventPipeEventMetadataEvent *metadata_event = NULL;

	file_stream_writer = ep_file_stream_writer_alloc (TEST_FILE);
	ep_raise_error_if_nok (file_stream_writer != NULL);

	test_location = 1;

	file = ep_file_alloc ((StreamWriter *)file_stream_writer, format);
	ep_raise_error_if_nok (file != NULL);

	file_stream_writer = NULL;
	if (!ep_file_initialize_file (file)) {
		result = FAILED ("ep_file_initialize_file failed");
		ep_raise_error ();
	}

	test_location = 2;

	if (write_event) {
		provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
		ep_raise_error_if_nok (provider != NULL);

		test_location = 3;

		ep_event = ep_event_alloc (provider, 1, 1, 1, EP_EVENT_LEVEL_VERBOSE, false, NULL, 0);
		ep_raise_error_if_nok (ep_event != NULL);

		test_location = 4;

		ep_event_instance = ep_event_instance_alloc (ep_event, 0, 0, NULL, 0, NULL, NULL);
		ep_raise_error_if_nok (ep_event_instance != NULL);

		test_location = 5;

		metadata_event = ep_build_event_metadata_event (ep_event_instance, 1);
		ep_raise_error_if_nok (metadata_event != NULL);

		ep_file_write_event (file, (EventPipeEventInstance *)metadata_event, 1, 1 , true);
	}

	if (write_sequence_point) {
		EventPipeSequencePoint sequence_point;
		ep_sequence_point_init (&sequence_point);
		ep_file_write_sequence_point (file, &sequence_point);
		ep_sequence_point_fini (&sequence_point);
	}

	ep_file_flush (file, EP_FILE_FLUSH_FLAGS_ALL_BLOCKS);

	test_location = 6;

ep_on_exit:
	ep_delete_provider (provider);
	ep_event_free (ep_event);
	ep_event_instance_free (ep_event_instance);
	ep_event_metadata_event_free (metadata_event);
	ep_file_free (file);
	ep_file_stream_writer_free (file_stream_writer);

	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_create_file_netperf_v3 (void)
{
	return test_create_file (EP_SERIALIZATION_FORMAT_NETPERF_V3);
}

static RESULT
test_create_file_nettrace_v4 (void)
{
	return test_create_file (EP_SERIALIZATION_FORMAT_NETTRACE_V4);
}

static RESULT
test_file_write_event_netperf_v3 (void)
{
	return test_file_write_event (EP_SERIALIZATION_FORMAT_NETPERF_V3, true, false);
}

static RESULT
test_file_write_event_nettrace_v4 (void)
{
	return test_file_write_event (EP_SERIALIZATION_FORMAT_NETTRACE_V4, true, false);
}

static RESULT
test_file_write_sequence_point_netperf_v3 (void)
{
	return test_file_write_event (EP_SERIALIZATION_FORMAT_NETPERF_V3, false, true);
}

static RESULT
test_file_write_sequence_point_nettrace_v4 (void)
{
	return test_file_write_event (EP_SERIALIZATION_FORMAT_NETTRACE_V4, false, true);
}

static Test ep_file_tests [] = {
	{"test_create_file_netperf_v3", test_create_file_netperf_v3},
	{"test_create_file_nettrace_v4", test_create_file_nettrace_v4},
	{"test_file_write_event_netperf_v3", test_file_write_event_netperf_v3},
	{"test_file_write_event_nettrace_v4", test_file_write_event_nettrace_v4},
	{"test_file_write_sequence_point_netperf_v3", test_file_write_sequence_point_netperf_v3},
	{"test_file_write_sequence_point_nettrace_v4", test_file_write_sequence_point_nettrace_v4},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_file_tests_init, ep_file_tests)
