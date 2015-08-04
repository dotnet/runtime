//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <assert.h>
#include <stdint.h>
#include <string.h>

#include "localeInformationConstants.hpp"

#include "unicode/dcfmtsym.h" //decimal
#include "unicode/dtfmtsym.h" //date
#include "unicode/localpointer.h"
#include "unicode/ulocdata.h"

/*
Function:
GetLocale

Returns a locale given the locale name
*/
Locale GetLocale(const UChar* localeName, bool canonize = false)
{
	char localeNameTemp[ULOC_FULLNAME_CAPACITY];

	if (localeName != NULL)
	{
		int32_t len = u_strlen(localeName);
		u_UCharsToChars(localeName, localeNameTemp, len);
		localeNameTemp[len] = (char)0;
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

/*
Function:
GopyCharToUChar

Copies the given null terminated char* to UChar with error checking
Replacement for u_charsToUChars
*/
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

/*
PAL Function:
getCanonicalLocaleName

Obtains a canonical locale given the locale name
*/
extern "C" bool GetLocaleName(const UChar* localeName, UChar* value, int32_t valueLength)
{
	Locale locale = GetLocale(localeName, true);

	if (locale.isBogus())
	{
		return false;
	}

	if (strlen(locale.getISO3Language()) == 0)
	{
		// unknown language; language is required (script and country optional)
		return false;
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

	return U_SUCCESS(status);
}

/*
Function:
GetMeasurementSystem

Obtains the measurement system for the local, determining if US or metric
*/
UErrorCode GetMeasurementSystem(const char *localeId, int32_t *value)
{
	UErrorCode status = U_ZERO_ERROR;

	UMeasurementSystem measurementSystem = ulocdata_getMeasurementSystem(localeId, &status);
	if (U_SUCCESS(status))
	{
		*value = (measurementSystem == UMeasurementSystem::UMS_US) ? 1 : 0;
	}

	return status;
}

/*
PAL Function:
GetLocaleInfoInt

Returns integer locale information
*/
extern "C" bool GetLocaleInfoInt(const UChar* localeName, LocaleNumberData localeNumberData, int32_t* value)
{
	Locale locale = GetLocale(localeName);
	if (locale.isBogus())
	{
		return false;
	}

	UErrorCode status = U_ZERO_ERROR;
	switch (localeNumberData)
	{
		case LanguageId:
			*value = locale.getLCID();
			break;
		case MeasurementSystem:
			status = GetMeasurementSystem(locale.getName(), value);
			break;
		case FractionalDigitsCount:
			*value = 2; //TODO: implement
			break;
		case NegativeNumberFormat:
			*value = 1; //TODO: implement
			break;
		case MonetaryFractionalDigitsCount:
			*value = 2; //TODO: implement
			break;
		case PositiveMonetaryNumberFormat:
			*value = 0; //TODO: implement
			break;
		case NegativeMonetaryNumberFormat:
			*value = 0; //TODO: implement
			break;
		case CalendarType:
			*value = 1; //TODO: implement
			break;
		case FirstWeekOfYear:
			*value = 0; //TODO: implement
			break;
		case ReadingLayout:
			*value = 0; //TODO: implement
			break;
		case NegativePercentFormat:
			*value = 0; //TODO: implement
			break;
		case PositivePercentFormat:
			*value = 0; //TODO: implement
			break;
		default:
			status = U_UNSUPPORTED_ERROR;
			assert(false);
			break;
	}

	assert(status != U_BUFFER_OVERFLOW_ERROR);

	return U_SUCCESS(status);
}

/*
Function:
GetLocaleInfoDecimalFormatSymbol

Obtains the value of a DecimalFormatSymbols
*/
UErrorCode GetLocaleInfoDecimalFormatSymbol(const Locale &locale, DecimalFormatSymbols::ENumberFormatSymbol symbol, UChar* value, int32_t valueLength)
{
	UErrorCode status = U_ZERO_ERROR;
	LocalPointer<DecimalFormatSymbols> decimalsymbols(new DecimalFormatSymbols(locale, status));
	if (decimalsymbols == NULL)
	{
		status = U_MEMORY_ALLOCATION_ERROR;
	}

	if (U_FAILURE(status))
	{
		return status;
	}

	UnicodeString s = decimalsymbols->getSymbol(symbol);

	s.extract(value, valueLength, status);
	return status;
}

/*
Function:
GetDigitSymbol

Obtains the value of a Digit DecimalFormatSymbols
*/
UErrorCode GetDigitSymbol(const Locale &locale, UErrorCode previousStatus, DecimalFormatSymbols::ENumberFormatSymbol symbol, int digit, UChar* value, int32_t valueLength)
{
	if (U_FAILURE(previousStatus))
	{
		return previousStatus;
	}

	return GetLocaleInfoDecimalFormatSymbol(locale, symbol, value + digit, valueLength - digit);
}

/*
Function:
GetLocaleInfoAmPm

Obtains the value of a DateFormatSymbols Am or Pm string
*/
UErrorCode GetLocaleInfoAmPm(const Locale &locale, bool am, UChar* value, int32_t valueLength)
{
	UErrorCode status = U_ZERO_ERROR;
	LocalPointer<DateFormatSymbols> dateFormatSymbols(new DateFormatSymbols(locale, status));
	if (dateFormatSymbols == NULL)
	{
		status = U_MEMORY_ALLOCATION_ERROR;
	}

	if (U_FAILURE(status))
	{
		return status;
	}

	int32_t count = 0;
	const UnicodeString *tempStr = dateFormatSymbols->getAmPmStrings(count);
	int offset = am ? 0 : 1;
	if (offset >= count)
	{
		return U_INTERNAL_PROGRAM_ERROR;
	}

	tempStr[offset].extract(value, valueLength, status);
	return status;
}

/*
PAL Function:
GetLocaleInfoString

Obtains string locale information
*/
extern "C" bool GetLocaleInfoString(const UChar* localeName, LocaleStringData localeStringData, UChar* value, int32_t valueLength)
{
	Locale locale = GetLocale(localeName);
	if (locale.isBogus())
	{
		return U_ILLEGAL_ARGUMENT_ERROR;
	}

	UnicodeString str;
	UErrorCode status = U_ZERO_ERROR;
	switch (localeStringData)
	{
		case LocalizedDisplayName:
			locale.getDisplayName(str);
			str.extract(value, valueLength, status);
			break;
		case EnglishDisplayName:
			locale.getDisplayName(Locale::getEnglish(), str);
			str.extract(value, valueLength, status);
			break;
		case NativeDisplayName:
			locale.getDisplayName(locale, str);
			str.extract(value, valueLength, status);
			break;
		case LocalizedLanguageName:
			locale.getDisplayLanguage(str);
			str.extract(value, valueLength, status);
			break;
		case EnglishLanguageName:
			locale.getDisplayLanguage(Locale::getEnglish(), str);
			str.extract(value, valueLength, status);
			break;
		case NativeLanguageName:
			locale.getDisplayLanguage(locale, str);
			str.extract(value, valueLength, status);
			break;
		case EnglishCountryName:
			locale.getDisplayCountry(Locale::getEnglish(), str);
			str.extract(value, valueLength, status);
			break;
		case NativeCountryName:
			locale.getDisplayCountry(locale, str);
			str.extract(value, valueLength, status);
			break;
		case ListSeparator:
			// fall through
		case ThousandSeparator:
			status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kGroupingSeparatorSymbol, value, valueLength);
			break;
		case DecimalSeparator:
			status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kDecimalSeparatorSymbol, value, valueLength);
			break;
		case Digits:
			status = GetDigitSymbol(locale, status, DecimalFormatSymbols::kZeroDigitSymbol, 0, value, valueLength);
			// symbols kOneDigitSymbol to kNineDigitSymbol are contiguous
			for (int32_t symbol = DecimalFormatSymbols::kOneDigitSymbol; symbol <= DecimalFormatSymbols::kNineDigitSymbol; symbol++)
			{
				int charIndex = symbol - DecimalFormatSymbols::kOneDigitSymbol + 1;
				status = GetDigitSymbol(locale, status, (DecimalFormatSymbols::ENumberFormatSymbol)symbol, charIndex, value, valueLength);
			}
			break;
		case MonetarySymbol:
			status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kCurrencySymbol, value, valueLength);
			break;
		case Iso4217MonetarySymbol:
			status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kIntlCurrencySymbol, value, valueLength);
			break;
		case MonetaryDecimalSeparator:
			status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kMonetarySeparatorSymbol, value, valueLength);
			break;
		case MonetaryThousandSeparator:
			status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kMonetaryGroupingSeparatorSymbol, value, valueLength);
			break;
		case AMDesignator:
			status = GetLocaleInfoAmPm(locale, true, value, valueLength);
			break;
		case PMDesignator:
			status = GetLocaleInfoAmPm(locale, false, value, valueLength);
			break;
		case PositiveSign:
			status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kPlusSignSymbol, value, valueLength);
			break;
		case NegativeSign:
			status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kMinusSignSymbol, value, valueLength);
			break;
		case Iso639LanguageName:
			status = u_charsToUChars_safe(locale.getLanguage(), value, valueLength);
			break;
		case Iso3166CountryName:
			// coreclr expects 2-character version, not 3 (3 would correspond to LOCALE_SISO3166CTRYNAME2 and locale.getISO3Country)
			status = u_charsToUChars_safe(locale.getCountry(), value, valueLength);
			break;
		case NaNSymbol:
			status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kNaNSymbol, value, valueLength);
			break;
		case PositiveInfinitySymbol:
			status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kInfinitySymbol, value, valueLength);
			break;
		case PercentSymbol:
			status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kPercentSymbol, value, valueLength);
			break;
		case PerMilleSymbol:
			status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kPerMillSymbol, value, valueLength);
			break;
		default:
			status = U_UNSUPPORTED_ERROR;
			assert(false);
			break;
	};

	assert(status != U_BUFFER_OVERFLOW_ERROR);

	return U_SUCCESS(status);
}
