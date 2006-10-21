/*
 * Portable Utility Functions
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
#include <unistd.h>
#include <errno.h>
#include <sys/types.h>
#include <pwd.h>
#include <pthread.h>

gchar *
g_build_path (const gchar *separator, const gchar *first_element, ...)
{
	GString *result;
	const char *s, *p, *next;
	int slen;
	va_list args;
	
	g_return_val_if_fail (separator != NULL, NULL);
	g_return_val_if_fail (first_element != NULL, NULL);

	result = g_string_sized_new (48);

	slen = strlen (separator);
	
	va_start (args, first_element);
	for (s = first_element; s != NULL; s = next){
		next = va_arg (args, char *);
		p = (s + strlen (s));

		if (next && p - slen > s){
			for (; strncmp (p-slen, separator, slen) == 0; ){
				p -= slen;
			}
		}
		g_string_append_len (result, s, p - s);

		if (next && *next){
			g_string_append (result, separator);

			for (; strncmp (next, separator, slen) == 0; )
				next += slen;
		}
	}
	g_string_append_c (result, 0);
	va_end (args);

	return g_string_free (result, FALSE);
}

gchar *
g_path_get_dirname (const gchar *filename)
{
	char *p, *r;
	int count;
	g_return_val_if_fail (filename != NULL, NULL);

	p = strrchr (filename, G_DIR_SEPARATOR);
	if (p == NULL)
		return g_strdup (".");
	count = p - filename;
	r = g_malloc (count + 1);
	strncpy (r, filename, count);
	r [count] = 0;

	return r;
}

gchar *
g_path_get_basename (const char *filename)
{
	char *r;
	g_return_val_if_fail (filename != NULL, NULL);

	/* Empty filename -> . */
	if (!*filename)
		return g_strdup (".");

	/* No separator -> filename */
	r = strrchr (filename, G_DIR_SEPARATOR);
	if (r == NULL)
		return g_strdup (filename);

	/* Trailing slash, remove component */
	if (r [1] == 0){
		char *copy = g_strdup (filename);
		copy [r-filename] = 0;
		r = strrchr (copy, G_DIR_SEPARATOR);

		if (r == NULL){
			g_free (copy);			
			return g_strdup ("/");
		}
		r = g_strdup (&r[1]);
		g_free (copy);
		return r;
	}

	return g_strdup (&r[1]);
}

gboolean
g_path_is_absolute (const char *filename)
{
	g_return_val_if_fail (filename != NULL, FALSE);
	return (*filename == '/');
}

gchar *
g_find_program_in_path (const gchar *program)
{
	char *p = g_strdup (getenv ("PATH"));
	char *x = p, *l;
	gchar *curdir = NULL;
	char *save;

	g_return_val_if_fail (program != NULL, NULL);

	if (x == NULL || *x == '\0') {
		curdir = g_get_current_dir ();
		x = curdir;
	}

	while ((l = strtok_r (x, G_SEARCHPATH_SEPARATOR_S, &save)) != NULL){
		char *probe_path; 
		
		x = NULL;
		probe_path = g_build_path (G_DIR_SEPARATOR_S, l, program, NULL);
		if (access (probe_path, X_OK) == 0){
			g_free (curdir);
			g_free (p);
			return probe_path;
		}
		g_free (probe_path);
	}
	g_free (curdir);
	g_free (p);
	return NULL;
}

gchar *
g_get_current_dir (void)
{
	int s = 32;
	char *buffer = NULL, *r;
	gboolean fail;
	
	do {
		buffer = g_realloc (buffer, s);
		r = getcwd  (buffer, s);
		fail = (r == NULL && errno == ERANGE);
		if (fail) {
			s <<= 1;
		}
	} while (fail);

	return r;
}

static pthread_mutex_t home_lock = PTHREAD_MUTEX_INITIALIZER;
static char *home_dir;

/* Give preference to /etc/passwd than HOME */
const gchar *
g_get_home_dir (void)
{
	if (home_dir == NULL){
		uid_t uid;

		pthread_mutex_lock (&home_lock);
		if (home_dir == NULL){
			struct passwd pwbuf, *track;
			char buf [4096];
			
			uid = getuid ();

			setpwent ();
			
			while (getpwent_r (&pwbuf, buf, sizeof (buf), &track) == 0){
				if (pwbuf.pw_uid == uid){
					home_dir = g_strdup (pwbuf.pw_dir);
					break;
				}
			}
			endpwent ();
			if (home_dir == NULL)
				home_dir = getenv ("HOME");
			pthread_mutex_unlock (&home_lock);
		}
	}
	return home_dir;
}

static char *tmp_dir;
static pthread_mutex_t tmp_lock = PTHREAD_MUTEX_INITIALIZER;

const gchar *
g_get_tmp_dir (void)
{
	if (tmp_dir == NULL){
		pthread_mutex_lock (&tmp_lock);
		if (tmp_dir == NULL){
			tmp_dir = getenv ("TMPDIR");
			if (tmp_dir == NULL){
				tmp_dir = getenv ("TMP");
				if (tmp_dir == NULL){
					tmp_dir = getenv ("TEMP");
					if (tmp_dir == NULL)
						tmp_dir = "/tmp";
				}
			}
		}
		pthread_mutex_unlock (&tmp_lock);
	}
	return tmp_dir;
}

const char *
g_get_user_name (void)
{
	return getenv ("USER");
}

static char *name;

void
g_set_prgname (const gchar *prgname)
{
	name = g_strdup (prgname);
}

gchar *
g_get_prgname (void)
{
	return name;
}
