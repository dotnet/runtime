//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <assert.h>
#include <string.h>

#include "locale.hpp"

#include "unicode/decimfmt.h"
#include "unicode/numfmt.h"
#include "unicode/ulocdata.h"

// invariant character definitions used by ICU
#define chCurrencySign                ((UChar)0x00A4) // international currency
#define chSpace                       ((UChar)0x0020) // space 
#define chSpace2                      ((UChar)0x00A0) // space
#define chPatternDigit                ((UChar)0x0023) // '#'
#define chPatternSeparator            ((UChar)0x003B) // ';'
#define chPatternMinus                ((UChar)0x002D) // '-'
#define chPercent                     ((UChar)0x0025) // '%'
#define chOpenParen                   ((UChar)0x0028) // '(' 
#define chCloseParen                  ((UChar)0x0029) // ')'

#define ARRAY_LENGTH(array) (sizeof(array)/sizeof(array[0]))

// Enum that corresponds to managed enum CultureData.LocaleNumberData.
// The numeric values of the enum members match their Win32 counterparts. 
enum LocaleNumberData : int32_t
{
	LanguageId = 0x00000001,
	MeasurementSystem = 0x0000000D,
	FractionalDigitsCount = 0x00000011,
	NegativeNumberFormat = 0x00001010,
	MonetaryFractionalDigitsCount = 0x00000019,
	PositiveMonetaryNumberFormat = 0x0000001B,
	NegativeMonetaryNumberFormat = 0x0000001C,
	CalendarType = 0x00001009,
	FirstWeekOfYear = 0x0000100D,
	ReadingLayout = 0x00000070,
	NegativePercentFormat = 0x00000074,
	PositivePercentFormat = 0x00000075,
	Digit = 0x00000010,
	Monetary = 0x00000018
};

/*
Function:
NormalizePattern

Returns a numeric string pattern in a format that we can match against the appropriate managed pattern.
*/
void NormalizePattern(const UnicodeString *srcPattern, UnicodeString *destPattern, bool isNegative)
{
	// A srcPattern example: "#,##0.00 C;(#,##0.00 C)" but where C is the international currency symbol (chCurrencySign)
	// The positive pattern comes first, then an optional negative pattern separated by a semicolon
	// A destPattern example: "(C n)" where C represents the currency symbol, and n is the number
	destPattern->remove();

	int iStart = 0;
	int iEnd = srcPattern->length() - 1;
	int32_t iNegativePatternStart = srcPattern->indexOf(chPatternSeparator);
	if (iNegativePatternStart >= 0)
	{
		if (isNegative)
		{
			iStart = iNegativePatternStart + 1;
		}
		else
		{
			iEnd = iNegativePatternStart - 1;
		}
	}

	bool minusAdded = false;
	bool digitAdded = false;
	bool currencyAdded = false;
	bool spaceAdded = false;

	for (int i = iStart; i <= iEnd; i++)
	{
		UChar ch = srcPattern->char32At(i);
		switch (ch)
		{
		case chPatternDigit:
			if (!digitAdded)
			{
				digitAdded = true;
				destPattern->append('n');
			}
			break;

		case chCurrencySign:
			if (!currencyAdded)
			{
				currencyAdded = true;
				destPattern->append('C');
			}
			break;

		case chSpace:
		case chSpace2:
			if (!spaceAdded)
			{
				spaceAdded = true;
				destPattern->append(chSpace);
			}
			else
			{
				assert(false);
			}
			break;

		case chPatternMinus:
		case chOpenParen:
		case chCloseParen:
			minusAdded = true;
			destPattern->append(ch);
			break;

		case chPercent:
			destPattern->append(ch);
			break;
		}
	}

	// if there is no negative subpattern, the convention is to prefix the minus sign
	if (isNegative && !minusAdded)
	{
		destPattern->insert(0, chPatternMinus);
	}
}

/*
Function:
GetPattern

Determines the pattern from the decimalFormat and returns the matching pattern's index from patterns[].
Returns index -1 if no pattern is found.
*/
int GetPattern(DecimalFormat *decimalFormat, const char* patterns[], int patternsCount, bool isNegative)
{
	const int INVALID_FORMAT = -1;
	const int MAX_DOTNET_NUMERIC_PATTERN_LENGTH = 6; // example: "(C n)" plus terminator
	char charPattern[MAX_DOTNET_NUMERIC_PATTERN_LENGTH] = { 0 };

	UnicodeString icuPattern;
	decimalFormat->toPattern(icuPattern);

	UnicodeString normalizedPattern;
	NormalizePattern(&icuPattern, &normalizedPattern, isNegative);

	assert(normalizedPattern.length() > 0);
	assert(normalizedPattern.length() < MAX_DOTNET_NUMERIC_PATTERN_LENGTH);
	if (normalizedPattern.length() == 0 || normalizedPattern.length() >= MAX_DOTNET_NUMERIC_PATTERN_LENGTH)
	{
		return INVALID_FORMAT;
	}

	u_UCharsToChars(normalizedPattern.getTerminatedBuffer(), charPattern, normalizedPattern.length() + 1);

	for (int i = 0; i < patternsCount; i++)
	{
		if (strcmp(charPattern, patterns[i]) == 0)
		{
			return i;
		}
	};

	assert(false); // should have found a valid pattern
	return INVALID_FORMAT;
}

/*
Function:
GetCurrencyNegativePattern

Implementation of NumberFormatInfo.CurrencyNegativePattern.
Returns the pattern index.
*/
int GetCurrencyNegativePattern(const Locale &locale)
{
	const int DEFAULT_VALUE = 0;
	static const char* Patterns[] = { "(Cn)", "-Cn", "C-n", "Cn-", "(nC)", "-nC", "n-C", "nC-", "-n C", "-C n", "n C-", "C n-", "C -n", "n- C", "(C n)", "(n C)" };
	UErrorCode status = U_ZERO_ERROR;

	LocalPointer<NumberFormat> format(NumberFormat::createInstance(locale, UNUM_CURRENCY, status));
	if (U_FAILURE(status))
	{
		assert(false);
		return DEFAULT_VALUE;
	}

	int value = GetPattern(static_cast<DecimalFormat*>(format.getAlias()), Patterns, ARRAY_LENGTH(Patterns), true);
	return (value >= 0) ? value : DEFAULT_VALUE;
}

/*
Function:
GetCurrencyPositivePattern

Implementation of NumberFormatInfo.CurrencyPositivePattern.
Returns the pattern index.
*/
int GetCurrencyPositivePattern(const Locale &locale)
{
	const int DEFAULT_VALUE = 0;
	static const char* Patterns[] = { "Cn", "nC", "C n", "n C" };
	UErrorCode status = U_ZERO_ERROR;

	LocalPointer<NumberFormat> format(NumberFormat::createInstance(locale, UNUM_CURRENCY, status));
	if (U_FAILURE(status))
	{
		assert(false);
		return DEFAULT_VALUE;
	}

	int value = GetPattern(static_cast<DecimalFormat*>(format.getAlias()), Patterns, ARRAY_LENGTH(Patterns), false);
	return (value >= 0) ? value : DEFAULT_VALUE;
}

/*
Function:
GetNumberNegativePattern

Implementation of NumberFormatInfo.NumberNegativePattern.
Returns the pattern index.
*/
int GetNumberNegativePattern(const Locale &locale)
{
	const int DEFAULT_VALUE = 1;
	static const char* Patterns[] = { "(n)", "-n", "- n", "n-", "n -" };
	UErrorCode status = U_ZERO_ERROR;

	LocalPointer<NumberFormat> format(NumberFormat::createInstance(locale, UNUM_DECIMAL, status));
	if (U_FAILURE(status))
	{
		assert(false);
		return DEFAULT_VALUE;
	}

	int value = GetPattern(static_cast<DecimalFormat*>(format.getAlias()), Patterns, ARRAY_LENGTH(Patterns), true);
	return (value >= 0) ? value : DEFAULT_VALUE;
}

/*
Function:
GetPercentNegativePattern

Implementation of NumberFormatInfo.PercentNegativePattern.
Returns the pattern index.
*/
int GetPercentNegativePattern(const Locale &locale)
{
	const int DEFAULT_VALUE = 0;
	static const char* Patterns[] = { "-n %", "-n%", "-%n", "%-n", "%n-", "n-%", "n%-", "-% n", "n %-", "% n-", "% -n", "n- %" };
	UErrorCode status = U_ZERO_ERROR;

	LocalPointer<NumberFormat> format(NumberFormat::createInstance(locale, UNUM_PERCENT, status));
	if (U_FAILURE(status))
	{
		assert(false);
		return DEFAULT_VALUE;
	}

	int value = GetPattern(static_cast<DecimalFormat*>(format.getAlias()), Patterns, ARRAY_LENGTH(Patterns), true);
	return (value >= 0) ? value : DEFAULT_VALUE;
}

/*
Function:
GetPercentPositivePattern

Implementation of NumberFormatInfo.PercentPositivePattern.
Returns the pattern index.
*/
int GetPercentPositivePattern(const Locale &locale)
{
	const int DEFAULT_VALUE = 0;
	static const char* Patterns[] = { "n %", "n%", "%n", "% n" };
	UErrorCode status = U_ZERO_ERROR;

	LocalPointer<NumberFormat> format(NumberFormat::createInstance(locale, UNUM_PERCENT, status));
	if (U_FAILURE(status))
	{
		assert(false);
		return DEFAULT_VALUE;
	}

	int value = GetPattern(static_cast<DecimalFormat*>(format.getAlias()), Patterns, ARRAY_LENGTH(Patterns), false);
	return (value >= 0) ? value : DEFAULT_VALUE;
}

/*
Function:
GetMeasurementSystem

Obtains the measurement system for the local, determining if US or metric.
Returns 1 for US, 0 otherwise.
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

Obtains integer locale information
Returns 1 for success, 0 otherwise
*/
extern "C" int32_t GetLocaleInfoInt(const UChar* localeName, LocaleNumberData localeNumberData, int32_t* value)
{
	Locale locale = GetLocale(localeName);
	if (locale.isBogus())
	{
		return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
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
	{
		UNumberFormat* numformat = unum_open(UNUM_DECIMAL, NULL, 0, locale.getName(), NULL, &status);
		if (U_SUCCESS(status))
		{
			*value = unum_getAttribute(numformat, UNUM_MAX_FRACTION_DIGITS);
			unum_close(numformat);
		}
		break;
	}
	case NegativeNumberFormat:
		*value = GetNumberNegativePattern(locale);
		break;
	case MonetaryFractionalDigitsCount:
	{
		UNumberFormat* numformat = unum_open(UNUM_CURRENCY, NULL, 0, locale.getName(), NULL, &status);
		if (U_SUCCESS(status))
		{
			*value = unum_getAttribute(numformat, UNUM_MAX_FRACTION_DIGITS);
			unum_close(numformat);
		}
		break;
	}
	case PositiveMonetaryNumberFormat:
		*value = GetCurrencyPositivePattern(locale);
		break;
	case NegativeMonetaryNumberFormat:
		*value = GetCurrencyNegativePattern(locale);
		break;
	case CalendarType:
		*value = 1; //TODO: implement
		break;
	case FirstWeekOfYear:
		*value = 0; //TODO: implement
		break;
	case ReadingLayout:
		*value = 0; //todo: implement
		break;
	case NegativePercentFormat:
		*value = GetPercentNegativePattern(locale);
		break;
	case PositivePercentFormat:
		*value = GetPercentPositivePattern(locale);
		break;
	default:
		status = U_UNSUPPORTED_ERROR;
		assert(false);
		break;
	}

	assert(status != U_BUFFER_OVERFLOW_ERROR);

	return UErrorCodeToBool(status);
}
