// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef EVENTPIPE_STRING_H
#define EVENTPIPE_STRING_H

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

#include <ctype.h> // isspace

static
inline
bool
ep_rt_utf8_string_is_null_or_empty (const ep_char8_t *str)
{
	if (str == NULL)
		return true;

	while (*str) {
		if (!isspace(*str))
			return false;
		str++;
	}
	return true;
}

// Convert a null-terminated string from UTF-8 to UTF-16LE
ep_char16_t *
ep_rt_utf8_to_utf16le_string (
	const ep_char8_t *str);

// Convert the specified number of characters of a string from UTF-8 to UTF-16LE
ep_char16_t *
ep_rt_utf8_to_utf16le_string_n (
	const ep_char8_t *str,
	size_t len);

// Convert a null-terminated string from UTF-16 to UTF-8
ep_char8_t *
ep_rt_utf16_to_utf8_string (
	const ep_char16_t *str);

// Convert the specified number of characters of a string from UTF-16 to UTF-8
ep_char8_t *
ep_rt_utf16_to_utf8_string_n (
	const ep_char16_t *str,
	size_t len);

// Convert a null-terminated string from UTF-16LE to UTF-8
ep_char8_t *
ep_rt_utf16le_to_utf8_string (
	const ep_char16_t *str);

// Convert the specified number of characters of a string from UTF-16LE to UTF-8
ep_char8_t *
ep_rt_utf16le_to_utf8_string_n (
	const ep_char16_t *str,
	size_t len);

#endif /* ENABLE_PERFTRACING */
#endif /* EVENTPIPE_STRING_H */
