#include <glib.h>

/* This test is just to be used with valgrind */
char *
test_strfreev ()
{
	gchar **array = g_new (gchar *, 4);
	array [0] = g_strdup ("one");
	array [1] = g_strdup ("two");
	array [2] = g_strdup ("three");
	array [3] = NULL;
	
	g_strfreev (array);
	g_strfreev (NULL);

	return NULL;
}

char *
test_concat ()
{
	gchar *x = g_strconcat ("Hello", ", ", "world", NULL);
	if (strcmp (x, "Hello, world") != 0)
		return g_strdup_printf ("concat failed, got: %s", x);
	g_free (x);
	return NULL;
}

char *
test_split ()
{
	gchar **v = g_strsplit("Hello world, how are we doing today?", " ", 0);
	int i = 0;
	
	if(v == NULL) {
		return g_strdup_printf("split failed, got NULL vector");
	} else {
		for(i = 0; v[i] != NULL; i++);
		if(i != 7) {
			return g_strdup_printf("split failed, expected 7 tokens, got %d\n", i);
		}
	}
	
	g_strfreev(v);
	return NULL;
}

