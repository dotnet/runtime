// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_datetime.h"
#import <Foundation/Foundation.h>

#if __has_feature(objc_arc)
#error This file uses manual memory management and must not use ARC, but ARC is enabled.
#endif

char* SystemNative_GetDefaultTimeZone(void)
{
    NSTimeZone *tz = [NSTimeZone localTimeZone];
    NSString *name = [tz name];
    return (name != nil) ? strdup([name UTF8String]) : NULL;
}
