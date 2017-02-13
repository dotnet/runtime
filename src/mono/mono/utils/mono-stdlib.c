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
	int len;

	len = strlen (templ);
	do {
		t = mktemp (templ);
		if (t == NULL) {
			errno = EINVAL;
			return -1;
		}

		if (*templ == '\0') {
			return -1;
		}

		ret = open (templ, O_RDWR | O_BINARY | O_CREAT | O_EXCL, 0600);
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
#endif

