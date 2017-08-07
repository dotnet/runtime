/*
 * Simple pattern matching
 *
 * Author:
 *   Gonzalo Paniagua Javier (gonzalo@novell.com
 *
 * (C) 2006 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#include <glib.h>
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#ifndef _MSC_VER
#include <unistd.h>
#endif

typedef enum {
	MATCH_LITERAL,
	MATCH_ANYCHAR,
	MATCH_ANYTHING,
	MATCH_ANYTHING_END,
	MATCH_INVALID = -1
} MatchType;

typedef struct {
	MatchType type;
	gchar *str;
} PData;

struct _GPatternSpec {
	GSList *pattern;
};

static GSList *
compile_pattern (const gchar *pattern)
{
	GSList *list;
	size_t i, len;
	PData *data;
	gchar c;
	MatchType last = MATCH_INVALID;
	GString *str;
	gboolean free_str;

	if (pattern == NULL)
		return NULL;

	data = NULL;
	list = NULL;
	free_str = TRUE;
	str = g_string_new ("");
	for (i = 0, len = strlen (pattern); i < len; i++) {
		c = pattern [i];
		if (c == '*' || c == '?') {
			if (str->len > 0) {
				data = g_new0 (PData, 1);
				data->type = MATCH_LITERAL;
				data->str = g_string_free (str, FALSE);
				list = g_slist_append (list, data);
				str = g_string_new ("");
			}

			if (last == MATCH_ANYTHING && c == '*')
				continue;

			data = g_new0 (PData, 1);
			data->type = (c == '*') ? MATCH_ANYTHING : MATCH_ANYCHAR;
			list = g_slist_append (list, data);
			last = data->type;
		} else {
			g_string_append_c (str, c);
			last = MATCH_LITERAL;
		}
	}

	if (last == MATCH_ANYTHING && str->len == 0) {
		data->type = MATCH_ANYTHING_END;
		free_str = TRUE;
	} else if (str->len > 0) {
		data = g_new0 (PData, 1);
		data->type = MATCH_LITERAL;
		data->str = str->str;
		free_str = FALSE;
		list = g_slist_append (list, data);
	}
	g_string_free (str, free_str);
	return list;
}

#ifdef DEBUG_PATTERN
static void
print_pattern (gpointer data, gpointer user_data)
{
	PData *d = (PData *) data;

	printf ("Type: %s", d->type == MATCH_LITERAL ? "literal" : d->type == MATCH_ANYCHAR ? "any char" : "anything");
	if (d->type == MATCH_LITERAL)
		printf (" String: %s", d->str);
	printf ("\n");
}
#endif

GPatternSpec *
g_pattern_spec_new (const gchar *pattern)
{
	GPatternSpec *spec;

	g_return_val_if_fail (pattern != NULL, NULL);
	spec = g_new0 (GPatternSpec, 1);
	if (pattern) {
		spec->pattern = compile_pattern (pattern);
#ifdef DEBUG_PATTERN
		g_slist_foreach (spec->pattern, print_pattern, NULL);
		printf ("\n");
#endif
	}
	return spec;
}

static void
free_pdata (gpointer data, gpointer user_data)
{
	PData *d = (PData *) data;

	if (d->str)
		g_free (d->str);
	g_free (d);
}

void
g_pattern_spec_free (GPatternSpec *pspec)
{
	if (pspec) {
		g_slist_foreach (pspec->pattern, free_pdata, NULL);
		g_slist_free (pspec->pattern);
		pspec->pattern = NULL;
	}
	g_free (pspec);
}

static gboolean
match_string (GSList *list, const gchar *str, size_t idx, size_t max)
{
	size_t len;

	while (list && idx < max) {
		PData *data = (PData *) list->data;

		if (data->type == MATCH_ANYTHING_END)
			return TRUE;

		if (data->type == MATCH_LITERAL) {
			len = strlen (data->str);
			if (strncmp (&str [idx], data->str, len) != 0)
				return FALSE;
			idx += len;
			list = list->next;
			if (list) {
				/* 
				 * When recursing, we need this to avoid returning FALSE
				 * because 'list' will not be NULL
				 */
				data = (PData *) list->data;
				if (data->type == MATCH_ANYTHING_END)
					return TRUE;
			}
		} else if (data->type == MATCH_ANYCHAR) {
			idx++;
			list = list->next;
		} else if (data->type == MATCH_ANYTHING) {
			while (idx < max) {
				if (match_string (list->next, str, idx++, max))
					return TRUE;
			}
			return FALSE;
		} else {
			g_assert_not_reached ();
		}
	}

	return (list == NULL && idx >= max);
}
gboolean
g_pattern_match_string (GPatternSpec *pspec, const gchar *string)
{
	g_return_val_if_fail (pspec != NULL, FALSE);
	g_return_val_if_fail (string != NULL, FALSE);

	if (pspec->pattern == NULL)
		return FALSE;
	return match_string (pspec->pattern, string, 0, strlen (string));
}


