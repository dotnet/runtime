// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_log.h"
#import <Foundation/Foundation.h>

void SystemNative_Log (uint8_t* buffer, int32_t length)
{
    NSString *msg = [[NSString alloc] initWithBytes: buffer length: length encoding: NSUTF16LittleEndianStringEncoding];
    if (length > 4096)
    {
        // Write in chunks of max 4096 characters; older versions of iOS seems to have a bug where NSLog may hang with long strings (!).
        // https://github.com/xamarin/maccore/issues/1014
        const char* utf8 = [msg UTF8String];
        size_t len = utf8 == NULL ? 0 : strlen (utf8);
        const size_t max_size = 4096;
        while (len > 0)
        {
            size_t chunk_size = len > max_size ? max_size : len;

            // Try to not break in the middle of a line, by looking backwards for a newline
            while (chunk_size > 0 && utf8 [chunk_size] != 0 && utf8 [chunk_size] != '\n')
            {
                chunk_size--;
            }
            if (chunk_size == 0)
            {
                // No newline found, break in the middle.
                chunk_size = len > max_size ? max_size : len;
            }
            NSLog (@"%.*s", (int) chunk_size, utf8);
            len -= chunk_size;
            utf8 += chunk_size;
        }
    }
    else
    {
        NSLog (@"%@", msg);
    }
    [msg release];
}

void SystemNative_LogError (uint8_t* buffer, int32_t length)
{
    SystemNative_Log (buffer, length);
}
