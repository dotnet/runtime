// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

class runtime_config_t;
struct fx_ver_t;

int execute_app(
    const pal::string_t& impl_dll_dir,
    const corehost_init_t* init,
    const int argc,
    const pal::char_t* argv[]);

class fx_muxer_t
{
public:
    static int execute(const int argc, const pal::char_t* argv[]);
private:
    static int parse_args_and_execute(const pal::string_t& own_dir, int argoff, int argc, const pal::char_t* argv[], bool exec_mode, bool* can_execute);
    static pal::string_t resolve_fx_dir(const pal::string_t& muxer_path, runtime_config_t* runtime);
    static pal::string_t resolve_cli_version(const pal::string_t& global);
    static bool resolve_sdk_dotnet_path(const pal::string_t& own_dir, pal::string_t* cli_sdk);
};

