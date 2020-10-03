// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cassert>

#include "coreclr.h"
#include "coreclr_resolver.h"
#include <utils.h>
#include <error_codes.h>

namespace
{
    coreclr_resolver_contract_t coreclr_contract;

    bool coreclr_bind(const pal::string_t& libcoreclr_path)
    {
        assert(coreclr_contract.coreclr_initialize == nullptr);
        coreclr_resolver_t::resolve_coreclr(libcoreclr_path, coreclr_contract);
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

    assert(coreclr_contract.coreclr_initialize != nullptr);

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
    hr = coreclr_contract.coreclr_initialize(
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
    assert(coreclr_contract.coreclr_execute_assembly != nullptr);

    return coreclr_contract.coreclr_execute_assembly(
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
    assert(coreclr_contract.coreclr_execute_assembly != nullptr);

    return coreclr_contract.coreclr_create_delegate(
        _host_handle,
        _domain_id,
        entryPointAssemblyName,
        entryPointTypeName,
        entryPointMethodName,
        delegate);
}

pal::hresult_t coreclr_t::shutdown(int* latchedExitCode)
{
    assert(coreclr_contract.coreclr_shutdown != nullptr);

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
    return coreclr_contract.coreclr_shutdown(_host_handle, _domain_id, latchedExitCode);
}

namespace
{
    const pal::char_t *PropertyNameMapping[] =
    {
        _X("TRUSTED_PLATFORM_ASSEMBLIES"),
        _X("NATIVE_DLL_SEARCH_DIRECTORIES"),
        _X("PLATFORM_RESOURCE_ROOTS"),
        _X("APP_CONTEXT_BASE_DIRECTORY"),
        _X("APP_CONTEXT_DEPS_FILES"),
        _X("FX_DEPS_FILE"),
        _X("PROBING_DIRECTORIES"),
        _X("STARTUP_HOOKS"),
        _X("APP_PATHS"),
        _X("APP_NI_PATHS"),
        _X("RUNTIME_IDENTIFIER"),
        _X("BUNDLE_PROBE"),
        _X("HOSTPOLICY_EMBEDDED")
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
