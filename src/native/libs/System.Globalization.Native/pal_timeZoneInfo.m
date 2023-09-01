// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <stdbool.h>
#include <stdint.h>
#include <stdlib.h>

#include "pal_errors_internal.h"
#include "pal_locale_internal.h"
#include "pal_timeZoneInfo.h"

#import <Foundation/Foundation.h>
#if defined(TARGET_MACCATALYST) || defined(TARGET_IOS) || defined(TARGET_TVOS)

/*
Gets the localized display name that is currently in effect for the specified time zone.
*/
int32_t GlobalizationNative_GetTimeZoneDisplayNameNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* timeZoneId, int32_t timeZoneIdLength,
                                                      TimeZoneDisplayNameType type, uint16_t* result, int32_t resultLength)
{
    @autoreleasepool
    {
        NSLocale *currentLocale;
        if(localeName == NULL || lNameLength == 0)
        {
            currentLocale = [NSLocale systemLocale];
        }
        else
        {
            NSString *locName = [NSString stringWithCharacters: localeName length: lNameLength];
            currentLocale = [NSLocale localeWithLocaleIdentifier:locName];
        }
        NSString* tzName = [NSString stringWithCharacters: timeZoneId length: timeZoneIdLength];
        NSTimeZone* timeZone = [NSTimeZone timeZoneWithName:tzName];
        NSTimeZoneNameStyle style;

        switch (type)
        {
            case TimeZoneDisplayName_Standard:
                style = NSTimeZoneNameStyleStandard;//;NSTimeZoneNameStyleShortStandard
                break;
            case TimeZoneDisplayName_DaylightSavings:
                style = NSTimeZoneNameStyleDaylightSaving;//;NSTimeZoneNameStyleShortDaylightSaving
                break;
            case TimeZoneDisplayName_Generic:
                style = NSTimeZoneNameStyleGeneric;//;NSTimeZoneNameStyleShortGeneric
                break;
            // case TimeZoneDisplayName_GenericLocation:
            //     style = NSTimeZoneNameStyleStandard;//???
            //     break;
            // case TimeZoneDisplayName_ExemplarCity:
            //     style = NSTimeZoneNameStyleStandard;//??
            //     break;
            default:
                return UnknownError;//-1;
        }

        NSString* timeZoneName = [timeZone localizedName:style locale:currentLocale];
        if (timeZoneName == NULL || timeZoneName.length == 0)
        {
            return UnknownError;//0
        }

        int32_t index = 0, dstIdx = 0, isError = 0;
        uint16_t dstCodepoint;
        while (index < timeZoneName.length)
        {
            dstCodepoint = [timeZoneName characterAtIndex: index];
            Append(result, dstIdx, resultLength, dstCodepoint, isError);
            index++;
        }
        dstCodepoint = '\0';
        Append(result, dstIdx, resultLength, dstCodepoint, isError);

        return isError ? UnknownError : Success;//timeZoneName.length;//;
    }
}
#endif
