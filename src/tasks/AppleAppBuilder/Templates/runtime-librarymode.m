// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#import <Foundation/Foundation.h>
#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-logger.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/class.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/object.h>
#include <mono/jit/jit.h>
#include <mono/jit/mono-private-unstable.h>
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
mono_ios_runtime_init (void)
{
#if INVARIANT_GLOBALIZATION
    setenv ("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1", TRUE);
#endif

#if HYBRID_GLOBALIZATION
    setenv ("DOTNET_SYSTEM_GLOBALIZATION_HYBRID", "1", TRUE);
#endif

#if ENABLE_RUNTIME_LOGGING
    setenv ("MONO_LOG_LEVEL", "debug", TRUE);
    setenv ("MONO_LOG_MASK", "all", TRUE);
#endif

    // build using DiagnosticPorts property in AppleAppBuilder
    // or set DOTNET_DiagnosticPorts env via mlaunch, xharness when undefined.
    // NOTE, using DOTNET_DiagnosticPorts requires app build using AppleAppBuilder and RuntimeComponents=diagnostics_tracing
#ifdef DIAGNOSTIC_PORTS
    setenv ("DOTNET_DiagnosticPorts", DIAGNOSTIC_PORTS, true);
#endif

    // When not bundling, this will make sure the runtime can access all the assemblies
    const char* bundle = get_bundle_path ();
    chdir (bundle);

    int res = invoke_netlibrary_entrypoints ();

    exit (res);
}
