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

int32_t UErrorCodeToBool(UErrorCode status) 
{ 
	if (U_SUCCESS(status))
	{
		return 1;
	}

	// assert errors that should never occur
	assert(status != U_BUFFER_OVERFLOW_ERROR);
	assert(status != U_INTERNAL_PROGRAM_ERROR);

	// add possible SetLastError support here

	return 0;
}

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

void FixupLocaleName(UChar* value, int32_t valueLength)
{
	for (int i = 0; i < valueLength; i++)
	{
		if (value[i] == (UChar)'\0')
		{
			break;
		}
		else if (value[i] == (UChar)'_')
		{
			value[i] = (UChar)'-';
		}
	}
}


extern "C" int32_t GetLocaleName(const UChar* localeName, UChar* value, int32_t valueLength)
{
	Locale locale = GetLocale(localeName, true);

	if (locale.isBogus())
	{
		// localeName not properly formatted
		return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
	}

	// other validation done on managed side

	UErrorCode status = u_charsToUChars_safe(locale.getName(), value, valueLength);
	if (U_SUCCESS(status))
	{
		FixupLocaleName(value, valueLength);
	}

	return UErrorCodeToBool(status);
}
