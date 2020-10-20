#include "mono/eventpipe/ep.h"
#include "mono/eventpipe/ep-block.h"
#include "mono/eventpipe/ep-event.h"
#include "mono/eventpipe/ep-event-instance.h"
#include "mono/eventpipe/ep-stream.h"
#include "eglib/test/test.h"

#define TEST_PROVIDER_NAME "MyTestProvider"

typedef struct _MemoryStreamWriter {
	StreamWriter stream_writer;
	uint8_t buffer [1024];
	uint8_t *current_ptr;
} MemoryStreamWriter;

static
void
memory_stream_writer_free_func (void *stream)
{
	g_free ((MemoryStreamWriter *)stream);
}

static
bool
memory_stream_writer_write_func (
	void *stream,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	*bytes_written = 0;

	MemoryStreamWriter *memory_stream = (MemoryStreamWriter *)stream;
	if (!stream)
		return false;

	if ((memory_stream->current_ptr + bytes_to_write) > (memory_stream->buffer + sizeof (memory_stream->buffer)))
		return false;

	memcpy (memory_stream->current_ptr, buffer, bytes_to_write);
	memory_stream->current_ptr += bytes_to_write;
	*bytes_written = bytes_to_write;

	return true;
}

static StreamWriterVtable memory_stream_writer_vtable = {
	memory_stream_writer_free_func,
	memory_stream_writer_write_func };

static RESULT
test_fast_serializer_object_fast_serialize (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	MemoryStreamWriter *stream_writer = NULL;
	FastSerializer *fast_serializer = NULL;
	EventPipeProvider *provider = NULL;
	EventPipeEvent *ep_event = NULL;
	EventPipeEventInstance *ep_event_instance = NULL;
	EventPipeEventBlock *event_block = NULL;

	// Use memory stream for testing.
	stream_writer = g_new0 (MemoryStreamWriter, 1);
	ep_raise_error_if_nok (stream_writer != NULL);

	test_location = 1;

	stream_writer->current_ptr = stream_writer->buffer;
	ep_stream_writer_init (&stream_writer->stream_writer, &memory_stream_writer_vtable);

	fast_serializer = ep_fast_serializer_alloc ((StreamWriter *)stream_writer);
	ep_raise_error_if_nok (fast_serializer != NULL);
	stream_writer = NULL;

	test_location = 2;

	provider = ep_create_provider (TEST_PROVIDER_NAME, NULL, NULL, NULL);
	ep_raise_error_if_nok (provider != NULL);

	test_location = 3;
	
	ep_event = ep_event_alloc (provider, 1, 1, 1, EP_EVENT_LEVEL_VERBOSE, false, NULL, 0);
	ep_raise_error_if_nok (ep_event != NULL);

	test_location = 4;

	ep_event_instance = ep_event_instance_alloc (ep_event, 0, 0, NULL, 0, NULL, NULL);
	event_block = ep_event_block_alloc (1024, EP_SERIALIZATION_FORMAT_NETTRACE_V4);
	ep_raise_error_if_nok (ep_event_instance != NULL && event_block != NULL);

	test_location = 5;

	ep_raise_error_if_nok (ep_event_block_base_write_event ((EventPipeEventBlockBase *)event_block, ep_event_instance, 0, 1, 0, false) == true);

	test_location = 6;

	ep_fast_serializable_object_fast_serialize ((FastSerializableObject *)event_block, fast_serializer);

	//Check memory stream results.
	stream_writer = (MemoryStreamWriter *)ep_fast_serializer_get_stream_writer (fast_serializer);
	if (stream_writer->buffer == stream_writer->current_ptr) {
		result = FAILED ("fast_serialize for EventPipeEventBlock didn't write any data into MemoryStreamWriter");
		ep_raise_error ();
	}

ep_on_exit:
	ep_event_instance_free (ep_event_instance);
	ep_event_block_free (event_block);
	ep_event_free (ep_event);
	ep_delete_provider (provider);
	ep_fast_serializer_free (fast_serializer);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_fast_serializer_event_block_free_vcall (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeEventBlock *event_block = ep_event_block_alloc (1024, EP_SERIALIZATION_FORMAT_NETTRACE_V4);
	ep_raise_error_if_nok (event_block != NULL);

	test_location = 1;

	ep_fast_serializable_object_free_vcall ((FastSerializableObject *)event_block);

ep_on_exit:
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_fast_serializer_metadata_block_free_vcall (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeMetadataBlock *metadata_block = ep_metadata_block_alloc (1024);
	ep_raise_error_if_nok (metadata_block != NULL);

	test_location = 1;

	ep_fast_serializable_object_free_vcall ((FastSerializableObject *)metadata_block);

ep_on_exit:
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_fast_serializer_sequence_point_block_free_vcall (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeSequencePoint sequence_point;
	EventPipeSequencePointBlock *sequence_point_block = NULL;

	ep_raise_error_if_nok (ep_sequence_point_init (&sequence_point) != NULL);

	test_location = 1;

	sequence_point_block = ep_sequence_point_block_alloc (&sequence_point);
	ep_raise_error_if_nok (sequence_point_block != NULL);

	test_location = 2;

	ep_fast_serializable_object_free_vcall ((FastSerializableObject *)sequence_point_block);

ep_on_exit:
	ep_sequence_point_fini (&sequence_point);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_fast_serializer_stack_block_free_vcall (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeStackBlock *stack_block = ep_stack_block_alloc (1024);
	ep_raise_error_if_nok (stack_block != NULL);

	test_location = 1;

	ep_fast_serializable_object_free_vcall ((FastSerializableObject *)stack_block);

ep_on_exit:
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_fast_serializer_event_block_get_type_name (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeEventBlock *event_block = ep_event_block_alloc (1024, EP_SERIALIZATION_FORMAT_NETTRACE_V4);
	ep_raise_error_if_nok (event_block != NULL);

	test_location = 1;

	const char *type_name = (char *)ep_fast_serializable_object_get_type_name ((FastSerializableObject *)event_block);
	if (strcmp (type_name, "EventBlock")) {
		result = FAILED ("get_type_name for EventPipeEventBlock returned unexpected value, retrieved: %s, expected: %s", type_name, "EventBlock");
		ep_raise_error ();
	}

ep_on_exit:
	ep_event_block_free (event_block);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_fast_serializer_metadata_block_get_type_name (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeMetadataBlock *metadata_block = ep_metadata_block_alloc (1024);
	ep_raise_error_if_nok (metadata_block != NULL);

	test_location = 1;

	const char *type_name = (char *)ep_fast_serializable_object_get_type_name ((FastSerializableObject *)metadata_block);
	if (strcmp (type_name, "MetadataBlock")) {
		result = FAILED ("get_type_name for EventPipeMetadataBlock returned unexpected value, retrieved: %s, expected: %s", type_name, "MetadataBlock");
		ep_raise_error ();
	}

ep_on_exit:
	ep_metadata_block_free (metadata_block);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_fast_serializer_sequence_point_block_get_type_name (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeSequencePoint sequence_point;
	EventPipeSequencePointBlock *sequence_point_block = NULL;

	ep_raise_error_if_nok (ep_sequence_point_init (&sequence_point) != NULL);

	test_location = 1;

	sequence_point_block = ep_sequence_point_block_alloc (&sequence_point);
	ep_raise_error_if_nok (sequence_point_block != NULL);

	test_location = 2;

	const char *type_name = (char *)ep_fast_serializable_object_get_type_name ((FastSerializableObject *)sequence_point_block);
	if (strcmp (type_name, "SPBlock")) {
		result = FAILED ("get_type_name for EventPipeSequencePointBlock returned unexpected value, retrieved: %s, expected: %s", type_name, "SPBlock");
		ep_raise_error ();
	}

ep_on_exit:
	ep_sequence_point_block_free (sequence_point_block);
	ep_sequence_point_fini (&sequence_point);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_fast_serializer_stack_block_get_type_name (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeStackBlock *stack_block = ep_stack_block_alloc (1024);
	ep_raise_error_if_nok (stack_block != NULL);

	test_location = 1;

	const char *type_name = (char *)ep_fast_serializable_object_get_type_name ((FastSerializableObject *)stack_block);
	if (strcmp (type_name, "StackBlock")) {
		result = FAILED ("get_type_name for EventPipeStackBlock returned unexpected value, retrieved: %s, expected: %s", type_name, "StackBlock");
		ep_raise_error ();
	}

ep_on_exit:
	ep_stack_block_free (stack_block);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

// TODO: Add perf test just doing write into fast serializer with different event types (no event alloc/instancing). Write into void
// stream but still write into same memory buffer to do something.

static Test ep_fastserializer_tests [] = {
	{"fast_serializer_object_fast_serialize", test_fast_serializer_object_fast_serialize},
	{"test_fast_serializer_event_block_free_vcall", test_fast_serializer_event_block_free_vcall},
	{"test_fast_serializer_metadata_block_free_vcall", test_fast_serializer_metadata_block_free_vcall},
	{"test_fast_serializer_sequence_point_block_free_vcall", test_fast_serializer_sequence_point_block_free_vcall},
	{"test_fast_serializer_stack_block_free_vcall", test_fast_serializer_stack_block_free_vcall},
	{"test_fast_serializer_event_block_get_type_name", test_fast_serializer_event_block_get_type_name},
	{"test_fast_serializer_metadata_block_get_type_name", test_fast_serializer_metadata_block_get_type_name},
	{"test_fast_serializer_sequence_point_block_get_type_name", test_fast_serializer_sequence_point_block_get_type_name},
	{"test_fast_serializer_stack_block_get_type_name", test_fast_serializer_stack_block_get_type_name},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_fastserializer_tests_init, ep_fastserializer_tests)
