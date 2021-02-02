// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_autoreleasepool.h"
#include <Foundation/Foundation.h>

@interface PlaceholderObject : NSObject
- (void)noop:(id)_;
@end

@implementation PlaceholderObject : NSObject
- (void)noop:(id)_
{
    [self release];
}
@end

void* SystemNative_CreateAutoreleasePool(void)
{
    if (![NSThread isMultiThreaded])
    {
        // Start another no-op thread with the NSThread APIs to get NSThread into multithreaded mode.
        // The NSAutoReleasePool APIs can't be used on secondary threads until NSThread is in multithreaded mode.
        PlaceholderObject* placeholderObject = [[PlaceholderObject alloc] init];
        [NSThread detachNewThreadSelector:@selector(noop:) toTarget:placeholderObject withObject:nil];
    }
    assert([NSThread isMultiThreaded]);

    return [[NSAutoreleasePool alloc] init];
}

void SystemNative_DrainAutoreleasePool(void* pool)
{
    [((NSAutoreleasePool*)pool) drain];
}
