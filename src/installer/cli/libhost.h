// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __LIBHOST_H__
#define __LIBHOST_H__

#include "fx_ver.h"

enum host_mode_t
{
    invalid = 0,
    muxer,
    standalone,
    split_fx
};

class fx_ver_t;
class runtime_config_t;

class corehost_init_t
{
    // // WARNING // WARNING // WARNING // WARNING // WARNING // WARNING //
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // !! If you change this class layout increment the s_version field; !!
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

public:
    static const int s_version = 0x8003;
private:
    int m_version;
    std::vector<pal::string_t> m_probe_paths;
    const pal::string_t m_deps_file;
    const pal::string_t m_fx_dir;
    host_mode_t m_host_mode;
    const runtime_config_t* m_runtime_config;
public:
    corehost_init_t(
        const pal::string_t& deps_file,
        const std::vector<pal::string_t>& probe_paths,
        const pal::string_t& fx_dir,
        const host_mode_t mode,
        const runtime_config_t* runtime_config)
        : m_fx_dir(fx_dir)
        , m_runtime_config(runtime_config)
        , m_deps_file(deps_file)
        , m_probe_paths(probe_paths)
        , m_host_mode(mode)
        , m_version(s_version)
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

    const std::vector<pal::string_t>& probe_paths() const
    {
        return m_probe_paths;
    }

    const pal::string_t& fx_dir() const
    {
        return m_fx_dir;
    }

    const runtime_config_t* runtime_config() const
    {
        return m_runtime_config;
    }

    int version() const
    {
        return m_version;
    }
};

void get_runtime_config_paths_from_app(const pal::string_t& file, pal::string_t* config_file, pal::string_t* dev_config_file);
void get_runtime_config_paths_from_arg(const pal::string_t& file, pal::string_t* config_file, pal::string_t* dev_config_file);

host_mode_t detect_operating_mode(const int argc, const pal::char_t* argv[], pal::string_t* own_dir = nullptr);

void try_patch_roll_forward_in_dir(const pal::string_t& cur_dir, const fx_ver_t& start_ver, pal::string_t* max_str);
void try_prerelease_roll_forward_in_dir(const pal::string_t& cur_dir, const fx_ver_t& start_ver, pal::string_t* max_str);

#endif // __LIBHOST_H__
