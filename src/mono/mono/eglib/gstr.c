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
*/
#define MONO_ERRNO_MAX 200
#define str(s) #s

#ifndef G_OS_WIN32
static pthread_mutex_t strerror_lock = PTHREAD_MUTEX_INITIALIZER;
#endif

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

#ifndef STRERROR_R_CHAR_P
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
#else /* STRERROR_R_CHAR_P */
		buff = strerror_r (errnum, buff, buff_len);
		if (!error_messages [errnum])
			error_messages [errnum] = g_strdup (buff);
#endif /* STRERROR_R_CHAR_P */

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

gchar *
g_strconcat (const gchar *first, ...)
{
	va_list args;
	size_t total = 0;
	char *s, *ret;
	g_return_val_if_fail (first != NULL, NULL);

	total += strlen (first);
	va_start (args, first);
	for (s = va_arg (args, char *); s != NULL; s = va_arg(args, char *)){
		total += strlen (s);
	}
	va_end (args);
	
	ret = g_malloc (total + 1);
	if (ret == NULL)
		return NULL;

	ret [total] = 0;
	strcpy (ret, first);
	va_start (args, first);
	for (s = va_arg (args, char *); s != NULL; s = va_arg(args, char *)){
		strcat (ret, s);
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
g_strjoin (const gchar *separator, ...)
{
	va_list args;
	char *res, *s, *r;
	size_t len, slen;

	if (separator != NULL)
		slen = strlen (separator);
	else
		slen = 0;
	
	len = 0;
	va_start (args, separator);
	for (s = va_arg (args, char *); s != NULL; s = va_arg (args, char *)){
		len += strlen (s);
		len += slen;
	}
	va_end (args);

	if (len == 0)
		return g_strdup ("");
	
	/* Remove the last separator */
	if (slen > 0 && len > 0)
		len -= slen;

	res = g_malloc (len + 1);
	va_start (args, separator);
	s = va_arg (args, char *);
	r = g_stpcpy (res, s);
	for (s = va_arg (args, char *); s != NULL; s = va_arg (args, char *)){
		if (separator != NULL)
			r = g_stpcpy (r, separator);
		r = g_stpcpy (r, s);
	}
	va_end (args);

	return res;
}

gchar *
g_strjoinv (const gchar *separator, gchar **str_array)
{
	char *res, *r;
	size_t slen, len, i;
	
	if (separator != NULL)
		slen = strlen (separator);
	else
		slen = 0;
	
	len = 0;
	for (i = 0; str_array [i] != NULL; i++){
		len += strlen (str_array [i]);
		len += slen;
	}

	if (len == 0)
		return g_strdup ("");

	if (slen > 0 && len > 0)
		len -= slen;

	res = g_malloc (len + 1);
	r = g_stpcpy (res, str_array [0]);
	for (i = 1; str_array [i] != NULL; i++){
		if (separator != NULL)
			r = g_stpcpy (r, separator);
		r = g_stpcpy (r, str_array [i]);
	}

	return res;
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
g_printf(gchar const *format, ...)
{
	va_list args;
	gint ret;

	va_start(args, format);
	ret = vprintf(format, args);
	va_end(args);

	return ret;
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

static const char hx [] = { '0', '1', '2', '3', '4', '5', '6', '7',
				  '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

static gboolean
char_needs_encoding (char c)
{
	if (((unsigned char)c) >= 0x80)
		return TRUE;
	
	if ((c >= '@' && c <= 'Z') ||
	    (c >= 'a' && c <= 'z') ||
	    (c >= '&' && c < 0x3b) ||
	    (c == '!') || (c == '$') || (c == '_') || (c == '=') || (c == '~'))
		return FALSE;
	return TRUE;
}

gchar *
g_filename_to_uri (const gchar *filename, const gchar *hostname, GError **error)
{
	size_t n;
	char *ret, *rp;
	const char *p;
#ifdef G_OS_WIN32
	const char *uriPrefix = "file:///";
#else
	const char *uriPrefix = "file://";
#endif
	
	g_return_val_if_fail (filename != NULL, NULL);

	if (hostname != NULL)
		g_warning ("%s", "eglib: g_filename_to_uri: hostname not handled");

	if (!g_path_is_absolute (filename)){
		if (error != NULL)
			*error = g_error_new (NULL, 2, "Not an absolute filename");
		
		return NULL;
	}
	
	n = strlen (uriPrefix) + 1;
	for (p = filename; *p; p++){
#ifdef G_OS_WIN32
		if (*p == '\\') {
			n++;
			continue;
		}
#endif
		if (char_needs_encoding (*p))
			n += 3;
		else
			n++;
	}
	ret = g_malloc (n);
	strcpy (ret, uriPrefix);
	for (p = filename, rp = ret + strlen (ret); *p; p++){
#ifdef G_OS_WIN32
		if (*p == '\\') {
			*rp++ = '/';
			continue;
		}
#endif
		if (char_needs_encoding (*p)){
			*rp++ = '%';
			*rp++ = hx [((unsigned char)(*p)) >> 4];
			*rp++ = hx [((unsigned char)(*p)) & 0xf];
		} else
			*rp++ = *p;
	}
	*rp = 0;
	return ret;
}

static int
decode (char p)
{
	if (p >= '0' && p <= '9')
		return p - '0';
	if (p >= 'A' && p <= 'F')
		return p - 'A';
	if (p >= 'a' && p <= 'f')
		return p - 'a';
	g_assert_not_reached ();
	return 0;
}

gchar *
g_filename_from_uri (const gchar *uri, gchar **hostname, GError **error)
{
	const char *p;
	char *r, *result;
	int flen = 0;
	
	g_return_val_if_fail (uri != NULL, NULL);

	if (hostname != NULL)
		g_warning ("%s", "eglib: g_filename_from_uri: hostname not handled");

	if (strncmp (uri, "file:///", 8) != 0){
		if (error != NULL)
			*error = g_error_new (NULL, 2, "URI does not start with the file: scheme");
		return NULL;
	}

	for (p = uri + 8; *p; p++){
		if (*p == '%'){
			if (p [1] && p [2] && isxdigit (p [1]) && isxdigit (p [2])){
				p += 2;
			} else {
				if (error != NULL)
					*error = g_error_new (NULL, 2, "URI contains an invalid escape sequence");
				return NULL;
			}
		} 
		flen++;
	}
#ifndef G_OS_WIN32
	flen++;
#endif

	result = g_malloc (flen + 1);
	result [flen] = 0;

#ifndef G_OS_WIN32
	*result = '/';
	r = result + 1;
#else
	r = result;
#endif

	for (p = uri + 8; *p; p++){
		if (*p == '%'){
			*r++ = (char)((decode (p [1]) << 4) | decode (p [2]));
			p += 2;
		} else
			*r++ = *p;
		flen++;
	}
	return result;
}

void
g_strdown (gchar *string)
{
	g_return_if_fail (string != NULL);

	while (*string){
		*string = (gchar)tolower (*string);
		string++;
	}
}

gchar
g_ascii_tolower (gchar c)
{
	return c >= 'A' && c <= 'Z' ? c + ('a' - 'A') : c;
}

gchar *
g_ascii_strdown (const gchar *str, gssize len)
{
	char *ret;
	int i;
	
	g_return_val_if_fail  (str != NULL, NULL);

	if (len == -1)
		len = strlen (str);
	
	ret = g_malloc (len + 1);
	for (i = 0; i < len; i++)
		ret [i] = (guchar) g_ascii_tolower (str [i]);
	ret [i] = 0;
	
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
		ret [i] = (guchar) g_ascii_toupper (str [i]);
	ret [i] = 0;
	
	return ret;
}

gint
g_ascii_strncasecmp (const gchar *s1, const gchar *s2, gsize n)
{
	gsize i;
	
	g_return_val_if_fail (s1 != NULL, 0);
	g_return_val_if_fail (s2 != NULL, 0);

	for (i = 0; i < n; i++) {
		gchar c1 = g_ascii_tolower (*s1++);
		gchar c2 = g_ascii_tolower (*s2++);
		
		if (c1 != c2)
			return c1 - c2;
	}
	
	return 0;
}

gint
g_ascii_strcasecmp (const gchar *s1, const gchar *s2)
{
	const char *sp1 = s1;
	const char *sp2 = s2;
	
	g_return_val_if_fail (s1 != NULL, 0);
	g_return_val_if_fail (s2 != NULL, 0);
	
	while (*sp1 != '\0') {
		char c1 = g_ascii_tolower (*sp1++);
		char c2 = g_ascii_tolower (*sp2++);
		
		if (c1 != c2)
			return c1 - c2;
	}
	
	return (*sp1) - (*sp2);
}

gchar *
g_strdelimit (gchar *string, const gchar *delimiters, gchar new_delimiter)
{
	gchar *ptr;

	g_return_val_if_fail (string != NULL, NULL);

	if (delimiters == NULL)
		delimiters = G_STR_DELIMITERS;

	for (ptr = string; *ptr; ptr++) {
		if (strchr (delimiters, *ptr))
			*ptr = new_delimiter;
	}
	
	return string;
}

gsize 
g_strlcpy (gchar *dest, const gchar *src, gsize dest_size)
{
#ifdef HAVE_STRLCPY
	return strlcpy (dest, src, dest_size);
#else
	gchar *d;
	const gchar *s;
	gchar c;
	gsize len;
	
	g_return_val_if_fail (src != NULL, 0);
	g_return_val_if_fail (dest != NULL, 0);

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

gchar *
g_stpcpy (gchar *dest, const char *src)
{
	g_return_val_if_fail (dest != NULL, dest);
	g_return_val_if_fail (src != NULL, dest);

#if HAVE_STPCPY
	return stpcpy (dest, src);
#else
	while (*src)
		*dest++ = *src++;
	
	*dest = '\0';
	
	return dest;
#endif
}

static const gchar escaped_dflt [256] = {
	1, 1, 1, 1, 1, 1, 1, 1, 'b', 't', 'n', 1, 'f', 'r', 1, 1,
	1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
	0, 0, '"', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, '\\', 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
	1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
	1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
	1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
	1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
	1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
	1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
	1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
};

gchar *
g_strescape (const gchar *source, const gchar *exceptions)
{
	gchar escaped [256];
	const gchar *ptr;
	gchar c;
	gchar op;
	gchar *result;
	gchar *res_ptr;

	g_return_val_if_fail (source != NULL, NULL);

	memcpy (escaped, escaped_dflt, 256);
	if (exceptions != NULL) {
		for (ptr = exceptions; *ptr; ptr++)
			escaped [(int) *ptr] = 0;
	}
	result = g_malloc (strlen (source) * 4 + 1); /* Worst case: everything octal. */
	res_ptr = result;
	for (ptr = source; *ptr; ptr++) {
		c = *ptr;
		op = escaped [(int) c];
		if (op == 0) {
			*res_ptr++ = c;
		} else {
			*res_ptr++ = '\\';
			if (op != 1) {
				*res_ptr++ = op;
			} else {
				*res_ptr++ = '0' + ((c >> 6) & 3);
				*res_ptr++ = '0' + ((c >> 3) & 7);
				*res_ptr++ = '0' + (c & 7);
			}
		}
	}
	*res_ptr = '\0';
	return result;
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
