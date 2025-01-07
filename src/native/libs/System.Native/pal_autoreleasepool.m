// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_autoreleasepool.h"
#include <Foundation/Foundation.h>
#include <objc/runtime.h>

#if __has_feature(objc_arc)
#error This file uses manual memory management and must not use ARC, but ARC is enabled.
#endif

void EnsureNSThreadIsMultiThreaded(void)
{
    if (![NSThread isMultiThreaded])
    {
        // Start another no-op thread with the NSThread APIs to get NSThread into multithreaded mode.
        // The NSAutoReleasePool APIs can't be used on secondary threads until NSThread is in multithreaded mode.
        // See https://developer.apple.com/documentation/foundation/nsautoreleasepool for more information.
        //
        // We need to use detachNewThreadSelector to put NSThread into multithreaded mode.
        // We can't use detachNewThreadWithBlock since it doesn't change NSThread into multithreaded mode for some reason.
        // See https://developer.apple.com/documentation/foundation/nswillbecomemultithreadednotification for more information.
        id placeholderObject = [[NSMutableString alloc] init];
        [NSThread detachNewThreadSelector:@selector(appendString:) toTarget:placeholderObject withObject:@""];
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wobjc-messaging-id"
        [placeholderObject release];
#pragma clang diagnostic pop
    }
    assert([NSThread isMultiThreaded]);
}

void* SystemNative_CreateAutoreleasePool(void)
{
    EnsureNSThreadIsMultiThreaded();
    return [[NSAutoreleasePool alloc] init];
}

void SystemNative_DrainAutoreleasePool(void* pool)
{
    [((NSAutoreleasePool*)pool) drain];
}
