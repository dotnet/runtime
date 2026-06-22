// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "utils.h"
#include "trace.h"
#include "bundle/info.h"
#if defined(TARGET_WINDOWS)
#include <_version.h>
#else
#include <_version.c>
#endif

bool file_exists_in_dir(const pal::string_t& dir, const pal::char_t* file_name, pal::string_t* out_file_path)
{
    pal_char_t* file_path = utils_find_file_in_dir(dir.c_str(), file_name);
    if (file_path == nullptr)
        return false;

    if (out_file_path != nullptr)
        out_file_path->assign(file_path);
    free(file_path);
    return true;
}

bool coreclr_exists_in_dir(const pal::string_t& candidate)
{
    pal::string_t test(candidate);
    append_path(&test, LIBCORECLR_NAME);
    trace::verbose(_X("Checking if CoreCLR path exists=[%s]"), test.c_str());
    return pal::file_exists(test);
}

bool utils::starts_with(const pal::string_t& value, const pal::char_t* prefix, size_t prefix_len, bool match_case)
{
    // Cannot start with an empty string.
    if (prefix_len == 0)
        return false;

    auto cmp = match_case ? pal::strncmp : pal::strncasecmp;
    return (value.size() >= prefix_len) &&
        cmp(value.c_str(), prefix, prefix_len) == 0;
}

bool utils::ends_with(const pal::string_t& value, const pal::char_t* suffix, size_t suffix_len, bool match_case)
{
    auto cmp = match_case ? pal::strcmp : pal::strcasecmp;
    return (value.size() >= suffix_len) &&
        cmp(value.c_str() + value.size() - suffix_len, suffix) == 0;
}

void append_path(pal::string_t* path1, const pal::char_t* path2)
{
    if (pal::strlen(path2) == 0)
        return;

    if (path1->empty())
    {
        path1->assign(path2);
        return;
    }

    if (path1->back() != DIR_SEPARATOR && path2[0] != DIR_SEPARATOR)
    {
        path1->push_back(DIR_SEPARATOR);
    }

    path1->append(path2);
}

pal::string_t strip_executable_ext(const pal::string_t& filename)
{
    const pal::char_t* exe_suffix = pal::exe_suffix();
    if (exe_suffix == nullptr)
        return filename;

    size_t suffix_len = pal::strlen(exe_suffix);
    if (suffix_len == 0)
        return filename;

    if (utils::ends_with(filename, exe_suffix, suffix_len, false))
    {
        // We need to strip off the old extension
        pal::string_t result(filename);
        result.erase(result.size() - suffix_len);
        return result;
    }

    return filename;
}

pal::string_t strip_file_ext(const pal::string_t& path)
{
    if (path.empty())
    {
        return path;
    }
    size_t sep_pos = path.rfind(_X("/\\"));
    size_t dot_pos = path.rfind(_X('.'));
    if (sep_pos != pal::string_t::npos && sep_pos > dot_pos)
    {
        return path;
    }
    return path.substr(0, dot_pos);
}

pal::string_t get_filename_without_ext(const pal::string_t& path)
{
    if (path.empty())
    {
        return path;
    }

    size_t name_pos = path.find_last_of(_X("/\\"));
    size_t dot_pos = path.rfind(_X('.'));
    size_t start_pos = (name_pos == pal::string_t::npos) ? 0 : (name_pos + 1);
    size_t count = (dot_pos == pal::string_t::npos || dot_pos < start_pos) ? pal::string_t::npos : (dot_pos - start_pos);
    return path.substr(start_pos, count);
}

pal::string_t get_filename(const pal::string_t& path)
{
    if (path.empty())
    {
        return path;
    }

    auto name_pos = path.find_last_of(DIR_SEPARATOR);
    if (name_pos == pal::string_t::npos)
    {
        return path;
    }

    return path.substr(name_pos + 1);
}

pal::string_t get_directory(const pal::string_t& path)
{
    pal_char_t* result = utils_get_directory(path.c_str());
    if (result == nullptr)
        return pal::string_t();

    pal::string_t ret(result);
    free(result);
    return ret;
}

void remove_trailing_dir_separator(pal::string_t* dir)
{
    if (dir->back() == DIR_SEPARATOR)
    {
        dir->pop_back();
    }
}

void replace_char(pal::string_t* path, pal::char_t match, pal::char_t repl)
{
    size_t pos = 0;
    while ((pos = path->find(match, pos)) != pal::string_t::npos)
    {
        (*path)[pos] = repl;
    }
}

pal::string_t get_replaced_char(const pal::string_t& path, pal::char_t match, pal::char_t repl)
{
    size_t pos = path.find(match);
    if (pos == pal::string_t::npos)
    {
        return path;
    }

    pal::string_t out = path;
    do
    {
        out[pos] = repl;
    } while ((pos = out.find(match, pos)) != pal::string_t::npos);

    return out;
}

namespace
{
    const pal::char_t* s_all_architectures[] =
    {
        _X("arm"),
        _X("arm64"),
        _X("armv6"),
        _X("loongarch64"),
        _X("ppc64le"),
        _X("riscv64"),
        _X("s390x"),
        _X("x64"),
        _X("x86"),
        _X("wasm")
    };
    static_assert((sizeof(s_all_architectures) / sizeof(*s_all_architectures)) == static_cast<size_t>(pal::architecture::__last), "Invalid known architectures count");
}

pal::architecture get_current_arch()
{
#if defined(TARGET_AMD64)
    return pal::architecture::x64;
#elif defined(TARGET_X86)
    return pal::architecture::x86;
#elif defined(TARGET_ARMV6)
    return pal::architecture::armv6;
#elif defined(TARGET_ARM)
    return pal::architecture::arm;
#elif defined(TARGET_ARM64)
    return pal::architecture::arm64;
#elif defined(TARGET_LOONGARCH64)
    return pal::architecture::loongarch64;
#elif defined(TARGET_RISCV64)
    return pal::architecture::riscv64;
#elif defined(TARGET_S390X)
    return pal::architecture::s390X;
#elif defined(TARGET_POWERPC64)
    return pal::architecture::ppc64le;
#elif defined(TARGET_WASM)
    return pal::architecture::wasm;
#else
#error "Unknown target"
#endif
}

const pal::char_t* get_arch_name(pal::architecture arch)
{
    int idx = static_cast<int>(arch);
    assert(0 <= idx && idx < static_cast<int>(pal::architecture::__last));
    return s_all_architectures[idx];
}

const pal::char_t* get_current_arch_name()
{
    assert(pal::strcmp(get_arch_name(get_current_arch()), _STRINGIFY(CURRENT_ARCH_NAME)) == 0);
    return _STRINGIFY(CURRENT_ARCH_NAME);
}

pal::string_t get_runtime_id()
{
    pal_char_t* rid = utils_get_runtime_id();
    if (rid == nullptr)
        return pal::string_t(_STRINGIFY(HOST_RID_PLATFORM) _X("-") _STRINGIFY(CURRENT_ARCH_NAME));

    pal::string_t result = rid;
    free(rid);
    return result;
}

bool try_get_runtime_id_from_env(pal::string_t& out_rid)
{
    return pal::getenv(_X("DOTNET_RUNTIME_ID"), &out_rid);
}

/**
* Multilevel Lookup is enabled by default
*  It can be disabled by setting DOTNET_MULTILEVEL_LOOKUP env var to a value that is not 1
*/
bool multilevel_lookup_enabled()
{
    pal::string_t env_lookup;
    bool multilevel_lookup = true;

    if (pal::getenv(_X("DOTNET_MULTILEVEL_LOOKUP"), &env_lookup))
    {
        auto env_val = pal::xtoi(env_lookup.c_str());
        multilevel_lookup = (env_val == 1);
        trace::verbose(_X("DOTNET_MULTILEVEL_LOOKUP is set to %s"), env_lookup.c_str());
    }
    trace::info(_X("Multilevel lookup is %s"), multilevel_lookup ? _X("true") : _X("false"));
    return multilevel_lookup;
}

void get_framework_locations(const pal::string_t& dotnet_dir, const bool disable_multilevel_lookup, std::vector<pal::string_t>* locations)
{
    bool multilevel_lookup = disable_multilevel_lookup ? false : multilevel_lookup_enabled();

    // Multi-level lookup will look for the most appropriate version in several locations
    // by following the priority rank below:
    //  .exe directory
    //  Global .NET directories
    // If it is not activated, then only .exe directory will be considered

    pal::string_t dotnet_dir_temp;
    if (!dotnet_dir.empty())
    {
        // own_dir contains DIR_SEPARATOR appended that we need to remove.
        dotnet_dir_temp = dotnet_dir;
        remove_trailing_dir_separator(&dotnet_dir_temp);

        locations->push_back(dotnet_dir_temp);
    }

    if (!multilevel_lookup)
        return;

    std::vector<pal::string_t> global_dirs;
    if (pal::get_global_dotnet_dirs(&global_dirs))
    {
        for (pal::string_t dir : global_dirs)
        {
            // avoid duplicate paths
            if (!pal::are_paths_equal_with_normalized_casing(dir, dotnet_dir_temp))
            {
                locations->push_back(dir);
            }
        }
    }
}

bool get_file_path_from_env(const pal::char_t* env_key, pal::string_t* recv)
{
    recv->clear();
    pal_char_t* file_path = utils_get_file_path_from_env(env_key);
    if (file_path == nullptr)
        return false;

    recv->assign(file_path);
    free(file_path);
    return true;
}

size_t index_of_non_numeric(const pal::string_t& str, size_t i)
{
    return str.find_first_not_of(_X("0123456789"), i);
}

bool try_stou(const pal::string_t& str, unsigned* num)
{
    if (str.empty())
    {
        return false;
    }
    if (index_of_non_numeric(str, 0u) != pal::string_t::npos)
    {
        return false;
    }
    *num = std::stoul(str);
    return true;
}

pal::string_t get_dotnet_root_env_var_for_arch(pal::architecture arch)
{
    return DOTNET_ROOT_ENV_VAR _X("_") + to_upper(get_arch_name(arch));
}

bool get_dotnet_root_from_env(pal::string_t* dotnet_root_env_var_name, pal::string_t* recv)
{
    const pal_char_t* env_var_name = nullptr;
    pal_char_t* dotnet_root = nullptr;
    if (!utils_get_dotnet_root_from_env(&env_var_name, &dotnet_root))
    {
        recv->clear();
        return false;
    }

    dotnet_root_env_var_name->assign(env_var_name);
    recv->assign(dotnet_root);
    free(dotnet_root);
    return true;
}

/**
* Given path to app binary, say app.dll or app.exe, retrieve the app.deps.json.
*/
pal::string_t get_deps_from_app_binary(const pal::string_t& app_base, const pal::string_t& app)
{
    pal::string_t deps_file;
    auto app_name = get_filename(app);

    deps_file.reserve(app_base.length() + 1 + app_name.length() + 5);
    deps_file.append(app_base);

    if (!app_base.empty() && app_base.back() != DIR_SEPARATOR)
    {
        deps_file.push_back(DIR_SEPARATOR);
    }
    deps_file.append(app_name, 0, app_name.find_last_of(_X(".")));
    deps_file.append(_X(".deps.json"));
    return deps_file;
}

pal::string_t get_runtime_config_path(const pal::string_t& path, const pal::string_t& name)
{
    auto json_path = path;
    auto json_name = name + _X(".runtimeconfig.json");
    append_path(&json_path, json_name.c_str());
    return json_path;
}

pal::string_t get_runtime_config_dev_path(const pal::string_t& path, const pal::string_t& name)
{
    auto dev_json_path = path;
    auto dev_json_name = name + _X(".runtimeconfig.dev.json");
    append_path(&dev_json_path, dev_json_name.c_str());
    return dev_json_path;
}

void get_runtime_config_paths(const pal::string_t& path, const pal::string_t& name, pal::string_t* cfg, pal::string_t* dev_cfg)
{
    cfg->assign(get_runtime_config_path(path, name));
    dev_cfg->assign(get_runtime_config_dev_path(path, name));

    trace::verbose(_X("Runtime config is cfg=%s dev=%s"), cfg->c_str(), dev_cfg->c_str());
}

pal::string_t get_dotnet_root_from_fxr_path(const pal::string_t& fxr_path)
{
    // If coreclr exists next to hostfxr, assume everything is local (e.g. self-contained)
    pal::string_t fxr_dir = get_directory(fxr_path);
    if (coreclr_exists_in_dir(fxr_dir))
        return fxr_dir;

    // Path to hostfxr is: <dotnet_root>/host/fxr/<version>/<hostfxr_file>
    pal::string_t fxr_root = get_directory(fxr_dir);
    return get_directory(get_directory(fxr_root));
}

pal::string_t get_download_url(const pal::char_t* framework_name, const pal::char_t* framework_version)
{
    pal_char_t url[MAX_DOWNLOAD_URL_LEN];
    utils_get_download_url(url, ARRAY_SIZE(url), framework_name, framework_version);
    return url;
}

pal::string_t get_host_version_description()
{
#if defined(TARGET_WINDOWS)
    return _STRINGIFY(VER_PRODUCTVERSION_STR);
#else
    pal::string_t info {_STRINGIFY(HOST_VERSION)};

    // sccsid is @(#)Version <file_version> [@Commit: <commit_hash>]
    // Get the commit portion if available
    char* commit_maybe = ::strchr(&sccsid[STRING_LENGTH("@(#)Version ")], '@');
    if (commit_maybe != nullptr)
    {
        info.append(" ");
        info.append(commit_maybe);
    }

    return info;
#endif
}

pal::string_t to_lower(const pal::char_t* in) {
    pal::string_t ret = in;
    std::transform(ret.begin(), ret.end(), ret.begin(),
        [](pal::char_t c) { return static_cast<pal::char_t>(::tolower(c)); });
    return ret;
}

pal::string_t to_upper(const pal::char_t* in) {
    pal::string_t ret = in;
    std::transform(ret.begin(), ret.end(), ret.begin(),
        [](pal::char_t c) { return static_cast<pal::char_t>(::toupper(c)); });
    return ret;
}

// Retrieves environment variable which is only used for testing.
// This will return the value of the variable only if the product binary is stamped
// with test-only marker.
bool test_only_getenv(const pal::char_t* name, pal::string_t* recv)
{
    pal_char_t* value = utils_test_only_getenv(name);
    if (value == nullptr)
        return false;

    recv->assign(value);
    free(value);
    return true;
}

// C-callable wrapper over get_host_version_description(), for use by C entrypoints (apphost.c).
extern "C" void utils_get_host_version_description(pal_char_t* out_desc, size_t out_desc_len)
{
    pal::string_t desc = get_host_version_description();
    pal_str_printf(out_desc, out_desc_len, _X("%s"), desc.c_str());
}
