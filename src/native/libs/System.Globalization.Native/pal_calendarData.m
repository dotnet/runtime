// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include "pal_icushim_internal.h"
#include "pal_calendarData.h"
#import <Foundation/Foundation.h>

#if defined(TARGET_MACCATALYST) || defined(TARGET_IOS) || defined(TARGET_TVOS)

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
            calendarIdentifier = NSCalendarIdentifierGregorian;
            break;
    }
    return calendarIdentifier;
}

/*
Function:
GlobalizationNative_GetCalendarInfoNative

Gets a single string of calendar information for a given locale, calendar, and calendar data type.
with the requested value.
*/
const char* GlobalizationNative_GetCalendarInfoNative(const char* localeName, CalendarId calendarId, CalendarDataType dataType)
{
    NSString *locName = [NSString stringWithFormat:@"%s", localeName];
    NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];

    NSString *calendarIdentifier = GetCalendarIdentifier(calendarId);
    NSCalendar *calendar = [[NSCalendar alloc] initWithCalendarIdentifier:calendarIdentifier];

    NSDateFormatter *dateFormat = [[NSDateFormatter alloc] init];
    dateFormat.locale = currentLocale;
    dateFormat.calendar = calendar;

    switch (dataType)
    {
        case CalendarData_NativeName:
            return calendar ? strdup([[calendar calendarIdentifier] UTF8String]) : NULL;
        case CalendarData_MonthDay:
        {
             NSString *formatString = [NSDateFormatter dateFormatFromTemplate:@"MMMMd" options:0 locale:currentLocale];
             return formatString ? strdup([formatString UTF8String]) : NULL;
        }
        case CalendarData_ShortDates:
        {
            [dateFormat setDateStyle:NSDateFormatterShortStyle]; // also NSDateFormatterMediumStyle ? and 'y', 'M', 'd'
            NSString *shortFormatString = [dateFormat dateFormat];
            return shortFormatString ? strdup([shortFormatString UTF8String]) : NULL;
        }
        case CalendarData_LongDates:
        {
            [dateFormat setDateStyle:NSDateFormatterLongStyle]; // also NSDateFormatterFullStyle ?
            NSString *longFormatString = [dateFormat dateFormat];
            return longFormatString ? strdup([longFormatString UTF8String]) : NULL;
        }
        case CalendarData_YearMonths:
        {
            NSString *formatString = [NSDateFormatter dateFormatFromTemplate:@"MMMM yyyy" options:0 locale:currentLocale];
            return formatString ? strdup([formatString UTF8String]) : NULL;
        }
        case CalendarData_DayNames:
        {
            NSArray *standaloneWeekdaySymbols = [dateFormat standaloneWeekdaySymbols];
            NSString *arrayToString = [[standaloneWeekdaySymbols valueForKey:@"description"] componentsJoinedByString:@"||"];
            return arrayToString ? strdup([arrayToString UTF8String]) : NULL;
        }
        case CalendarData_AbbrevDayNames:
        {
            NSArray *shortStandaloneWeekdaySymbols = [dateFormat shortStandaloneWeekdaySymbols];
            NSString *arrayToString = [[shortStandaloneWeekdaySymbols valueForKey:@"description"] componentsJoinedByString:@"||"];
            return arrayToString ? strdup([arrayToString UTF8String]) : NULL;
        }
        case CalendarData_MonthNames:
        {
            NSArray *standaloneMonthSymbols = [dateFormat standaloneMonthSymbols];
            NSString *arrayToString = [[standaloneMonthSymbols valueForKey:@"description"] componentsJoinedByString:@"||"];
            return arrayToString ? strdup([arrayToString UTF8String]) : NULL;
        }
        case CalendarData_AbbrevMonthNames:
        {
            NSArray *shortStandaloneMonthSymbols = [dateFormat shortStandaloneMonthSymbols];
            NSString *arrayToString = [[shortStandaloneMonthSymbols valueForKey:@"description"] componentsJoinedByString:@"||"];
            return arrayToString ? strdup([arrayToString UTF8String]) : NULL;
        }
        case CalendarData_SuperShortDayNames:
        {
            NSArray *veryShortStandaloneWeekdaySymbols = [dateFormat veryShortStandaloneWeekdaySymbols];
            NSString *arrayToString = [[veryShortStandaloneWeekdaySymbols valueForKey:@"description"] componentsJoinedByString:@"||"];
            return arrayToString ? strdup([arrayToString UTF8String]) : NULL;
        }
        case CalendarData_MonthGenitiveNames:
        {
            NSArray *monthSymbols = [dateFormat monthSymbols];
            NSString *arrayToString = [[monthSymbols valueForKey:@"description"] componentsJoinedByString:@"||"];
            return arrayToString ? strdup([arrayToString UTF8String]) : NULL;
        }
        case CalendarData_AbbrevMonthGenitiveNames:
        {
            NSArray *shortMonthSymbols = [dateFormat shortMonthSymbols];
            NSString *arrayToString = [[shortMonthSymbols valueForKey:@"description"] componentsJoinedByString:@"||"];
            return arrayToString ? strdup([arrayToString UTF8String]) : NULL;
        }
        case CalendarData_EraNames:
        case CalendarData_AbbrevEraNames:
        {
            NSArray *eraSymbols = [dateFormat eraSymbols];
            NSString *arrayToString = [[eraSymbols valueForKey:@"description"] componentsJoinedByString:@"||"];
            return arrayToString ? strdup([arrayToString UTF8String]) : NULL;
        }
        default:
            assert(false);
            return NULL;
    }
}
#endif
