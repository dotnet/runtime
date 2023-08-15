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
            return calendar ? strdup([[calendar calendarIdentifier] UTF8String]) : NULL;

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
#endif
