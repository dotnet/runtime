/*
 * gstr.c: String Utility Functions.
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
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
#include <stdio.h>
#include <string.h>
#include <ctype.h>
#include <glib.h>

#ifndef G_OS_WIN32
#include <pthread.h>
#endif

#include <errno.h>

/*
 * g_strndup and g_vasprintf need to allocate memory with g_malloc if
 * ENABLE_OVERRIDABLE_ALLOCATORS is defined so that it can be safely freed with g_free
 * rather than free.
 */

/* This is not a macro, because I dont want to put _GNU_SOURCE in the glib.h header */
gchar *
g_strndup (const gchar *str, gsize n)
{
#if defined (HAVE_STRNDUP) && !defined (ENABLE_OVERRIDABLE_ALLOCATORS)
	return strndup (str, n);
#else
	if (str) {
		char *retval = g_malloc(n+1);
		if (retval) {
			strncpy(retval, str, n)[n] = 0;
		}
		return retval;
	}
	return NULL;
#endif
}

gint g_vasprintf (gchar **ret, const gchar *fmt, va_list ap)
{
#if defined (HAVE_VASPRINTF) && !defined (ENABLE_OVERRIDABLE_ALLOCATORS)
  return vasprintf (ret, fmt, ap);
#else
	char *buf;
	int len;
	size_t buflen;
	va_list ap2;

#if defined(_MSC_VER) || defined(__MINGW64_VERSION_MAJOR)
	ap2 = ap;
	len = _vscprintf(fmt, ap2); // NOTE MS specific extension ( :-( )
#else
	va_copy(ap2, ap);
	len = vsnprintf(NULL, 0, fmt, ap2);
#endif

	if (len >= 0 && (buf = g_malloc ((buflen = (size_t) (len + 1)))) != NULL) {
		len = vsnprintf(buf, buflen, fmt, ap);
		*ret = buf;
	} else {
		*ret = NULL;
		len = -1;
	}

	va_end(ap2);
	return len;
#endif
}

void
g_strfreev (gchar **str_array)
{
	gchar **orig = str_array;
	if (str_array == NULL)
		return;
	while (*str_array != NULL){
		g_free (*str_array);
		str_array++;
	}
	g_free (orig);
}

gchar **
g_strdupv (gchar **str_array)
{
	guint length;
	gchar **ret;
	guint i;

	if (!str_array)
		return NULL;

	length = g_strv_length(str_array);
	ret = g_new0(gchar *, length + 1);
	for (i = 0; str_array[i]; i++) {
		ret[i] = g_strdup(str_array[i]);
	}
	ret[length] = NULL;
	return ret;
}

guint
g_strv_length(gchar **str_array)
{
	gint length = 0;
	g_return_val_if_fail(str_array != NULL, 0);
	for(length = 0; str_array[length] != NULL; length++);
	return length;
}

gboolean
g_str_has_suffix(const gchar *str, const gchar *suffix)
{
	size_t str_length;
	size_t suffix_length;

	g_return_val_if_fail(str != NULL, FALSE);
	g_return_val_if_fail(suffix != NULL, FALSE);

	str_length = strlen(str);
	suffix_length = strlen(suffix);

	return suffix_length <= str_length ?
		strncmp(str + str_length - suffix_length, suffix, suffix_length) == 0 :
		FALSE;
}

gboolean
g_str_has_prefix(const gchar *str, const gchar *prefix)
{
	size_t str_length;
	size_t prefix_length;

	g_return_val_if_fail(str != NULL, FALSE);
	g_return_val_if_fail(prefix != NULL, FALSE);

	str_length = strlen(str);
	prefix_length = strlen(prefix);

	return prefix_length <= str_length ?
		strncmp(str, prefix, prefix_length) == 0 :
		FALSE;
}

gchar *
g_strdup_vprintf (const gchar *format, va_list args)
{
	int n;
	char *ret;

	n = g_vasprintf (&ret, format, args);
	if (n == -1)
		return NULL;

	return ret;
}

gchar *
g_strdup_printf (const gchar *format, ...)
{
	gchar *ret;
	va_list args;
	int n;

	va_start (args, format);
	n = g_vasprintf (&ret, format, args);
	va_end (args);
	if (n == -1)
		return NULL;

	return ret;
}


/*
Max error number we support. It's empirically found by looking at our target OS.

Last this was checked was June-2017.

Apple is at 106.
Android is at 133.

Haiku starts numbering at 0x8000_7000 (like HRESULT on Win32) for POSIX errno,
but errors from BeAPI or custom user libraries could be lower or higher.
(Technically, this is C and old POSIX compliant, but not new POSIX compliant.)
The big problem with this is that it effectively means errors start at a
negative offset. As such, disable the whole strerror caching mechanism.

*/
#define MONO_ERRNO_MAX 200
#define str(s) #s

#ifndef G_OS_WIN32
static pthread_mutex_t strerror_lock = PTHREAD_MUTEX_INITIALIZER;
#endif

#if defined(__HAIKU__)
const gchar *
g_strerror (gint errnum)
{
	/* returns a const char* on Haiku */
	return strerror(errnum);
}
#else
static char *error_messages [MONO_ERRNO_MAX];

const gchar *
g_strerror (gint errnum)
{
	if (errnum < 0)
		errnum = -errnum;
	if (errnum >= MONO_ERRNO_MAX)
		return ("Error number higher than " str (MONO_ERRNO_MAX));

	if (!error_messages [errnum]) {
#ifndef G_OS_WIN32
		pthread_mutex_lock (&strerror_lock);
#endif

#ifdef HAVE_STRERROR_R
		char tmp_buff [128]; //Quite arbitrary, should be large enough
		char *buff = tmp_buff;
		size_t buff_len = sizeof (tmp_buff);
		buff [0] = 0;

#if HAVE_GNU_STRERROR_R
                buff = strerror_r (errnum, buff, buff_len);
                if (!error_messages [errnum])
                        error_messages [errnum] = g_strdup (buff);
#else /* HAVE_GNU_STRERROR_R */
		int r;
		while ((r = strerror_r (errnum, buff, buff_len - 1))) {
			if (r != ERANGE) {
				buff = g_strdup_printf ("Invalid Error code '%d'", errnum);
				break;
			}
			if (buff == tmp_buff)
				buff = g_malloc (buff_len * 2);
			else
				buff = g_realloc (buff, buff_len * 2);
			buff_len *= 2;
		 //Spec is not clean on whether size argument includes space for null terminator or not
		}
		if (!error_messages [errnum])
			error_messages [errnum] = g_strdup (buff);
		if (buff != tmp_buff)
			g_free (buff);
#endif /* HAVE_GNU_STRERROR_R */

#else /* HAVE_STRERROR_R */
		if (!error_messages [errnum])
			error_messages [errnum] = g_strdup_printf ("Error code '%d'", errnum);
#endif /* HAVE_STRERROR_R */


#ifndef G_OS_WIN32
		pthread_mutex_unlock (&strerror_lock);
#endif

	}
	return error_messages [errnum];
}
#endif

gchar *
g_strconcat (const gchar *first, ...)
{
	va_list args;
	char *s, *ret;
	g_return_val_if_fail (first != NULL, NULL);

	size_t len = strlen (first);
	va_start (args, first);
	for (s = va_arg (args, char *); s != NULL; s = va_arg(args, char *)){
		len += strlen (s);
	}
	va_end (args);

	ret = (char*)g_malloc (len + 1);
	if (ret == NULL)
		return NULL;

	ret [len] = 0;
	len = strlen (first);
	memcpy (ret, first, len);
	va_start (args, first);
	first = ret; // repurpose first as cursor
	for (s = va_arg (args, char *); s != NULL; s = va_arg(args, char *)){
		first += len;
		memcpy ((char*)first, s, len = strlen (s));
	}
	va_end (args);

	return ret;
}

static void
add_to_vector (gchar ***vector, int size, gchar *token)
{
	*vector = *vector == NULL ?
		(gchar **)g_malloc(2 * sizeof(*vector)) :
		(gchar **)g_realloc(*vector, (size + 1) * sizeof(*vector));

	(*vector)[size - 1] = token;
}

gchar **
g_strsplit (const gchar *string, const gchar *delimiter, gint max_tokens)
{
	const gchar *c;
	gchar *token, **vector;
	gint size = 1;

	g_return_val_if_fail (string != NULL, NULL);
	g_return_val_if_fail (delimiter != NULL, NULL);
	g_return_val_if_fail (delimiter[0] != 0, NULL);

	if (strncmp (string, delimiter, strlen (delimiter)) == 0) {
		vector = (gchar **)g_malloc (2 * sizeof(vector));
		vector[0] = g_strdup ("");
		size++;
		string += strlen (delimiter);
	} else {
		vector = NULL;
	}

	while (*string && !(max_tokens > 0 && size >= max_tokens)) {
		c = string;
		if (strncmp (string, delimiter, strlen (delimiter)) == 0) {
			token = g_strdup ("");
			string += strlen (delimiter);
		} else {
			while (*string && strncmp (string, delimiter, strlen (delimiter)) != 0) {
				string++;
			}

			if (*string) {
				gsize toklen = (string - c);
				token = g_strndup (c, toklen);

				/* Need to leave a trailing empty
				 * token if the delimiter is the last
				 * part of the string
				 */
				if (strcmp (string, delimiter) != 0) {
					string += strlen (delimiter);
				}
			} else {
				token = g_strdup (c);
			}
		}

		add_to_vector (&vector, size, token);
		size++;
	}

	if (*string) {
		if (strcmp (string, delimiter) == 0)
			add_to_vector (&vector, size, g_strdup (""));
		else {
			/* Add the rest of the string as the last element */
			add_to_vector (&vector, size, g_strdup (string));
		}
		size++;
	}

	if (vector == NULL) {
		vector = (gchar **) g_malloc (2 * sizeof (vector));
		vector [0] = NULL;
	} else if (size > 0) {
		vector[size - 1] = NULL;
	}

	return vector;
}

static gboolean
charcmp (gchar testchar, const gchar *compare)
{
	while(*compare) {
		if (*compare == testchar) {
			return TRUE;
		}
		compare++;
	}

	return FALSE;
}

gchar **
g_strsplit_set (const gchar *string, const gchar *delimiter, gint max_tokens)
{
	const gchar *c;
	gchar *token, **vector;
	gint size = 1;

	g_return_val_if_fail (string != NULL, NULL);
	g_return_val_if_fail (delimiter != NULL, NULL);
	g_return_val_if_fail (delimiter[0] != 0, NULL);

	if (charcmp (*string, delimiter)) {
		vector = (gchar **)g_malloc (2 * sizeof(vector));
		vector[0] = g_strdup ("");
		size++;
		string++;
	} else {
		vector = NULL;
	}

	c = string;
	while (*string && !(max_tokens > 0 && size >= max_tokens)) {
		if (charcmp (*string, delimiter)) {
			gsize toklen = (string - c);
			if (toklen == 0) {
				token = g_strdup ("");
			} else {
				token = g_strndup (c, toklen);
			}

			c = string + 1;

			add_to_vector (&vector, size, token);
			size++;
		}

		string++;
	}

	if (max_tokens > 0 && size >= max_tokens) {
		if (*string) {
			/* Add the rest of the string as the last element */
			add_to_vector (&vector, size, g_strdup (string));
			size++;
		}
	} else {
		if (*c) {
			/* Fill in the trailing last token */
			add_to_vector (&vector, size, g_strdup (c));
			size++;
		} else {
			/* Need to leave a trailing empty token if the
			 * delimiter is the last part of the string
			 */
			add_to_vector (&vector, size, g_strdup (""));
			size++;
		}
	}

	if (vector == NULL) {
		vector = (gchar **) g_malloc (2 * sizeof (vector));
		vector [0] = NULL;
	} else if (size > 0) {
		vector[size - 1] = NULL;
	}

	return vector;
}

gchar *
g_strreverse (gchar *str)
{
	size_t i, j;
	gchar c;

	if (str == NULL)
		return NULL;

	if (*str == 0)
		return str;

	for (i = 0, j = strlen (str) - 1; i < j; i++, j--) {
		c = str [i];
		str [i] = str [j];
		str [j] = c;
	}

	return str;
}

gchar *
g_strchug (gchar *str)
{
	size_t len;
	gchar *tmp;

	if (str == NULL)
		return NULL;

	tmp = str;
	while (*tmp && isspace (*tmp)) tmp++;
	if (str != tmp) {
		len = strlen (str) - (tmp - str - 1);
		memmove (str, tmp, len);
	}
	return str;
}

gchar *
g_strchomp (gchar *str)
{
	gchar *tmp;

	if (str == NULL)
		return NULL;

	tmp = str + strlen (str) - 1;
	while (*tmp && isspace (*tmp)) tmp--;
	*(tmp + 1) = '\0';
	return str;
}

gint
g_fprintf(FILE *file, gchar const *format, ...)
{
	va_list args;
	gint ret;

	va_start(args, format);
	ret = vfprintf(file, format, args);
	va_end(args);

	return ret;
}

gint
g_sprintf(gchar *string, gchar const *format, ...)
{
	va_list args;
	gint ret;

	va_start(args, format);
	ret = vsprintf(string, format, args);
	va_end(args);

	return ret;
}

gint
g_snprintf(gchar *string, gulong n, gchar const *format, ...)
{
	va_list args;
	gint ret;

	va_start(args, format);
	ret = vsnprintf(string, n, format, args);
	va_end(args);

	return ret;
}

gchar
g_ascii_tolower (gchar c)
{
	return c >= 'A' && c <= 'Z' ? c + ('a' - 'A') : c;
}

void
g_ascii_strdown_no_alloc (char* dst, const char* src, gsize len)
{
	// dst can equal src. no_alloc means this function does no
	// allocation; caller may very well.

	for (gsize i = 0; i < len; ++i)
		dst [i] = g_ascii_tolower (src [i]);
}

gchar *
g_ascii_strdown (const gchar *str, gssize len)
{
	char *ret;

	g_return_val_if_fail  (str != NULL, NULL);

	if (len == -1)
		len = strlen (str);

	ret = g_malloc (len + 1);
	g_ascii_strdown_no_alloc (ret, str, len);
	ret [len] = 0;

	return ret;
}

gchar
g_ascii_toupper (gchar c)
{
	return c >= 'a' && c <= 'z' ? c + ('A' - 'a') : c;
}

gchar *
g_ascii_strup (const gchar *str, gssize len)
{
	char *ret;
	int i;

	g_return_val_if_fail  (str != NULL, NULL);

	if (len == -1)
		len = strlen (str);

	ret = g_malloc (len + 1);
	for (i = 0; i < len; i++)
		ret [i] = g_ascii_toupper (str [i]);
	ret [i] = 0;

	return ret;
}

static
int
g_ascii_charcmp (char c1, char c2)
{
	// Do not subtract, to avoid overflow.
	// Use unsigned to mimic strcmp, and so
	// shorter strings compare as less.

	const guchar u1 = (guchar)c1;
	const guchar u2 = (guchar)c2;
	return (u1 < u2) ? -1 : (u1 > u2) ? 1 : 0;
}

static
int
g_ascii_charcasecmp (char c1, char c2)
{
	return g_ascii_charcmp (g_ascii_tolower (c1), g_ascii_tolower (c2));
}

gint
g_ascii_strncasecmp (const gchar *s1, const gchar *s2, gsize n)
{
	// Unlike strncmp etc. this function does not stop at nul,
	// unless there is a mismatch.

	if (s1 == s2)
		return 0;

	gsize i;

	g_return_val_if_fail (s1 != NULL, 0);
	g_return_val_if_fail (s2 != NULL, 0);

	for (i = 0; i < n; i++) {
		const int j = g_ascii_charcasecmp (*s1++, *s2++);
		if (j)
			return j;
	}

	return 0;
}

gint
g_ascii_strcasecmp (const gchar *s1, const gchar *s2)
{
	if (s1 == s2)
		return 0;

	g_return_val_if_fail (s1 != NULL, 0);
	g_return_val_if_fail (s2 != NULL, 0);

	char c1;

	while ((c1 = *s1)) {
		++s1;
		const int j = g_ascii_charcasecmp (c1, *s2++);
		if (j)
			return j;
	}

	return g_ascii_charcmp (0, *s2);
}

void
g_strdelimit (gchar *string, gchar delimiter, gchar new_delimiter)
{
	gchar *ptr;

	g_return_if_fail (string != NULL);

	for (ptr = string; *ptr; ptr++) {
		if (delimiter == *ptr)
			*ptr = new_delimiter;
	}
}

gsize
g_strlcpy (gchar *dest, const gchar *src, gsize dest_size)
{
	g_assert (src);
	g_assert (dest);

#ifdef HAVE_STRLCPY
	return strlcpy (dest, src, dest_size);
#else
	gchar *d;
	const gchar *s;
	gchar c;
	gsize len;

	len = dest_size;
	if (len == 0)
		return 0;

	s = src;
	d = dest;
	while (--len) {
		c = *s++;
		*d++ = c;
		if (c == '\0')
			return (dest_size - len - 1);
	}

	/* len is 0 i we get here */
	*d = '\0';
	/* we need to return the length of src here */
	while (*s++) ; /* instead of a plain strlen, we use 's' */
	return s - src - 1;
#endif
}

gint
g_ascii_xdigit_value (gchar c)
{
	return ((isxdigit (c) == 0) ? -1 :
		((c >= '0' && c <= '9') ? (c - '0') :
		 ((c >= 'a' && c <= 'f') ? (c - 'a' + 10) :
		  (c - 'A' + 10))));
}

gchar *
g_strnfill (gsize length, gchar fill_char)
{
	gchar *ret = g_new (gchar, length + 1);

	memset (ret, fill_char, length);
	ret [length] = 0;
	return ret;
}

size_t
g_utf16_len (const gunichar2 *a)
{
#ifdef G_OS_WIN32
	return wcslen (a);
#else
	size_t length = 0;
	while (a [length])
		++length;
	return length;
#endif
}

gsize
g_strnlen (const char* s, gsize n)
{
	gsize i;
	for (i = 0; i < n && s [i]; ++i) ;
	return i;
}
