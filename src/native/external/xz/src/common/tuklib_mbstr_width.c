// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       tuklib_mbstr_width.c
/// \brief      Calculate width of a multibyte string
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#include "tuklib_mbstr.h"
#include <string.h>

#ifdef HAVE_MBRTOWC
#	include <wchar.h>
#endif


extern size_t
tuklib_mbstr_width(const char *str, size_t *bytes)
{
	const size_t len = strlen(str);
	if (bytes != NULL)
		*bytes = len;

	return tuklib_mbstr_width_mem(str, len);
}


extern size_t
tuklib_mbstr_width_mem(const char *str, size_t len)
{
#ifndef HAVE_MBRTOWC
	// In single-byte mode, the width of the string is the same
	// as its length.
	(void)str;
	return len;

#else
	mbstate_t state;
	memset(&state, 0, sizeof(state));

	size_t width = 0;
	size_t i = 0;

	// Convert one multibyte character at a time to wchar_t
	// and get its width using wcwidth().
	while (i < len) {
		wchar_t wc;
		const size_t ret = mbrtowc(&wc, str + i, len - i, &state);
		if (ret < 1 || ret > len - i)
			return (size_t)-1;

		i += ret;

#ifdef HAVE_WCWIDTH
		const int wc_width = wcwidth(wc);
		if (wc_width < 0)
			return (size_t)-1;

		width += (size_t)wc_width;
#else
		// Without wcwidth() (like in a native Windows build),
		// assume that one multibyte char == one column. With
		// UTF-8, this is less bad than one byte == one column.
		// This way quite a few languages will be handled correctly
		// in practice; CJK chars will be very wrong though.
		++width;
#endif
	}

	// It's good to check that the string ended in the initial state.
	// However, in practice this is redundant:
	//
	//   - No one will use this code with character sets that have
	//     locking shift states.
	//
	//   - We already checked that mbrtowc() didn't return (size_t)-2
	//     which would indicate a partial multibyte character.
	if (!mbsinit(&state))
		return (size_t)-1;

	return width;
#endif
}
