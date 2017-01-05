// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __LIBHOST_H__
#define __LIBHOST_H__
#include <stdint.h>
#include "trace.h"
#include "runtime_config.h"
#include "fx_ver.h"

enum host_mode_t
{
    invalid = 0,
    
    muxer,          // Invoked as "dotnet.exe".
    
    standalone,     // Invoked as "appname.exe" from the application base: either "standalone" or "branded". When implementing branded exes, rename this to "apphost"

    split_fx        // Invoked as "corehost.exe" for xunit scenarios -- this has to be fixed by the CLI to not use this executable and this mode should not be supported.
                    // Split FX means, the host is operating like "corerun.exe" in a split location from the application base (CORE_ROOT equivalent), but it has its "hostfxr.dll"
                    // next to it.
};

class fx_ver_t;
class runtime_config_t;

#define _HOST_INTERFACE_PACK 1
#pragma pack(push, _HOST_INTERFACE_PACK)
struct strarr_t
{
    // DO NOT modify this struct. It is used in a layout
    // dependent manner. Create another for your use.
    size_t len;
    const pal::char_t** arr;
};

struct host_interface_t
{
    size_t version_lo;                // Just assign sizeof() to this field.
    size_t version_hi;                // Breaking changes to the layout -- increment HOST_INTERFACE_LAYOUT_VERSION
    strarr_t config_keys;
    strarr_t config_values;
    const pal::char_t* fx_dir;
    const pal::char_t* fx_name;
    const pal::char_t* deps_file;
    size_t is_portable;
    strarr_t probe_paths;
    size_t patch_roll_forward;
    size_t prerelease_roll_forward;
    size_t host_mode;
    const pal::char_t* tfm;
    // !! WARNING / WARNING / WARNING / WARNING / WARNING / WARNING / WARNING / WARNING / WARNING
    // !! 1. Only append to this structure to maintain compat.
    // !! 2. Any nested structs should not use compiler specific padding (pack with _HOST_INTERFACE_PACK)
    // !! 3. Do not take address of the fields of this struct or be prepared to deal with unaligned accesses.
    // !! 4. Must be POD types; only use non-const size_t and pointer types; no access modifiers.
    // !! 5. Do not reorder fields or change any existing field types.
    // !! 6. Add static asserts for fields you add.
};
#pragma pack(pop)
static_assert(_HOST_INTERFACE_PACK == 1, "Packing size should not be modified for back compat");
static_assert(offsetof(host_interface_t, version_lo) == 0 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, version_hi) == 1 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, config_keys) == 2 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, config_values) == 4 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_dir) == 6 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_name) == 7 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, deps_file) == 8 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, is_portable) == 9 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, probe_paths) == 10 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, patch_roll_forward) == 12 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, prerelease_roll_forward) == 13 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, host_mode) == 14 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, tfm) == 15 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(sizeof(host_interface_t) == 16 * sizeof(size_t), "Did you add static asserts for the newly added fields?");

#define HOST_INTERFACE_LAYOUT_VERSION_HI 0x16041101 // YYMMDD:nn always increases when layout breaks compat.
#define HOST_INTERFACE_LAYOUT_VERSION_LO sizeof(host_interface_t)

class corehost_init_t
{
private:
    std::vector<pal::string_t> m_clr_keys;
    std::vector<pal::string_t> m_clr_values;
    std::vector<const pal::char_t*> m_clr_keys_cstr;
    std::vector<const pal::char_t*> m_clr_values_cstr;
    const pal::string_t m_tfm;
    const pal::string_t m_fx_dir;
    const pal::string_t m_fx_name;
    const pal::string_t m_deps_file;
    bool m_portable;
    std::vector<pal::string_t> m_probe_paths;
    std::vector<const pal::char_t*> m_probe_paths_cstr;
    bool m_patch_roll_forward;
    bool m_prerelease_roll_forward;
    host_mode_t m_host_mode;
    host_interface_t m_host_interface;
    const pal::string_t m_fx_ver;
public:
    corehost_init_t(
        const pal::string_t& deps_file,
        const std::vector<pal::string_t>& probe_paths,
        const pal::string_t& fx_dir,
        const host_mode_t mode,
        const runtime_config_t& runtime_config)
        : m_fx_dir(fx_dir)
        , m_fx_name(runtime_config.get_fx_name())
        , m_deps_file(deps_file)
        , m_portable(runtime_config.get_portable())
        , m_probe_paths(probe_paths)
        , m_patch_roll_forward(runtime_config.get_patch_roll_fwd())
        , m_prerelease_roll_forward(runtime_config.get_prerelease_roll_fwd())
        , m_host_mode(mode)
        , m_host_interface()
        , m_fx_ver(runtime_config.get_fx_version())
        , m_tfm(runtime_config.get_tfm())
    {
        runtime_config.config_kv(&m_clr_keys, &m_clr_values);
        make_cstr_arr(m_clr_keys, &m_clr_keys_cstr);
        make_cstr_arr(m_clr_values, &m_clr_values_cstr);
        make_cstr_arr(m_probe_paths, &m_probe_paths_cstr);
    }

    const pal::string_t& fx_dir() const
    {
        return m_fx_dir;
    }

    const pal::string_t& tfm() const
    {
        return m_tfm;
    }

    const pal::string_t& fx_name() const
    {
        return m_fx_name;
    }

    const pal::string_t& fx_version() const
    {
        return m_fx_ver;
    }

    const host_interface_t& get_host_init_data()
    {
        host_interface_t& hi = m_host_interface;

        hi.version_lo = HOST_INTERFACE_LAYOUT_VERSION_LO;
        hi.version_hi = HOST_INTERFACE_LAYOUT_VERSION_HI;

        hi.config_keys.len = m_clr_keys_cstr.size();
        hi.config_keys.arr = m_clr_keys_cstr.data();

        hi.config_values.len = m_clr_values_cstr.size();
        hi.config_values.arr = m_clr_values_cstr.data();

        hi.fx_dir = m_fx_dir.c_str();
        hi.fx_name = m_fx_name.c_str();
        hi.deps_file = m_deps_file.c_str();
        hi.is_portable = m_portable;

        hi.probe_paths.len = m_probe_paths_cstr.size();
        hi.probe_paths.arr = m_probe_paths_cstr.data();

        hi.patch_roll_forward = m_patch_roll_forward;
        hi.prerelease_roll_forward = m_prerelease_roll_forward;
        hi.host_mode = m_host_mode;

        hi.tfm = m_tfm.c_str();
        
        return hi;
    }

private:

    static void make_cstr_arr(const std::vector<pal::string_t>& arr, std::vector<const pal::char_t*>* out)
    {
        out->reserve(arr.size());
        for (const auto& str : arr)
        {
            out->push_back(str.c_str());
        }
    }
};


struct hostpolicy_init_t
{
    std::vector<std::vector<char>> cfg_keys;
    std::vector<std::vector<char>> cfg_values;
    pal::string_t deps_file;
    std::vector<pal::string_t> probe_paths;
    pal::string_t tfm;
    pal::string_t fx_dir;
    pal::string_t fx_name;
    host_mode_t host_mode;
    bool patch_roll_forward;
    bool prerelease_roll_forward;
    bool is_portable;

    static bool init(host_interface_t* input, hostpolicy_init_t* init)
    {
        // Check if there are any breaking changes.
        if (input->version_hi != HOST_INTERFACE_LAYOUT_VERSION_HI)
        {
            trace::error(_X("The version of the data layout used to initialize %s is [0x%04x]; expected version [0x%04x]"), LIBHOSTPOLICY_NAME, input->version_hi, HOST_INTERFACE_LAYOUT_VERSION_HI);
            return false;
        }

        trace::verbose(_X("Reading from host interface version: [0x%04x:%d] to initialize policy version: [0x%04x:%d]"), input->version_hi, input->version_lo, HOST_INTERFACE_LAYOUT_VERSION_HI, HOST_INTERFACE_LAYOUT_VERSION_LO);

        if (input->version_lo >= offsetof(host_interface_t, host_mode) + sizeof(input->host_mode))
        {
            make_clrstr_arr(input->config_keys.len, input->config_keys.arr, &init->cfg_keys);
            make_clrstr_arr(input->config_values.len, input->config_values.arr, &init->cfg_values);

            init->fx_dir = input->fx_dir;
            init->fx_name = input->fx_name;
            init->deps_file = input->deps_file;
            init->is_portable = input->is_portable;

            make_palstr_arr(input->probe_paths.len, input->probe_paths.arr, &init->probe_paths);

            init->patch_roll_forward = input->patch_roll_forward;
            init->prerelease_roll_forward = input->prerelease_roll_forward;
            init->host_mode = (host_mode_t)input->host_mode;
        }
        else
        {
            trace::error(_X("The size of the data layout used to initialize %s is %d; expected at least %d"), LIBHOSTPOLICY_NAME, input->version_lo, 
                offsetof(host_interface_t, host_mode) + sizeof(input->host_mode));
        }

        if (input->version_lo >= offsetof(host_interface_t, tfm) + sizeof(input->tfm))
        {
            init->tfm = input->tfm;
        }

        return true;
    }

private:
    static void make_palstr_arr(int argc, const pal::char_t** argv, std::vector<pal::string_t>* out)
    {
        out->reserve(argc);
        for (int i = 0; i < argc; ++i)
        {
            out->push_back(argv[i]);
        }
    }

    static void make_clrstr_arr(int argc, const pal::char_t** argv, std::vector<std::vector<char>>* out)
    {
        out->resize(argc);
        for (int i = 0; i < argc; ++i)
        {
            pal::pal_clrstring(pal::string_t(argv[i]), &(*out)[i]);
        }
    }
};

void get_runtime_config_paths_from_app(const pal::string_t& file, pal::string_t* config_file, pal::string_t* dev_config_file);
void get_runtime_config_paths_from_arg(const pal::string_t& file, pal::string_t* config_file, pal::string_t* dev_config_file);

host_mode_t detect_operating_mode(const pal::string_t& own_dir, const pal::string_t& own_dll, const pal::string_t& own_name);
bool hostpolicy_exists_in_svc(pal::string_t* resolved_dir);

void try_patch_roll_forward_in_dir(const pal::string_t& cur_dir, const fx_ver_t& start_ver, pal::string_t* max_str);
void try_prerelease_roll_forward_in_dir(const pal::string_t& cur_dir, const fx_ver_t& start_ver, pal::string_t* max_str);

#endif // __LIBHOST_H__
