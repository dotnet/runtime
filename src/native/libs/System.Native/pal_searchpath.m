// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_searchpath.h"
#import <Foundation/Foundation.h>

#if __has_feature(objc_arc)
#error This file uses manual memory management and must not use ARC, but ARC is enabled.
#endif

const char* SystemNative_SearchPath(int32_t folderId)
{
    NSSearchPathDirectory spd = (NSSearchPathDirectory) folderId;
    NSURL* url = [[[NSFileManager defaultManager] URLsForDirectory:spd inDomains:NSUserDomainMask] lastObject];
    const char* path = [[url path] UTF8String];
    return path == NULL ? NULL : strdup (path);
}

const char* SystemNative_SearchPath_TempDirectory(void)
{
    NSString* tempPath = NSTemporaryDirectory();
    const char *path = [tempPath UTF8String];
    return path == NULL ? NULL : strdup (path);
}
