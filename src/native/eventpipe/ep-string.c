// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#include "ep-rt.h"
#include <minipal/utf8.h>

static
ep_char16_t *
ep_utf8_to_utf16le_string_impl (
	const ep_char8_t *str,
	size_t len)
{
	if (len == 0) {
		// Return an empty string if the length is 0
		ep_char16_t * empty_str = ep_rt_utf16_string_alloc(1);
		if(empty_str == NULL)
			return NULL;

		*empty_str = '\0';
		return empty_str;
	}

	int32_t flags = MINIPAL_MB_NO_REPLACE_INVALID_CHARS | MINIPAL_TREAT_AS_LITTLE_ENDIAN;
	size_t ret = minipal_get_length_utf8_to_utf16 (str, len, flags);
	if (ret <= 0)
		return NULL;

	ep_char16_t * converted_str = ep_rt_utf16_string_alloc(ret + 1);
	if (converted_str == NULL)
		return NULL;

	ret = minipal_convert_utf8_to_utf16 (str, len, (CHAR16_T *)converted_str, ret, flags);
	converted_str[ret] = '\0';
	return converted_str;
}

ep_char16_t *
ep_rt_utf8_to_utf16le_string (
	const ep_char8_t *str)
{
	if (!str)
		return NULL;

	return ep_utf8_to_utf16le_string_impl (str, strlen(str));
}

ep_char16_t *
ep_rt_utf8_to_utf16le_string_n (
	const ep_char8_t *str,
	size_t len)
{
	if (!str)
		return NULL;

	return ep_utf8_to_utf16le_string_impl (str, len);
}

static
ep_char8_t *
ep_utf16_to_utf8_string_impl (
	const ep_char16_t *str,
	size_t len,
	bool treat_as_le)
{
	if (len == 0) {
		// Return an empty string if the length is 0
		ep_char8_t * empty_str = ep_rt_utf8_string_alloc(1);
		if (empty_str == NULL)
			return NULL;

		*empty_str = '\0';
		return empty_str;
	}

	int32_t flags = treat_as_le ? MINIPAL_TREAT_AS_LITTLE_ENDIAN : 0;
	size_t ret = minipal_get_length_utf16_to_utf8 ((const CHAR16_T *)str, len, flags);
	if (ret <= 0)
		return NULL;

	ep_char8_t * converted_str = ep_rt_utf8_string_alloc(ret + 1);
	if (converted_str == NULL)
		return NULL;

	ret = minipal_convert_utf16_to_utf8 ((const CHAR16_T *)str, len, converted_str, ret, flags);
	converted_str[ret] = '\0';
	return converted_str;
}

ep_char8_t *
ep_rt_utf16_to_utf8_string (
	const ep_char16_t *str)
{
	if (!str)
		return NULL;

	return ep_utf16_to_utf8_string_impl(str, ep_rt_utf16_string_len (str), /*treat_as_le*/ false);
}

ep_char8_t *
ep_rt_utf16_to_utf8_string_n (
	const ep_char16_t *str,
	size_t len)
{
	if (!str)
		return NULL;

	return ep_utf16_to_utf8_string_impl(str, len, /*treat_as_le*/ false);
}

ep_char8_t *
ep_rt_utf16le_to_utf8_string (
	const ep_char16_t *str)
{
	if (!str)
		return NULL;

	return ep_utf16_to_utf8_string_impl(str, ep_rt_utf16_string_len (str), /*treat_as_le*/ true);
}

ep_char8_t *
ep_rt_utf16le_to_utf8_string_n (
	const ep_char16_t *str,
	size_t len)
{
	if (!str)
		return NULL;

	return ep_utf16_to_utf8_string_impl(str, len, /*treat_as_le*/ true);
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_string;
const char quiet_linker_empty_file_warning_eventpipe_string = 0;
#endif
