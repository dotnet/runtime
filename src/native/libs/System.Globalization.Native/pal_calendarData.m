// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include "pal_icushim_internal.h"
#include "pal_calendarData.h"
#import <Foundation/Foundation.h>

#if !__has_feature(objc_arc)
#error This file relies on ARC for memory management, but ARC is not enabled.
#endif

#if defined(TARGET_MACCATALYST) || defined(TARGET_IOS) || defined(TARGET_TVOS)
#define GREGORIAN_NAME "gregorian"
#define JAPANESE_NAME "japanese"
#define BUDDHIST_NAME "buddhist"
#define HEBREW_NAME "hebrew"
#define DANGI_NAME "dangi"
#define PERSIAN_NAME "persian"
#define ISLAMIC_NAME "islamic"
#define ISLAMIC_UMALQURA_NAME "islamic-umalqura"
#define ROC_NAME "roc"
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
    else if (strcasecmp(calendarName, DANGI_NAME) == 0)
        return KOREA;
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
            return strdup([[currentLocale localizedStringForCalendarIdentifier:calendarIdentifier] UTF8String]);//calendar ? strdup([[calendar calendarIdentifier] UTF8String]) : NULL;

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
GetLatestJapaneseEra

Gets the latest era in the Japanese calendar.
*/
int32_t GlobalizationNative_GetLatestJapaneseEraNative()
{
    // Create an NSCalendar with the Japanese calendar identifier
    NSCalendar *japaneseCalendar = [[NSCalendar alloc] initWithCalendarIdentifier:NSCalendarIdentifierJapanese];
    // Get the latest era
    NSDateComponents *latestEraComponents = [japaneseCalendar components:NSCalendarUnitEra fromDate:[NSDate date]];
    // Extract the era component
    NSInteger latestEra = [latestEraComponents era];
    return (int32_t)latestEra;
}

/*
Function:
GetJapaneseEraInfo

Gets the starting Gregorian date of the specified Japanese Era.
*/
const char* GlobalizationNative_GetJapaneseEraStartDateNative(int32_t era)
{
    NSCalendar *japaneseCalendar = [[NSCalendar alloc] initWithCalendarIdentifier:NSCalendarIdentifierJapanese];
    NSDateComponents *startDateComponents = [[NSDateComponents alloc] init];
    startDateComponents.era = era;
    startDateComponents.month = 1;
    startDateComponents.day = 1;
    startDateComponents.year = 1;
    NSDate *date = [japaneseCalendar dateFromComponents:startDateComponents];
    NSDate *startDay = date;
    int32_t currentEra;

    for (int month = 0; month <= 12; month++)
    {
        NSDateComponents *eraComponents = [japaneseCalendar components:NSCalendarUnitEra fromDate:date];
        // Extract the era component
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
                    
                    startDay = [japaneseCalendar dateFromComponents:startDateComponents];
                    //eraComponents = [japaneseCalendar components:NSCalendarUnitEra fromDate:date];

                    break;
                }
            }
        }
        // add 1 month at a time until we get into the specified Era
        startDateComponents.month = startDateComponents.month + 1;
        date = [japaneseCalendar dateFromComponents:startDateComponents];
        eraComponents = [japaneseCalendar components:NSCalendarUnitEra fromDate:date];
        currentEra = [eraComponents era];
    }

    // Create an NSDateFormatter to format the Gregorian date
    NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
    dateFormatter.dateFormat = @"yyyy-MM-dd";

    // Format and print the Gregorian start date
    NSString *formattedStartDate = [dateFormatter stringFromDate:startDay];
    return formattedStartDate ? strdup([formattedStartDate UTF8String]) : NULL;
}

/*
Function:
GetCalendars

Returns the list of CalendarIds that are available for the specified locale.
*/
int32_t GlobalizationNative_GetCalendarsNative(
    const char* localeName, CalendarId* calendars, int32_t calendarsCapacity)
{

    @autoreleasepool
    {
        NSString *locName = [NSString stringWithFormat:@"%s", localeName];
        NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
        NSArray *calendarIdentifiers = @[
                NSCalendarIdentifierGregorian,
                NSCalendarIdentifierBuddhist,
                NSCalendarIdentifierChinese,
                NSCalendarIdentifierCoptic,
                NSCalendarIdentifierEthiopicAmeteMihret,
                NSCalendarIdentifierEthiopicAmeteAlem,
                NSCalendarIdentifierHebrew,
                NSCalendarIdentifierISO8601,
                NSCalendarIdentifierIndian,
                NSCalendarIdentifierIslamicCivil,
                NSCalendarIdentifierIslamicTabular,
                NSCalendarIdentifierIslamicUmmAlQura,
                NSCalendarIdentifierIslamic,
                NSCalendarIdentifierJapanese,
                NSCalendarIdentifierPersian,
                NSCalendarIdentifierRepublicOfChina,
            ];
        int32_t calendarsReturned = 0;
        for (int i = 0; i < calendarIdentifiers.count && calendarsReturned < calendarsCapacity; i++)
        {
            NSString *calendarIdentifier = calendarIdentifiers[i];
            calendars[calendarsReturned] = GetCalendarId([calendarIdentifier UTF8String]);
            calendarsReturned++;
        }
        return calendarsReturned;
    }
}
#endif
