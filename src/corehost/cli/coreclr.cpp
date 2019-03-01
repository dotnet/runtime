// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>

#include "coreclr.h"
#include "utils.h"
#include "error_codes.h"

// Prototype of the coreclr_initialize function from coreclr.dll
using coreclr_initialize_fn = pal::hresult_t(STDMETHODCALLTYPE *)(
    const char* exePath,
    const char* appDomainFriendlyName,
    int propertyCount,
    const char** propertyKeys,
    const char** propertyValues,
    coreclr_t::host_handle_t* hostHandle,
    unsigned int* domainId);

// Prototype of the coreclr_shutdown function from coreclr.dll
using coreclr_shutdown_fn = pal::hresult_t(STDMETHODCALLTYPE *)(
    coreclr_t::host_handle_t hostHandle,
    unsigned int domainId,
    int* latchedExitCode);

// Prototype of the coreclr_execute_assembly function from coreclr.dll
using coreclr_execute_assembly_fn = pal::hresult_t(STDMETHODCALLTYPE *)(
    coreclr_t::host_handle_t hostHandle,
    unsigned int domainId,
    int argc,
    const char** argv,
    const char* managedAssemblyPath,
    unsigned int* exitCode);

// Prototype of the coreclr_create_delegate function from coreclr.dll
using coreclr_create_delegate_fn = pal::hresult_t(STDMETHODCALLTYPE *)(
    coreclr_t::host_handle_t hostHandle,
    unsigned int domainId,
    const char* entryPointAssemblyName,
    const char* entryPointTypeName,
    const char* entryPointMethodName,
    void** delegate);

namespace
{
    pal::dll_t g_coreclr = nullptr;
    coreclr_shutdown_fn coreclr_shutdown = nullptr;
    coreclr_initialize_fn coreclr_initialize = nullptr;
    coreclr_execute_assembly_fn coreclr_execute_assembly = nullptr;
    coreclr_create_delegate_fn coreclr_create_delegate = nullptr;

    bool coreclr_bind(const pal::string_t& libcoreclr_path)
    {
        assert(g_coreclr == nullptr);

        pal::string_t coreclr_dll_path(libcoreclr_path);
        append_path(&coreclr_dll_path, LIBCORECLR_NAME);

        if (!pal::load_library(&coreclr_dll_path, &g_coreclr))
        {
            return false;
        }

        coreclr_initialize = (coreclr_initialize_fn)pal::get_symbol(g_coreclr, "coreclr_initialize");
        coreclr_shutdown = (coreclr_shutdown_fn)pal::get_symbol(g_coreclr, "coreclr_shutdown_2");
        coreclr_execute_assembly = (coreclr_execute_assembly_fn)pal::get_symbol(g_coreclr, "coreclr_execute_assembly");
        coreclr_create_delegate = (coreclr_create_delegate_fn)pal::get_symbol(g_coreclr, "coreclr_create_delegate");

        assert(coreclr_initialize != nullptr
            && coreclr_shutdown != nullptr
            && coreclr_execute_assembly != nullptr
            && coreclr_create_delegate != nullptr);

        return true;
    }
    
    void coreclr_unload()
    {
        assert(g_coreclr != nullptr && coreclr_initialize != nullptr);

        // [TODO] Unloading coreclr is not presently supported
        // pal::unload_library(g_coreclr);
    }
}

pal::hresult_t coreclr_t::create(
    const pal::string_t& libcoreclr_path,
    const char* exe_path,
    const char* app_domain_friendly_name,
    coreclr_property_bag_t &properties,
    std::unique_ptr<coreclr_t> &inst)
{
    if (!coreclr_bind(libcoreclr_path))
    {
        trace::error(_X("Failed to bind to CoreCLR at '%s'"), libcoreclr_path.c_str());
        return StatusCode::CoreClrBindFailure;
    }

    assert(g_coreclr != nullptr && coreclr_initialize != nullptr);

    host_handle_t host_handle;
    domain_id_t domain_id;

    pal::hresult_t hr;
    hr = coreclr_initialize(
        exe_path,
        app_domain_friendly_name,
        properties.count(),
        properties.keys(),
        properties.values(),
        &host_handle,
        &domain_id);

    if (!SUCCEEDED(hr))
        return hr;

    inst.reset(new coreclr_t(host_handle, domain_id));
    return StatusCode::Success;
}

coreclr_t::coreclr_t(host_handle_t host_handle, domain_id_t domain_id)
    : _is_shutdown{ false }
    , _host_handle{ host_handle }
    , _domain_id{ domain_id }
{
}

coreclr_t::~coreclr_t()
{
    (void)shutdown(nullptr);
    coreclr_unload();
}

pal::hresult_t coreclr_t::execute_assembly(
    int argc,
    const char** argv,
    const char* managed_assembly_path,
    unsigned int* exit_code)
{
    assert(g_coreclr != nullptr && coreclr_execute_assembly != nullptr);

    return coreclr_execute_assembly(
        _host_handle,
        _domain_id,
        argc,
        argv,
        managed_assembly_path,
        exit_code);
}

pal::hresult_t coreclr_t::create_delegate(
    const char* entryPointAssemblyName,
    const char* entryPointTypeName,
    const char* entryPointMethodName,
    void** delegate)
{
    assert(g_coreclr != nullptr && coreclr_execute_assembly != nullptr);

    return coreclr_create_delegate(
        _host_handle,
        _domain_id,
        entryPointAssemblyName,
        entryPointTypeName,
        entryPointMethodName,
        delegate);
}

pal::hresult_t coreclr_t::shutdown(int* latchedExitCode)
{
    assert(g_coreclr != nullptr && coreclr_shutdown != nullptr);

    std::lock_guard<std::mutex> lock{ _shutdown_lock };

    // If already shut down return success since the result
    // has already been reported to a previous caller.
    if (_is_shutdown)
    {
        if (latchedExitCode != nullptr)
            *latchedExitCode = StatusCode::Success;

        return StatusCode::Success;
    }

    _is_shutdown = true;
    return coreclr_shutdown(_host_handle, _domain_id, latchedExitCode);
}

namespace
{
    const char *PropertyNameMapping[] =
    {
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "NATIVE_DLL_SEARCH_DIRECTORIES",
        "PLATFORM_RESOURCE_ROOTS",
        "AppDomainCompatSwitch",
        "APP_CONTEXT_BASE_DIRECTORY",
        "APP_CONTEXT_DEPS_FILES",
        "FX_DEPS_FILE",
        "PROBING_DIRECTORIES",
        "FX_PRODUCT_VERSION",
        "JIT_PATH",
        "STARTUP_HOOKS",
        "APP_PATHS",
        "APP_NI_PATHS"
    };

    static_assert((sizeof(PropertyNameMapping) / sizeof(*PropertyNameMapping)) == static_cast<size_t>(common_property::Last), "Invalid property count");
}

coreclr_property_bag_t::coreclr_property_bag_t()
{
    // Optimize the bag for at least twice as many common properties.
    const size_t init_size = 2 * static_cast<size_t>(common_property::Last);
    _keys.reserve(init_size);
    _values.reserve(init_size);
}

void coreclr_property_bag_t::add(common_property key, const char *value)
{
    int idx = static_cast<int>(key);
    assert(0 <= idx && idx < static_cast<int>(common_property::Last));

    add(PropertyNameMapping[idx], value);
}

void coreclr_property_bag_t::add(const char *key, const char *value)
{
    if (key == nullptr || value == nullptr)
        return;

    assert(_keys.size() == _values.size());
    _keys.push_back(key);
    _values.push_back(value);
}

bool coreclr_property_bag_t::try_get(common_property key, const char **value)
{
    int idx = static_cast<int>(key);
    assert(0 <= idx && idx < static_cast<int>(common_property::Last));

    return try_get(PropertyNameMapping[idx], value);
}

bool coreclr_property_bag_t::try_get(const char *key, const char **value)
{
    assert(key != nullptr && value != nullptr);
    for (int i = 0; i < count(); ++i)
    {
        if (0 == pal::cstrcasecmp(_keys[i], key))
        {
            *value = _values[i];
            return true;
        }
    }

    return false;
}

void coreclr_property_bag_t::log_properties()
{
    for (int i = 0; i < count(); ++i)
    {
        pal::string_t key, val;
        pal::clr_palstring(_keys[i], &key);
        pal::clr_palstring(_values[i], &val);
        trace::verbose(_X("Property %s = %s"), key.c_str(), val.c_str());
    }
}

int coreclr_property_bag_t::count()
{
    assert(_keys.size() == _values.size());
    return static_cast<int>(_keys.size());
}

const char** coreclr_property_bag_t::keys()
{
    return _keys.data();
}

const char** coreclr_property_bag_t::values()
{
    return _values.data();
}
