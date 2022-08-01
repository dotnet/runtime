/**
 * \file
 * stdlib replacement functions.
 *
 * Authors:
 * 	Gonzalo Paniagua Javier (gonzalo@novell.com)
 *
 * (C) 2006 Novell, Inc.  http://www.novell.com
 *
 */
#include <config.h>
#include <glib.h>
#include <errno.h>
#include <mono/utils/mono-errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <fcntl.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include "mono-stdlib.h"

#ifndef HAVE_MKSTEMP
#ifndef O_BINARY
#define O_BINARY	0
#endif

int
mono_mkstemp (char *templ)
{
	int ret;
	int count = 27; /* Windows doc. */
	char *t;
	size_t len;

	len = strlen (templ);
	do {
		t = g_mktemp (templ);

		if (t == NULL) {
			mono_set_errno (EINVAL);
			return -1;
		}

		if (*templ == '\0') {
			return -1;
		}

		ret = g_open (templ, O_RDWR | O_BINARY | O_CREAT | O_EXCL, 0600);
		if (ret == -1) {
			if (errno != EEXIST)
				return -1;
			memcpy (templ + len - 6, "XXXXXX", 6);
		} else {
			break;
		}

	} while (count-- > 0);

	return ret;
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_stdlib);

#endif

