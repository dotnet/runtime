// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#import <Foundation/Foundation.h>
#include "coreclrhost.h"
#include "host_runtime_contract.h"
#include <TargetConditionals.h>
#import <os/log.h>
#include <sys/stat.h>
#include <sys/mman.h>
#include <stdlib.h>
#include <stdio.h>
#include <dlfcn.h>

#import "util.h"

#define APPLE_RUNTIME_IDENTIFIER "//%APPLE_RUNTIME_IDENTIFIER%"

const char *
get_bundle_path (void)
{
    static char *bundle_path = NULL;
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
compute_trusted_platform_assemblies ()
{
    const char *bundle_path = get_bundle_path ();

    NSMutableArray<NSString *> *files = [NSMutableArray array];
    NSMutableArray<NSString *> *exes = [NSMutableArray array];

    NSFileManager *manager = [NSFileManager defaultManager];
    NSString *dir = [NSString stringWithUTF8String: bundle_path];
    NSDirectoryEnumerator *enumerator = [manager enumeratorAtURL:[NSURL fileURLWithPath: dir]
                                                 includingPropertiesForKeys:@[NSURLNameKey, NSURLIsDirectoryKey]
                                                 options:NSDirectoryEnumerationSkipsSubdirectoryDescendants
                                                 errorHandler:nil];
    for (NSURL *file in enumerator) {
        // skip subdirectories
        NSNumber *isDirectory = nil;
        if (![file getResourceValue:&isDirectory forKey:NSURLIsDirectoryKey error:nil] || [isDirectory boolValue])
            continue;

        NSString *name = nil;
        if (![file getResourceValue:&name forKey:NSURLNameKey error:nil])
            continue;
        if ([name length] < 4)
            continue;
       if ([name compare: @".dll" options: NSCaseInsensitiveSearch range: NSMakeRange ([name length] - 4, 4)] == NSOrderedSame) {
            [files addObject: [dir stringByAppendingPathComponent: name]];
        }
    }

    // Join them all together with a colon separating them
    NSString *joined = [files componentsJoinedByString: @":"];
    return strdup([joined UTF8String]);
}

const void*
pinvoke_override (const char *libraryName, const char *entrypointName)
{
    if (!strcmp (libraryName, "__Internal"))
    {
        return dlsym (RTLD_DEFAULT, entrypointName);
    }

    return NULL;
}

#include <mach-o/dyld.h>
#include <mach-o/loader.h>
size_t get_image_size(const struct mach_header_64* header)
{
    const struct load_command* cmd = (const struct load_command*)((const char*)header + sizeof(struct mach_header_64));

    size_t image_size = 0;
    for (uint32_t j = 0; j < header->ncmds; ++j)
    {
        if (cmd->cmd == LC_SEGMENT_64)
        {
            const struct segment_command_64* seg = (const struct segment_command_64*)cmd;
            size_t end_addr = (size_t)(seg->vmaddr + seg->vmsize);
            if (end_addr > image_size)
                image_size = end_addr;
        }

        cmd = (const struct load_command*)((const char*)cmd + cmd->cmdsize);
    }

    return image_size;
}

bool get_native_code_data(const struct host_runtime_contract_native_code_context* context, struct host_runtime_contract_native_code_data* data)
{
    if (!context || !data || !context->assembly_path || !context->owner_composite_name)
        return false;

    // Look for the owner composite R2R image in the same directory as the assembly
    char r2r_path[PATH_MAX];
    const char *last_slash = strrchr(context->assembly_path, '/');
    size_t dir_len = last_slash ? (size_t)(last_slash - context->assembly_path) : 0;
    if (dir_len >= sizeof(r2r_path) - 1)
        return false;

    strncpy(r2r_path, context->assembly_path, dir_len);
    int written = snprintf(r2r_path + dir_len, sizeof(r2r_path) - dir_len, "/%s", context->owner_composite_name);
    if (written <= 0 || (size_t)written >= sizeof(r2r_path) - dir_len)
        return false;

    void* handle = dlopen(r2r_path, RTLD_LAZY | RTLD_LOCAL);
    if (handle == NULL)
        return false;

    void* r2r_header = dlsym(handle, "RTR_HEADER");
    if (r2r_header == NULL)
    {
        dlclose(handle);
        return false;
    }

    Dl_info info;
    if (dladdr(r2r_header, &info) == 0)
    {
        dlclose(handle);
        return false;
    }

    // The base address points to the Mach header
    void* base_address = info.dli_fbase;
    const struct mach_header_64* header = (const struct mach_header_64*)base_address;

    data->size = sizeof(struct host_runtime_contract_native_code_data);
    data->r2r_header_ptr = r2r_header;
    data->image_size = get_image_size(header);
    data->image_base = base_address;
    return true;
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

    // build using DiagnosticPorts property in AppleAppBuilder
    // or set DOTNET_DiagnosticPorts env via mlaunch, xharness when undefined.
    // NOTE, using DOTNET_DiagnosticPorts requires app build using AppleAppBuilder and RuntimeComponents=diagnostics_tracing
#ifdef DIAGNOSTIC_PORTS
    setenv ("DOTNET_DiagnosticPorts", DIAGNOSTIC_PORTS, true);
#endif

%EnvVariables%

    char **managed_argv;
    int argi = get_managed_args (&managed_argv);

    bool wait_for_debugger = FALSE;

    const char* bundle = get_bundle_path ();
    chdir (bundle);

    char icu_dat_path [1024];
    int res;
#if defined(HYBRID_GLOBALIZATION)
    res = snprintf (icu_dat_path, sizeof (icu_dat_path) - 1, "%s/%s", bundle, "icudt_hybrid.dat");
#else
    res = snprintf (icu_dat_path, sizeof (icu_dat_path) - 1, "%s/%s", bundle, "icudt.dat");
#endif
    assert (res > 0);

    // Contract lasts the lifetime of the app. The app exists before the end of this function.
    struct host_runtime_contract host_contract = {
        .size = sizeof(struct host_runtime_contract),
        .pinvoke_override = &pinvoke_override,
        .get_native_code_data = &get_native_code_data
    };

    char contract_str[19]; // 0x + 16 hex digits + '\0'
    snprintf(contract_str, 19, "0x%zx", (size_t)(&host_contract));

    // TODO: set TRUSTED_PLATFORM_ASSEMBLIES, APP_PATHS and NATIVE_DLL_SEARCH_DIRECTORIES
    const char *appctx_keys [] = {
        "RUNTIME_IDENTIFIER",
        "APP_CONTEXT_BASE_DIRECTORY",
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "HOST_RUNTIME_CONTRACT",
#if !defined(INVARIANT_GLOBALIZATION)
        "ICU_DAT_FILE_PATH"
#endif
    };
    const char *appctx_values [] = {
        APPLE_RUNTIME_IDENTIFIER,
        bundle,
        compute_trusted_platform_assemblies(),
        contract_str,
#if !defined(INVARIANT_GLOBALIZATION)
        icu_dat_path
#endif
    };

    const char* executable = "%EntryPointLibName%";
    const char *executablePath = [[[[NSBundle mainBundle] executableURL] path] UTF8String];
    unsigned int coreclr_domainId = 0;
    void *coreclr_handle = NULL;

    char path [1024];
    res = snprintf (path, sizeof (path) - 1, "%s/%s", bundle, executable);
    assert (res > 0);

    res = coreclr_initialize (
        executablePath, executable,
        sizeof (appctx_keys) / sizeof (appctx_keys [0]), appctx_keys, appctx_values,
        &coreclr_handle, &coreclr_domainId);
    assert (res == 0);

    coreclr_execute_assembly (coreclr_handle, coreclr_domainId, argi, managed_argv, path, &res);
    // Print this so apps parsing logs can detect when we exited
    os_log_info (OS_LOG_DEFAULT, EXIT_CODE_TAG ": %d", res);

    free_managed_args (&managed_argv, argi);

    exit (res);
}
