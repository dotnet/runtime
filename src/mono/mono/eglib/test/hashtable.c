#include <stdio.h>
#include <string.h>
#include <glib.h>
#include "test.h"

int foreach_count = 0;
int foreach_fail = 0;

static void
foreach (gpointer key, gpointer value, gpointer user_data)
{
	foreach_count++;
	if (GPOINTER_TO_INT (user_data) != 'a')
		foreach_fail = 1;
}

static RESULT
hash_t1 (void)
{
	GHashTable *t = g_hash_table_new (g_str_hash, g_str_equal);

	foreach_count = 0;
	foreach_fail = 0;
	g_hash_table_insert (t, (char*)"hello", (char*)"world");
	g_hash_table_insert (t, (char*)"my", (char*)"god");

	g_hash_table_foreach (t, foreach, GINT_TO_POINTER('a'));
	if (foreach_count != 2)
		return FAILED ("did not find all keys, got %d expected 2", foreach_count);
	if (foreach_fail)
		return FAILED("failed to pass the user-data to foreach");
	
	if (!g_hash_table_remove (t, (char*)"my"))
		return FAILED ("did not find known key");
	if (g_hash_table_size (t) != 1)
		return FAILED ("unexpected size");
	g_hash_table_insert(t, (char*)"hello", (char*)"moon");
	if (strcmp (g_hash_table_lookup (t, (char*)"hello"), (char*)"moon") != 0)
		return FAILED ("did not replace world with moon");
		
	if (!g_hash_table_remove (t, (char*)"hello"))
		return FAILED ("did not find known key");
	if (g_hash_table_size (t) != 0)
		return FAILED ("unexpected size");
	g_hash_table_destroy (t);

	return OK;
}

static RESULT
hash_t2 (void)
{
	return OK;
}

static RESULT
hash_default (void)
{
	GHashTable *hash = g_hash_table_new (NULL, NULL);

	if (hash == NULL)
		return FAILED ("g_hash_table_new should return a valid hash");

	g_hash_table_destroy (hash);
	return NULL;
}

static RESULT
hash_null_lookup (void)
{
	GHashTable *hash = g_hash_table_new (NULL, NULL);
	gpointer ok, ov;
		
	g_hash_table_insert (hash, NULL, GINT_TO_POINTER (1));
	g_hash_table_insert (hash, GINT_TO_POINTER(1), GINT_TO_POINTER(2));

	if (!g_hash_table_lookup_extended (hash, NULL, &ok, &ov))
		return FAILED ("Did not find the NULL");
	if (ok != NULL)
		return FAILED ("Incorrect key found");
	if (ov != GINT_TO_POINTER (1))
		return FAILED ("Got wrong value %p\n", ov);

	if (!g_hash_table_lookup_extended (hash, GINT_TO_POINTER(1), &ok, &ov))
		return FAILED ("Did not find the 1");
	if (ok != GINT_TO_POINTER(1))
		return FAILED ("Incorrect key found");
	if (ov != GINT_TO_POINTER (2))
		return FAILED ("Got wrong value %p\n", ov);
	
	g_hash_table_destroy (hash);

	return NULL;
}

static void
counter (gpointer key, gpointer value, gpointer user_data)
{
	int *counter = (int *) user_data;

	(*counter)++;
}

static RESULT
hash_grow (void)
{
	GHashTable *hash = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, g_free);
	int i, count = 0;
	
	for (i = 0; i < 1000; i++)
		g_hash_table_insert (hash, g_strdup_printf ("%d", i), g_strdup_printf ("x-%d", i));

	for (i = 0; i < 1000; i++){
		char buffer [30];
		gpointer value;
		
		sprintf (buffer, "%d", i);

		value = g_hash_table_lookup (hash, buffer);
		sprintf (buffer, "x-%d", i);
		if (strcmp (value, buffer) != 0){
			return FAILED ("Failed to lookup the key %d, the value was %s\n", i, value);
		}
	}

	if (g_hash_table_size (hash) != 1000)
		return FAILED ("Did not find 1000 elements on the hash, found %d\n", g_hash_table_size (hash));

	/* Now do the manual count, lets not trust the internals */
	g_hash_table_foreach (hash, counter, &count);
	if (count != 1000){
		return FAILED ("Foreach count is not 1000");
	}

	g_hash_table_destroy (hash);
	return NULL;
}

static RESULT
hash_iter (void)
{
#if !defined(GLIB_MAJOR_VERSION) || GLIB_CHECK_VERSION(2, 16, 0)
	GHashTable *hash = g_hash_table_new_full (g_direct_hash, g_direct_equal, NULL, NULL);
	GHashTableIter iter;
	int i, sum, keys_sum, values_sum;
	gpointer key, value;

	sum = 0;
	for (i = 0; i < 1000; i++) {
		sum += i;
		g_hash_table_insert (hash, GUINT_TO_POINTER (i), GUINT_TO_POINTER (i));
	}

	keys_sum = values_sum = 0;
	g_hash_table_iter_init (&iter, hash);
	while (g_hash_table_iter_next (&iter, &key, &value)) {
		if (key != value)
			return FAILED ("key != value");
		keys_sum += GPOINTER_TO_UINT (key);
		values_sum += GPOINTER_TO_UINT (value);
	}
	if (keys_sum != sum || values_sum != sum)
		return FAILED ("Did not find all key-value pairs");
	g_hash_table_destroy (hash);
	return NULL;
#else
	/* GHashTableIter was added in glib 2.16 */
	return NULL;
#endif
}

static Test hashtable_tests [] = {
	{"t1", hash_t1},
	{"t2", hash_t2},
	{"grow", hash_grow},
	{"default", hash_default},
	{"null_lookup", hash_null_lookup},
	{"iter", hash_iter},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(hashtable_tests_init, hashtable_tests)
