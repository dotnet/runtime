// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_datetime.h"
#import <Foundation/Foundation.h>

char* SystemNative_GetDefaultTimeZone()
{
    NSTimeZone *tz = [NSTimeZone localTimeZone];
    NSString *name = [tz name];
    return (name != nil) ? strdup([name UTF8String]) : NULL;
}