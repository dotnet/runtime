#include <glib.h>
#include <string.h>
#include <stdio.h>
#include "test.h"

#define sfail(k,p) if (s->str [p] != k) { g_string_free (s,TRUE); return FAILED("Got %s, Failed at %d, expected '%c'", s->str, p, k);}

RESULT
test_append_speed()
{
	GString *s = g_string_new("");
	gint i;
	
	for(i = 0; i < 1024; i++) {
		g_string_append(s, "x");
	}
	
	if(strlen (s->str) != 1024) {
		return FAILED("Incorrect string size, got: %s %d", 
			s->str, strlen(s->str));
	}
	
	g_string_free (s, TRUE);

	return OK;
}

RESULT
test_append_c_speed()
{
	GString *s = g_string_new("");
	gint i;
	
	for(i = 0; i < 1024; i++) {
		g_string_append_c(s, 'x');
	}
	
	if(strlen(s->str) != 1024) {
		return FAILED("Incorrect string size, got: %s %d", s->str, 
			strlen(s->str));
	}
	
	g_string_free(s, TRUE);

	return OK;
}

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

 	s = g_string_new_len ("H" "\000" "H", 3);
	g_string_append_len (s, "1" "\000" "2", 3);
	sfail ('H', 0);
	sfail ( 0, 1);
	sfail ('H', 2);
	sfail ('1', 3);
	sfail ( 0, 4);
	sfail ('2', 5);
	g_string_free (s, TRUE);
	
	return OK;
}

RESULT
test_sized ()
{
	GString *s = g_string_sized_new (20);

	if (s->str [0] != 0)
		return FAILED ("Expected an empty string");
	if (s->len != 0)
		return FAILED ("Expected an empty len");

	g_string_free (s, TRUE);
	
	return NULL;
}

RESULT
test_truncate ()
{
	GString *s = g_string_new ("0123456789");
	g_string_truncate (s, 3);

	if (strlen (s->str) != 3)
		return FAILED ("size of string should have been 3, instead it is [%s]\n", s->str);
	g_string_free (s, TRUE);
	
	s = g_string_new ("a");
	s = g_string_truncate (s, 10);
	if (strlen (s->str) != 1)
		return FAILED ("The size is not 1");
	g_string_truncate (s, (gsize)-1);
	if (strlen (s->str) != 1)
		return FAILED ("The size is not 1");
	g_string_truncate (s, 0);
	if (strlen (s->str) != 0)
		return FAILED ("The size is not 0");
	
	g_string_free (s, TRUE);

	return NULL;
}

RESULT
test_prepend ()
{
	GString *s = g_string_new ("dingus");
	g_string_prepend (s, "one");

	if (strcmp (s->str, "onedingus") != 0)
		return FAILED ("Failed, expected onedingus, got [%s]", s->str);

	g_string_free (s, TRUE);

	/* This is to force the code that where stuff does not fit in the allocated block */
	s = g_string_sized_new (1);
	g_string_prepend (s, "one");
	if (strcmp (s->str, "one") != 0)
		return FAILED ("Got erroneous result, expected [one] got [%s]", s->str);
	g_string_free (s, TRUE);

	/* This is to force the path where things fit */
	s = g_string_new ("123123123123123123123123");
	g_string_truncate (s, 1);
	if (strcmp (s->str, "1") != 0)
		return FAILED ("Expected [1] string, got [%s]", s->str);

	g_string_prepend (s, "pre");
	if (strcmp (s->str, "pre1") != 0)
		return FAILED ("Expected [pre1], got [%s]", s->str);
	g_string_free (s, TRUE);
	
	return NULL;
}

RESULT
test_appendlen ()
{
	GString *s = g_string_new ("");

	g_string_append_len (s, "boo\000x", 0);
	if (s->len != 0)
		return FAILED ("The length is not zero %d", s->len);
	g_string_append_len (s, "boo\000x", 5);
	if (s->len != 5)
		return FAILED ("The length is not five %d", s->len);
	g_string_append_len (s, "ha", -1);
	if (s->len != 7)
		return FAILED ("The length is not seven %d", s->len);
		
	g_string_free (s, TRUE);

	return NULL;
}

RESULT
test_macros ()
{
	char *s = g_strdup (G_STRLOC);
	char *p = strchr (s + 2, ':');
	int n;
	
	if (p == NULL)
		return FAILED ("Did not find a separator");
	n = atoi (p+1);
	if (n <= 0)
		return FAILED ("did not find a valid line number");

	*p = 0;
	if (strcmp (s + strlen(s) - 8 , "string.c") != 0)
		return FAILED ("This did not store the filename on G_STRLOC");
	
	g_free (s);
	return NULL;
}

static Test string_tests [] = {
	{"append-speed", test_append_speed},
	{"append_c-speed", test_append_c_speed},
	{"ctor+append", test_gstring },
	{"ctor+sized", test_sized },
	{"truncate", test_truncate },
	{"prepend", test_prepend },
	{"append_len", test_appendlen },
	{"macros", test_macros },
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(string_tests_init, string_tests)
