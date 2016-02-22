// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"

class runtime_config_t
{
public:
    runtime_config_t(const pal::string_t& path);
    bool is_valid() { return m_valid; }
    const pal::string_t& get_path() { return m_path; }
    const pal::string_t& get_gc_server() const;
    const pal::string_t& get_fx_version() const;
    const pal::string_t& get_fx_name() const;
    bool get_fx_roll_fwd() const;
    bool get_portable() const;

private:
    bool ensure_parsed();
    
    pal::string_t m_gc_server;
    pal::string_t m_fx_name;
    pal::string_t m_fx_ver;
    bool m_fx_roll_fwd;

    pal::string_t m_path;
    bool m_portable;
    bool m_valid;
};
