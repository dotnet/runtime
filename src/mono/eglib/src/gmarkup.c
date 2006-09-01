/*
 * gmakrup.c: Minimal XML markup reader.
 *
 * Unlike the GLib one, this can not be restarted with more text
 * as the Mono use does not require it
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
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
#include <stdio.h>
#include <glib.h>

#define set_error(msg...) do { if (error != NULL) *error = g_error_new (GINT_TO_POINTER (1), 1, msg); } while (0);

typedef enum {
	START,
	START_ELEMENT,
	TEXT
} ParseState;

struct _GMarkupParseContext {
	GMarkupParser  parser;
	gpointer       user_data;
	GDestroyNotify user_data_dnotify;
	ParseState     state;
};

GMarkupParseContext *
g_markup_parse_context_new (const GMarkupParser *parser,
			    GMarkupParseFlags flags,
			    gpointer user_data,
			    GDestroyNotify user_data_dnotify)
{
	GMarkupParseContext *context = g_new0 (GMarkupParseContext, 1);

	context->parser = *parser;
	context->user_data = user_data;
	context->user_data_dnotify = user_data_dnotify;

	return context;
}

void
g_markup_parse_context_free (GMarkupParseContext *context)
{
	g_free (context);
}

static const char *
skip_space (const char *p, const char *end)
{
	for (; p < end && isspace (*p); p++)
		;
	return p;
}

static const char *
parse_value (const char *p, const char *end, char **value, GError **error)
{
	const char *start;
	int l;
	
	if (*p != '"'){
		set_error ("Expected the attribute value to start with a quote");
		return end;
	}
	start = ++p;
	for (++p; p < end && *p != '"'; p++)
	if (p == end)
		return end;
	l = p - start;
	p++;
	*value = malloc (l + 1);
	if (*value == NULL)
		return end;
	strncpy (*value, start, l);
	(*value) [l] = 0;
	return p;
}

static const char *
parse_name (const char *p, const char *end, char **value)
{
	const char *start = p;
	int l;
	
	for (; p < end && isalnum (*p); p++)
		;
	if (p == end)
		return end;

	l = p - start;
	*value = malloc (l + 1);
	if (*value == NULL)
		return end;
	strncpy (*value, start, l);
	(*value) [l] = 0;
	return p;
}

static const char *
parse_attributes (const char *p, const char *end, char ***names, char ***values, GError **error, int *full_stop)
{
	int nnames = 0;

	while (TRUE){
		p = skip_space (p, end);
		if (p == end)
			return end;
			
		if (*p == '>'){
			*full_stop = 0;
			return p; 
		}
		if (*p == '/' && ((p+1) < end && *p == '>')){
			*full_stop = 1;
			return p+1;
		} else {
			char *name, *value;
			
			p = parse_name (p, end, &name);
			if (p == end)
				return p;
			p = skip_space (p, end);
			if (p == end)
				return p;
			if (*p != '='){
				set_error ("Expected an = after the attribute name `%s'", name);
				return end;
			}
			p++;
			p = skip_space (p, end);
			if (p == end)
				return end;

			p = parse_value (p, end, &value, error);
			if (p == end)
				return p;

			++nnames;
			*names = g_realloc (*names, sizeof (char **) * (nnames+1));
			*values = g_realloc (*values, sizeof (char **) * (nnames+1));
			(*names) [nnames-1] = name;
			(*values) [nnames-1] = name;			
			(*names) [nnames] = NULL;
			(*values) [nnames] = NULL;			
		}
	} 
}

gboolean
g_markup_parse_context_parse (GMarkupParseContext *context,
			      const gchar *text, gssize text_len,
			      GError **error)
{
	const char *p,  *end;
	
	g_return_val_if_fail (context != NULL, FALSE);
	g_return_val_if_fail (text != NULL, FALSE);
	g_return_val_if_fail (text_len >= 0, FALSE);

	end = text + text_len;
	
	for (p = text; p < end; p++){
		char c = *p;
		
		switch (context->state){
		case START:
			if (c == ' ' || c == '\t' || c == '\f' || c == '\n')
				continue;
			if (c == '<'){
				context->state = START_ELEMENT;
				continue;
			}
			set_error ("Expected < to start the document");
			
			return FALSE;


		case START_ELEMENT: {
			const char *element_start = p, *element_end;
			int full_stop = 0;
			gchar **names = NULL, **values = NULL;

			if (!(isascii (*p) && isalpha (*p)))
				set_error ("Must start with a letter");
			
			for (++p; p < end && isalnum (*p); p++)
				;
			if (p == end){
				set_error ("Expected an element");
				return FALSE;
			}
			element_end = p;
			
			for (; p < end && isspace (*p); p++)
				;
			if (p == end){
				set_error ("Unfinished element");
				return FALSE;
			}
			p = parse_attributes (p, end, &names, &values, error, &full_stop);
			if (p == end){
				if (*error == NULL)
					set_error ("Unfinished sequence");
				
				return FALSE;
			}
			if (context->parser.start_element != NULL){
				int l = element_end - element_start;
				char *ename = malloc (l + 1);

				if (ename == NULL)
					return FALSE;
				strncpy (ename, element_start, l);
				ename [l] = 0;
				
				context->parser.start_element (context, ename,
							       (const gchar **) names,
							       (const gchar **) values,
							       context->user_data, error);
				free (ename);
			}
			if (names != NULL){
				g_strfreev (names);
				g_strfreev (values);
			}
			if (*error != NULL)
				return FALSE;
			context->state = full_stop ? START : TEXT;
			break;
		} /* case START_ELEMENT */

		case TEXT: {
			break;
		}
			
		}
	}

	return TRUE;
}

