/*
 * Shell utility functions.
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
#include <stdio.h>
#include <glib.h>

static int
split_cmdline (const gchar *cmdline, GPtrArray *array, GError **error)
{
	gchar *ptr;
	gchar c;
	gboolean in_quote = FALSE;
	gboolean escaped = FALSE;
	gchar quote_char = '\0';
	GString *str;

	str = g_string_new ("");
	ptr = (gchar *) cmdline;
	while ((c = *ptr++) != '\0') {
		if (escaped) {
			escaped = FALSE;
			if (!g_ascii_isspace (c))
				g_string_append_c (str, c);
		} else if (in_quote) {
			if (c == quote_char) {
				in_quote = FALSE;
				quote_char = '\0';
				g_ptr_array_add (array, g_string_free (str, FALSE));
				str = g_string_new ("");
			} else {
				g_string_append_c (str, c);
			}
		} else if (g_ascii_isspace (c)) {
			if (str->len > 0) {
				g_ptr_array_add (array, g_string_free (str, FALSE));
				str = g_string_new ("");
			}
		} else if (c == '\\') {
			escaped = TRUE;
		} else if (c == '\'' || c == '"') {
			in_quote = TRUE;
			quote_char = c;
		} else {
			g_string_append_c (str, c);
		}
	}

	if (escaped) {
		if (error)
			*error = g_error_new (G_LOG_DOMAIN, 0, "Unfinished escape.");
		g_string_free (str, TRUE);
		return -1;
	}

	if (in_quote) {
		if (error)
			*error = g_error_new (G_LOG_DOMAIN, 0, "Unfinished quote.");
		g_string_free (str, TRUE);
		return -1;
	}

	if (str->len > 0) {
		g_ptr_array_add (array, g_string_free (str, FALSE));
	} else {
		g_string_free (str, TRUE);
	}
	g_ptr_array_add (array, NULL);
	return 0;
}

gboolean
g_shell_parse_argv (const gchar *command_line, gint *argcp, gchar ***argvp, GError **error)
{
	GPtrArray *array;
	gint argc;
	gchar **argv;

	g_return_val_if_fail (command_line, FALSE);
	g_return_val_if_fail (error == NULL || *error == NULL, FALSE);

	array = g_ptr_array_new();
	if (split_cmdline (command_line, array, error)) {
		g_ptr_array_add (array, NULL);
		g_strfreev ((gchar **) array->pdata);
		g_ptr_array_free (array, FALSE);
		return FALSE;
	}

	argc = array->len;
	argv = (gchar **) array->pdata;

	if (argc == 1) {
		g_strfreev (argv);
		g_ptr_array_free (array, FALSE);
		return FALSE;
	}

	if (argcp) {
		*argcp = array->len - 1;
	}

	if (argvp) {
		*argvp = argv;
	} else {
		g_strfreev (argv);
	}

	g_ptr_array_free (array, FALSE);
	return TRUE;
}

gchar *
g_shell_quote (const gchar *unquoted_string)
{
	GString *result = g_string_new ("'");
	const gchar *p;
	
	for (p = unquoted_string; *p; p++){
		if (*p == '\'')
			g_string_append (result, "'\\'");
		g_string_append_c (result, *p);
	}
	g_string_append_c (result, '\'');
	return g_string_free (result, FALSE);
}

gchar *
g_shell_unquote (const gchar *quoted_string, GError **error)
{
//	g_error ("%s", "Not implemented");
	return g_strdup (quoted_string);
//	return NULL;
}
