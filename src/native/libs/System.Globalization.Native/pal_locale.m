// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <string.h>
#include "pal_locale_internal.h"
#include "pal_localeStringData.h"
#include "pal_localeNumberData.h"

#import <Foundation/Foundation.h>

#if !__has_feature(objc_arc)
#error This file relies on ARC for memory management, but ARC is not enabled.
#endif

char* DetectDefaultAppleLocaleName(void)
{
    NSLocale *currentLocale = [NSLocale currentLocale];
    NSString *localeName = @"";

    if (!currentLocale)
    {
        return strdup([localeName UTF8String]);
    }

    if ([currentLocale.languageCode length] > 0 && [currentLocale.countryCode length] > 0)
    {
        localeName = [NSString stringWithFormat:@"%@-%@", currentLocale.languageCode, currentLocale.countryCode];
    }
    else
    {
        localeName = currentLocale.localeIdentifier;
    }

    return strdup([localeName UTF8String]);
}




