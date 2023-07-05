// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __HOSTPOLICY_INIT_H__
#define __HOSTPOLICY_INIT_H__

#include "host_interface.h"
#include "host_startup_info.h"
#include "fx_definition.h"
#include "bundle/info.h"

struct hostpolicy_init_t
{
    std::vector<pal::string_t> cfg_keys;
    std::vector<pal::string_t> cfg_values;
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

    static bool init(const host_interface_t* input, hostpolicy_init_t* init);

    static void init_host_command(const host_interface_t* input, hostpolicy_init_t* init);
};

#endif // __HOSTPOLICY_INIT_H__
