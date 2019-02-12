// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

class corehost_init_t;
class runtime_config_t;
class fx_definition_t; 
struct fx_ver_t;
struct host_startup_info_t;

#include <corehost.h>
#include "libhost.h"

const int Max_Framework_Resolve_Retries = 100;

class fx_muxer_t
{
public:
    static int execute(
        const pal::string_t host_command,
        const int argc,
        const pal::char_t* argv[],
        const host_startup_info_t& host_info,
        pal::char_t result_buffer[],
        int32_t buffer_size,
        int32_t* required_buffer_size);
    static int get_com_activation_delegate(
        const host_startup_info_t &host_info,
        void **delegate);
private:
    static int parse_args(
        const host_startup_info_t& host_info,
        int argoff,
        int argc,
        const pal::char_t* argv[],
        bool exec_mode,
        host_mode_t mode,
        int* new_argoff,
        pal::string_t& app_candidate,
        opt_map_t& opts);
    static int handle_exec(
        const host_startup_info_t& host_info,
        const pal::string_t& app_candidate,
        const opt_map_t& opts,
        int argc,
        const pal::char_t* argv[],
        int argoff,
        host_mode_t mode);
    static int handle_exec_host_command(
        const pal::string_t& host_command,
        const host_startup_info_t& host_info,
        const pal::string_t& app_candidate,
        const opt_map_t& opts,
        int argc,
        const pal::char_t* argv[],
        int argoff,
        host_mode_t mode,
        pal::char_t result_buffer[],
        int32_t buffer_size,
        int32_t* required_buffer_size);
    static int handle_cli(
        const host_startup_info_t& host_info,
        int argc,
        const pal::char_t* argv[]);
    static std::vector<host_option> get_known_opts(
        bool exec_mode,
        host_mode_t mode,
        bool get_all_options = false);
    static int read_config_and_execute(
        const pal::string_t& host_command,
        const host_startup_info_t& host_info,
        const pal::string_t& app_candidate,
        const opt_map_t& opts,
        int new_argc,
        const pal::char_t** new_argv,
        host_mode_t mode,
        pal::char_t out_buffer[],
        int32_t buffer_size,
        int32_t* required_buffer_size);
    static bool resolve_hostpolicy_dir(
        host_mode_t mode,
        const pal::string_t& dotnet_root,
        const fx_definition_vector_t& fx_definitions,
        const pal::string_t& app_candidate,
        const pal::string_t& specified_deps_file,
        const std::vector<pal::string_t>& probe_realpaths,
        pal::string_t* impl_dir);
    static fx_ver_t resolve_framework_version(
        const std::vector<fx_ver_t>& version_list,
        const pal::string_t& fx_ver,
        const fx_ver_t& specified,
        bool patch_roll_fwd,
        roll_fwd_on_no_candidate_fx_option roll_fwd_on_no_candidate_fx);
    static int read_framework(
        const host_startup_info_t& host_info,
        const fx_reference_t& override_settings,
        const runtime_config_t& config,
        fx_name_to_fx_reference_map_t& newest_references,
        fx_name_to_fx_reference_map_t& oldest_references,
        fx_definition_vector_t& fx_definitions);
    static fx_definition_t* resolve_fx(
        const fx_reference_t& config,
        const pal::string_t& oldest_requested_version,
        const pal::string_t& dotnet_dir);
    static void muxer_usage(bool is_sdk_present);
    static int soft_roll_forward_helper(
        const fx_reference_t& newer,
        const fx_reference_t& older,
        bool older_is_hard_roll_forward,
        fx_name_to_fx_reference_map_t& newest_references);
    static int soft_roll_forward(
        const fx_reference_t existing_ref,
        bool current_is_hard_roll_forward,
        fx_name_to_fx_reference_map_t& newest_references);
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
};
