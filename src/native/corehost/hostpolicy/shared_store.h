// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef SHARED_STORE_H
#define SHARED_STORE_H

#include <host_interface.h>
#include <pal.h>

namespace shared_store
{
    std::vector<pal::string_t> get_paths(const pal::string_t& tfm, host_mode_t host_mode, const pal::string_t& host_path);
}

#endif // SHARED_STORE_H
