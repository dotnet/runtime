// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include "pal_locale_internal.h"

#import <Foundation/Foundation.h>

const char* DetectDefaultAppleLocaleName()
{
    NSLocale *currentLocale = [NSLocale currentLocale];
    NSString *localeName = @"";
    const char* envLocaleName;
    
    // Match the ICU behavior where the default locale can be overriden by 
    // and env variable.
    envLocaleName = getenv("LANG");

    if (envLocaleName != NULL)
    {
        return envLocaleName;
    }
    
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
        localeName = [currentLocale.localeIdentifier stringByReplacingOccurrencesOfString:@"_" withString:@"-"];
    }
    
    return strdup([localeName UTF8String]);
}

