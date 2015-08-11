//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <assert.h>
#include <stdint.h>
#include <string.h>

#include "locale.hpp"

#include "unicode/dcfmtsym.h" //decimal
#include "unicode/dtfmtsym.h" //date
#include "unicode/localpointer.h"
#include "unicode/ulocdata.h"

inline int32_t UErrorCodeToBool(UErrorCode code) { return U_SUCCESS(code) ? 1 : 0; }

Locale GetLocale(const UChar* localeName, bool canonize)
{
	char localeNameTemp[ULOC_FULLNAME_CAPACITY];

	if (localeName != NULL)
	{
		int32_t len = u_strlen(localeName);
		u_UCharsToChars(localeName, localeNameTemp, len + 1);
	}

	Locale loc;
	if (canonize)
	{
		loc = Locale::createCanonical(localeName == NULL ? NULL : localeNameTemp);
	}
	else
	{
		loc = Locale::createFromName(localeName == NULL ? NULL : localeNameTemp);
	}

	return loc;
}

UErrorCode u_charsToUChars_safe(const char *str, UChar* value, int32_t valueLength)
{
	int len = strlen(str);
	if (len >= valueLength)
	{
		return U_BUFFER_OVERFLOW_ERROR;
	}
	u_charsToUChars(str, value, len + 1);
	return U_ZERO_ERROR;
}

extern "C" int32_t GetLocaleName(const UChar* localeName, UChar* value, int32_t valueLength)
{
	Locale locale = GetLocale(localeName, true);

	if (locale.isBogus())
	{
		// localeName not properly formatted
		return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
	}

	if (strlen(locale.getISO3Language()) == 0)
	{
		// unknown language; language is required (script and country optional)
		return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
	}

	UErrorCode status = u_charsToUChars_safe(locale.getName(), value, valueLength);
	if (U_SUCCESS(status))
	{
		// replace underscores with hyphens to interop with existing .NET code
		for (UChar* ch = value; *ch != (UChar)'\0'; ch++)
		{
			if (*ch == (UChar)'_')
			{
				*ch = (UChar)'-';
			}
		}
	}

	assert(status != U_BUFFER_OVERFLOW_ERROR);

	return UErrorCodeToBool(status);
}
