#include <stdio.h>
#include <string.h>
#include <glib.h>
#include "test.h"

#define do_bad_test(s) do { char *r = markup_test (s); if (r == NULL) return FAILED ("Failed on test " # s); } while (0)
#define do_ok_test(s) do { char *r = markup_test (s); if (r != NULL) return FAILED ("Could not parse valid " # s); } while (0)

static char *
markup_test (const char *s)
{
	GMarkupParser *parser = g_new0 (GMarkupParser, 1);
	GMarkupParseContext *context;
	GError *error = NULL;
	
	context = g_markup_parse_context_new (parser, 0, 0, 0);

	g_markup_parse_context_parse (context, s, strlen (s), &error);
	g_markup_parse_context_free (context);

	if (error != NULL){
		char *msg = g_strdup (error->message);
		g_error_free (error);

		return msg;
	}
	return NULL;
}

RESULT invalid_documents (void)
{
	/* These should fail */
	do_bad_test ("<1>");
	do_bad_test ("<a<");
	do_bad_test ("</a>");
	do_bad_test ("<a b>");
	do_bad_test ("<a b=>");
	do_bad_test ("<a b=c>");
	
	return OK;
}


RESULT valid_documents (void)
{
	/* These should fail */
	do_ok_test ("<a>");
	do_ok_test ("<a a=\"b\">");
	
	return OK;
}

static Test markup_tests [] = {
	{"invalid_documents", invalid_documents},
	{"good_documents", valid_documents},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(markup_tests_init, markup_tests)

