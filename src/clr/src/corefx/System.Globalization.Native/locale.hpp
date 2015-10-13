//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "unicode/locid.h"

/*
PAL Function:
GetLocaleName

Obtains a canonical locale name given a user-specified locale name
Returns 1 for success, 0 otherwise
*/
extern "C" int32_t GetLocaleName(const UChar* localeName, UChar* value, int32_t valueLength);

/*
Function:
UErrorCodeToBool

Convert an ICU UErrorCode to a Bool compatible with Win32
Returns 1 for success, 0 otherwise
*/
int32_t UErrorCodeToBool(UErrorCode code);

/*
Function:
GetLocale

Returns a locale given the locale name
*/
Locale GetLocale(const UChar* localeName, bool canonize = false);

/*
Function:
u_charsToUChars_safe

Copies the given null terminated char* to UChar with error checking. Replacement for ICU u_charsToUChars
*/
UErrorCode u_charsToUChars_safe(const char* str, UChar* value, int32_t valueLength);

/*
Function:
FixupLocaleName

Replace underscores with hyphens to interop with existing .NET code.
Returns the length of the string.
*/
int FixupLocaleName(UChar* value, int32_t valueLength);
