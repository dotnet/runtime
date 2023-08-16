// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef EVENTPIPE_STRING_H
#define EVENTPIPE_STRING_H

#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ep-types.h"

ep_char16_t *
ep_rt_utf8_to_utf16le_string (
	const ep_char8_t *str,
	size_t len);

ep_char8_t *
ep_rt_utf16_to_utf8_string (
	const ep_char16_t *str,
	size_t len);

ep_char8_t *
ep_rt_utf16le_to_utf8_string (
	const ep_char16_t *str,
	size_t len);

#endif /* ENABLE_PERFTRACING */
#endif /* EVENTPIPE_STRING_H */
