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

	g_queue_free (queue);
	return OK;
}

RESULT
test_queue_pop ()
{
	GQueue *queue = g_queue_new ();
	gpointer data;

	g_queue_push_head (queue, "foo");
	g_queue_push_head (queue, "bar");
	g_queue_push_head (queue, "baz");

	data = g_queue_pop_head (queue);
	if (strcmp ("baz", data))
		return FAILED ("expect baz.");

	data = g_queue_pop_head (queue);
	if (strcmp ("bar", data))
		return FAILED ("expect bar.");	

	data = g_queue_pop_head (queue);
	if (strcmp ("foo", data))
		return FAILED ("expect foo.");
	
	if (g_queue_is_empty (queue) == FALSE)
		return FAILED ("expect is_empty.");

	if (queue->length != 0)
		return FAILED ("expect 0 length .");

	g_queue_free (queue);
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

	g_queue_free (queue);
	return OK;
}

RESULT
test_queue_is_empty ()
{
	GQueue *queue = g_queue_new ();

	if (g_queue_is_empty (queue) == FALSE)
		return FAILED ("new queue should be empty");

	g_queue_push_head (queue, "foo");

	if (g_queue_is_empty (queue) == TRUE)
		return FAILED ("expected TRUE");

	g_queue_free (queue);

	return OK;
}

static Test queue_tests [] = {
	{    "push", test_queue_push},
	{     "pop", test_queue_pop},
	{     "new", test_queue_new},
	{"is_empty", test_queue_is_empty},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(queue_tests_init, queue_tests)

