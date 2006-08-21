#include <stdio.h>
#include <string.h>
#include <glib.h>
#include "test.h"

RESULT
test_queue_push ()
{
	GQueue *queue = g_queue_new ();

	g_queue_push_head (queue, "foo");
	g_queue_push_head (queue, "bar");
	g_queue_push_head (queue, "baz");

	if (queue->length != 3)
		return FAILED ("push failed");

	return OK;
}

RESULT
test_queue_pop ()
{
	GQueue *queue = g_queue_new ();

	g_queue_push_head (queue, "foo");
	g_queue_push_head (queue, "bar");
	g_queue_push_head (queue, "baz");

	if (strcmp ("baz", g_queue_pop_head (queue)))
		return FAILED ("expect baz.");

	if (strcmp ("bar", g_queue_pop_head (queue)))
		return FAILED ("expect bar.");	

	if (strcmp ("foo", g_queue_pop_head (queue)))
		return FAILED ("expect foo.");
	
	if (g_queue_is_empty (queue) == FALSE)
		return FAILED ("expect is_empty.");

	if (queue->length != 0)
		return FAILED ("expect 0 length .");

	return OK;
}

RESULT
test_queue_new ()
{
	GQueue *queue = g_queue_new ();

	if (queue->length != 0)
		return FAILED ("expect length == 0");

	if (queue->head != NULL)
		return FAILED ("expect head == NULL");

	if (queue->tail != NULL)
		return FAILED ("expect tail == NULL");

	return OK;
}

RESULT
test_queue_is_empty ()
{
	if (g_queue_is_empty (g_queue_new ()) == FALSE)
		return FAILED ("new queue should be empty");

	else {
		GQueue *queue = g_queue_new ();
		g_queue_push_head (queue, "foo");

		if (g_queue_is_empty (queue) == TRUE)
			return FAILED ("expected TRUE");

		return OK;
	}
}

static Test queue_tests [] = {
	{    "push", test_queue_push},
	{     "pop", test_queue_pop},
	{     "new", test_queue_new},
	{"is_empty", test_queue_is_empty},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(queue_tests_init, queue_tests)

