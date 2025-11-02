// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __INSTALL_INFO_H__
#define __INSTALL_INFO_H__

#include "pal.h"
#include <functional>

namespace install_info
{
    bool enumerate_other_architectures(std::function<void(pal::architecture, const pal::string_t&, bool)> callback);
    bool print_environment(const pal::char_t* leading_whitespace);
    bool print_other_architectures(const pal::char_t* leading_whitespace);
    bool try_get_install_location(pal::architecture arch, pal::string_t& out_install_location, bool* out_is_registered = nullptr);
};

#endif // __INSTALL_INFO_H__
