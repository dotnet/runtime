// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <cstdio>
#include <coreclrhost.h>

static void log_error_info(const char* line)
{
    std::fprintf(stderr, "log error: %s\n", line);
}

// The current CoreCLR instance details.
static void* CurrentClrInstance;
static unsigned int CurrentAppDomainId;

static int run()
{
    const char* exe_path = "<coreclr-wasm>";
    const char* app_domain_name = "corewasmrun";
    const char* entry_assembly = "ManagedAssembly.dll";

    coreclr_set_error_writer(log_error_info);

    printf("call coreclr_initialize\n");
    int retval = coreclr_initialize(exe_path, app_domain_name, 0, nullptr, nullptr, &CurrentClrInstance, &CurrentAppDomainId);

    if (retval < 0)
    {
        std::fprintf(stderr, "coreclr_initialize failed - Error: 0x%08x\n", retval);
        return -1;
    }
    else
    {
        printf("coreclr_initialize succeeded - retval: 0x%08x\n", retval);
    }

    // coreclr_execute_assembly();
    // coreclr_shutdown();

    return retval;
}

int main()
{
    int retval = run();

    return retval;
}
