#include <glib.h>
#include <string.h>
#include <stdio.h>
#include "test.h"

#define sfail(k,p) if (s->str [p] != k) { g_string_free (s,TRUE); return FAILED("Got %s, Failed at %d, expected '%c'", s->str, p, k);}

RESULT
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
		return FAILED("Did not copy correctly, got: %s", s->str+4);
	}

	g_string_free (s, TRUE);
	s = g_string_new ("");
	for (i = 0; i < 1024; i++){
		g_string_append (s, "x");
	}
	if (strlen (s->str) != 1024){
		return FAILED("Incorrect string size, got: %s %d", s->str, strlen (s->str));
	}
	g_string_free (s, TRUE);

	s = g_string_new ("");
	for (i = 0; i < 1024; i++){
		g_string_append_c (s, 'x');
	}
	if (strlen (s->str) != 1024){
		return FAILED("Incorrect string size, got: %s %d\n", s->str, strlen (s->str));
	}
	g_string_free (s, TRUE);

	s = g_string_new ("hola");
	g_string_sprintfa (s, "%s%d", ", bola", 5);
	if (strcmp (s->str, "hola, bola5") != 0){
		return FAILED("Incorrect data, got: %s\n", s->str);
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
	sfail ( 0, 1);
	sfail ('H', 2);
	sfail ('1', 3);
	sfail ( 0, 4);
	sfail ('2', 5);
	g_string_free (s, TRUE);
	
	return OK;
}

static Test string_tests [] = {
	{"GString", test_gstring},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(string_tests_init, string_tests)
