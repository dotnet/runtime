// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <cstdio>
#include <vector>
#include <emscripten.h>

#include <pal.h>
#include <minipal/utils.h>
#include <host_runtime_contract.h>

static void log_error_info(const char* line)
{
    std::fprintf(stderr, "log error: %s\n", line);
}

// The current CoreCLR instance details.
static void* CurrentClrInstance;
static unsigned int CurrentAppDomainId;

typedef void (*coreclr_error_writer_callback_fn)(const char* line);
extern "C"
{
    pal::hresult_t coreclr_initialize(
        const char* exePath,
        const char* appDomainFriendlyName,
        int propertyCount,
        const char** propertyKeys,
        const char** propertyValues,
        void** hostHandle,
        unsigned int* domainId);

    pal::hresult_t coreclr_shutdown_2(
        void* hostHandle,
        unsigned int domainId,
        int* latchedExitCode);

    pal::hresult_t coreclr_execute_assembly(
        void* hostHandle,
        unsigned int domainId,
        int argc,
        const char** argv,
        const char* managedAssemblyPath,
        unsigned int* exitCode);

    pal::hresult_t coreclr_create_delegate(
        void* hostHandle,
        unsigned int domainId,
        const char* entryPointAssemblyName,
        const char* entryPointTypeName,
        const char* entryPointMethodName,
        void** delegate);

    pal::hresult_t coreclr_set_error_writer(
        coreclr_error_writer_callback_fn error_writer);

    const void* SystemResolveDllImport(const char* name);
    const void* BrowserResolveDllImport(const char* name);
    const void* GlobalizationResolveDllImport(const char* name);
    const void* CompressionResolveDllImport(const char* name);
}

bool bundle_probe(const char* path, int64_t* offset, int64_t* size, int64_t* compressedSize)
{
    // WASMTODO: Not implemented
    return false;
}

bool external_assembly_probe(const char* path, /*out*/ void **data_start, /*out*/ int64_t* size)
{
    // WASMTODO: Not implemented
    return false;
}

size_t get_runtime_property(const char* key, char* value_buffer, size_t value_buffer_size, void* contract_context)
{
    // WASMTODO: Not implemented
    return -1;
}

const void* pinvoke_override(const char* library_name, const char* entry_point_name)
{
    if (strcmp(library_name, "libSystem.Native") == 0)
    {
        return SystemResolveDllImport(entry_point_name);
    }
    if (strcmp(library_name, "libBrowser.Native") == 0)
    {
        return BrowserResolveDllImport(entry_point_name);
    }
    if (strcmp(library_name, "libSystem.Globalization.Native") == 0)
    {
        return GlobalizationResolveDllImport(entry_point_name);
    }
    if (strcmp(library_name, "libSystem.IO.Compression.Native") == 0)
    {
        return CompressionResolveDllImport(entry_point_name);
    }

    return nullptr;
}

static int run()
{
    pal::string_t exe_path;
    pal::getenv("CWD", &exe_path);
    const pal::string_t app_domain_name = "corehost";

    // Set base initialization properties.
    std::vector<const char*> propertyKeys;
    std::vector<const char*> propertyValues;

    propertyKeys.push_back("TRUSTED_PLATFORM_ASSEMBLIES");
    propertyValues.push_back("./HelloWorld.dll:./System.Private.CoreLib.dll:./System.Runtime.dll:./System.Console.dll:./System.Threading.dll:./System.Runtime.InteropServices.dll");
    propertyKeys.push_back("NATIVE_DLL_SEARCH_DIRECTORIES");
    propertyValues.push_back(exe_path.c_str());


    host_runtime_contract host_contract = { sizeof(host_runtime_contract), nullptr };
    host_contract.bundle_probe = &bundle_probe;
    host_contract.pinvoke_override = &pinvoke_override;
    host_contract.get_runtime_property = &get_runtime_property;
    host_contract.external_assembly_probe = &external_assembly_probe;

    pal::char_t buffer[STRING_LENGTH("0xffffffffffffffff")];
    pal::snwprintf(buffer, ARRAY_SIZE(buffer), _X("0x%zx"), (size_t)(&host_contract));

    propertyKeys.push_back(HOST_PROPERTY_RUNTIME_CONTRACT);
    propertyValues.push_back(buffer);

    coreclr_set_error_writer(log_error_info);

    printf("BEGIN: call coreclr_initialize\n");
    int retval = coreclr_initialize(exe_path.c_str(), app_domain_name.c_str(), 1, propertyKeys.data(), propertyValues.data(), &CurrentClrInstance, &CurrentAppDomainId);
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
