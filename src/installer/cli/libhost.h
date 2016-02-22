// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __LIBHOST_H__
#define __LIBHOST_H__

#define LIBHOST_NAME MAKE_LIBNAME("hostpolicy")

enum host_mode_t
{
    invalid = 0,
    muxer,
    standalone,
    split_fx
};

class runtime_config_t;

class corehost_init_t
{
    const pal::string_t m_probe_path;
    const pal::string_t m_deps_file;
    const pal::string_t m_fx_dir;
    host_mode_t m_host_mode;
    const runtime_config_t* m_runtime_config;
public:
    corehost_init_t(
        const pal::string_t& deps_file,
        const pal::string_t& probe_path,
        const pal::string_t& fx_dir,
        const host_mode_t mode,
        const runtime_config_t* runtime_config)
        : m_fx_dir(fx_dir)
        , m_runtime_config(runtime_config)
        , m_deps_file(deps_file)
        , m_probe_path(probe_path)
        , m_host_mode(mode)
    {
    }

    const host_mode_t host_mode() const
    {
        return m_host_mode;
    }

    const pal::string_t& deps_file() const
    {
        return m_deps_file;
    }

    const pal::string_t& probe_dir() const
    {
        return m_probe_path;
    }

    const pal::string_t& fx_dir() const
    {
        return m_fx_dir;
    }

    const runtime_config_t* runtime_config() const
    {
        return m_runtime_config;
    }
};

pal::string_t get_runtime_config_json(const pal::string_t& app_path);
host_mode_t detect_operating_mode(const int argc, const pal::char_t* argv[], pal::string_t* own_dir = nullptr);

#endif // __LIBHOST_H__
