// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "trace.h"
#include "utils.h"
#include "cpprest/json.h"
#include "startup_config.h"

startup_config_t::startup_config_t()
    : m_valid(false)
{
}

void startup_config_t::parse(const pal::string_t& path)
{
    trace::verbose(_X("Attempting to read startup config: %s"), path.c_str());

    m_valid = parse_internal(path);
    if (!m_valid)
    {
        trace::verbose(_X("Did not successfully parse the startup.config.json"));
    }
}

bool startup_config_t::parse_internal(const pal::string_t& path)
{
    pal::string_t retval;
    if (!pal::file_exists(path))
    {
        // Not existing is not an error.
        return true;
    }

    pal::ifstream_t file(path);
    if (!file.good())
    {
        trace::verbose(_X("File stream not good %s"), path.c_str());
        return false;
    }

    if (skip_utf8_bom(&file))
    {
        trace::verbose(_X("UTF-8 BOM skipped while reading [%s]"), path.c_str());
    }

    try
    {
        const auto root = web::json::value::parse(file);
        const auto& json = root.as_object();
        const auto options = json.find(_X("startupOptions"));
        if (options != json.end())
        {
            const auto& prop_obj = options->second.as_object();

            auto appRoot = prop_obj.find(_X("appRoot"));
            if (appRoot != prop_obj.end())
            {
                m_app_root = appRoot->second.as_string();
            }
        }
    }
    catch (const std::exception& je)
    {
        pal::string_t jes;
        (void)pal::utf8_palstring(je.what(), &jes);
        trace::error(_X("A JSON parsing exception occurred in [%s]: %s"), path.c_str(), jes.c_str());
        return false;
    }

    return true;
}
