// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __COMMAND_LINE_H__
#define __COMMAND_LINE_H__

#include <host_interface.h>
#include <host_startup_info.h>
#include <pal.h>

enum class known_options
{
    additional_probing_path,
    deps_file,
    runtime_config,
    fx_version,
    roll_forward,
    additional_deps,
    roll_forward_on_no_candidate_fx,

    __last // Sentinel value
};

struct known_options_hash
{
    inline size_t operator()(const known_options& opt) const
    {
        return static_cast<size_t>(opt);
    }
};

typedef std::unordered_map<known_options, std::vector<pal::string_t>, known_options_hash> opt_map_t;

namespace command_line
{
    const pal::char_t* get_option_name(known_options opt);
    pal::string_t get_option_value(
        const opt_map_t& opts,
        known_options opt,
        const pal::string_t& de_fault);

    // Returns '0' on success, 'AppArgNotRunnable' if should be routed to CLI, otherwise error code.
    int parse_args_for_mode(
        host_mode_t mode,
        const host_startup_info_t& host_info,
        const int argc,
        const pal::char_t* argv[],
        /*out*/ int *new_argoff,
        /*out*/ pal::string_t &app_candidate,
        /*out*/ opt_map_t &opts,
        bool args_include_running_executable = true);
    int parse_args_for_sdk_command(
        const host_startup_info_t& host_info,
        const int argc,
        const pal::char_t* argv[],
        /*out*/ int *new_argoff,
        /*out*/ pal::string_t &app_candidate,
        /*out*/ opt_map_t &opts);

    void print_muxer_info(const pal::string_t &dotnet_root);
    void print_muxer_usage(bool is_sdk_present);
};

#endif // __COMMAND_LINE_H__
