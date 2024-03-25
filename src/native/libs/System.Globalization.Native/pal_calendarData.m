// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include "pal_icushim_internal.h"
#include "pal_calendarData.h"
#import <Foundation/Foundation.h>

#if !__has_feature(objc_arc)
#error This file relies on ARC for memory management, but ARC is not enabled.
#endif

#if defined(APPLE_HYBRID_GLOBALIZATION)
/*
Function:
GetCalendarIdentifier

Gets the associated NSCalendarIdentifier for the CalendarId.
*/
static NSString* GetCalendarIdentifier(CalendarId calendarId)
{
    NSString *calendarIdentifier = NSCalendarIdentifierGregorian;
    switch (calendarId)
    {
        case JAPAN:
            calendarIdentifier = NSCalendarIdentifierJapanese;
            break;
        case THAI:
            calendarIdentifier = NSCalendarIdentifierBuddhist;
            break;
        case HEBREW:
            calendarIdentifier = NSCalendarIdentifierHebrew;
            break;
        case PERSIAN:
            calendarIdentifier = NSCalendarIdentifierPersian;
            break;
        case HIJRI:
            calendarIdentifier = NSCalendarIdentifierIslamic;
            break;
        case UMALQURA:
            calendarIdentifier = NSCalendarIdentifierIslamicUmmAlQura;
            break;
        case TAIWAN:
            calendarIdentifier = NSCalendarIdentifierRepublicOfChina;
            break;
        default:
            break;
    }
    return calendarIdentifier;
}

/*
Function:
GetCalendarId

Gets the associated CalendarId for the calendar name.
*/
static CalendarId GetCalendarId(const char* calendarName)
{
    if (strcasecmp(calendarName, GREGORIAN_NAME) == 0)
        return GREGORIAN;
    else if (strcasecmp(calendarName, JAPANESE_NAME) == 0)
        return JAPAN;
    else if (strcasecmp(calendarName, BUDDHIST_NAME) == 0)
        return THAI;
    else if (strcasecmp(calendarName, HEBREW_NAME) == 0)
        return HEBREW;
    else if (strcasecmp(calendarName, PERSIAN_NAME) == 0)
        return PERSIAN;
    else if (strcasecmp(calendarName, ISLAMIC_NAME) == 0)
        return HIJRI;
    else if (strcasecmp(calendarName, ISLAMIC_UMALQURA_NAME) == 0)
        return UMALQURA;
    else if (strcasecmp(calendarName, ROC_NAME) == 0)
        return TAIWAN;
    else
        return UNINITIALIZED_VALUE;
}
/*
Function:
GlobalizationNative_GetCalendarInfoNative

Gets a single string of calendar information for a given locale, calendar, and calendar data type.
with the requested value.
*/
const char* GlobalizationNative_GetCalendarInfoNative(const char* localeName, CalendarId calendarId, CalendarDataType dataType)
{
    @autoreleasepool
    {
        NSString *locName = [NSString stringWithFormat:@"%s", localeName];
        NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];

        if (dataType == CalendarData_MonthDay)
        {
            NSString *formatString = [NSDateFormatter dateFormatFromTemplate:@"MMMMd" options:0 locale:currentLocale];
            return formatString ? strdup([formatString UTF8String]) : NULL;
        }
        else if (dataType == CalendarData_YearMonths)
        {
            NSString *formatString = [NSDateFormatter dateFormatFromTemplate:@"MMMM yyyy" options:0 locale:currentLocale];
            return formatString ? strdup([formatString UTF8String]) : NULL;
        }

        NSString *calendarIdentifier = GetCalendarIdentifier(calendarId);
        NSCalendar *calendar = [[NSCalendar alloc] initWithCalendarIdentifier:calendarIdentifier];

        if (dataType == CalendarData_NativeName)
            return strdup([[currentLocale localizedStringForCalendarIdentifier:calendarIdentifier] UTF8String]);

        NSDateFormatter *dateFormat = [[NSDateFormatter alloc] init];
        dateFormat.locale = currentLocale;
        dateFormat.calendar = calendar;

        NSArray *result;
        switch (dataType)
        {
            case CalendarData_ShortDates:
            {
                [dateFormat setDateStyle:NSDateFormatterShortStyle];
                NSString *shortFormatString = [dateFormat dateFormat];
                [dateFormat setDateStyle:NSDateFormatterMediumStyle];
                NSString *mediumFormatString = [dateFormat dateFormat];
                NSString *yearMonthDayFormat = [NSDateFormatter dateFormatFromTemplate:@"yMd" options:0 locale:currentLocale];
                result = @[shortFormatString, mediumFormatString, yearMonthDayFormat];
                break;
            }
            case CalendarData_LongDates:
            {
                [dateFormat setDateStyle:NSDateFormatterLongStyle];
                NSString *longFormatString = [dateFormat dateFormat];
                [dateFormat setDateStyle:NSDateFormatterFullStyle];
                NSString *fullFormatString = [dateFormat dateFormat];
                result = @[longFormatString, fullFormatString];
                break;
            }
            case CalendarData_DayNames:
                result = [dateFormat standaloneWeekdaySymbols];
                break;
            case CalendarData_AbbrevDayNames:
                result = [dateFormat shortStandaloneWeekdaySymbols];
                break;
            case CalendarData_MonthNames:
                result = [dateFormat standaloneMonthSymbols];
                break;
            case CalendarData_AbbrevMonthNames:
                result = [dateFormat shortStandaloneMonthSymbols];
                break;
            case CalendarData_SuperShortDayNames:
                result = [dateFormat veryShortStandaloneWeekdaySymbols];
                break;
            case CalendarData_MonthGenitiveNames:
                result = [dateFormat monthSymbols];
                break;
            case CalendarData_AbbrevMonthGenitiveNames:
                result = [dateFormat shortMonthSymbols];
                break;
            case CalendarData_EraNames:
            case CalendarData_AbbrevEraNames:
                result = [dateFormat eraSymbols];
                break;
            default:
                assert(false);
                return NULL;
        }

        NSString *arrayToString = [[result valueForKey:@"description"] componentsJoinedByString:@"||"];
        return arrayToString ? strdup([arrayToString UTF8String]) : NULL;
    }
}

/*
Function:
GetLatestJapaneseEraNative

Gets the latest era in the Japanese calendar.
*/
int32_t GlobalizationNative_GetLatestJapaneseEraNative(void)
{
    @autoreleasepool
    {
        // Create an NSCalendar with the Japanese calendar identifier
        NSCalendar *japaneseCalendar = [[NSCalendar alloc] initWithCalendarIdentifier:NSCalendarIdentifierJapanese];
        // Get the latest era
        NSDateComponents *latestEraComponents = [japaneseCalendar components:NSCalendarUnitEra fromDate:[NSDate date]];
        // Extract the era component
        NSInteger latestEra = [latestEraComponents era];
        return (int32_t)latestEra;
    }
}

/*
Function:
GetJapaneseEraStartDateNative

Gets the starting Gregorian date of the specified Japanese Era.
*/
int32_t GlobalizationNative_GetJapaneseEraStartDateNative(int32_t era, int32_t* startYear, int32_t* startMonth, int32_t* startDay)
{
    @autoreleasepool
    {
        NSCalendar *japaneseCalendar = [[NSCalendar alloc] initWithCalendarIdentifier:NSCalendarIdentifierJapanese];
        NSDateComponents *startDateComponents = [[NSDateComponents alloc] init];
        startDateComponents.era = era;
        // set the date to Jan 1, 1
        startDateComponents.month = 1;
        startDateComponents.day = 1;
        startDateComponents.year = 1;
        NSDate *date = [japaneseCalendar dateFromComponents:startDateComponents];
        int32_t currentEra;

        for (int month = 0; month <= 12; month++)
        {
            NSDateComponents *eraComponents = [japaneseCalendar components:NSCalendarUnitEra fromDate:date];
            currentEra = [eraComponents era];
            if (currentEra == era)
            {
                for (int day = 0; day < 31; day++)
                {
                    // subtract 1 day at a time until we get out of the specified Era
                    startDateComponents.day = startDateComponents.day - 1;
                    date = [japaneseCalendar dateFromComponents:startDateComponents];
                    eraComponents = [japaneseCalendar components:NSCalendarUnitEra fromDate:date];
                    currentEra = [eraComponents era];
                    if (currentEra != era)
                    {
                        // add back 1 day to get back into the specified Era
                        startDateComponents.day = startDateComponents.day + 1;
                        date = [japaneseCalendar dateFromComponents:startDateComponents];
                        NSCalendar *gregorianCalendar = [[NSCalendar alloc] initWithCalendarIdentifier:NSCalendarIdentifierGregorian];
                        NSDateComponents *components = [gregorianCalendar components:NSCalendarUnitDay | NSCalendarUnitMonth | NSCalendarUnitYear fromDate:date];
                        *startYear = [components year];
                        *startMonth = [components month];
                        *startDay = [components day];
                        return 1;
                    }
                }
            }
            // add 1 month at a time until we get into the specified Era
            startDateComponents.month = startDateComponents.month + 1;
            date = [japaneseCalendar dateFromComponents:startDateComponents];
            eraComponents = [japaneseCalendar components:NSCalendarUnitEra fromDate:date];
            currentEra = [eraComponents era];
        }

        return 0;
    }
}

/*
Function:
GetCalendarsNative

Returns the list of CalendarIds that are available for the specified locale.
*/
int32_t GlobalizationNative_GetCalendarsNative(const char* localeName, CalendarId* calendars, int32_t calendarsCapacity)
{
    @autoreleasepool
    {
        NSArray *calendarIdentifiers = @[
            NSCalendarIdentifierGregorian,
            NSCalendarIdentifierBuddhist,
            NSCalendarIdentifierHebrew,
            NSCalendarIdentifierIslamicUmmAlQura,
            NSCalendarIdentifierIslamic,
            NSCalendarIdentifierJapanese,
            NSCalendarIdentifierPersian,
            NSCalendarIdentifierRepublicOfChina,
        ];

        NSString *locName = [NSString stringWithFormat:@"%s", localeName];
        NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
        NSString *defaultCalendarIdentifier = [currentLocale calendarIdentifier];
        int32_t calendarCount = MIN(calendarIdentifiers.count, calendarsCapacity);
        int32_t calendarIndex = 0;
        CalendarId defaultCalendarId = GetCalendarId([defaultCalendarIdentifier UTF8String]);
        // If the default calendar is not supported, return the Gregorian calendar as the default.
        calendars[calendarIndex++] = defaultCalendarId == UNINITIALIZED_VALUE ? GREGORIAN : defaultCalendarId;
        for (int i = 0; i < calendarCount; i++)
        {
            CalendarId calendarId = GetCalendarId([calendarIdentifiers[i] UTF8String]);
            if (calendarId == UNINITIALIZED_VALUE || calendarId == defaultCalendarId)
                continue;
            calendars[calendarIndex++] = calendarId;
        }
        return calendarCount;
    }
}
#endif
