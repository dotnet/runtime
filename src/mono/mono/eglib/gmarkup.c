/*
 * gmakrup.c: Minimal XML markup reader.
 *
 * Unlike the GLib one, this can not be restarted with more text
 * as the Mono use does not require it.
 *
 * Actually, with further thought, I think that this could be made
 * to restart very easily.  The pos == end condition would mean
 * "return to caller" and only at end parse this would be a fatal
 * error.
 *
 * Not that it matters to Mono, but it is very simple to change, there
 * is a tricky situation: there are a few places where we check p+n
 * in the source, and that would have to change to be progressive, instead
 * of depending on the string to be complete at that point, so we would
 * have to introduce extra states to cope with that.
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
#include <ctype.h>
#include <glib.h>

#define set_error(msg, ...) do { if (error != NULL) *error = g_error_new (GINT_TO_POINTER (1), 1, msg, __VA_ARGS__); } while (0);

typedef enum {
	START,
	START_ELEMENT,
	TEXT,
	FLUSH_TEXT,
	CLOSING_ELEMENT,
	COMMENT,
	SKIP_XML_DECLARATION
} ParseState;

struct _GMarkupParseContext {
	GMarkupParser  parser;
	gpointer       user_data;
	GDestroyNotify user_data_dnotify;
	ParseState     state;

	/* Stores the name of the current element, so we can issue the end_element */
	GSList         *level;

	GString        *text;
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
	GSList *l;
	
	g_return_if_fail (context != NULL);

	if (context->user_data_dnotify != NULL)
		(context->user_data_dnotify) (context->user_data);
	
	if (context->text != NULL)
		g_string_free (context->text, TRUE);
	for (l = context->level; l; l = l->next)
		g_free (l->data);
	g_slist_free (context->level);
	g_free (context);
}

static gboolean
my_isspace (char c)
{
	if (c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\v')
		return TRUE;
	return FALSE;
}

static gboolean
my_isalnum (char c)
{
	if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
		return TRUE;
	if (c >= '0' && c <= '9')
		return TRUE;

	return FALSE;
}

static gboolean
my_isalpha (char c)
{
	if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
		return TRUE;
	return FALSE;
}

static const char *
skip_space (const char *p, const char *end)
{
	for (; p < end && my_isspace (*p); p++)
		;
	return p;
}

static const char *
parse_value (const char *p, const char *end, char **value, GError **error)
{
	const char *start;
	int l;
	
	if (*p != '"'){
		set_error ("%s", "Expected the attribute value to start with a quote");
		return end;
	}
	start = ++p;
	for (; p < end && *p != '"'; p++)
		;
	if (p == end)
		return end;
	l = (int)(p - start);
	p++;
	*value = g_malloc (l + 1);
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
	
	for (; p < end && my_isalnum (*p); p++)
		;
	if (p == end)
		return end;

	l = (int)(p - start);
	*value = g_malloc (l + 1);
	if (*value == NULL)
		return end;
	strncpy (*value, start, l);
	(*value) [l] = 0;
	return p;
}

static const char *
parse_attributes (const char *p, const char *end, char ***names, char ***values, GError **error, int *full_stop, int state)
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
		if (state == SKIP_XML_DECLARATION && *p == '?' && ((p+1) < end) && *(p+1) == '>'){
			*full_stop = 0;
			return p+1;
		}
		
		if (*p == '/' && ((p+1) < end && *(p+1) == '>')){
			*full_stop = 1;
			return p+1;
		} else {
			char *name, *value;
			
			p = parse_name (p, end, &name);
			if (p == end)
				return p;

			p = skip_space (p, end);
			if (p == end){
				g_free (name);
				return p;
			}
			if (*p != '='){
				set_error ("Expected an = after the attribute name `%s'", name);
				g_free (name);
				return end;
			}
			p++;
			p = skip_space (p, end);
			if (p == end){
				g_free (name);
				return end;
			}

			p = parse_value (p, end, &value, error);
			if (p == end){
				g_free (name);
				return p;
			}

			++nnames;
			*names = g_realloc (*names, sizeof (char **) * (nnames+1));
			*values = g_realloc (*values, sizeof (char **) * (nnames+1));
			(*names) [nnames-1] = name;
			(*values) [nnames-1] = value;
			(*names) [nnames] = NULL;
			(*values) [nnames] = NULL;			
		}
	} 
}

static void
destroy_parse_state (GMarkupParseContext *context)
{
	GSList *p;

	for (p = context->level; p != NULL; p = p->next)
		g_free (p->data);
	
	g_slist_free (context->level);
	if (context->text != NULL)
		g_string_free (context->text, TRUE);
	context->text = NULL;
	context->level = NULL;
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
			if (c == ' ' || c == '\t' || c == '\f' || c == '\n' || (c & 0x80))
				continue;
			if (c == '<'){
				if (p+1 < end && p [1] == '?'){
					context->state = SKIP_XML_DECLARATION;
					p++;
				} else
					context->state = START_ELEMENT;
				continue;
			}
			set_error ("%s", "Expected < to start the document");
			goto fail;

		case SKIP_XML_DECLARATION:
		case START_ELEMENT: {
			const char *element_start = p, *element_end;
			char *ename = NULL;
			int full_stop = 0, l;
			gchar **names = NULL, **values = NULL;

			for (; p < end && my_isspace (*p); p++)
				;
			if (p == end){
				set_error ("%s", "Unfinished element");
				goto fail;
			}

			if (*p == '!' && (p+2 < end) && (p [1] == '-') && (p [2] == '-')){
				context->state = COMMENT;
				p += 2;
				break;
			}
			
			if (!my_isalpha (*p)){
				set_error ("%s", "Expected an element name");
				goto fail;
			}
			
			for (++p; p < end && (my_isalnum (*p) || (*p == '.')); p++)
				;
			if (p == end){
				set_error ("%s", "Expected an element");
				goto fail;
			}
			element_end = p;
			
			for (; p < end && my_isspace (*p); p++)
				;
			if (p == end){
				set_error ("%s", "Unfinished element");
				goto fail;
			}
			p = parse_attributes (p, end, &names, &values, error, &full_stop, context->state);
			if (p == end){
				if (names != NULL) {
					g_strfreev (names);
					g_strfreev (values);
				}
				/* Only set the error if parse_attributes did not */
				if (error != NULL && *error == NULL)
					set_error ("%s", "Unfinished sequence");
				goto fail;
			}
			l = (int)(element_end - element_start);
			ename = g_malloc (l + 1);
			if (ename == NULL)
				goto fail;
			strncpy (ename, element_start, l);
			ename [l] = 0;

			if (context->state == START_ELEMENT)
				if (context->parser.start_element != NULL)
					context->parser.start_element (context, ename,
								       (const gchar **) names,
								       (const gchar **) values,
								       context->user_data, error);

			if (names != NULL){
				g_strfreev (names);
				g_strfreev (values);
			}

			if (error != NULL && *error != NULL){
				g_free (ename);
				goto fail;
			}
			
			if (full_stop){
				if (context->parser.end_element != NULL &&  context->state == START_ELEMENT){
					context->parser.end_element (context, ename, context->user_data, error);
					if (error != NULL && *error != NULL){
						g_free (ename);
						goto fail;
					}
				}
				g_free (ename);
			} else {
				context->level = g_slist_prepend (context->level, ename);
			}
			
			context->state = TEXT;
			break;
		} /* case START_ELEMENT */

		case TEXT: {
			if (c == '<'){
				context->state = FLUSH_TEXT;
				break;
			}
			if (context->parser.text != NULL){
				if (context->text == NULL)
					context->text = g_string_new ("");
				g_string_append_c (context->text, c);
			}
			break;
		}

		case COMMENT:
			if (*p != '-')
				break;
			if (p+2 < end && (p [1] == '-') && (p [2] == '>')){
				context->state = TEXT;
				p += 2;
				break;
			}
			break;
			
		case FLUSH_TEXT:
			if (context->parser.text != NULL && context->text != NULL){
				context->parser.text (context, context->text->str, context->text->len,
						      context->user_data, error);
				if (error != NULL && *error != NULL)
					goto fail;
			}
			
			if (c == '/')
				context->state = CLOSING_ELEMENT;
			else {
				p--;
				context->state = START_ELEMENT;
			}
			break;

		case CLOSING_ELEMENT: {
			GSList *current = context->level;
			char *text;

			if (context->level == NULL){
				set_error ("%s", "Too many closing tags, not enough open tags");
				goto fail;
			}
			
			text = current->data;
			if (context->parser.end_element != NULL){
				context->parser.end_element (context, text, context->user_data, error);
				if (error != NULL && *error != NULL){
					g_free (text);
					goto fail;
				}
			}
			g_free (text);

			while (p < end && *p != '>')
				p++;
			
			context->level = context->level->next;
			g_slist_free_1 (current);
			context->state = TEXT;
			break;
		} /* case CLOSING_ELEMENT */
			
		} /* switch */
	}


	return TRUE;
 fail:
	if (context->parser.error && error != NULL && *error)
		context->parser.error (context, *error, context->user_data);
	
	destroy_parse_state (context);
	return FALSE;
}

gboolean
g_markup_parse_context_end_parse (GMarkupParseContext *context, GError **error)
{
	g_return_val_if_fail (context != NULL, FALSE);

	/*
	 * In our case, we always signal errors during parse, not at the end
	 * see the notes at the top of this file for details on how this
	 * could be moved here
	 */
	return TRUE;
}
