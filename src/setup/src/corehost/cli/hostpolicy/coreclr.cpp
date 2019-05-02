// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <cassert>

#include "coreclr.h"
#include <utils.h>
#include <error_codes.h>

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

        coreclr_initialize = reinterpret_cast<coreclr_initialize_fn>(pal::get_symbol(g_coreclr, "coreclr_initialize"));
        coreclr_shutdown = reinterpret_cast<coreclr_shutdown_fn>(pal::get_symbol(g_coreclr, "coreclr_shutdown_2"));
        coreclr_execute_assembly = reinterpret_cast<coreclr_execute_assembly_fn>(pal::get_symbol(g_coreclr, "coreclr_execute_assembly"));
        coreclr_create_delegate = reinterpret_cast<coreclr_create_delegate_fn>(pal::get_symbol(g_coreclr, "coreclr_create_delegate"));

        assert(coreclr_initialize != nullptr
            && coreclr_shutdown != nullptr
            && coreclr_execute_assembly != nullptr
            && coreclr_create_delegate != nullptr);

        return true;
    }
}

pal::hresult_t coreclr_t::create(
    const pal::string_t& libcoreclr_path,
    const char* exe_path,
    const char* app_domain_friendly_name,
    const coreclr_property_bag_t &properties,
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

    int propertyCount = properties.count();
    std::vector<std::vector<char>> keys_strs(propertyCount);
    std::vector<const char*> keys(propertyCount);
    std::vector<std::vector<char>> values_strs(propertyCount);
    std::vector<const char*> values(propertyCount);
    int index = 0;
    std::function<void (const pal::string_t &,const pal::string_t &)> callback = [&] (const pal::string_t& key, const pal::string_t& value)
    {
        pal::pal_clrstring(key, &keys_strs[index]);
        keys[index] = keys_strs[index].data();
        pal::pal_clrstring(value, &values_strs[index]);
        values[index] = values_strs[index].data();
        ++index;
    };
    properties.enumerate(callback);

    pal::hresult_t hr;
    hr = coreclr_initialize(
        exe_path,
        app_domain_friendly_name,
        propertyCount,
        keys.data(),
        values.data(),
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
    const pal::char_t *PropertyNameMapping[] =
    {
        _X("TRUSTED_PLATFORM_ASSEMBLIES"),
        _X("NATIVE_DLL_SEARCH_DIRECTORIES"),
        _X("PLATFORM_RESOURCE_ROOTS"),
        _X("AppDomainCompatSwitch"),
        _X("APP_CONTEXT_BASE_DIRECTORY"),
        _X("APP_CONTEXT_DEPS_FILES"),
        _X("FX_DEPS_FILE"),
        _X("PROBING_DIRECTORIES"),
        _X("FX_PRODUCT_VERSION"),
        _X("JIT_PATH"),
        _X("STARTUP_HOOKS"),
        _X("APP_PATHS"),
        _X("APP_NI_PATHS")
    };

    static_assert((sizeof(PropertyNameMapping) / sizeof(*PropertyNameMapping)) == static_cast<size_t>(common_property::Last), "Invalid property count");
}

const pal::char_t* coreclr_property_bag_t::common_property_to_string(common_property key)
{
    int idx = static_cast<int>(key);
    assert(0 <= idx && idx < static_cast<int>(common_property::Last));

    return PropertyNameMapping[idx];
}

coreclr_property_bag_t::coreclr_property_bag_t()
{
    // Optimize the bag for at least twice as many common properties.
    const size_t init_size = 2 * static_cast<size_t>(common_property::Last);
    _properties.reserve(init_size);
}

bool coreclr_property_bag_t::add(common_property key, const pal::char_t *value)
{
    int idx = static_cast<int>(key);
    assert(0 <= idx && idx < static_cast<int>(common_property::Last));

    return add(PropertyNameMapping[idx], value);
}

bool coreclr_property_bag_t::add(const pal::char_t *key, const pal::char_t *value)
{
    if (key == nullptr || value == nullptr)
        return false;

    auto iter = _properties.find(key);
    if (iter == _properties.cend())
    {
        _properties.emplace(key, value);
        return true;
    }
    else
    {
        trace::verbose(_X("Overwriting property %s. New value: '%s'. Old value: '%s'."), key, value, (*iter).second.c_str());
        _properties[key] = value;
        return false;
    }
}

bool coreclr_property_bag_t::try_get(common_property key, const pal::char_t **value) const
{
    int idx = static_cast<int>(key);
    assert(0 <= idx && idx < static_cast<int>(common_property::Last));

    return try_get(PropertyNameMapping[idx], value);
}

bool coreclr_property_bag_t::try_get(const pal::char_t *key, const pal::char_t **value) const
{
    assert(key != nullptr && value != nullptr);
    auto iter = _properties.find(key);
    if (iter == _properties.cend())
        return false;

    *value = (*iter).second.c_str();
    return true;
}

void coreclr_property_bag_t::remove(const pal::char_t *key)
{
    if (key == nullptr)
        return;

    auto iter = _properties.find(key);
    if (iter == _properties.cend())
        return;

    trace::verbose(_X("Removing property %s. Old value: '%s'."), key, (*iter).second.c_str());
    _properties.erase(iter);
}

void coreclr_property_bag_t::log_properties() const
{
    for (auto &kv : _properties)
        trace::verbose(_X("Property %s = %s"), kv.first.c_str(), kv.second.c_str());
}

int coreclr_property_bag_t::count() const
{
    return static_cast<int>(_properties.size());
}

void coreclr_property_bag_t::enumerate(std::function<void(const pal::string_t&, const pal::string_t&)> &callback) const
{
    for (auto &kv : _properties)
        callback(kv.first, kv.second);
}
