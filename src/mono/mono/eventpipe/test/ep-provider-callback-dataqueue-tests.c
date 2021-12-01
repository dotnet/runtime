#if defined(_MSC_VER) && defined(_DEBUG)
#include "ep-tests-debug.h"
#endif

#include <eventpipe/ep.h>
#include <eglib/test/test.h>

#define TEST_PROVIDER_NAME "MyTestProvider"
#define TEST_FILE "./ep_test_create_file.txt"

#ifdef _CRTDBG_MAP_ALLOC
static _CrtMemState eventpipe_memory_start_snapshot;
static _CrtMemState eventpipe_memory_end_snapshot;
static _CrtMemState eventpipe_memory_diff_snapshot;
#endif

static RESULT
test_provider_callback_data_queue_setup (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&eventpipe_memory_start_snapshot);
#endif
	return NULL;
}

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
	;
}

static RESULT
test_provider_callback_data_queue (void)
{
	RESULT result = NULL;
	uint32_t test_location = 0;

	EventPipeProviderCallbackDataQueue callback_data_queue;
	EventPipeProviderCallbackDataQueue *provider_callback_data_queue = ep_provider_callback_data_queue_init (&callback_data_queue);

	for (uint32_t i = 0; i < 1000; ++i) {
		EventPipeProviderCallbackData enqueue_callback_data;
		EventPipeProviderCallbackData *provider_enqueue_callback_data = ep_provider_callback_data_init (
			&enqueue_callback_data,
			"",
			provider_callback,
			NULL,
			1,
			EP_EVENT_LEVEL_LOGALWAYS,
			true);
		ep_provider_callback_data_queue_enqueue (provider_callback_data_queue, provider_enqueue_callback_data);
		ep_provider_callback_data_fini (provider_enqueue_callback_data);
	}

	EventPipeProviderCallbackData dequeue_callback_data;
	uint32_t deque_counter = 0;
	while (ep_provider_callback_data_queue_try_dequeue(provider_callback_data_queue, &dequeue_callback_data)) {
		deque_counter++;
		ep_provider_callback_data_fini (&dequeue_callback_data);
	}

	if (deque_counter != 1000) {
		result = FAILED ("Unexpected number of provider callback invokes");
		ep_raise_error ();
	}

ep_on_exit:
	ep_provider_callback_data_queue_fini (provider_callback_data_queue);
	return result;

ep_on_error:
	if (!result)
		result = FAILED ("Failed at test location=%i", test_location);
	ep_exit_error_handler ();
}

static RESULT
test_provider_callback_data_queue_teardown (void)
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

static Test ep_provider_callback_data_queue_tests [] = {
	{"test_provider_callback_data_queue_setup", test_provider_callback_data_queue_setup},
	{"test_provider_callback_data_queue", test_provider_callback_data_queue},
	{"test_provider_callback_data_queue_teardown", test_provider_callback_data_queue_teardown},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ep_provider_callback_data_queue_tests_init, ep_provider_callback_data_queue_tests)
