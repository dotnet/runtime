// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __HOST_CONTEXT_H__
#define __HOST_CONTEXT_H__

#include <pal.h>

#include <corehost_context_contract.h>
#include "corehost_init.h"
#include <hostfxr.h>
#include "hostpolicy_resolver.h"

enum class host_context_type
{
    empty,        // Not populated, cannot be used for context-based operations
    initialized,  // Created, but not active (runtime not loaded)
    active,       // Runtime loaded for this context
    secondary,    // Created after runtime was loaded using another context
    invalid,      // Failed on loading runtime
};

struct host_context_t
{
public: // static
    static int create(
        const hostpolicy_contract_t &hostpolicy_contract,
        corehost_init_t &init,
        int32_t initialization_options,
        /*out*/ std::unique_ptr<host_context_t> &context);
    static int create_secondary(
        const hostpolicy_contract_t &hostpolicy_contract,
        std::unordered_map<pal::string_t, pal::string_t> &config_properties,
        int32_t initialization_options,
        /*out*/ std::unique_ptr<host_context_t> &context);
    static host_context_t* from_handle(const hostfxr_handle handle, bool allow_invalid_type = false);

public:
    int32_t marker; // used as an indication for validity

    host_context_type type;
    const hostpolicy_contract_t hostpolicy_contract;
    const corehost_context_contract hostpolicy_context_contract;

    // Whether or not the context was initialized for an app. argv will be empty for non-app contexts.
    bool is_app;
    std::vector<pal::string_t> argv;

    // Frameworks used for active context
    std::unordered_map<pal::string_t, const fx_ver_t> fx_versions_by_name;

    // Config properties for secondary contexts
    std::unordered_map<pal::string_t, pal::string_t> config_properties;

    host_context_t(
        host_context_type type,
        const hostpolicy_contract_t &hostpolicy_contract,
        const corehost_context_contract &hostpolicy_context_contract);

    void close();
};

#endif // __HOST_CONTEXT_H__