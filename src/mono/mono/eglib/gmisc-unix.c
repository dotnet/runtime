/*
 * gmisc.c: Misc functions with no place to go (right now)
 *
 * Author:
 *   Aaron Bockover (abockover@novell.com)
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

#include <config.h>
#include <stdlib.h>
#include <glib.h>
#include <pthread.h>

#ifdef HAVE_PWD_H
#include <pwd.h>
#endif

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

static pthread_mutex_t env_lock = PTHREAD_MUTEX_INITIALIZER;

/* MONO Comment
 * 
 * As per the UNIX spec, 
 * "The return value from getenv() may point to static data which may be overwritten by subsequent calls to getenv(), setenv(), or unsetenv()."
 * Source: Unix Manual Pages for getenv, IEEE Std 1003.1
 *
 * This means that using pointers returned from getenv may (and does) lead to many
 * pointers which refer to the same piece of memory. When one is freed, all will be freed.
 *
 * This is unsafe and an ergonomics risk to fix in the callers. While the caller could lock,
 * this introduces the risk for looping or exiting while inside of a lock. For this reason,
 * g_getenv does not mimic the behavior of POSIX getenv anymore.
 *
 * The memory address returned will be unique to the invocaton, and must be freed.
 * */ 
gchar *
g_getenv (const gchar *variable)
{
	gchar *ret = NULL;
	pthread_mutex_lock (&env_lock);
	gchar *res = getenv(variable);
	if (res)
		ret = g_strdup(res);
	pthread_mutex_unlock (&env_lock);

	return ret;
}

/*
 * This function checks if the given variable is non-NULL
 * in the environment. It's useful because it removes memory
 * freeing requirements.
 *
 */
gboolean
g_hasenv (const gchar *variable)
{
	pthread_mutex_lock (&env_lock);
	gchar *res = getenv(variable);
	gboolean not_null = (res != NULL);
	pthread_mutex_unlock (&env_lock);

	return not_null;
}

gboolean
g_setenv(const gchar *variable, const gchar *value, gboolean overwrite)
{
	gboolean res;
	pthread_mutex_lock (&env_lock);
	res = (setenv(variable, value, overwrite) == 0);
	pthread_mutex_unlock (&env_lock);
	return res;
}

void
g_unsetenv(const gchar *variable)
{
	pthread_mutex_lock (&env_lock);
	unsetenv(variable);
	pthread_mutex_unlock (&env_lock);
}

gchar*
g_win32_getlocale(void)
{
	return NULL;
}

gboolean
g_path_is_absolute (const char *filename)
{
	g_return_val_if_fail (filename != NULL, FALSE);

	return (*filename == '/');
}

static pthread_mutex_t pw_lock = PTHREAD_MUTEX_INITIALIZER;
static const gchar *home_dir;
static const gchar *user_name;

static void
get_pw_data (void)
{
#ifdef HAVE_GETPWUID_R
	struct passwd pw;
	struct passwd *result = NULL;
	char buf [4096];
#endif

	if (user_name != NULL)
		return;

	pthread_mutex_lock (&pw_lock);
	if (user_name != NULL) {
		pthread_mutex_unlock (&pw_lock);
		return;
	}

	home_dir = g_getenv ("HOME");
	user_name = g_getenv ("USER");

#ifdef HAVE_GETPWUID_R
	if (home_dir == NULL || user_name == NULL) {
		if (getpwuid_r (getuid (), &pw, buf, 4096, &result) == 0 && result) {
			if (home_dir == NULL)
				home_dir = g_strdup (pw.pw_dir);
			if (user_name == NULL)
				user_name = g_strdup (pw.pw_name);
		}
	}
#endif

	if (user_name == NULL)
		user_name = "somebody";
	if (home_dir == NULL)
		home_dir = "/";

	pthread_mutex_unlock (&pw_lock);
}

const gchar *
g_get_home_dir (void)
{
	get_pw_data ();
	return home_dir;
}

const gchar *
g_get_user_name (void)
{
	get_pw_data ();
	return user_name;
}

static const char *tmp_dir;

static pthread_mutex_t tmp_lock = PTHREAD_MUTEX_INITIALIZER;

const gchar *
g_get_tmp_dir (void)
{
	if (tmp_dir == NULL){
		pthread_mutex_lock (&tmp_lock);
		if (tmp_dir == NULL){
			tmp_dir = g_getenv ("TMPDIR");
			if (tmp_dir == NULL){
				tmp_dir = g_getenv ("TMP");
				if (tmp_dir == NULL){
					tmp_dir = g_getenv ("TEMP");
					if (tmp_dir == NULL)
						tmp_dir = "/tmp";
				}
			}
		}
		pthread_mutex_unlock (&tmp_lock);
	}
	return tmp_dir;
}

