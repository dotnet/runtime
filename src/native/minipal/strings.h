// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_STRINGS_H
#define HAVE_MINIPAL_STRINGS_H

#include <minipal/types.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/**
 * Convert a UTF-16 character to uppercase using invariant culture.
 *
 * @param code The UTF-16 character to be converted.
 * @return The uppercase equivalent of the character or the character itself if no conversion is necessary.
 */
CHAR16_T minipal_toupper_invariant(CHAR16_T code);

/**
 * Convert a UTF-16 character to lowercase using invariant culture.
 *
 * @param code The UTF-16 character to be converted.
 * @return The lowercase equivalent of the character or the character itself if no conversion is necessary.
 */
CHAR16_T minipal_tolower_invariant(CHAR16_T code);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* HAVE_MINIPAL_STRINGS_H */
