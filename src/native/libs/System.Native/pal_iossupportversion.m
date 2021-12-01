// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_iossupportversion.h"
#include "pal_autoreleasepool.h"
#import <Foundation/Foundation.h>

const char* SystemNative_iOSSupportVersion()
{
    EnsureNSThreadIsMultiThreaded();

    @autoreleasepool
    {
        NSDictionary *plist = [NSDictionary dictionaryWithContentsOfFile:@"/System/Library/CoreServices/SystemVersion.plist"];
        NSString *iOSSupportVersion = (NSString *)[plist objectForKey:@"iOSSupportVersion"];
        return strdup([iOSSupportVersion UTF8String]);
    }
}
