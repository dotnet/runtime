// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       tuklib_mbstr_nonprint.c
/// \brief      Find and replace non-printable characters with question marks
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#include "tuklib_mbstr_nonprint.h"
#include <stdlib.h>
#include <string.h>
#include <errno.h>

#ifdef HAVE_MBRTOWC
#	include <wchar.h>
#	include <wctype.h>
#else
#	include <ctype.h>
#endif


static bool
is_next_printable(const char *str, size_t len, size_t *next_len)
{
#ifdef HAVE_MBRTOWC
	// This assumes that character sets with locking shift states aren't
	// used, and thus mbsinit() is never needed.
	mbstate_t ps;
	memset(&ps, 0, sizeof(ps));

	wchar_t wc;
	*next_len = mbrtowc(&wc, str, len, &ps);

	if (*next_len == (size_t)-2) {
		// Incomplete multibyte sequence: Treat the whole sequence
		// as a single non-printable multibyte character that ends
		// the string.
		*next_len = len;
		return false;
	}

	// Check more broadly than just ret == (size_t)-1 to be safe
	// in case mbrtowc() returns something weird. This check
	// covers (size_t)-1 (that is, SIZE_MAX) too because len is from
	// strlen() and the terminating '\0' isn't part of the length.
	if (*next_len < 1 || *next_len > len) {
		// Invalid multibyte sequence: Treat the first byte as
		// a non-printable single-byte character. Decoding will
		// be restarted from the next byte on the next call to
		// this function.
		*next_len = 1;
		return false;
	}

#	if defined(_WIN32) && !defined(__CYGWIN__)
	// On Windows, wchar_t stores UTF-16 code units, thus characters
	// outside the Basic Multilingual Plane (BMP) don't fit into
	// a single wchar_t. In an UTF-8 locale, UCRT's mbrtowc() returns
	// successfully when the input is a non-BMP character but the
	// output is the replacement character U+FFFD.
	//
	// iswprint() returns 0 for U+FFFD on Windows for some reason. Treat
	// U+FFFD as printable and thus also all non-BMP chars as printable.
	if (wc == 0xFFFD)
		return true;
#	endif

	return iswprint((wint_t)wc) != 0;
#else
	(void)len;
	*next_len = 1;
	return isprint((unsigned char)str[0]) != 0;
#endif
}


static bool
has_nonprint(const char *str, size_t len)
{
	for (size_t i = 0; i < len; ) {
		size_t next_len;
		if (!is_next_printable(str + i, len - i, &next_len))
			return true;

		i += next_len;
	}

	return false;
}


extern bool
tuklib_has_nonprint(const char *str)
{
	const int saved_errno = errno;
	const bool ret = has_nonprint(str, strlen(str));
	errno = saved_errno;
	return ret;
}


extern const char *
tuklib_mask_nonprint_r(const char *str, char **mem)
{
	const int saved_errno = errno;

	// Free the old string, if any.
	free(*mem);
	*mem = NULL;

	// If the whole input string contains only printable characters,
	// return the input string.
	const size_t len = strlen(str);
	if (!has_nonprint(str, len)) {
		errno = saved_errno;
		return str;
	}

	// Allocate memory for the masked string. Since we use the single-byte
	// character '?' to mask non-printable characters, it's possible that
	// a few bytes less memory would be needed in reality if multibyte
	// characters are masked.
	//
	// If allocation fails, return "???" because it should be safer than
	// returning the unmasked string.
	*mem = malloc(len + 1);
	if (*mem == NULL) {
		errno = saved_errno;
		return "???";
	}

	// Replace all non-printable characters with '?'.
	char *dest = *mem;

	for (size_t i = 0; i < len; ) {
		size_t next_len;
		if (is_next_printable(str + i, len - i, &next_len)) {
			memcpy(dest, str + i, next_len);
			dest += next_len;
		} else {
			*dest++ = '?';
		}

		i += next_len;
	}

	*dest = '\0';

	errno = saved_errno;
	return *mem;
}


extern const char *
tuklib_mask_nonprint(const char *str)
{
	static char *mem = NULL;
	return tuklib_mask_nonprint_r(str, &mem);
}
