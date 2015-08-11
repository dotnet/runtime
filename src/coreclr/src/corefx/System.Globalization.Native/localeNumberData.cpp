//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <assert.h>

#include "locale.hpp"

#include "unicode/ulocdata.h"

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

	return UErrorCodeToBool(status);
}
