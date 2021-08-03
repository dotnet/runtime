// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <Foundation/Foundation.h>
#include <xplatform.h>

volatile int NumReleaseCalls = 0;

@interface AutoReleaseTest : NSObject
- (void)release;
@end

@implementation AutoReleaseTest : NSObject
- (void)release
{
    NumReleaseCalls++;
    [super release];
}
@end

extern "C" DLL_EXPORT AutoReleaseTest* STDMETHODCALLTYPE initObject()
{
    return [[AutoReleaseTest alloc] init];
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE autoreleaseObject(AutoReleaseTest* art)
{
    [art autorelease];
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE getNumReleaseCalls()
{
    return NumReleaseCalls;
}
