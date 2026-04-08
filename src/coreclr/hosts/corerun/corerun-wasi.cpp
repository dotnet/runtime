// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Minimal CoreCLR host for WASI — bypasses the full corerun machinery.

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <dirent.h>
#include <coreclrhost.h>

extern void add_pinvoke_override();

// Build TPA list by scanning directory for .dll files
static void build_tpa_list(const char* directory, char* tpa_list, size_t tpa_list_size)
{
    tpa_list[0] = '\0';
    DIR* dir = opendir(directory);
    if (dir == NULL)
        return;

    struct dirent* entry;
    while ((entry = readdir(dir)) != NULL)
    {
        const char* ext = strrchr(entry->d_name, '.');
        if (ext != NULL && strcmp(ext, ".dll") == 0)
        {
            if (tpa_list[0] != '\0')
                strncat(tpa_list, ":", tpa_list_size - strlen(tpa_list) - 1);

            strncat(tpa_list, directory, tpa_list_size - strlen(tpa_list) - 1);
            strncat(tpa_list, "/", tpa_list_size - strlen(tpa_list) - 1);
            strncat(tpa_list, entry->d_name, tpa_list_size - strlen(tpa_list) - 1);
        }
    }
    closedir(dir);
}

int main(int argc, char* argv[])
{
    if (argc < 2)
    {
        fprintf(stderr, "Usage: corerun-wasi [-c <clr-path>] <assembly> [args...]\n");
        return 1;
    }

    const char* clr_path = NULL;
    const char* assembly = NULL;
    int assembly_argc = 0;
    const char** assembly_argv = NULL;

    int i = 1;
    while (i < argc)
    {
        if ((strcmp(argv[i], "-c") == 0 || strcmp(argv[i], "--clr-path") == 0) && i + 1 < argc)
        {
            clr_path = argv[++i];
        }
        else
        {
            assembly = argv[i];
            assembly_argc = argc - i - 1;
            assembly_argv = (const char**)&argv[i + 1];
            break;
        }
        i++;
    }

    if (assembly == NULL)
    {
        fprintf(stderr, "Error: no assembly specified\n");
        return 1;
    }

    if (clr_path == NULL)
    {
        clr_path = getenv("CORE_ROOT");
        if (clr_path == NULL)
            clr_path = ".";
    }

    fprintf(stderr, "corerun-wasi: clr_path=%s assembly=%s\n", clr_path, assembly);

    // Build TPA list
    static char tpa_list[65536];
    build_tpa_list(clr_path, tpa_list, sizeof(tpa_list));

    char app_path[4096];
    strncpy(app_path, assembly, sizeof(app_path) - 1);
    char* last_slash = strrchr(app_path, '/');
    if (last_slash != NULL)
        *last_slash = '\0';
    else
        strcpy(app_path, ".");

    add_pinvoke_override();

    const char* property_keys[] = {
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "APP_PATHS",
    };
    const char* property_values[] = {
        tpa_list,
        app_path,
    };

    void* host_handle = NULL;
    unsigned int domain_id = 0;

    fprintf(stderr, "corerun-wasi: calling coreclr_initialize...\n");
    int hr = coreclr_initialize(
        assembly,
        "wasi_appdomain",
        sizeof(property_keys) / sizeof(property_keys[0]),
        property_keys,
        property_values,
        &host_handle,
        &domain_id);

    if (hr < 0)
    {
        fprintf(stderr, "corerun-wasi: coreclr_initialize failed: 0x%08x\n", hr);
        return 1;
    }

    fprintf(stderr, "corerun-wasi: executing assembly...\n");

    unsigned int exit_code = 0;
    hr = coreclr_execute_assembly(
        host_handle,
        domain_id,
        assembly_argc,
        assembly_argv,
        assembly,
        &exit_code);

    if (hr < 0)
    {
        fprintf(stderr, "corerun-wasi: coreclr_execute_assembly failed: 0x%08x\n", hr);
        return 1;
    }

    fprintf(stderr, "corerun-wasi: exit code %u\n", exit_code);

    int latch = 0;
    coreclr_shutdown_2(host_handle, domain_id, &latch);

    return exit_code;
}
