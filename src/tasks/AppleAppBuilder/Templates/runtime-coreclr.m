// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#import <Foundation/Foundation.h>
#include <coreclrhost.h>
#include <TargetConditionals.h>
#import <os/log.h>
#include <sys/stat.h>
#include <sys/mman.h>
#include <stdlib.h>
#include <stdio.h>

static char *bundle_path;

#define APPLE_RUNTIME_IDENTIFIER "//%APPLE_RUNTIME_IDENTIFIER%"

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

char *
strdup_printf (const char *msg, ...)
{
    va_list args;
    char *formatted = NULL;
    va_start (args, msg);
    vasprintf (&formatted, msg, args);
    va_end (args);
    return formatted;
}

void
log_callback (const char *log_domain, const char *log_level, const char *message, bool fatal, void *user_data)
{
    os_log_info (OS_LOG_DEFAULT, "(%s %s) %s", log_domain, log_level, message);
    if (fatal) {
        os_log_info (OS_LOG_DEFAULT, "Exit code: %d.", 1);
        exit (1);
    }
}

void
mono_ios_runtime_init (void)
{
#if INVARIANT_GLOBALIZATION
    setenv ("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1", TRUE);
#endif

    id args_array = [[NSProcessInfo processInfo] arguments];
    assert ([args_array count] <= 128);
    const char *managed_argv [128];
    int argi;
    for (argi = 0; argi < [args_array count]; argi++) {
        NSString* arg = [args_array objectAtIndex: argi];
        managed_argv[argi] = [arg UTF8String];
    }

    const char* bundle = get_bundle_path ();
    chdir (bundle);

    char icu_dat_path [1024];
    int res;

    res = snprintf (icu_dat_path, sizeof (icu_dat_path) - 1, "%s/%s", bundle, "icudt.dat");
    assert (res > 0);

    // TODO: set TRUSTED_PLATFORM_ASSEMBLIES, APP_PATHS and NATIVE_DLL_SEARCH_DIRECTORIES
    const char *appctx_keys [] = {
        "RUNTIME_IDENTIFIER", 
        "APP_CONTEXT_BASE_DIRECTORY",
#if !defined(INVARIANT_GLOBALIZATION) && !TARGET_OS_MACCATALYST
        "ICU_DAT_FILE_PATH"
#endif
    };
    const char *appctx_values [] = {
        APPLE_RUNTIME_IDENTIFIER,
        bundle,
#if !defined(INVARIANT_GLOBALIZATION) && !TARGET_OS_MACCATALYST
        icu_dat_path
#endif
    };

    void* hostHandle;
    unsigned int domainId;
    const char* propertyKeys[] = {
        "TRUSTED_PLATFORM_ASSEMBLIES"
    };
    const char* propertyValues[] = {
        ""
    };

    coreclr_initialize (
        bundle, 
        APPLE_RUNTIME_IDENTIFIER, 
        sizeof(propertyKeys) / sizeof(char*),
        propertyKeys,
        propertyValues,
        &hostHandle,
        &domainId);

    const char* executable = "%EntryPointLibName%";

    coreclr_execute_assembly (
        hostHandle,
        domainId,
        argi,
        managed_argv,
        executable,
        (unsigned int*)&res);
    os_log_info (OS_LOG_DEFAULT, "Executable: %s", executable);

    // Print this so apps parsing logs can detect when we exited
    os_log_info (OS_LOG_DEFAULT, "Exit code: %d.", res);

    exit (res);
}
