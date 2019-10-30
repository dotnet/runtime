// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_icushim.h"

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

Converts a managed localeName into something ICU understands and can use as a localeName.
*/
int32_t GetLocale(const UChar* localeName,
                  char* localeNameResult,
                  int32_t localeNameResultLength,
                  UBool canonicalize,
                  UErrorCode* err);

/*
Function:
u_charsToUChars_safe

Copies the given null terminated char* to UChar with error checking. Replacement for ICU u_charsToUChars
*/
void u_charsToUChars_safe(const char* str, UChar* value, int32_t valueLength, UErrorCode* err);

/*
Function:
FixupLocaleName

Replace underscores with hyphens to interop with existing .NET code.
Returns the length of the string.
*/
int32_t FixupLocaleName(UChar* value, int32_t valueLength);

/*
Function:
DetectDefaultLocaleName

Detect the default locale for the machine, defaulting to Invaraint if
we can't compute one (different from uloc_getDefault()) would do.
*/
const char* DetectDefaultLocaleName();

DLLEXPORT int32_t GlobalizationNative_GetLocales(UChar *value, int32_t valueLength);

DLLEXPORT int32_t GlobalizationNative_GetLocaleName(const UChar* localeName, UChar* value, int32_t valueLength);

DLLEXPORT int32_t GlobalizationNative_GetDefaultLocaleName(UChar* value, int32_t valueLength);
