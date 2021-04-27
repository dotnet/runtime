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
#include <dlfcn.h>

static char *bundle_path;
static char *tpa_list;

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

const char *
get_tpa_list (const char *bundle)
{
    if (tpa_list)
        return tpa_list;
    NSString *path = @(bundle_path);
    NSArray *dirs = [[NSFileManager defaultManager] contentsOfDirectoryAtPath:path error:NULL];
    NSMutableArray *assemblies = [[NSMutableArray alloc] init];
    [dirs enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {
        NSString *filename = (NSString *)obj;
        NSString *extension = [[filename pathExtension] lowercaseString];
        if ([extension isEqualToString:@"dll"]) {
            [assemblies addObject:[path stringByAppendingPathComponent:filename]];
        }
    }];

    NSString *ns_tpa_list = [assemblies componentsJoinedByString:@":"];
    tpa_list = strdup ([ns_tpa_list UTF8String]);

    return tpa_list;
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

void *
pinvoke_override (const char *libraryName, const char *entrypointName)
{
    void *symbol = NULL;

    if (strcmp (libraryName, "__Internal") == 0) {
        symbol = dlsym (RTLD_DEFAULT, entrypointName);
    }
    return symbol;
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

    const char* tpa = get_tpa_list (bundle);

    char icu_dat_path [1024];
    int res;

    res = snprintf (icu_dat_path, sizeof (icu_dat_path) - 1, "%s/%s", bundle, "icudt.dat");
    assert (res > 0);

    char *pinvokeOverride = strdup_printf ("%p", &pinvoke_override);

    void* hostHandle;
    unsigned int domainId;
    const char* propertyKeys[] = {
        "PINVOKE_OVERRIDE",
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "RUNTIME_IDENTIFIER", 
        "APP_CONTEXT_BASE_DIRECTORY",
#if !defined(INVARIANT_GLOBALIZATION) && !TARGET_OS_MACCATALYST
        "ICU_DAT_FILE_PATH"
#endif
    };
    const char* propertyValues[] = {
        pinvokeOverride,
        tpa,
        APPLE_RUNTIME_IDENTIFIER,
        bundle,
#if !defined(INVARIANT_GLOBALIZATION) && !TARGET_OS_MACCATALYST
        icu_dat_path
#endif
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
