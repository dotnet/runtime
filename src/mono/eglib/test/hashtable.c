#include <stdio.h>
#include <string.h>
#include <glib.h>
#include "test.h"

int foreach_count = 0;
int foreach_fail = 0;

void foreach (gpointer key, gpointer value, gpointer user_data)
{
	foreach_count++;
	if (GPOINTER_TO_INT (user_data) != 'a')
		foreach_fail = 1;
}

RESULT hash_t1 (void)
{
	GHashTable *t = g_hash_table_new (g_str_hash, g_str_equal);

	foreach_count = 0;
	foreach_fail = 0;
	g_hash_table_insert (t, "hello", "world");
	g_hash_table_insert (t, "my", "god");

	g_hash_table_foreach (t, foreach, GINT_TO_POINTER('a'));
	if (foreach_count != 2)
		return FAILED ("did not find all keys, got %d expected 2", foreach_count);
	if (foreach_fail)
		return FAILED("failed to pass the user-data to foreach");
	
	if (!g_hash_table_remove (t, "my"))
		return FAILED ("did not find known key");
	if (g_hash_table_size (t) != 1)
		return FAILED ("unexpected size");
	g_hash_table_insert(t, "hello", "moon");
	if (strcmp (g_hash_table_lookup (t, "hello"), "moon") != 0)
		return FAILED ("did not replace world with moon");
		
	if (!g_hash_table_remove (t, "hello"))
		return FAILED ("did not find known key");
	if (g_hash_table_size (t) != 0)
		return FAILED ("unexpected size");
	g_hash_table_destroy (t);

	return OK;
}

RESULT hash_t2 (void)
{
	return OK;
}

static Test hashtable_tests [] = {
	{"hash_t1", hash_t1},
	{"hash_t2", hash_t2},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(hashtable_tests_init, hashtable_tests)

