// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

class corehost_init_t;
class runtime_config_t;
class fx_definition_t; 
struct fx_ver_t;
struct host_startup_info_t;

#include "libhost.h"

int execute_app(
    const pal::string_t& impl_dll_dir,
    corehost_init_t* init,
    const int argc,
    const pal::char_t* argv[]);

int execute_host_command(
    const pal::string_t& impl_dll_dir,
    corehost_init_t* init,
    const int argc,
    const pal::char_t* argv[],
    pal::char_t result_buffer[],
    int32_t buffer_size,
    int32_t* required_buffer_size);

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
    static bool resolve_sdk_dotnet_path(const pal::string_t& dotnet_root, const pal::string_t& cwd, pal::string_t* cli_sdk);
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
        const pal::string_t& specified_fx_version,
        const std::vector<pal::string_t>& probe_realpaths,
        pal::string_t* impl_dir);
    static fx_ver_t resolve_framework_version(
        const std::vector<fx_ver_t>& version_list,
        const pal::string_t& fx_ver,
        const fx_ver_t& specified,
        bool patch_roll_fwd,
        roll_fwd_on_no_candidate_fx_option roll_fwd_on_no_candidate_fx);
    static fx_definition_t* resolve_fx(
        host_mode_t mode,
        const runtime_config_t& config,
        const pal::string_t& dotnet_dir,
        const pal::string_t& specified_fx_version);
    static pal::string_t resolve_cli_version(const pal::string_t& global);
    static bool resolve_sdk_dotnet_path(const pal::string_t& dotnet_root, pal::string_t* cli_sdk);
    static void muxer_usage(bool is_sdk_present);
};
