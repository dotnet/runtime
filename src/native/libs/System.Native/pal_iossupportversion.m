// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_iossupportversion.h"
#include "pal_autoreleasepool.h"
#import <Foundation/Foundation.h>

#if __has_feature(objc_arc)
#error This file uses manual memory management and must not use ARC, but ARC is enabled.
#endif

const char* SystemNative_iOSSupportVersion(void)
{
    NSDictionary *plist = [[NSDictionary alloc] initWithContentsOfFile:@"/System/Library/CoreServices/SystemVersion.plist"];
    NSString *iOSSupportVersion = (NSString *)[plist objectForKey:@"iOSSupportVersion"];
    const char* version = strdup([iOSSupportVersion UTF8String]);
    [plist release];

    return version;
}
