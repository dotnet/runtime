// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef _COREHOST_CLI_CORECLR_H_
#define _COREHOST_CLI_CORECLR_H_

#include "pal.h"
#include "trace.h"
#include <atomic>
#include <cstdint>
#include <memory>
#include <vector>

class coreclr_property_bag_t;

class coreclr_t
{
public: // static
    static pal::hresult_t create(
        const pal::string_t& libcoreclr_path,
        const char* exe_path,
        const char* app_domain_friendly_name,
        coreclr_property_bag_t &properties,
        std::unique_ptr<coreclr_t> &inst);

public:
    using host_handle_t = void*;
    using domain_id_t = std::uint32_t;

    coreclr_t(host_handle_t host_handle, domain_id_t domain_id);
    ~coreclr_t();

    pal::hresult_t execute_assembly(
        int argc,
        const char** argv,
        const char* managed_assembly_path,
        unsigned int* exit_code);

    pal::hresult_t create_delegate(
        const char* entryPointAssemblyName,
        const char* entryPointTypeName,
        const char* entryPointMethodName,
        void** delegate);

    pal::hresult_t shutdown(int* latchedExitCode);

private:
    std::atomic_bool _is_shutdown;
    host_handle_t _host_handle;
    domain_id_t _domain_id;
};

enum class common_property
{
    TrustedPlatformAssemblies,
    NativeDllSearchDirectories,
    PlatformResourceRoots,
    AppDomainCompatSwitch,
    AppContextBaseDirectory,
    AppContextDepsFiles,
    FxDepsFile,
    ProbingDirectories,
    FxProductVersion,
    JitPath,
    StartUpHooks,
    AppPaths,
    AppNIPaths,

    // Sentinel value - new values should be defined above
    Last
};

class coreclr_property_bag_t
{
public:
    coreclr_property_bag_t();

    void add(common_property key, const char *value);

    void add(const char *key, const char *value);

    bool try_get(common_property key, const char **value);

    bool try_get(const char *key, const char **value);

    void log_properties();

public:
    int count();

    const char** keys();

    const char** values();

private:
    std::vector<const char*> _keys;
    std::vector<const char*> _values;
};

#endif // _COREHOST_CLI_CORECLR_H_
