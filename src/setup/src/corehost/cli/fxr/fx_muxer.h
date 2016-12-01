// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

class corehost_init_t;
class runtime_config_t;
struct fx_ver_t;

#include "libhost.h"

int execute_app(
    const pal::string_t& impl_dll_dir,
    corehost_init_t* init,
    const int argc,
    const pal::char_t* argv[]);

class fx_muxer_t
{
public:
    static int execute(const pal::string_t& exe_type, const int argc, const pal::char_t* argv[]);
private:
    static int read_config_and_execute(
        const pal::string_t& own_dir,
        const pal::string_t& app_candidate,
        const std::unordered_map<pal::string_t, std::vector<pal::string_t>>& opts,
        int new_argc, const pal::char_t** new_argv, host_mode_t mode);
    static int parse_args_and_execute(const pal::string_t& own_dir, const pal::string_t& own_dll, int argoff, int argc, const pal::char_t* argv[], bool exec_mode, host_mode_t mode, bool* can_execute);
    static bool resolve_hostpolicy_dir(host_mode_t mode,
        const pal::string_t& own_dir,
        const pal::string_t& fx_dir,
        const pal::string_t& app_or_deps_dir,
        const pal::string_t& specified_deps_file,
        const pal::string_t& specified_fx_version,
        const std::vector<pal::string_t>& probe_realpaths,
        const runtime_config_t& config,
        pal::string_t* impl_dir);
    static pal::string_t resolve_fx_dir(host_mode_t mode, const pal::string_t& own_dir, const runtime_config_t& config, const pal::string_t& specified_fx_version);
    static pal::string_t resolve_cli_version(const pal::string_t& global);
    static bool resolve_sdk_dotnet_path(const pal::string_t& own_dir, pal::string_t* cli_sdk);
};

