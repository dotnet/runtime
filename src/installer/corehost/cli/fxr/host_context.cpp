// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "host_context.h"
#include <trace.h>

namespace
{
    const int32_t valid_host_context_marker = 0xabababab;
    const int32_t closed_host_context_marker = 0xcdcdcdcd;

    int create_context_common(
        const hostpolicy_contract_t &hostpolicy_contract,
        const host_interface_t *host_interface,
        const corehost_initialize_request_t *init_request,
        uint32_t initialization_options,
        bool already_loaded,
        /*out*/ corehost_context_contract *hostpolicy_context_contract)
    {
        if (hostpolicy_contract.initialize == nullptr)
        {
            trace::error(_X("This component must target .NET Core 3.0 or a higher version."));
            return StatusCode::HostApiUnsupportedVersion;
        }

        int rc = StatusCode::Success;
        {
            propagate_error_writer_t propagate_error_writer_to_corehost(hostpolicy_contract.set_error_writer);
            if (!already_loaded)
            {
                assert (host_interface != nullptr);
                rc = hostpolicy_contract.load(host_interface);
            }

            if (rc == StatusCode::Success)
            {
                initialization_options |= initialization_options_t::context_contract_version_set;
                hostpolicy_context_contract->version = sizeof(corehost_context_contract);
                rc = hostpolicy_contract.initialize(init_request, initialization_options, hostpolicy_context_contract);
            }
        }

        return rc;
    }
}

int host_context_t::create(
    const hostpolicy_contract_t &hostpolicy_contract,
    corehost_init_t &init,
    uint32_t initialization_options,
    /*out*/ std::unique_ptr<host_context_t> &context)
{
    const host_interface_t &host_interface = init.get_host_init_data();
    corehost_context_contract hostpolicy_context_contract = {};
    int rc = create_context_common(hostpolicy_contract, &host_interface, nullptr, initialization_options, /*already_loaded*/ false, &hostpolicy_context_contract);
    if (rc == StatusCode::Success)
    {
        std::unique_ptr<host_context_t> context_local(new host_context_t(host_context_type::initialized, hostpolicy_contract, hostpolicy_context_contract));
        context_local->initialize_frameworks(init);
        context = std::move(context_local);
    }

    return rc;
}

int host_context_t::create_secondary(
    const hostpolicy_contract_t &hostpolicy_contract,
    std::unordered_map<pal::string_t, pal::string_t> &config_properties,
    uint32_t initialization_options,
    /*out*/ std::unique_ptr<host_context_t> &context)
{
    std::vector<const pal::char_t*> config_keys;
    std::vector<const pal::char_t*> config_values;
    for (auto &kv : config_properties)
    {
        config_keys.push_back(kv.first.c_str());
        config_values.push_back(kv.second.c_str());
    }

    corehost_initialize_request_t init_request;
    init_request.version = sizeof(corehost_initialize_request_t);
    init_request.config_keys.len = config_keys.size();
    init_request.config_keys.arr = config_keys.data();
    init_request.config_values.len = config_values.size();
    init_request.config_values.arr = config_values.data();

    corehost_context_contract hostpolicy_context_contract = {};
    int rc = create_context_common(hostpolicy_contract, nullptr, &init_request, initialization_options, /*already_loaded*/ true, &hostpolicy_context_contract);
    if (STATUS_CODE_SUCCEEDED(rc))
    {
        std::unique_ptr<host_context_t> context_local(new host_context_t(host_context_type::secondary, hostpolicy_contract, hostpolicy_context_contract));
        context_local->config_properties = config_properties;
        context = std::move(context_local);
    }

    assert(rc != StatusCode::Success);
    return rc;
}

host_context_t* host_context_t::from_handle(const hostfxr_handle handle, bool allow_invalid_type)
{
    if (handle == nullptr)
        return nullptr;

    host_context_t *context = static_cast<host_context_t*>(handle);
    int32_t marker = context->marker;
    if (marker == valid_host_context_marker)
    {
        if (allow_invalid_type || context->type != host_context_type::invalid)
            return context;

        trace::error(_X("Host context is in an invalid state"));
    }
    else if (marker == closed_host_context_marker)
    {
        trace::error(_X("Host context has already been closed"));
    }
    else
    {
        trace::error(_X("Invalid host context handle marker: 0x%x"), marker);
    }

    return nullptr;
}

host_context_t::host_context_t(
    host_context_type type,
    const hostpolicy_contract_t &hostpolicy_contract,
    const corehost_context_contract &hostpolicy_context_contract)
    : marker { valid_host_context_marker }
    , type { type }
    , hostpolicy_contract { hostpolicy_contract }
    , hostpolicy_context_contract { hostpolicy_context_contract }
{ }

void host_context_t::initialize_frameworks(const corehost_init_t& init)
{
    init.get_found_fx_versions(fx_versions_by_name);
    init.get_included_frameworks(included_fx_versions_by_name);
}

void host_context_t::close()
{
    marker = closed_host_context_marker;
}
