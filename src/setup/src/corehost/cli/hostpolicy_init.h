// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __HOSTPOLICY_INIT_H__
#define __HOSTPOLICY_INIT_H__

#include "host_interface.h"
#include "host_startup_info.h"
#include "fx_definition.h"
#include "fx_ver.h"

struct hostpolicy_init_t
{
    std::vector<std::vector<char>> cfg_keys;
    std::vector<std::vector<char>> cfg_values;
    pal::string_t deps_file;
    pal::string_t additional_deps_serialized;
    std::vector<pal::string_t> probe_paths;
    fx_definition_vector_t fx_definitions;
    pal::string_t tfm;
    host_mode_t host_mode;
    bool patch_roll_forward;
    bool prerelease_roll_forward;
    bool is_framework_dependent;
    pal::string_t host_command;
    host_startup_info_t host_info;

    static bool init(host_interface_t* input, hostpolicy_init_t* init);

    static void init_host_command(host_interface_t* input, hostpolicy_init_t* init);
};

#endif // __HOSTPOLICY_INIT_H__
