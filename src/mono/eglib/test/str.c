#include <glib.h>
#include <stdio.h>

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

#define sfail(k,p) if (s->str [p] != k) { g_string_free (s,TRUE); return g_strdup_printf ("Failed at %d, expected '%c'", p, k);}

char *
test_gstring ()
{
	GString *s = g_string_new_len ("My stuff", 2);
	char *ret;
	int i;

	if (strcmp (s->str, "My") != 0)
		return "Expected only 'My' on the string";
	g_string_free (s, TRUE);

	s = g_string_new_len ("My\0\0Rest", 6);
	if (s->str [2] != 0)
		return "Null was not copied";
	if (strcmp (s->str+4, "Re") != 0){
		return "Did not find the 'Re' part";
	}

	g_string_append (s, "lalalalalalalalalalalalalalalalalalalalalalal");
	if (s->str [2] != 0)
		return "Null as not copied";
	if (strncmp (s->str+4, "Relala", 6) != 0){
		printf ("got: %s\n", s->str+4);
		return "Did not copy correctly";
	}

	g_string_free (s, TRUE);
	s = g_string_new ("");
	for (i = 0; i < 1024; i++){
		g_string_append (s, "x");
	}
	if (strlen (s->str) != 1024){
		printf ("got: %s %d\n", s->str, strlen (s->str));
		return "Incorrect string size";
	}
	g_string_free (s, TRUE);

	s = g_string_new ("");
	for (i = 0; i < 1024; i++){
		g_string_append_c (s, 'x');
	}
	if (strlen (s->str) != 1024){
		printf ("got: %s %d\n", s->str, strlen (s->str));
		return "Incorrect string size";
	}
	g_string_free (s, TRUE);

	s = g_string_new ("hola");
	g_string_sprintfa (s, "%s%d", ", bola", 5);
	if (strcmp (s->str, "hola, bola5") != 0){
		printf ("got: %s\n", s->str);
		return "Got incorrect data";
	}
	g_string_free (s, TRUE);

	s = g_string_new ("Hola");
	g_string_printf (s, "Dingus");
	
	/* Test that it does not release it */
	ret = g_string_free (s, FALSE);
	g_free (ret);

	s = g_string_new_len ("H\000H", 3);
	g_string_append_len (s, "1\0002", 3);
	sfail ('H', 0);
	sfail (0, 1);
	sfail ('y', 2);
	sfail ('1', 3);
	sfail (0, 4);
	sfail ('2', 5);
	g_string_free (s, TRUE);
	
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

char *
test_strreverse ()
{
	gchar *a = g_strdup ("onetwothree");
	gchar *a_target = "eerhtowteno";
	gchar *b = g_strdup ("onetwothre");
	gchar *b_target = "erhtowteno";

	g_strreverse (a);
	if (strcmp (a, a_target)) {
		g_free (b);
		g_free (a);
		return g_strdup_printf ("strreverse failed. Expecting: '%s' and got '%s'\n", a, a_target);
	}

	g_strreverse (b);
	if (strcmp (b, b_target)) {
		g_free (b);
		g_free (a);
		return g_strdup_printf ("strreverse failed. Expecting: '%s' and got '%s'\n", b, b_target);
	}
	g_free (b);
	g_free (a);
	return NULL;
}

