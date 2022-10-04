// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "utils.h"
#include "trace.h"
#include "bundle/info.h"

bool library_exists_in_dir(const pal::string_t& lib_dir, const pal::string_t& lib_name, pal::string_t* p_lib_path)
{
    pal::string_t lib_path = lib_dir;
    append_path(&lib_path, lib_name.c_str());

    if (!pal::file_exists(lib_path))
    {
        return false;
    }
    if (p_lib_path)
    {
        *p_lib_path = lib_path;
    }
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

bool ends_with(const pal::string_t& value, const pal::string_t& suffix, bool match_case)
{
    return utils::ends_with(value, suffix.c_str(), suffix.size(), match_case);
}

bool starts_with(const pal::string_t& value, const pal::string_t& prefix, bool match_case)
{
    return utils::starts_with(value, prefix.c_str(), prefix.size(), match_case);
}

void append_path(pal::string_t* path1, const pal::char_t* path2)
{
    if (pal::is_path_rooted(path2))
    {
        path1->assign(path2);
    }
    else
    {
        if (!path1->empty() && path1->back() != DIR_SEPARATOR)
        {
            path1->push_back(DIR_SEPARATOR);
        }
        path1->append(path2);
    }
}

pal::string_t strip_executable_ext(const pal::string_t& filename)
{
    pal::string_t exe_suffix = pal::exe_suffix();
    if (exe_suffix.empty())
    {
        return filename;
    }

    if (ends_with(filename, exe_suffix, false))
    {
        // We need to strip off the old extension
        pal::string_t result(filename);
        result.erase(result.size() - exe_suffix.size());
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
    pal::string_t ret = path;
    while (!ret.empty() && ret.back() == DIR_SEPARATOR)
    {
        ret.pop_back();
    }

    // Find the last dir separator
    auto path_sep = ret.find_last_of(DIR_SEPARATOR);
    if (path_sep == pal::string_t::npos)
    {
        return ret + DIR_SEPARATOR;
    }

    int pos = static_cast<int>(path_sep);
    while (pos >= 0 && ret[pos] == DIR_SEPARATOR)
    {
        pos--;
    }
    return ret.substr(0, static_cast<size_t>(pos) + 1) + DIR_SEPARATOR;
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
        _X("x86")
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
    return get_arch_name(get_current_arch());
}

pal::string_t get_current_runtime_id(bool use_fallback)
{
    pal::string_t rid;
    if (pal::getenv(_X("DOTNET_RUNTIME_ID"), &rid))
        return rid;

    rid = pal::get_current_os_rid_platform();
    if (rid.empty() && use_fallback)
        rid = pal::get_current_os_fallback_rid();

    if (!rid.empty())
    {
        rid.append(_X("-"));
        rid.append(get_current_arch_name());
    }

    return rid;
}

bool get_env_shared_store_dirs(std::vector<pal::string_t>* dirs, const pal::string_t& arch, const pal::string_t& tfm)
{
    pal::string_t path;
    if (!pal::getenv(_X("DOTNET_SHARED_STORE"), &path))
    {
        return false;
    }

    pal::string_t tok;
    pal::stringstream_t ss(path);
    while (std::getline(ss, tok, PATH_SEPARATOR))
    {
        if (pal::realpath(&tok))
        {
            append_path(&tok, arch.c_str());
            append_path(&tok, tfm.c_str());
            dirs->push_back(tok);
        }
    }
    return true;
}

bool get_global_shared_store_dirs(std::vector<pal::string_t>* dirs, const pal::string_t& arch, const pal::string_t& tfm)
{
    std::vector<pal::string_t> global_dirs;
    if (!pal::get_global_dotnet_dirs(&global_dirs))
    {
        return false;
    }

    for (pal::string_t dir : global_dirs)
    {
        append_path(&dir, RUNTIME_STORE_DIRECTORY_NAME);
        append_path(&dir, arch.c_str());
        append_path(&dir, tfm.c_str());
        dirs->push_back(dir);
    }
    return true;
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

void get_framework_and_sdk_locations(const pal::string_t& dotnet_dir, const bool disable_multilevel_lookup, std::vector<pal::string_t>* locations)
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
    pal::string_t file_path;
    if (pal::getenv(env_key, &file_path))
    {
        if (pal::realpath(&file_path))
        {
            recv->assign(file_path);
            return true;
        }
        trace::verbose(_X("Did not find [%s] directory [%s]"), env_key, file_path.c_str());
    }

    return false;
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
    *dotnet_root_env_var_name = get_dotnet_root_env_var_for_arch(get_current_arch());
    if (get_file_path_from_env(dotnet_root_env_var_name->c_str(), recv))
        return true;

#if defined(WIN32)
    if (pal::is_running_in_wow64())
    {
        *dotnet_root_env_var_name = _X("DOTNET_ROOT(x86)");
        if (get_file_path_from_env(dotnet_root_env_var_name->c_str(), recv))
            return true;
    }
#endif

    // If no architecture-specific environment variable was set
    // fallback to the default DOTNET_ROOT.
    *dotnet_root_env_var_name = DOTNET_ROOT_ENV_VAR;
    return get_file_path_from_env(dotnet_root_env_var_name->c_str(), recv);
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
    pal::string_t url = DOTNET_CORE_APPLAUNCH_URL _X("?");
    if (framework_name != nullptr && pal::strlen(framework_name) > 0)
    {
        url.append(_X("framework="));
        url.append(framework_name);
        if (framework_version != nullptr && pal::strlen(framework_version) > 0)
        {
            url.append(_X("&framework_version="));
            url.append(framework_version);
        }
    }
    else
    {
        url.append(_X("missing_runtime=true"));
    }

    url.append(_X("&arch="));
    url.append(get_current_arch_name());
    pal::string_t rid = get_current_runtime_id(true /*use_fallback*/);
    url.append(_X("&rid="));
    url.append(rid);

    return url;
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

#define TEST_ONLY_MARKER "d38cc827-e34f-4453-9df4-1e796e9f1d07"

// Retrieves environment variable which is only used for testing.
// This will return the value of the variable only if the product binary is stamped
// with test-only marker.
bool test_only_getenv(const pal::char_t* name, pal::string_t* recv)
{
    // This is a static variable which is embedded in the product binary (somewhere).
    // The marker values is a GUID so that it's unique and can be found by doing a simple search on the file
    // The first character is used as the decider:
    //  - Default value is 'd' (stands for disabled) - test only behavior is disabled
    //  - To enable test-only behaviors set it to 'e' (stands for enabled)
    constexpr size_t EMBED_SIZE = sizeof(TEST_ONLY_MARKER) / sizeof(TEST_ONLY_MARKER[0]);
    volatile static char embed[EMBED_SIZE] = TEST_ONLY_MARKER;

    if (embed[0] != 'e')
    {
        return false;
    }

    return pal::getenv(name, recv);
}
