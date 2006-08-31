#include <stdio.h>
#include <string.h>
#include <glib.h>
#include "test.h"

#define do_test(s) do { char *r = markup_test (s); if (r != NULL) FAILED (r); } while (0)

static char *
markup_test (const char *s)
{
	GMarkupParser *parser = g_new0 (GMarkupParser, 1);
	GMarkupParseContext *context;
	GError *error = NULL;
	
	context = g_markup_parse_context_new (parser, 0, 0, 0);

	g_markup_parse_context_parse (context, s, strlen (s), &error);
	g_markup_parse_context_free (context);

	if (error != NULL)
		return error->message;
	return NULL;
}

RESULT invalid_documents (void)
{
	/* These should fail */
	do_test ("<1>");
	do_test ("<a<");
	do_test ("</a>");
	
	return OK;
}

static Test markup_tests [] = {
	{"invalid_documents", invalid_documents},
	{"t2", hash_t2},
	{"grow", hash_grow},
	{"default", hash_default},
	{"null_lookup", hash_null_lookup},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(hashtable_tests_init, hashtable_tests)

