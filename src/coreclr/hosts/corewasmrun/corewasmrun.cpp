// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cstdio>
#include <coreclrhost.h>
#include <vector>

#include "corerun.wasm.hpp"

static void log_error_info(const char* line)
{
    std::fprintf(stderr, "log error: %s\n", line);
}

// The current CoreCLR instance details.
static void* CurrentClrInstance;
static unsigned int CurrentAppDomainId;

static int run()
{
    const char* exe_path = "/";
    const char* app_domain_name = "corewasmrun";
    const char* entry_assembly = "ManagedAssembly.dll";

    // Set base initialization properties.
    std::vector<const char*> propertyKeys;
    std::vector<const char*> propertyValues;

    propertyKeys.push_back("TRUSTED_PLATFORM_ASSEMBLIES");
    propertyValues.push_back("/HelloWorld.dll:/System.Private.CoreLib.dll:/System.Runtime.dll:/System.Console.dll:/System.Threading.dll:/System.Runtime.InteropServices.dll");
    propertyKeys.push_back("NATIVE_DLL_SEARCH_DIRECTORIES");
    propertyValues.push_back("/:.:");

    coreclr_set_error_writer(log_error_info);

    wasm_add_pinvoke_override();

    printf("BEGIN: call wasm_load_icu_data\n");
    int retval = wasm_load_icu_data("/");
    printf("END: call wasm_load_icu_data\n");

    if (retval == 0)
    {
        std::fprintf(stderr, "Failed to load the ICU data\n");
        return -1;
    }

    printf("BEGIN: call coreclr_initialize\n");
    retval = coreclr_initialize(exe_path, app_domain_name, (int)propertyKeys.size(), propertyKeys.data(), propertyValues.data(), &CurrentClrInstance, &CurrentAppDomainId);
    printf("END: call coreclr_initialize\n");

    if (retval < 0)
    {
        std::fprintf(stderr, "coreclr_initialize failed - Error: 0x%08x\n", retval);
        return -1;
    }
    else
    {
        printf("coreclr_initialize succeeded - retval: 0x%08x\n", retval);
    }

    int exit_code;
    printf("BEGIN: call coreclr_execute_assembly\n");
    retval = coreclr_execute_assembly(CurrentClrInstance, CurrentAppDomainId, 0, nullptr, "HelloWorld.dll", (uint32_t*)&exit_code);
    printf("END: call coreclr_execute_assembly\n");

    if (retval < 0)
    {
        std::fprintf(stderr, "coreclr_execute_assembly failed - Error: 0x%08x\n", retval);
        return -1;
    }

    int latched_exit_code = 0;
    printf("BEGIN: call coreclr_shutdown_2\n");
    retval = coreclr_shutdown_2(CurrentClrInstance, CurrentAppDomainId, &latched_exit_code);
    printf("END: call coreclr_shutdown_2\n");
    if (retval < 0)
    {
        std::fprintf(stderr, "coreclr_shutdown_2 failed - Error: 0x%08x\n", retval);
        exit_code = -1;
    }

    return retval;
}

int main()
{
    int retval = run();

    return retval;
}
