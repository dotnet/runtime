/*
 * glob.c: Simple glob support for the class libraries
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com).
 *
 * (C) 2002 Ximian, Inc.
 */
#include <sys/types.h>
#include <glib.h>
#include <config.h>
#include <regex.h>
#include "wrapper.h"

gpointer
mono_glob_compile (const char *glob)
{
	regex_t *compiled = g_new (regex_t, 1);
	GString *str = g_string_new ("^");
	const char *p;
	
	for (p = glob; *p; p++){
		switch (*p){
		case '?':
			g_string_append_c (str, '.');
			break;
		case '*':
			g_string_append (str, ".*");
			break;
			
		case '[': case ']': case '\\': case '(': case ')':
		case '^': case '$': case '.':
			g_string_append_c (str, '\\');
			/* fall */
		default:
			g_string_append_c (str, *p);
		}
	}
	g_string_append_c (str, '$');
	regcomp (compiled, str->str, 0);

	return compiled;
}

int
mono_glob_match (gpointer handle, const char *str)
{
	regex_t *compiled = (regex_t *) handle;

	return regexec (compiled, str, 0, NULL, 0) == 0;
}

void
mono_glob_dispose (gpointer handle)
{
	regfree ((regex_t *) handle);
}
