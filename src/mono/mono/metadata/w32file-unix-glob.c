/*	$OpenBSD: glob.c,v 1.26 2005/11/28 17:50:12 deraadt Exp $ */
/**
 * \file
 * Copyright (c) 1989, 1993
 *	The Regents of the University of California.  All rights reserved.
 *
 * This code is derived from software contributed to Berkeley by
 * Guido van Rossum.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the University nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE REGENTS AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 */

/*
 * mono_w32file_unix_glob(3) -- a subset of the one defined in POSIX 1003.2.
 *
 * Optional extra services, controlled by flags not defined by POSIX:
 *
 * GLOB_MAGCHAR:
 *	Set in gl_flags if pattern contained a globbing character.
 */
#include <sys/types.h>
#include <sys/stat.h>

#include <glib.h>
#include <ctype.h>
#include <errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

#include "w32file-unix-glob.h"

#define	EOS		'\0'
#define	NOT		'!'
#define	QUESTION	'?'
#define	QUOTE		'\\'
#define	STAR		'*'

#ifndef DEBUG

#define	M_QUOTE		0x8000
#define	M_PROTECT	0x4000
#define	M_MASK		0xffff
#define	M_ASCII		0x00ff

typedef unsigned short Char;

#else

#define	M_QUOTE		0x80
#define	M_PROTECT	0x40
#define	M_MASK		0xff
#define	M_ASCII		0x7f

typedef char Char;

#endif


#define	CHAR(c)		((gchar)((c)&M_ASCII))
#define	META(c)		((gchar)((c)|M_QUOTE))
#define	M_ALL		META('*')
#define	M_ONE		META('?')
#define	ismeta(c)	(((c)&M_QUOTE) != 0)


static int
g_Ctoc(const gchar *, char *, unsigned int);

static int
glob0(GDir *dir, const gchar *, mono_w32file_unix_glob_t *, gboolean, gboolean);
static int
glob1(GDir *dir, gchar *, gchar *, mono_w32file_unix_glob_t *, size_t *, gboolean, gboolean);

static int
glob3(GDir *dir, gchar *, gchar *, mono_w32file_unix_glob_t *, size_t *, gboolean, gboolean);

static int
globextend(const gchar *, mono_w32file_unix_glob_t *, size_t *);

static int
match(const gchar *, gchar *, gchar *, gboolean);

#ifdef DEBUG_ENABLED
static void	 qprintf(const char *, Char *);
#endif

int
mono_w32file_unix_glob(GDir *dir, const char *pattern, int flags, mono_w32file_unix_glob_t *pglob)
{
	const unsigned char *patnext;
	int c;
	gchar *bufnext, *bufend, patbuf[PATH_MAX];

	patnext = (unsigned char *) pattern;
	if (!(flags & W32FILE_UNIX_GLOB_APPEND)) {
		pglob->gl_pathc = 0;
		pglob->gl_pathv = NULL;
		pglob->gl_offs = 0;
	}
	pglob->gl_flags = flags & ~W32FILE_UNIX_GLOB_MAGCHAR;

	bufnext = patbuf;
	bufend = bufnext + PATH_MAX - 1;

	/* Protect the quoted characters. */
	while (bufnext < bufend && (c = *patnext++) != EOS)
		if (c == QUOTE) {
			if ((c = *patnext++) == EOS) {
				c = QUOTE;
				--patnext;
			}
			*bufnext++ = c | M_PROTECT;
		} else
			*bufnext++ = c;

	*bufnext = EOS;

	return glob0(dir, patbuf, pglob, flags & W32FILE_UNIX_GLOB_IGNORECASE,
		     flags & W32FILE_UNIX_GLOB_UNIQUE);
}

/*
 * The main glob() routine: compiles the pattern (optionally processing
 * quotes), calls glob1() to do the real pattern matching, and finally
 * sorts the list (unless unsorted operation is requested).  Returns 0
 * if things went well, nonzero if errors occurred.  It is not an error
 * to find no matches.
 */
static int
glob0(GDir *dir, const gchar *pattern, mono_w32file_unix_glob_t *pglob, gboolean ignorecase,
	gboolean unique)
{
	const gchar *qpatnext;
	int c, err, oldpathc;
	gchar *bufnext, patbuf[PATH_MAX];
	size_t limit = 0;

	qpatnext = pattern;
	oldpathc = pglob->gl_pathc;
	bufnext = patbuf;

	/* We don't need to check for buffer overflow any more. */
	while ((c = *qpatnext++) != EOS) {
		switch (c) {
		case QUESTION:
			pglob->gl_flags |= W32FILE_UNIX_GLOB_MAGCHAR;
			*bufnext++ = M_ONE;
			break;
		case STAR:
			pglob->gl_flags |= W32FILE_UNIX_GLOB_MAGCHAR;
			/* collapse adjacent stars to one,
			 * to avoid exponential behavior
			 */
			if (bufnext == patbuf || bufnext[-1] != M_ALL)
				*bufnext++ = M_ALL;
			break;
		default:
			*bufnext++ = CHAR(c);
			break;
		}
	}
	*bufnext = EOS;
#ifdef DEBUG_ENABLED
	qprintf("glob0:", patbuf);
#endif

	if ((err = glob1(dir, patbuf, patbuf+PATH_MAX-1, pglob, &limit,
			 ignorecase, unique)) != 0)
		return(err);

	if (pglob->gl_pathc == oldpathc) {
		return(W32FILE_UNIX_GLOB_NOMATCH);
	}

	return(0);
}

static int
glob1(GDir *dir, gchar *pattern, gchar *pattern_last, mono_w32file_unix_glob_t *pglob,
      size_t *limitp, gboolean ignorecase, gboolean unique)
{
	/* A null pathname is invalid -- POSIX 1003.1 sect. 2.4. */
	if (*pattern == EOS)
		return(0);
	return(glob3(dir, pattern, pattern_last, pglob, limitp, ignorecase,
		     unique));
}

static gboolean contains (mono_w32file_unix_glob_t *pglob, const gchar *name)
{
	int i;
	char **pp;
	
	if (pglob->gl_pathv != NULL) {
		pp = pglob->gl_pathv + pglob->gl_offs;
		for (i = pglob->gl_pathc; i--; ++pp) {
			if (*pp) {
				if (!strcmp (*pp, name)) {
					return(TRUE);
				}
			}
		}
	}
	
	return(FALSE);
}

static int
glob3(GDir *dir, gchar *pattern, gchar *pattern_last, mono_w32file_unix_glob_t *pglob,
      size_t *limitp, gboolean ignorecase, gboolean unique)
{
	const gchar *name;

	/* Search directory for matching names. */
	while ((name = g_dir_read_name(dir))) {
		if (!match(name, pattern, pattern + strlen (pattern),
			   ignorecase)) {
			continue;
		}
		if (!unique ||
		    !contains (pglob, name)) {
			globextend (name, pglob, limitp);
		}
	}

	return(0);
}


/*
 * Extend the gl_pathv member of a mono_w32file_unix_glob_t structure to accommodate a new item,
 * add the new item, and update gl_pathc.
 *
 * This assumes the BSD realloc, which only copies the block when its size
 * crosses a power-of-two boundary; for v7 realloc, this would cause quadratic
 * behavior.
 *
 * Return 0 if new item added, error code if memory couldn't be allocated.
 *
 * Invariant of the mono_w32file_unix_glob_t structure:
 *	Either gl_pathc is zero and gl_pathv is NULL; or gl_pathc > 0 and
 *	gl_pathv points to (gl_offs + gl_pathc + 1) items.
 */
static int
globextend(const gchar *path, mono_w32file_unix_glob_t *pglob, size_t *limitp)
{
	char **pathv;
	int i;
	unsigned int newsize, len;
	char *copy;
	const gchar *p;

	newsize = sizeof(*pathv) * (2 + pglob->gl_pathc + pglob->gl_offs);
	/* FIXME: Can just use realloc(). */
	pathv = (char **)(pglob->gl_pathv ? g_realloc ((char *)pglob->gl_pathv, newsize) :
	    g_malloc (newsize));
	if (pathv == NULL) {
		if (pglob->gl_pathv) {
			g_free (pglob->gl_pathv);
			pglob->gl_pathv = NULL;
		}
		return(W32FILE_UNIX_GLOB_NOSPACE);
	}

	if (pglob->gl_pathv == NULL && pglob->gl_offs > 0) {
		/* first time around -- clear initial gl_offs items */
		pathv += pglob->gl_offs;
		for (i = pglob->gl_offs; --i >= 0; )
			*--pathv = NULL;
	}
	pglob->gl_pathv = pathv;

	for (p = path; *p++;)
		;
	len = (size_t)(p - path);
	*limitp += len;
	if ((copy = (char *)malloc(len)) != NULL) {
		if (g_Ctoc(path, copy, len)) {
			g_free (copy);
			return(W32FILE_UNIX_GLOB_NOSPACE);
		}
		pathv[pglob->gl_offs + pglob->gl_pathc++] = copy;
	}
	pathv[pglob->gl_offs + pglob->gl_pathc] = NULL;

#if 0
	/* Broken on opensuse 11 */
	if ((pglob->gl_flags & W32FILE_UNIX_GLOB_LIMIT) &&
	    newsize + *limitp >= ARG_MAX) {
		errno = 0;
		return(W32FILE_UNIX_GLOB_NOSPACE);
	}
#endif

	return(copy == NULL ? W32FILE_UNIX_GLOB_NOSPACE : 0);
}


/*
 * pattern matching function for filenames.  Each occurrence of the *
 * pattern causes a recursion level.
 */
static int
match(const gchar *name, gchar *pat, gchar *patend, gboolean ignorecase)
{
	gchar c;

	while (pat < patend) {
		c = *pat++;
		switch (c & M_MASK) {
		case M_ALL:
			if (pat == patend)
				return(1);
			do {
				if (match(name, pat, patend, ignorecase))
					return(1);
			} while (*name++ != EOS);
			return(0);
		case M_ONE:
			if (*name++ == EOS)
				return(0);
			break;
		default:
			if (ignorecase) {
				if (g_ascii_tolower (*name++) != g_ascii_tolower (c))
					return(0);
			} else {
				if (*name++ != c)
					return(0);
			}
			
			break;
		}
	}
	return(*name == EOS);
}

/* Free allocated data belonging to a mono_w32file_unix_glob_t structure. */
void
mono_w32file_unix_globfree(mono_w32file_unix_glob_t *pglob)
{
	int i;
	char **pp;

	if (pglob->gl_pathv != NULL) {
		pp = pglob->gl_pathv + pglob->gl_offs;
		for (i = pglob->gl_pathc; i--; ++pp)
			if (*pp)
				g_free (*pp);
		g_free (pglob->gl_pathv);
		pglob->gl_pathv = NULL;
	}
}

static int
g_Ctoc(const gchar *str, char *buf, unsigned int len)
{

	while (len--) {
		if ((*buf++ = *str++) == EOS)
			return (0);
	}
	return (1);
}

#ifdef DEBUG_ENABLED
static void
qprintf(const char *str, Char *s)
{
	Char *p;

	(void)printf("%s:\n", str);
	for (p = s; *p; p++)
		(void)printf("%c", CHAR(*p));
	(void)printf("\n");
	for (p = s; *p; p++)
		(void)printf("%c", *p & M_PROTECT ? '"' : ' ');
	(void)printf("\n");
	for (p = s; *p; p++)
		(void)printf("%c", ismeta(*p) ? '_' : ' ');
	(void)printf("\n");
}
#endif
