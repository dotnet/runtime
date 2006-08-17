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

char *hash_t1 (void)
{
	GHashTable *t = g_hash_table_new (g_str_hash, g_str_equal);

	g_hash_table_insert (t, "hello", "world");
	g_hash_table_insert (t, "my", "god");

	g_hash_table_foreach (t, foreach, GINT_TO_POINTER('a'));
	if (foreach_count != 2)
		return "did not find all keys";
	if (foreach_fail)
		return "failed to pass the user-data to foreach";
	
	if (!g_hash_table_remove (t, "my"))
		return "did not find known key";
	if (g_hash_table_size (t) != 1)
		return "unexpected size";
	g_hash_table_insert(t, "hello", "moon");
	if (strcmp (g_hash_table_lookup (t, "hello"), "moon") != 0)
		return "did not replace world with moon";
		
	if (!g_hash_table_remove (t, "hello"))
		return "did not find known key";
	if (g_hash_table_size (t) != 0)
		return "unexpected size";
	g_hash_table_destroy (t);

	return NULL;
}

char *hash_t2 (void)
{
	return NULL;
}

static Test hashtable_tests [] = {
	{"hash_t1", hash_t1},
	{"hash_t2", hash_t2},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(hashtable_tests_init, hashtable_tests)

