// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __HOSTPOLICY_CONTEXT_H__
#define __HOSTPOLICY_CONTEXT_H__

#include <pal.h>

#include "args.h"
#include "coreclr.h"
#include <corehost_context_contract.h>
#include <host_runtime_contract.h>
#include "hostpolicy_init.h"

struct hostpolicy_context_t
{
public:
    struct tpa_path_t
    {
        const char* directory;
        std::string file_name;
    };

    pal::string_t application;
    pal::string_t clr_dir;
    pal::string_t clr_path;
    host_mode_t host_mode;
    pal::string_t host_path;
    pal::string_t invocation_name;

    bool breadcrumbs_enabled;
    mutable std::unordered_set<pal::string_t> breadcrumbs;

    coreclr_property_bag_t coreclr_properties;

    std::unique_ptr<coreclr_t> coreclr;
    host_runtime_contract host_contract;

    std::vector<const char*> tpa_names;
    std::vector<std::string> tpa_directories;
    std::unordered_map<std::string, tpa_path_t> tpa_paths;

    int initialize(const hostpolicy_init_t &hostpolicy_init, const arguments_t &args, bool enable_breadcrumbs);
    const pal::string_t& get_reconstructed_tpa_property();

public: // static
    static bool should_read_rid_fallback_graph(const hostpolicy_init_t &init);

private:
    std::once_flag reconstructed_tpa_property_once;
    pal::string_t reconstructed_tpa_property;
};

#endif // __HOSTPOLICY_CONTEXT_H__
