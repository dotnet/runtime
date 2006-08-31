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

#define set_error(msg...) do { if (error != NULL) *error = g_error_new (1, 1, msg); } while (0);

typedef enum {
	START,
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

gboolean
g_markup_parse_context_parse (GMarkupParseContext *context,
			      const gchar *text, gssize text_len, GError **error)
{
	char *p, *end;
	
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
			char *element_start = p;
			char **names, *values;

			if (!(isascii (*p) && isalpha (*p)))
				set_error ("Must start with a letter");
			
			for (++p; p < end && isalnum (*p); p++)
				;
			if (p == end){
				set_error ("Expected an element");
				return FALSE;
			}
			for (; p < end && isspace (*p); p++)
				;
			if (p == end){
				set_error ("Unfinished element");
				return FALSE;
			}
			p = parse_attributes (p, end, &names, &values);
			if (p == end){
				set_error ("unfinished element");
				return FALSE;
			}
			
		}
		}
	}
}

