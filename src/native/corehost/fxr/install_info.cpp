// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "install_info.h"
#include "pal.h"
#include "trace.h"
#include "utils.h"

#include <algorithm>
#include <vector>

bool install_info::print_environment(const pal::char_t* leading_whitespace)
{
    // Enumerate environment variables and filter for DOTNET_
    std::vector<std::pair<pal::string_t, pal::string_t>> env_vars;
    bool found_complus_var = false;
    pal::enumerate_environment_variables([&](const pal::char_t* name, const pal::char_t* value)
    {
        // Check if the environment variable starts with DOTNET_
#if defined(TARGET_WINDOWS)
        // Environment variables are case-insensitive on Windows
        auto comp_func = pal::strncasecmp;
#else
        auto comp_func = pal::strncmp;
#endif
        if (comp_func(name, _X("DOTNET_"), STRING_LENGTH("DOTNET_")) == 0)
        {
            env_vars.push_back(std::make_pair(name, value));
        }
        else if (!found_complus_var && comp_func(name, _X("COMPlus_"), STRING_LENGTH("COMPlus_")) == 0)
        {
            found_complus_var = true;
        }
    });

    // Sort for consistent output
    std::sort(env_vars.begin(), env_vars.end());

    // Print all relevant environment variables
    const pal::char_t* fmt = _X("%s%-40s [%s]");
    for (const auto& env_var : env_vars)
    {
        trace::println(fmt, leading_whitespace, env_var.first.c_str(), env_var.second.c_str());
    }

    if (found_complus_var)
        trace::println(_X("%sDetected COMPlus_* environment variable(s). Consider transitioning to DOTNET_* equivalent."), leading_whitespace);

    return env_vars.size() > 0 || found_complus_var;
}

bool install_info::try_get_install_location(pal::architecture arch, pal::string_t& out_install_location, bool* out_is_registered)
{
    pal::string_t install_location;
    bool is_registered = pal::get_dotnet_self_registered_dir_for_arch(arch, &install_location);
    bool found = is_registered
        || (pal::get_default_installation_dir_for_arch(arch, &install_location) && pal::directory_exists(install_location));
    if (!found)
        return false;

    remove_trailing_dir_separator(&install_location);
    out_install_location = install_location;
    if (out_is_registered != nullptr)
        *out_is_registered = is_registered;

    return true;
}

bool install_info::enumerate_other_architectures(std::function<void(pal::architecture, const pal::string_t&, bool)> callback)
{
    bool found_any = false;
    for (uint32_t i = 0; i < static_cast<uint32_t>(pal::architecture::__last); ++i)
    {
        pal::architecture arch = static_cast<pal::architecture>(i);
        if (arch == get_current_arch())
            continue;

        bool is_registered;
        pal::string_t install_location;
        if (try_get_install_location(arch, install_location, &is_registered))
        {
            found_any = true;
            callback(arch, install_location, is_registered);
        }
    }

    return found_any;
}

bool install_info::print_other_architectures(const pal::char_t* leading_whitespace)
{
    return enumerate_other_architectures(
        [&](pal::architecture arch, const pal::string_t& install_location, bool is_registered)
        {
            trace::println(_X("%s%-5s [%s]"), leading_whitespace, get_arch_name(arch), install_location.c_str());
            if (is_registered)
            {
                trace::println(_X("%s  registered at [%s]"), leading_whitespace, pal::get_dotnet_self_registered_config_location(arch).c_str());
            }
        });
}
