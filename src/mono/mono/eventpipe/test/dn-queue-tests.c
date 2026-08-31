#if defined(_MSC_VER) && defined(_DEBUG)
#define _CRTDBG_MAP_ALLOC
#include <stdlib.h>
#include <crtdbg.h>
#endif

#include <eglib/test/test.h>
#include <containers/dn-queue.h>


#ifdef _CRTDBG_MAP_ALLOC
static _CrtMemState dn_queue_memory_start_snapshot;
static _CrtMemState dn_queue_memory_end_snapshot;
static _CrtMemState dn_queue_memory_diff_snapshot;
#endif

#define N_ELEMS 101
#define POINTER_TO_INT32(v) ((int32_t)(ptrdiff_t)(v))
#define INT32_TO_POINTER(v) ((void *)(ptrdiff_t)(v))

static
void
DN_CALLBACK_CALLTYPE
test_queue_dispose_func (void *data)
{
	(*(uint32_t *)data)++;
}

static
RESULT
test_queue_setup (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&dn_queue_memory_start_snapshot);
#endif
	return OK;
}

static
RESULT
test_queue_alloc (void)
{
	dn_queue_t *queue = dn_queue_alloc ();
	if (!queue)
		return FAILED ("failed to alloc queue");

	dn_queue_free (queue);

	return OK;
}

static
RESULT
test_queue_init (void)
{
	dn_queue_t queue;
	if (!dn_queue_init (&queue))
		return FAILED ("failed to init queue");

	dn_queue_dispose (&queue);

	return OK;
}

static
RESULT
test_queue_free (void)
{
	uint32_t dispose_count = 0;
	dn_queue_t *queue = dn_queue_custom_alloc (DN_DEFAULT_ALLOCATOR);
	if (!queue)
		return FAILED ("failed to custom alloc queue");

	dn_queue_push (queue, &dispose_count);
	dn_queue_push (queue, &dispose_count);

	dn_queue_custom_free (queue, test_queue_dispose_func);

	if (dispose_count != 2)
		return FAILED ("invalid dispose count on free");

	return OK;
}

static
RESULT
test_queue_dispose (void)
{
	uint32_t dispose_count = 0;
	dn_queue_t queue;
	if (!dn_queue_custom_init (&queue, DN_DEFAULT_ALLOCATOR))
		return FAILED ("failed to custom init queue");

	dn_queue_push (&queue, &dispose_count);
	dn_queue_push (&queue, &dispose_count);

	dn_queue_custom_dispose (&queue, test_queue_dispose_func);

	if (dispose_count != 2)
		return FAILED ("invalid dispose count on free");

	return OK;
}

static
RESULT
test_queue_front (void)
{
	const char * items[] = { "first", "second" };

	dn_queue_t *queue = dn_queue_alloc ();
	if (!queue)
		return FAILED ("failed to alloc queue");

	dn_queue_push (queue, (char *)items [0]);

	if (*dn_queue_front_t (queue, char *) != items [0])
		return FAILED ("failed queue front #1");

	dn_queue_push (queue, (char *)items [1]);

	if (*dn_queue_front_t (queue, char *) != items [0])
		return FAILED ("failed queue front #2");

	dn_queue_free (queue);

	return OK;
}

static
RESULT
test_queue_back (void)
{
	const char * items[] = { "first", "second" };

	dn_queue_t *queue = dn_queue_alloc ();
	if (!queue)
		return FAILED ("failed to alloc queue");

	dn_queue_push (queue, (char *)items [0]);

	if (*dn_queue_back_t (queue, char *) != items [0])
		return FAILED ("failed queue front #1");

	dn_queue_push (queue, (char *)items [1]);

	if (*dn_queue_back_t (queue, char *) != items [1])
		return FAILED ("failed queue front #2");

	dn_queue_free (queue);

	return OK;
}

static
RESULT
test_queue_empty (void)
{
	const char * items[] = { "first", "second" };

	dn_queue_t *queue = dn_queue_alloc ();
	if (!queue)
		return FAILED ("failed to alloc queue");

	if (!dn_queue_empty (queue))
		return FAILED ("failed empty #1");

	dn_queue_push (queue, (char *)items [0]);

	if (dn_queue_empty (queue))
		return FAILED ("failed empty #2");

	dn_queue_push (queue, (char *)items [1]);

	if (dn_queue_empty (queue))
		return FAILED ("failed empty #3");

	dn_queue_pop (queue);

	if (dn_queue_empty (queue))
		return FAILED ("failed empty #4");

	dn_queue_pop (queue);

	if (!dn_queue_empty (queue))
		return FAILED ("failed empty #5");

	dn_queue_free (queue);

	return OK;
}

static
RESULT
test_queue_size (void)
{
	dn_queue_t *queue = dn_queue_alloc ();
	if (!queue)
		return FAILED ("failed to alloc queue");

	for (uint32_t i = 0; i < N_ELEMS; i++)
		dn_queue_push_t (queue, uint32_t, i);

	if (dn_queue_size (queue) != N_ELEMS)
		return FAILED ("failed queue size");

	dn_queue_free (queue);

	return OK;
}

static
RESULT
test_queue_push_pop (void)
{
	uint32_t dispose_count = 0;
	dn_queue_t *queue = dn_queue_alloc ();
	if (!queue)
		return FAILED ("failed to alloc queue");

	for (uint32_t i = 0; i < N_ELEMS; i++) {
		if (!dn_queue_push_t (queue, uint32_t, i))
			return FAILED ("failed to push to queue");
	}

	if (*dn_queue_back_t (queue, uint32_t) != N_ELEMS - 1)
		return FAILED ("incorrect back of queue");

	uint32_t count;
	for (count = 0; count < N_ELEMS && !dn_queue_empty (queue); count++) {
		uint32_t current = *dn_queue_front_t (queue, uint32_t);
		if (current != count)
			return FAILED ("incorrect queue order");

		dn_queue_pop (queue);
	}

	if (count != N_ELEMS)
		return FAILED ("incorrect queue count");

	dn_queue_clear (queue);

	dn_queue_push (queue, &dispose_count);
	dn_queue_push (queue, &dispose_count);
	dn_queue_push (queue, &dispose_count);

	dn_queue_custom_pop (queue, test_queue_dispose_func);
	dn_queue_custom_pop (queue, test_queue_dispose_func);

	if (dn_queue_size (queue) != 1)
		return FAILED ("incorrect queue count after custom pop");
	if (dispose_count != 2)
		return FAILED ("incorrect dispose count after custom pop");

	dn_queue_free (queue);

	return OK;
}

static
RESULT
test_queue_clear (void)
{
	uint32_t dispose_count = 0;
	const char * items[] = { "first", "second" };

	dn_queue_t *queue = dn_queue_alloc ();
	if (!queue)
		return FAILED ("failed to alloc queue");

	if (!dn_queue_empty (queue))
		return FAILED ("failed empty #1");

	dn_queue_push (queue, (char *)items [0]);

	if (dn_queue_empty (queue))
		return FAILED ("failed empty #2");

	dn_queue_push (queue, (char *)items [1]);

	dn_queue_clear (queue);

	if (!dn_queue_empty (queue))
		return FAILED ("failed empty #3");

	dn_queue_free (queue);

	queue = dn_queue_custom_alloc (DN_DEFAULT_ALLOCATOR);

	dn_queue_push (queue, &dispose_count);
	dn_queue_push (queue, &dispose_count);

	dn_queue_custom_clear (queue, test_queue_dispose_func);

	if (dispose_count != 2)
		return FAILED ("invalid dispose count on clear");

	dispose_count = 0;
	dn_queue_custom_free (queue, test_queue_dispose_func);

	if (dispose_count != 0)
		return FAILED ("invalid dispose count on clear/free");

	return OK;
}

static
RESULT
test_queue_teardown (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&dn_queue_memory_end_snapshot);
	if ( _CrtMemDifference(&dn_queue_memory_diff_snapshot, &dn_queue_memory_start_snapshot, &dn_queue_memory_end_snapshot) ) {
		_CrtMemDumpStatistics( &dn_queue_memory_diff_snapshot );
		return FAILED ("memory leak detected!");
	}
#endif
	return OK;
}

static Test dn_queue_tests [] = {
	{"test_queue_setup", test_queue_setup},
	{"test_queue_alloc", test_queue_alloc},
	{"test_queue_init", test_queue_init},
	{"test_queue_free", test_queue_free},
	{"test_queue_dispose", test_queue_dispose},
	{"test_queue_front", test_queue_front},
	{"test_queue_back", test_queue_back},
	{"test_queue_empty", test_queue_empty},
	{"test_queue_size", test_queue_size},
	{"test_queue_push_pop", test_queue_push_pop},
	{"test_queue_clear", test_queue_clear},
	{"test_queue_teardown", test_queue_teardown},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(dn_queue_tests_init, dn_queue_tests)
