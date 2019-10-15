// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __COREHOST_CONTEXT_CONTRACT_H__
#define __COREHOST_CONTEXT_CONTRACT_H__

#include "host_interface.h"
#include "hostpolicy.h"
#include <pal.h>

enum intialization_options_t : int32_t
{
    none = 0x0,
    wait_for_initialized = 0x1,  // Wait until initialization through a different request is completed
    get_contract = 0x2,          // Get the contract for the initialized hostpolicy
};

// Delegates for these types will have the stdcall calling convention unless otherwise specified
enum class coreclr_delegate_type
{
    invalid,
    com_activation,
    load_in_memory_assembly,
    winrt_activation,
    com_register,
    com_unregister,
    load_assembly_and_get_function_pointer
};

#pragma pack(push, _HOST_INTERFACE_PACK)
struct corehost_initialize_request_t
{
    size_t version;
    strarr_t config_keys;
    strarr_t config_values;
};
static_assert(offsetof(corehost_initialize_request_t, version) == 0 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(corehost_initialize_request_t, config_keys) == 1 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(corehost_initialize_request_t, config_values) == 3 * sizeof(size_t), "Struct offset breaks backwards compatibility");

struct corehost_context_contract
{
    size_t version;
    int (HOSTPOLICY_CALLTYPE *get_property_value)(
        const pal::char_t *key,
        /*out*/ const pal::char_t **value);
    int (HOSTPOLICY_CALLTYPE *set_property_value)(
        const pal::char_t *key,
        const pal::char_t *value);
    int (HOSTPOLICY_CALLTYPE *get_properties)(
        /*inout*/ size_t *count,
        /*out*/ const pal::char_t **keys,
        /*out*/ const pal::char_t **values);
    int (HOSTPOLICY_CALLTYPE *load_runtime)();
    int (HOSTPOLICY_CALLTYPE *run_app)(
        const int argc,
        const pal::char_t **argv);
    int (HOSTPOLICY_CALLTYPE *get_runtime_delegate)(
        coreclr_delegate_type type,
        /*out*/ void **delegate);
};
static_assert(offsetof(corehost_context_contract, version) == 0 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(corehost_context_contract, get_property_value) == 1 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(corehost_context_contract, set_property_value) == 2 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(corehost_context_contract, get_properties) == 3 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(corehost_context_contract, load_runtime) == 4 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(corehost_context_contract, run_app) == 5 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(corehost_context_contract, get_runtime_delegate) == 6 * sizeof(size_t), "Struct offset breaks backwards compatibility");
#pragma pack(pop)

#endif // __COREHOST_CONTEXT_CONTRACT_H__