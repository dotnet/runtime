// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cstdio>
#include <vector>

#include <pal.h>
#include <minipal/utils.h>
#include <host_runtime_contract.h>

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
    const void* SystemJSResolveDllImport(const char* name);
    const void* SystemJSInteropResolveDllImport(const char* name);
    const void* GlobalizationResolveDllImport(const char* name);
    const void* CompressionResolveDllImport(const char* name);

    bool BrowserHost_ExternalAssemblyProbe(const char* pathPtr, /*out*/ void **outDataStartPtr, /*out*/ int64_t* outSize);
    void BrowserHost_ResolveMain(int exitCode);
    void BrowserHost_RejectMain(const char *reason);
}

// The current CoreCLR instance details.
static void* CurrentClrInstance;
static unsigned int CurrentAppDomainId;

static void log_error_info(const char* line)
{
    std::fprintf(stderr, "log error: %s\n", line);
}

static const void* pinvoke_override(const char* library_name, const char* entry_point_name)
{
    if (strcmp(library_name, "libSystem.Native") == 0)
    {
        return SystemResolveDllImport(entry_point_name);
    }
    if (strcmp(library_name, "libSystem.JavaScript") == 0)
    {
        return SystemJSResolveDllImport(entry_point_name);
    }
    if (strcmp(library_name, "libSystem.Runtime.InteropServices.JavaScript") == 0)
    {
        return SystemJSInteropResolveDllImport(entry_point_name);
    }
    if (strcmp(library_name, "libSystem.IO.Compression.Native") == 0)
    {
        return CompressionResolveDllImport(entry_point_name);
    }
    // duplicates https://github.com/dotnet/runtime/blob/7a33b4bb6ced097f081b1eeab575cfb1c8c88bb5/src/coreclr/vm/pinvokeoverride.cpp#L21-L36 for clarity
    if (strcmp(library_name, "libSystem.Globalization.Native") == 0)
    {
        return GlobalizationResolveDllImport(entry_point_name);
    }

    return nullptr;
}

static pal::string_t app_path;
static pal::string_t search_paths;
static pal::string_t tpa;
static const pal::string_t app_domain_name = "corehost";
static const pal::string_t exe_path = "/managed";
static std::vector<const char*> propertyKeys;
static std::vector<const char*> propertyValues;
static pal::char_t ptr_to_string_buffer[STRING_LENGTH("0xffffffffffffffff") + 1];

// WASM-TODO: pass TPA via argument, not env
// WASM-TODO: pass app_path via argument, not env
// WASM-TODO: pass search_paths via argument, not env
extern "C" int BrowserHost_InitializeCoreCLR(void)
{
    pal::getenv(HOST_PROPERTY_APP_PATHS, &app_path);
    pal::getenv(HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES, &search_paths);
    pal::getenv(HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES, &tpa);

    // Set base initialization properties.
    propertyKeys.push_back(HOST_PROPERTY_APP_PATHS);
    propertyValues.push_back(app_path.c_str());
    propertyKeys.push_back(HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES);
    propertyValues.push_back(search_paths.c_str());
    propertyKeys.push_back(HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES);
    propertyValues.push_back(tpa.c_str());

    host_runtime_contract host_contract = { sizeof(host_runtime_contract), nullptr };
    host_contract.pinvoke_override = &pinvoke_override;
    host_contract.external_assembly_probe = &BrowserHost_ExternalAssemblyProbe;

    pal::snwprintf(ptr_to_string_buffer, ARRAY_SIZE(ptr_to_string_buffer), _X("0x%zx"), (size_t)(&host_contract));

    propertyKeys.push_back(HOST_PROPERTY_RUNTIME_CONTRACT);
    propertyValues.push_back(ptr_to_string_buffer);

    coreclr_set_error_writer(log_error_info);

    int retval = coreclr_initialize(exe_path.c_str(), app_domain_name.c_str(), (int)propertyKeys.size(), propertyKeys.data(), propertyValues.data(), &CurrentClrInstance, &CurrentAppDomainId);

    if (retval < 0)
    {
        std::fprintf(stderr, "coreclr_initialize failed - Error: 0x%08x\n", retval);
        return -1;
    }
    return 0;
}

// WASM-TODO: browser needs async entrypoint
// WASM-TODO: don't coreclr_shutdown_2 when browser
extern "C" int BrowserHost_ExecuteAssembly(const char* assemblyPath)
{
    int exit_code;
    int retval = coreclr_execute_assembly(CurrentClrInstance, CurrentAppDomainId, 0, nullptr, assemblyPath, (uint32_t*)&exit_code);

    if (retval < 0)
    {
        std::fprintf(stderr, "coreclr_execute_assembly failed - Error: 0x%08x\n", retval);
        return -1;
    }

    int latched_exit_code = 0;

    retval = coreclr_shutdown_2(CurrentClrInstance, CurrentAppDomainId, &latched_exit_code);

    if (retval < 0)
    {
        std::fprintf(stderr, "coreclr_shutdown_2 failed - Error: 0x%08x\n", retval);
        exit_code = -1;
        // WASM-TODO: this is too trivial
        BrowserHost_RejectMain("coreclr_shutdown_2 failed");
    }

    // WASM-TODO: this is too trivial
    // because nothing runs continuations yet and also coreclr_execute_assembly is sync looping
    BrowserHost_ResolveMain(exit_code);
    return retval;
}
