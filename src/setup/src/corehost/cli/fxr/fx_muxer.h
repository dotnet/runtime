// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

class corehost_init_t;
class runtime_config_t;
class fx_definition_t; 
struct fx_ver_t;
struct host_startup_info_t;

#include "fx_definition.h"
#include "host_interface.h"
#include "host_startup_info.h"
#include "error_codes.h"

enum class coreclr_delegate_type
{
    invalid,
    com_activation,
    load_in_memory_assembly
};

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
    static int load_runtime_and_get_delegate(
        const host_startup_info_t& host_info,
        host_mode_t mode,
        coreclr_delegate_type delegate_type,
        void** delegate
    );
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
    static void muxer_usage(bool is_sdk_present);
};
