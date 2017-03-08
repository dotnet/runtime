/**
 * \file
 * Parsing for GC options.
 *
 * Copyright (C) 2015 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
 * \param str points to the first digit of the number
 * \param out pointer to the variable that will receive the value
 * Tries to extract a number from the passed string, taking in to account m, k
 * and g suffixes
 * \returns TRUE if passing was successful
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
