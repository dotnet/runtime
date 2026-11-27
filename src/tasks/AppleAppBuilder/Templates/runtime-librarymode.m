// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#import <Foundation/Foundation.h>
#include <TargetConditionals.h>
#import <os/log.h>
#include <sys/stat.h>
#include <sys/mman.h>
#include <stdlib.h>
#include <stdio.h>

#import "util.h"

static char *bundle_path;

int SayHello (void);

int invoke_netlibrary_entrypoints (void)
{
    return SayHello ();
}

const char *
get_bundle_path (void)
{
    if (bundle_path)
        return bundle_path;
    NSBundle* main_bundle = [NSBundle mainBundle];
    NSString* path = [main_bundle bundlePath];

#if TARGET_OS_MACCATALYST
    path = [path stringByAppendingString:@"/Contents/Resources"];
#endif

    bundle_path = strdup ([path UTF8String]);

    return bundle_path;
}

void
library_mode_init (void)
{
#if INVARIANT_GLOBALIZATION
    setenv ("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1", TRUE);
#endif

    // When not bundling, this will make sure the runtime can access all the assemblies
    const char* bundle = get_bundle_path ();
    chdir (bundle);

    int res = invoke_netlibrary_entrypoints ();
    // Print the return code so XHarness can detect it in the logs
    os_log_info (OS_LOG_DEFAULT, EXIT_CODE_TAG ": %d", res);

    exit (res);
}
