// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "utils.h"
#include "cpprest/json.h"
#include "runtime_config.h"
#include <cassert>

typedef web::json::value json_value;

runtime_config_t::runtime_config_t(const pal::string_t& path)
    : m_fx_roll_fwd(true)
    , m_path(path)
    , m_portable(false)
    , m_gc_server(_X("0"))
{
    m_valid = ensure_parsed();
} 

void parse_fx(const json_value& opts, pal::string_t* name, pal::string_t* version, bool* roll_fwd, bool* portable)
{
    name->clear();
    version->clear();
    *roll_fwd = true;
    *portable = false;

    if (opts.is_null())
    {
        return;
    }

    const auto& opts_obj = opts.as_object();
    auto framework =  opts_obj.find(_X("framework"));
    if (framework == opts_obj.end())
    {
        return;
    }

    *portable = true;

    const auto& fx_obj = framework->second.as_object();
    *name = fx_obj.at(_X("name")).as_string();
    *version = fx_obj.at(_X("version")).as_string();

    auto value = fx_obj.find(_X("rollForward"));
    if (value == fx_obj.end())
    {
        return;
    }

    *roll_fwd = value->second.as_bool();
}

bool runtime_config_t::ensure_parsed()
{
    pal::string_t retval;
    if (!pal::file_exists(m_path))
    {
        // Not existing is not an error.
        return true;
    }

    pal::ifstream_t file(m_path);
    if (!file.good())
    {
        return false;
    }

    try
    {
        const auto root = json_value::parse(file);
        const auto& json = root.as_object();
        const auto iter = json.find(_X("runtimeOptions"));
        if (iter != json.end())
        {
            parse_fx(iter->second, &m_fx_name, &m_fx_ver, &m_fx_roll_fwd, &m_portable);
        }
    }
    catch (...)
    {
        return false;
    }
    return true;
}

const pal::string_t& runtime_config_t::get_gc_server() const
{
    assert(m_valid);
    return m_gc_server;
}

const pal::string_t& runtime_config_t::get_fx_name() const
{
    assert(m_valid);
    return m_fx_name;
}

const pal::string_t& runtime_config_t::get_fx_version() const
{
    assert(m_valid);
    return m_fx_ver;
}

bool runtime_config_t::get_fx_roll_fwd() const
{
    assert(m_valid);
    return m_fx_roll_fwd;
}

bool runtime_config_t::get_portable() const
{
    return m_portable;
}
