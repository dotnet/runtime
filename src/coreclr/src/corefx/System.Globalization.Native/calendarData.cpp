//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <assert.h>
#include <string.h>

#include "locale.hpp"

#include <unicode/dtfmtsym.h>
#include <unicode/dtptngen.h>
#include <unicode/locdspnm.h>

/*
* These values should be kept in sync with System.Globalization.CalendarId
*/
enum CalendarId : int16_t
{
	UNINITIALIZED_VALUE = 0,
	GREGORIAN = 1,     // Gregorian (localized) calendar
	GREGORIAN_US = 2,     // Gregorian (U.S.) calendar
	JAPAN = 3,     // Japanese Emperor Era calendar
				   /* SSS_WARNINGS_OFF */
	TAIWAN = 4,     // Taiwan Era calendar /* SSS_WARNINGS_ON */ 
	KOREA = 5,     // Korean Tangun Era calendar
	HIJRI = 6,     // Hijri (Arabic Lunar) calendar
	THAI = 7,     // Thai calendar
	HEBREW = 8,     // Hebrew (Lunar) calendar
	GREGORIAN_ME_FRENCH = 9,     // Gregorian Middle East French calendar
	GREGORIAN_ARABIC = 10,     // Gregorian Arabic calendar
	GREGORIAN_XLIT_ENGLISH = 11,     // Gregorian Transliterated English calendar
	GREGORIAN_XLIT_FRENCH = 12,
	// Note that all calendars after this point are MANAGED ONLY for now.
	JULIAN = 13,
	JAPANESELUNISOLAR = 14,
	CHINESELUNISOLAR = 15,
	SAKA = 16,     // reserved to match Office but not implemented in our code
	LUNAR_ETO_CHN = 17,     // reserved to match Office but not implemented in our code
	LUNAR_ETO_KOR = 18,     // reserved to match Office but not implemented in our code
	LUNAR_ETO_ROKUYOU = 19,     // reserved to match Office but not implemented in our code
	KOREANLUNISOLAR = 20,
	TAIWANLUNISOLAR = 21,
	PERSIAN = 22,
	UMALQURA = 23,
	LAST_CALENDAR = 23      // Last calendar ID
};

/*
* These values should be kept in sync with System.Globalization.CalendarDataType
*/
enum CalendarDataType : int32_t
{
	Uninitialized = 0,
	NativeName = 1,
	MonthDay = 2,
	ShortDates = 3,
	LongDates = 4,
	YearMonths = 5,
	DayNames = 6,
	AbbrevDayNames = 7,
	MonthNames = 8,
	AbbrevMonthNames = 9,
	SuperShortDayNames = 10,
	MonthGenitiveNames = 11,
	AbbrevMonthGenitiveNames = 12,
	EraNames = 13,
	AbbrevEraNames = 14,
};

/*
* These values should be kept in sync with System.Globalization.CalendarDataResult
*/
enum CalendarDataResult : int32_t
{
	Success = 0,
	UnknownError = 1,
	InsufficentBuffer = 2,
};

// the function pointer definition for the callback used in EnumCalendarInfo
typedef void(*EnumCalendarInfoCallback)(const UChar*, const void*);

/*
Function:
GetCalendarDataResult

Converts a UErrorCode to a CalendarDataResult.
*/
CalendarDataResult GetCalendarDataResult(UErrorCode err)
{
	if (U_SUCCESS(err))
	{
		return Success;
	}

	if (err == U_BUFFER_OVERFLOW_ERROR)
	{
		return InsufficentBuffer;
	}

	return UnknownError;
}

/*
Function:
GetCalendarName

Gets the associated ICU calendar name for the CalendarId.
*/
const char* GetCalendarName(CalendarId calendarId)
{
	switch (calendarId)
	{
		case JAPAN:
			return "japanese";
		case THAI:
			return "buddhist";
		case HEBREW:
			return "hebrew";
		case CHINESELUNISOLAR:
		case KOREANLUNISOLAR:
		case JAPANESELUNISOLAR:
		case TAIWANLUNISOLAR:
			return "chinese";
		case KOREA:
			return "dangi";
		case PERSIAN:
			return "persian";
		case HIJRI:
			return "islamic";
		case UMALQURA:
			return "islamic-umalqura";
		case TAIWAN:
			return "roc";
		case GREGORIAN:
		case GREGORIAN_US:
		case GREGORIAN_ARABIC:
		case GREGORIAN_ME_FRENCH:
		case GREGORIAN_XLIT_ENGLISH:
		case GREGORIAN_XLIT_FRENCH:
		case JULIAN:
		case LUNAR_ETO_CHN:
		case LUNAR_ETO_KOR:
		case LUNAR_ETO_ROKUYOU:
		case SAKA:
		default:
			return "gregorian";
	}
}

/*
Function:
GetCalendarName

Gets the associated CalendarId for the ICU calendar name.
*/
CalendarId GetCalendarId(const char* calendarName)
{
	if (strcmp(calendarName, "gregorian") == 0)
		//TODO: what about the other gregorian types?
		return GREGORIAN;
	else if (strcmp(calendarName, "japanese") == 0)
		return JAPAN;
	else if (strcmp(calendarName, "buddhist") == 0)
		return THAI;
	else if (strcmp(calendarName, "hebrew") == 0)
		return HEBREW;
	else if (strcmp(calendarName, "chinese") == 0)
		//TODO: what about these other lunisolar types?
		//JAPANESELUNISOLAR:
		//TAIWANLUNISOLAR:
		//KOREANLUNISOLAR:
		return CHINESELUNISOLAR;
	else if (strcmp(calendarName, "dangi") == 0)
		return KOREA;
	else if (strcmp(calendarName, "persian") == 0)
		return PERSIAN;
	else if (strcmp(calendarName, "islamic") == 0)
		return HIJRI;
	else if (strcmp(calendarName, "islamic-umalqura") == 0)
		return UMALQURA;
	else if (strcmp(calendarName, "roc") == 0)
		return TAIWAN;
	else
		return UNINITIALIZED_VALUE;
}

/*
Function:
GetCalendars

Returns the list of CalendarIds that are available for the specified locale.
*/
extern "C" int32_t GetCalendars(const UChar* localeName, CalendarId* calendars, int32_t calendarsCapacity)
{
	Locale locale = GetLocale(localeName);
	if (locale.isBogus())
		return 0;

	UErrorCode err = U_ZERO_ERROR;
	LocalPointer<StringEnumeration> stringEnumerator(Calendar::getKeywordValuesForLocale("calendar", locale, TRUE, err));

	if (stringEnumerator.isNull() || U_FAILURE(err))
		return 0;

	int stringEnumeratorCount = stringEnumerator->count(err);
	if (U_FAILURE(err))
		return 0;

	int calendarsReturned = 0;
	for (int i = 0; i < stringEnumeratorCount && calendarsReturned < calendarsCapacity; i++)
	{
		int32_t calendarNameLength = 0;
		const char* calendarName = stringEnumerator->next(&calendarNameLength, err);
		if (U_SUCCESS(err))
		{
			CalendarId calendarId = GetCalendarId(calendarName);
			if (calendarId != UNINITIALIZED_VALUE)
			{
				calendars[calendarsReturned] = calendarId;
				calendarsReturned++;
			}
		}
	}

	return calendarsReturned;
}

/*
Function:
GetMonthDayPattern

Gets the Month-Day DateTime pattern for the specified locale.
*/
CalendarDataResult GetMonthDayPattern(Locale& locale, UChar* sMonthDay, int32_t stringCapacity)
{
	UErrorCode err = U_ZERO_ERROR;
	LocalPointer<DateTimePatternGenerator> generator(DateTimePatternGenerator::createInstance(locale, err));
	if (U_FAILURE(err))
		return GetCalendarDataResult(err);

	UnicodeString monthDayPattern = generator->getBestPattern(UnicodeString("MMMMd"), err);
	if (U_FAILURE(err))
		return GetCalendarDataResult(err);

	monthDayPattern.extract(sMonthDay, stringCapacity, err);

	return GetCalendarDataResult(err);
}

/*
Function:
GetNativeCalendarName

Gets the native calendar name.
*/
CalendarDataResult GetNativeCalendarName(Locale& locale, CalendarId calendarId, UChar* nativeName, int32_t stringCapacity)
{
	LocalPointer<LocaleDisplayNames> displayNames(LocaleDisplayNames::createInstance(locale));

	UnicodeString calendarName;
	displayNames->keyValueDisplayName("calendar", GetCalendarName(calendarId), calendarName);

	UErrorCode err = U_ZERO_ERROR;
	calendarName.extract(nativeName, stringCapacity, err);

	return GetCalendarDataResult(err);
}

/*
Function:
GetCalendarInfo

Gets a single string of calendar information by filling the result parameter with the requested value.
*/
extern "C" CalendarDataResult GetCalendarInfo(const UChar* localeName, CalendarId calendarId, CalendarDataType dataType, UChar* result, int32_t resultCapacity)
{
	Locale locale = GetLocale(localeName);
	if (locale.isBogus())
		return UnknownError;

	switch (dataType)
	{
		case NativeName:
			return GetNativeCalendarName(locale, calendarId, result, resultCapacity);
		case MonthDay:
			return GetMonthDayPattern(locale, result, resultCapacity);
		default:
			assert(false);
			return UnknownError;
	}
}

/*
Function:
InvokeCallbackForDateTimePattern

Gets the DateTime pattern for the specified skeleton and invokes the callback with the retrieved value.
*/
bool InvokeCallbackForDateTimePattern(Locale& locale, const char* patternSkeleton, EnumCalendarInfoCallback callback, const void* context)
{
	UErrorCode err = U_ZERO_ERROR;
	LocalPointer<DateTimePatternGenerator> generator(DateTimePatternGenerator::createInstance(locale, err));
	if (U_FAILURE(err))
		return false;

	UnicodeString pattern = generator->getBestPattern(UnicodeString(patternSkeleton), err);
	if (U_SUCCESS(err))
	{
		callback(pattern.getTerminatedBuffer(), context);
		return true;
	}

	return false;
}

/*
Function:
EnumCalendarArray

Enumerates an array of strings and invokes the callback for each value.
*/
bool EnumCalendarArray(const UnicodeString* srcArray, int32_t srcArrayCount, EnumCalendarInfoCallback callback, const void* context)
{
	for (int i = 0; i < srcArrayCount; i++)
	{
		UnicodeString src = srcArray[i];
		callback(src.getTerminatedBuffer(), context);
	}

	return true;
}

/*
Function:
EnumWeekdays

Enumerates all the weekday names of the specified context and width, invoking the callback function
for each weekday name.
*/
bool EnumWeekdays(
	Locale& locale,
	CalendarId calendarId,
	DateFormatSymbols::DtContextType dtContext,
	DateFormatSymbols::DtWidthType dtWidth,
	EnumCalendarInfoCallback callback,
	const void* context)
{
	UErrorCode err = U_ZERO_ERROR;
	DateFormatSymbols dateFormatSymbols(locale, GetCalendarName(calendarId), err);
	if (U_FAILURE(err))
		return false;

	int32_t daysCount;
	const UnicodeString* dayNames = dateFormatSymbols.getWeekdays(daysCount, dtContext, dtWidth);

	// ICU returns an empty string for the first/zeroth element in the weekdays array.
	// So skip the first element.
	dayNames++;
	daysCount--;

	return EnumCalendarArray(dayNames, daysCount, callback, context);
}

/*
Function:
EnumMonths

Enumerates all the month names of the specified context and width, invoking the callback function
for each month name.
*/
bool EnumMonths(
	Locale& locale,
	CalendarId calendarId,
	DateFormatSymbols::DtContextType dtContext,
	DateFormatSymbols::DtWidthType dtWidth,
	EnumCalendarInfoCallback callback,
	const void* context)
{
	UErrorCode err = U_ZERO_ERROR;
	DateFormatSymbols dateFormatSymbols(locale, GetCalendarName(calendarId), err);
	if (U_FAILURE(err))
		return false;

	int32_t monthsCount;
	const UnicodeString* monthNames = dateFormatSymbols.getMonths(monthsCount, dtContext, dtWidth);
	return EnumCalendarArray(monthNames, monthsCount, callback, context);
}

/*
Function:
EnumEraNames

Enumerates all the era names of the specified locale and calendar, invoking the callback function
for each era name.
*/
bool EnumEraNames(Locale& locale, CalendarId calendarId, CalendarDataType dataType, EnumCalendarInfoCallback callback, const void* context)
{
	UErrorCode err = U_ZERO_ERROR;
	const char* calendarName = GetCalendarName(calendarId);
	DateFormatSymbols dateFormatSymbols(locale, calendarName, err);
	if (U_FAILURE(err))
		return false;

	int32_t eraNameCount;
	const UnicodeString* eraNames;

	if (dataType == EraNames)
	{
		eraNames = dateFormatSymbols.getEras(eraNameCount);
	}
	else if (dataType == AbbrevEraNames)
	{
		if (strcmp(calendarName, "gregorian") == 0)
		{
			// NOTE: On Windows, the EraName is "A.D." and AbbrevEraName is "AD".
			// But ICU/CLDR only supports "Anno Domini", "AD", and "A".
			// So returning getEras (i.e. "AD") for both EraNames and AbbrevEraNames.
			eraNames = dateFormatSymbols.getEras(eraNameCount);
		}
		else
		{
			eraNames = dateFormatSymbols.getNarrowEras(eraNameCount);
		}
	}
	else
	{
		assert(false);
		return false;
	}

	return EnumCalendarArray(eraNames, eraNameCount, callback, context);
}

/*
Function:
EnumCalendarInfo

Retrieves a collection of calendar string data specified by the locale, calendar, and data type.
Allows for a collection of calendar string data to be retrieved by invoking
the callback for each value in the collection.
The context parameter is passed through to the callback along with each string.
*/
extern "C" int32_t EnumCalendarInfo(
	EnumCalendarInfoCallback callback,
	const UChar* localeName,
	CalendarId calendarId,
	CalendarDataType dataType,
	const void* context)
{
	Locale locale = GetLocale(localeName);
	if (locale.isBogus())
		return false;

	switch (dataType)
	{
		case ShortDates:
			return InvokeCallbackForDateTimePattern(locale, "Mdyyyy", callback, context);
		case LongDates:
			// TODO: need to replace the "EEEE"s with "dddd"s for .net
			// Also, "LLLL"s to "MMMM"s
			// Also, "G"s to "g"s
			return InvokeCallbackForDateTimePattern(locale, "eeeeMMMMddyyyy", callback, context);
		case YearMonths:
			return InvokeCallbackForDateTimePattern(locale, "yyyyMMMM", callback, context);
		case DayNames:
			return EnumWeekdays(locale, calendarId, DateFormatSymbols::STANDALONE, DateFormatSymbols::WIDE, callback, context);
		case AbbrevDayNames:
			return EnumWeekdays(locale, calendarId, DateFormatSymbols::STANDALONE, DateFormatSymbols::ABBREVIATED, callback, context);
		case MonthNames:
			return EnumMonths(locale, calendarId, DateFormatSymbols::STANDALONE, DateFormatSymbols::WIDE, callback, context);
		case AbbrevMonthNames:
			return EnumMonths(locale, calendarId, DateFormatSymbols::STANDALONE, DateFormatSymbols::ABBREVIATED, callback, context);
		case SuperShortDayNames:
			return EnumWeekdays(locale, calendarId, DateFormatSymbols::STANDALONE, DateFormatSymbols::SHORT, callback, context);
		case MonthGenitiveNames:
			return EnumMonths(locale, calendarId, DateFormatSymbols::FORMAT, DateFormatSymbols::WIDE, callback, context);
		case AbbrevMonthGenitiveNames:
			return EnumMonths(locale, calendarId, DateFormatSymbols::FORMAT, DateFormatSymbols::ABBREVIATED, callback, context);
		case EraNames:
		case AbbrevEraNames:
			return EnumEraNames(locale, calendarId, dataType, callback, context);
		default:
			assert(false);
			return false;
	}
}

/*
Function:
GetLatestJapaneseEra

Gets the latest era in the Japanese calendar.
*/
extern "C" int32_t GetLatestJapaneseEra()
{
	UErrorCode err = U_ZERO_ERROR;
	Locale japaneseLocale("ja_JP@calendar=japanese");
	LocalPointer<Calendar> calendar(Calendar::createInstance(japaneseLocale, err));

	if (U_FAILURE(err))
		return 0;

	return calendar->getMaximum(UCAL_ERA); 
}

/*
Function:
GetJapaneseEraInfo

Gets the starting Gregorian date of the specified Japanese Era.
*/
extern "C" int32_t GetJapaneseEraStartDate(
	int32_t era,
	int32_t* startYear,
	int32_t* startMonth,
	int32_t* startDay)
{
	UErrorCode err = U_ZERO_ERROR;
	Locale japaneseLocale("ja_JP@calendar=japanese");
	LocalPointer<Calendar> calendar(Calendar::createInstance(japaneseLocale, err));
	if (U_FAILURE(err))
		return false;

	calendar->set(UCAL_ERA, era);
	calendar->set(UCAL_YEAR, 1);

	// UCAL_EXTENDED_YEAR is the gregorian year for the JapaneseCalendar
	*startYear = calendar->get(UCAL_EXTENDED_YEAR, err);
	if (U_FAILURE(err))
		return false;

	// set the date to Jan 1
	calendar->set(UCAL_MONTH, 0);
	calendar->set(UCAL_DATE, 1);

	int32_t currentEra = calendar->get(UCAL_ERA, err);
	if (U_FAILURE(err))
		return false;

	if (currentEra == era)
	{
		// if Jan 1 is still in the specified Era, then the Era must have started on Jan 1.
		*startMonth = 1;
		*startDay = 1;
		return true;
	}

	for (int i = 0; i < 12; i++)
	{
		// add 1 month at a time until we get into the specified Era
		calendar->add(UCAL_MONTH, 1, err);
		if (U_FAILURE(err))
			return false;

		currentEra = calendar->get(UCAL_ERA, err);
		if (U_FAILURE(err))
			return false;

		if (currentEra == era)
		{
			for (int i = 0; i < 32; i++)
			{
				// now subtract 1 day at a time until we get out of the specified Era
				calendar->add(Calendar::DATE, -1, err);
				if (U_FAILURE(err))
					return false;

				currentEra = calendar->get(UCAL_ERA, err);
				if (U_FAILURE(err))
					return false;

				if (currentEra != era)
				{
					// add back 1 day to get back into the specified Era
					calendar->add(UCAL_DATE, 1, err);
					if (U_FAILURE(err))
						return false;

					*startMonth = calendar->get(UCAL_MONTH, err) + 1;  // ICU Calendar months are 0-based, but .NET is 1-based
					if (U_FAILURE(err))
						return false;

					*startDay = calendar->get(UCAL_DATE, err);
					if (U_FAILURE(err))
						return false;

					return true;
				}
			}
		}
	}

	return false;
}
