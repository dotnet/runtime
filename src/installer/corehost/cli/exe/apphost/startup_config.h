// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __STARTUP_CONFIG_H__
#define __STARTUP_CONFIG_H__

#include "pal.h"

class startup_config_t
{
public:
    startup_config_t();
    void parse(const pal::string_t& path);
    const bool is_valid() const { return m_valid; }
    const pal::string_t& get_app_root() const { return m_app_root; }

private:
    bool parse_internal(const pal::string_t& path);

    pal::string_t m_app_root;
    bool m_valid;
};
#endif // __STARTUP_CONFIG_H__
