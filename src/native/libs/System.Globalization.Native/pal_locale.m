// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <string.h>
#include "pal_hybrid.h"
#include "pal_common.h"

#import <Foundation/Foundation.h>
#import <Foundation/NSFormatter.h>

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


const char* GlobalizationNative_GetICUDataPathRelativeToAppBundleRoot(const char* path)
{
    @autoreleasepool
    {
        NSString *bundlePath = [[NSBundle mainBundle] bundlePath];
        NSString *dataPath = [bundlePath stringByAppendingPathComponent: [NSString stringWithFormat:@"%s", path]];

        return strdup([dataPath UTF8String]);
    }
}

const char* GlobalizationNative_GetICUDataPathFallback(void)
{
    @autoreleasepool
    {
        NSString *dataPath = [[NSBundle mainBundle] pathForResource:@"icudt" ofType:@"dat"];
        return strdup([dataPath UTF8String]);
    }
}

