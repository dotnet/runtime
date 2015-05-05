/*
 * parse.c: Parsing for GC options.
 *
 * Copyright (C) 2015 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

#include <config.h>
#include <glib.h>
#include <string.h>
#include <errno.h>
#include <ctype.h>
#include <stdlib.h>

#include "parse.h"

/**
 * mono_gc_parse_environment_string_extract_number:
 *
 * @str: points to the first digit of the number
 * @out: pointer to the variable that will receive the value
 *
 * Tries to extract a number from the passed string, taking in to account m, k
 * and g suffixes
 *
 * Returns true if passing was successful
 */
gboolean
mono_gc_parse_environment_string_extract_number (const char *str, size_t *out)
{
	char *endptr;
	int len = strlen (str), shift = 0;
	size_t val;
	gboolean is_suffix = FALSE;
	char suffix;

	if (!len)
		return FALSE;

	suffix = str [len - 1];

	switch (suffix) {
		case 'g':
		case 'G':
			shift += 10;
		case 'm':
		case 'M':
			shift += 10;
		case 'k':
		case 'K':
			shift += 10;
			is_suffix = TRUE;
			break;
		default:
			if (!isdigit (suffix))
				return FALSE;
			break;
	}

	errno = 0;
	val = strtol (str, &endptr, 10);

	if ((errno == ERANGE && (val == LONG_MAX || val == LONG_MIN))
			|| (errno != 0 && val == 0) || (endptr == str))
		return FALSE;

	if (is_suffix) {
		size_t unshifted;

		if (*(endptr + 1)) /* Invalid string. */
			return FALSE;

		unshifted = (size_t)val;
		val <<= shift;
		if (((size_t)val >> shift) != unshifted) /* value too large */
			return FALSE;
	}

	*out = val;
	return TRUE;
}
