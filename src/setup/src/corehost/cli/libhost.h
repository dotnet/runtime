// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __LIBHOST_H__
#define __LIBHOST_H__
#include <stdint.h>
#include "trace.h"
#include "host_startup_info.h"
#include "runtime_config.h"
#include "fx_definition.h"
#include "fx_ver.h"

enum host_mode_t
{
    invalid = 0,

    muxer,          // Invoked as "dotnet.exe".

    apphost,        // Invoked as <appname>.exe from the application base; this is the renamed "apphost.exe".

    split_fx,       // Invoked as "corehost.exe" for xunit scenarios. Supported for backwards compat for 1.x apps.
                    // Split FX means, the host is operating like "corerun.exe" in a split location from the application base (CORE_ROOT equivalent),
                    // but it has its "hostfxr.dll" next to it.

    libhost,        // Invoked from a non-exe scenario (e.g. COM Activation or self-hosting native application)
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
    size_t is_framework_dependent;
    strarr_t probe_paths;
    size_t patch_roll_forward;
    size_t prerelease_roll_forward;
    size_t host_mode;
    const pal::char_t* tfm;
    const pal::char_t* additional_deps_serialized;
    const pal::char_t* fx_ver;
    strarr_t fx_names;
    strarr_t fx_dirs;
    strarr_t fx_requested_versions;
    strarr_t fx_found_versions;
    const pal::char_t* host_command;
    const pal::char_t* host_info_host_path;
    const pal::char_t* host_info_dotnet_root;
    const pal::char_t* host_info_app_path;
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
static_assert(offsetof(host_interface_t, is_framework_dependent) == 9 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, probe_paths) == 10 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, patch_roll_forward) == 12 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, prerelease_roll_forward) == 13 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, host_mode) == 14 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, tfm) == 15 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, additional_deps_serialized) == 16 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_ver) == 17 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_names) == 18 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_dirs) == 20 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_requested_versions) == 22 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, fx_found_versions) == 24 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, host_command) == 26 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, host_info_host_path) == 27 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, host_info_dotnet_root) == 28 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(offsetof(host_interface_t, host_info_app_path) == 29 * sizeof(size_t), "Struct offset breaks backwards compatibility");
static_assert(sizeof(host_interface_t) == 30 * sizeof(size_t), "Did you add static asserts for the newly added fields?");

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
    const pal::string_t m_deps_file;
    const pal::string_t m_additional_deps_serialized;
    bool m_is_framework_dependent;
    std::vector<pal::string_t> m_probe_paths;
    std::vector<const pal::char_t*> m_probe_paths_cstr;
    bool m_patch_roll_forward;
    bool m_prerelease_roll_forward;
    host_mode_t m_host_mode;
    host_interface_t m_host_interface;
    std::vector<pal::string_t> m_fx_names;
    std::vector<const pal::char_t*> m_fx_names_cstr;
    std::vector<pal::string_t> m_fx_dirs;
    std::vector<const pal::char_t*> m_fx_dirs_cstr;
    std::vector<pal::string_t> m_fx_requested_versions;
    std::vector<const pal::char_t*> m_fx_requested_versions_cstr;
    std::vector<pal::string_t> m_fx_found_versions;
    std::vector<const pal::char_t*> m_fx_found_versions_cstr;
    const pal::string_t m_host_command;
    const pal::string_t m_host_info_host_path;
    const pal::string_t m_host_info_dotnet_root;
    const pal::string_t m_host_info_app_path;
public:
    corehost_init_t(
        const pal::string_t& host_command,
        const host_startup_info_t& host_info,
        const pal::string_t& deps_file,
        const pal::string_t& additional_deps_serialized,
        const std::vector<pal::string_t>& probe_paths,
        const host_mode_t mode,
        const fx_definition_vector_t& fx_definitions)
        : m_host_command(host_command)
        , m_host_info_host_path(host_info.host_path)
        , m_host_info_dotnet_root(host_info.dotnet_root)
        , m_host_info_app_path(host_info.app_path)
        , m_deps_file(deps_file)
        , m_additional_deps_serialized(additional_deps_serialized)
        , m_is_framework_dependent(get_app(fx_definitions).get_runtime_config().get_is_framework_dependent())
        , m_probe_paths(probe_paths)
        , m_host_mode(mode)
        , m_host_interface()
        , m_tfm(get_app(fx_definitions).get_runtime_config().get_tfm())
    {
        make_cstr_arr(m_probe_paths, &m_probe_paths_cstr);

        int fx_count = fx_definitions.size();
        m_fx_names.reserve(fx_count);
        m_fx_dirs.reserve(fx_count);
        m_fx_requested_versions.reserve(fx_count);
        m_fx_found_versions.reserve(fx_count);

        std::unordered_map<pal::string_t, pal::string_t> combined_properties;
        for (auto& fx : fx_definitions)
        {
            fx->get_runtime_config().combine_properties(combined_properties);

            m_fx_names.push_back(fx->get_name());
            m_fx_dirs.push_back(fx->get_dir());
            m_fx_requested_versions.push_back(fx->get_requested_version());
            m_fx_found_versions.push_back(fx->get_found_version());
        }

        for (const auto& kv : combined_properties)
        {
            m_clr_keys.push_back(kv.first);
            m_clr_values.push_back(kv.second);
        }

        make_cstr_arr(m_fx_names, &m_fx_names_cstr);
        make_cstr_arr(m_fx_dirs, &m_fx_dirs_cstr);
        make_cstr_arr(m_fx_requested_versions, &m_fx_requested_versions_cstr);
        make_cstr_arr(m_fx_found_versions, &m_fx_found_versions_cstr);
        make_cstr_arr(m_clr_keys, &m_clr_keys_cstr);
        make_cstr_arr(m_clr_values, &m_clr_values_cstr);
    }

    const pal::string_t& tfm() const
    {
        return m_tfm;
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

        // Keep these for backwards compat
        if (m_fx_names_cstr.size() > 1)
        {
            hi.fx_name = m_fx_names_cstr[1];
            hi.fx_dir = m_fx_dirs_cstr[1];
            hi.fx_ver = m_fx_requested_versions_cstr[1];
        }
        else
        {
            hi.fx_name = _X("");
            hi.fx_dir = _X("");
            hi.fx_ver = _X("");
        }

        hi.deps_file = m_deps_file.c_str();
        hi.additional_deps_serialized = m_additional_deps_serialized.c_str();
        hi.is_framework_dependent = m_is_framework_dependent;

        hi.probe_paths.len = m_probe_paths_cstr.size();
        hi.probe_paths.arr = m_probe_paths_cstr.data();

        hi.patch_roll_forward = m_patch_roll_forward;
        hi.prerelease_roll_forward = m_prerelease_roll_forward;
        hi.host_mode = m_host_mode;

        hi.tfm = m_tfm.c_str();

        hi.fx_names.len = m_fx_names_cstr.size();
        hi.fx_names.arr = m_fx_names_cstr.data();

        hi.fx_dirs.len = m_fx_dirs_cstr.size();
        hi.fx_dirs.arr = m_fx_dirs_cstr.data();

        hi.fx_requested_versions.len = m_fx_requested_versions_cstr.size();
        hi.fx_requested_versions.arr = m_fx_requested_versions_cstr.data();

        hi.fx_found_versions.len = m_fx_found_versions_cstr.size();
        hi.fx_found_versions.arr = m_fx_found_versions_cstr.data();

        hi.host_command = m_host_command.c_str();

        hi.host_info_host_path = m_host_info_host_path.c_str();
        hi.host_info_dotnet_root = m_host_info_dotnet_root.c_str();
        hi.host_info_app_path = m_host_info_app_path.c_str();

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
    pal::string_t additional_deps_serialized;
    std::vector<pal::string_t> probe_paths;
    fx_definition_vector_t fx_definitions;
    pal::string_t tfm;
    host_mode_t host_mode;
    bool patch_roll_forward;
    bool prerelease_roll_forward;
    bool is_framework_dependent;
    pal::string_t host_command;
    host_startup_info_t host_info;

    static bool init(host_interface_t* input, hostpolicy_init_t* init)
    {
        // Check if there are any breaking changes.
        if (input->version_hi != HOST_INTERFACE_LAYOUT_VERSION_HI)
        {
            trace::error(_X("The version of the data layout used to initialize %s is [0x%04x]; expected version [0x%04x]"), LIBHOSTPOLICY_NAME, input->version_hi, HOST_INTERFACE_LAYOUT_VERSION_HI);
            return false;
        }

        trace::verbose(_X("Reading from host interface version: [0x%04x:%d] to initialize policy version: [0x%04x:%d]"), input->version_hi, input->version_lo, HOST_INTERFACE_LAYOUT_VERSION_HI, HOST_INTERFACE_LAYOUT_VERSION_LO);

        //This check is to ensure is an old hostfxr can still load new hostpolicy.
        //We should not read garbage due to potentially shorter struct size

        pal::string_t fx_requested_ver;

        if (input->version_lo >= offsetof(host_interface_t, host_mode) + sizeof(input->host_mode))
        {
            make_clrstr_arr(input->config_keys.len, input->config_keys.arr, &init->cfg_keys);
            make_clrstr_arr(input->config_values.len, input->config_values.arr, &init->cfg_values);

            init->deps_file = input->deps_file;
            init->is_framework_dependent = input->is_framework_dependent;

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

        //An old hostfxr may not provide these fields.
        //The version_lo (sizeof) the old hostfxr saw at build time will be
        //smaller and we should not attempt to read the fields in that case.
        if (input->version_lo >= offsetof(host_interface_t, tfm) + sizeof(input->tfm))
        {
            init->tfm = input->tfm;
        }
        
        if (input->version_lo >= offsetof(host_interface_t, fx_ver) + sizeof(input->fx_ver))
        {
            init->additional_deps_serialized = input->additional_deps_serialized;
            fx_requested_ver = input->fx_ver;
        }

        int fx_count = 0;
        if (input->version_lo >= offsetof(host_interface_t, fx_names) + sizeof(input->fx_names))
        {
            int fx_count = input->fx_names.len;
            assert(fx_count > 0);
            assert(fx_count == input->fx_dirs.len);
            assert(fx_count == input->fx_requested_versions.len);
            assert(fx_count == input->fx_found_versions.len);

            std::vector<pal::string_t> fx_names;
            std::vector<pal::string_t> fx_dirs;
            std::vector<pal::string_t> fx_requested_versions;
            std::vector<pal::string_t> fx_found_versions;

            make_palstr_arr(input->fx_names.len, input->fx_names.arr, &fx_names);
            make_palstr_arr(input->fx_dirs.len, input->fx_dirs.arr, &fx_dirs);
            make_palstr_arr(input->fx_requested_versions.len, input->fx_requested_versions.arr, &fx_requested_versions);
            make_palstr_arr(input->fx_found_versions.len, input->fx_found_versions.arr, &fx_found_versions);

            init->fx_definitions.reserve(fx_count);
            for (int i = 0; i < fx_count; ++i)
            {
                auto fx = new fx_definition_t(fx_names[i], fx_dirs[i], fx_requested_versions[i], fx_found_versions[i]);
                init->fx_definitions.push_back(std::unique_ptr<fx_definition_t>(fx));
            }
        }
        else
        {
            // Backward compat; create the fx_definitions[0] and [1] from the previous information
            init->fx_definitions.reserve(2);

            auto fx = new fx_definition_t();
            init->fx_definitions.push_back(std::unique_ptr<fx_definition_t>(fx));

            if (init->is_framework_dependent)
            {
                pal::string_t fx_dir = input->fx_dir;
                pal::string_t fx_name = input->fx_name;

                // The found_ver was not passed previously, so obtain that from fx_dir
                pal::string_t fx_found_ver;
                int index = fx_dir.rfind(DIR_SEPARATOR);
                if (index != pal::string_t::npos)
                {
                    fx_found_ver = fx_dir.substr(index + 1);
                }

                fx = new fx_definition_t(fx_name, fx_dir, fx_requested_ver, fx_found_ver);
                init->fx_definitions.push_back(std::unique_ptr<fx_definition_t>(fx));
            }
        }

        // Initialize the host command
        init_host_command(input, init);

        if (input->version_lo >= offsetof(host_interface_t, host_info_host_path) + sizeof(input->host_info_host_path))
        {
            init->host_info.host_path = input->host_info_host_path;
            init->host_info.dotnet_root = input->host_info_dotnet_root;
            init->host_info.app_path = input->host_info_app_path;
            // For the backwards compat case, this will be later initialized with argv[0]
        }

        return true;
    }

    static void init_host_command(host_interface_t* input, hostpolicy_init_t* init)
    {
        if (input->version_lo >= offsetof(host_interface_t, host_command) + sizeof(input->host_command))
        {
            init->host_command = input->host_command;
        }
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
void get_runtime_config_paths(const pal::string_t& path, const pal::string_t& name, pal::string_t* config_file, pal::string_t* dev_config_file);

host_mode_t detect_operating_mode(const host_startup_info_t& host_info);
bool hostpolicy_exists_in_svc(pal::string_t* resolved_dir);

void try_patch_roll_forward_in_dir(const pal::string_t& cur_dir, const fx_ver_t& start_ver, pal::string_t* max_str);
void try_prerelease_roll_forward_in_dir(const pal::string_t& cur_dir, const fx_ver_t& start_ver, pal::string_t* max_str);

#endif // __LIBHOST_H__
