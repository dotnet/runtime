// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FX_RESOLVER_H__
#define __FX_RESOLVER_H__

#include "error_codes.h"
#include "fx_reference.h"
#include "fx_definition.h"

class runtime_config_t;
struct host_startup_info_t;

class fx_resolver_t
{
public:
    static StatusCode resolve_frameworks_for_app(
        const host_startup_info_t& host_info,
        const fx_reference_t& override_settings,
        const runtime_config_t& app_config,
        fx_definition_vector_t& fx_definitions);

private:
    fx_resolver_t();

    StatusCode soft_roll_forward_helper(
        const fx_reference_t& newer,
        const fx_reference_t& older,
        bool older_is_hard_roll_forward);
    StatusCode soft_roll_forward(
        const fx_reference_t existing_ref,
        bool current_is_hard_roll_forward);
    StatusCode read_framework(
        const host_startup_info_t& host_info,
        const fx_reference_t& override_settings,
        const runtime_config_t& config,
        fx_definition_vector_t& fx_definitions);

    static void display_missing_framework_error(
        const pal::string_t& fx_name,
        const pal::string_t& fx_version,
        const pal::string_t& fx_dir,
        const pal::string_t& dotnet_root);
    static void display_incompatible_framework_error(
        const pal::string_t& higher,
        const fx_reference_t& lower);
    static void display_compatible_framework_trace(
        const pal::string_t& higher,
        const fx_reference_t& lower);
    static void display_retry_framework_trace(
        const fx_reference_t& fx_existing,
        const fx_reference_t& fx_new);
    static void display_summary_of_frameworks(
        const fx_definition_vector_t& fx_definitions,
        const fx_name_to_fx_reference_map_t& newest_references);

    // Map of FX Name -> FX Reference of the most up-to-date resolved references so far. This map is keeping the state
    // of the resolution algorithm. For each framework it holds the highest version referenced and the merged
    // roll-forward settings. If the reference has been resolved against the frameworks on disk, this map will hold
    // the version of the actually resolved version on the disk (so called hard-roll-forward).
    fx_name_to_fx_reference_map_t m_newest_references;

    // Map of FX Name -> FX Reference of the oldest reference found for the framework yet. This map is only used
    // to fill the "oldest reference" for each resolved framework in the end. It does not affect the behavior
    // of the algorithm.
    fx_name_to_fx_reference_map_t m_oldest_references;
};

#endif // __FX_RESOLVER_H__