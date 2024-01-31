// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "pal_errors_internal.h"
#include "pal_common.h"
#include "pal_hybrid.h"
#include "pal_timeZoneInfo_hg.h"
#import <Foundation/Foundation.h>

#if !__has_feature(objc_arc)
#error This file relies on ARC for memory management, but ARC is not enabled.
#endif

#if defined(APPLE_HYBRID_GLOBALIZATION)
/*
Gets the localized display name that is currently in effect for the specified time zone.
*/
int32_t GlobalizationNative_GetTimeZoneDisplayNameNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* timeZoneId, int32_t timeZoneIdLength,
                                                         TimeZoneDisplayNameType type, uint16_t* result, int32_t resultLength)
{
    @autoreleasepool
    {
        NSString* tzName = [NSString stringWithCharacters: timeZoneId length: timeZoneIdLength];
        NSTimeZone* timeZone = [NSTimeZone timeZoneWithName:tzName];
        if (timeZone == NULL)
        {
            return UnknownError;
        }
        NSString* timeZoneName;

        if (type == TimeZoneDisplayName_TimeZoneName)
            timeZoneName = timeZone.name;
        else
        {
            NSLocale *currentLocale;
            if (localeName == NULL || lNameLength == 0)
            {
                currentLocale = [NSLocale systemLocale];
            }
            else
            {
                NSString *locName = [NSString stringWithCharacters: localeName length: lNameLength];
                currentLocale = [NSLocale localeWithLocaleIdentifier:locName];
            }
            NSTimeZoneNameStyle style;

            switch (type)
            {
                case TimeZoneDisplayName_Standard:
                    style = NSTimeZoneNameStyleStandard;
                    break;
                case TimeZoneDisplayName_DaylightSavings:
                    style = NSTimeZoneNameStyleDaylightSaving;
                    break;
                case TimeZoneDisplayName_Generic:
                    style = NSTimeZoneNameStyleGeneric;
                    break;
                default:
                    return UnknownError;
            }

            timeZoneName = [timeZone localizedName:style locale:currentLocale];
        }
        if (timeZoneName == NULL || timeZoneName.length == 0)
        {
            return UnknownError;
        }

        int32_t index = 0, dstIdx = 0, resultCode = Success;
        uint16_t dstCodepoint;
        while (index < timeZoneName.length)
        {
            dstCodepoint = [timeZoneName characterAtIndex: index];
            Append(result, dstIdx, resultLength, dstCodepoint, resultCode);
            if (resultCode != Success)
                return resultCode;
            index++;
        }
        dstCodepoint = '\0';
        Append(result, dstIdx, resultLength, dstCodepoint, resultCode);

        return resultCode;
    }
}
#endif
