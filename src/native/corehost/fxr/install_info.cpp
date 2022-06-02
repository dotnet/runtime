// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "install_info.h"
#include "pal.h"
#include "trace.h"
#include "utils.h"

bool install_info::print_installs(const pal::char_t* leading_whitespace, bool skip_current_arch)
{
    bool found_any = false;
    for (uint32_t i = 0; i < static_cast<uint32_t>(pal::known_architecture::__last); ++i)
    {
        pal::known_architecture arch = static_cast<pal::known_architecture>(i);
        if (skip_current_arch && arch == get_current_arch())
            continue;

        pal::string_t install_location;
        bool is_registered = pal::get_dotnet_self_registered_dir_for_arch(arch, &install_location);
        if (is_registered
            || (pal::get_default_installation_dir_for_arch(arch, &install_location) && pal::directory_exists(install_location)))
        {
            found_any = true;
            remove_trailing_dir_separator(&install_location);
            trace::println(_X("%s%-5s [%s]"), leading_whitespace, get_arch_name(arch), install_location.c_str());
            if (is_registered)
            {
                trace::println(_X("%s  registered at [%s]"), leading_whitespace, pal::get_dotnet_self_registered_config_location(arch).c_str());
            }
        }
    }

    return found_any;
}
