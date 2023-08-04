// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_autoreleasepool.h"
#include <Foundation/Foundation.h>
#include <objc/runtime.h>

static void noop_release(id self, SEL _cmd)
{
    [self release];
}

void EnsureNSThreadIsMultiThreaded(void)
{
    if (![NSThread isMultiThreaded])
    {
        NSString *placeholderClassName = @"PlaceholderObject";
        NSString *uuidClassName = [[NSUUID UUID] UUIDString];
        const char* className = [[placeholderClassName stringByAppendingString:uuidClassName] UTF8String];

        // For the overwhelming majority of all cases, there will only ever be one runtime at a time in a process. However,
        // for library mode both in NativeAOT and Mono, more than one library can be used. Therefore, 
        // the placeholder class needs to be registered at runtime and its name unique in the event (unlikely as it may be)
        // this is called more than once. 
        Class defPlaceholderObject = objc_allocateClassPair([NSObject class], className, 0);
        class_addMethod(defPlaceholderObject, @selector(noop:), (IMP)noop_release, "v@:");
        objc_registerClassPair(defPlaceholderObject);

        id placeholderObject = [[defPlaceholderObject alloc] init];

        // Start another no-op thread with the NSThread APIs to get NSThread into multithreaded mode.
        // The NSAutoReleasePool APIs can't be used on secondary threads until NSThread is in multithreaded mode.
        // See https://developer.apple.com/documentation/foundation/nsautoreleasepool for more information.
        //
        // We need to use detachNewThreadSelector to put NSThread into multithreaded mode.
        // We can't use detachNewThreadWithBlock since it doesn't change NSThread into multithreaded mode for some reason.
        // See https://developer.apple.com/documentation/foundation/nswillbecomemultithreadednotification for more information.
        [NSThread detachNewThreadSelector:@selector(noop:) toTarget:placeholderObject withObject:nil];
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
