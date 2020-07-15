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

bool ends_with(const pal::string_t& value, const pal::string_t& suffix, bool match_case)
{
    auto cmp = match_case ? pal::strcmp : pal::strcasecmp;
    return (value.size() >= suffix.size()) &&
        cmp(value.c_str() + value.size() - suffix.size(), suffix.c_str()) == 0;
}

bool starts_with(const pal::string_t& value, const pal::string_t& prefix, bool match_case)
{
    if (prefix.empty())
    {
        // Cannot start with an empty string.
        return false;
    }
    auto cmp = match_case ? pal::strncmp : pal::strncasecmp;
    return (value.size() >= prefix.size()) &&
        cmp(value.c_str(), prefix.c_str(), prefix.size()) == 0;
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

void remove_trailing_dir_seperator(pal::string_t* dir)
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

const pal::char_t* get_arch()
{
#if defined(TARGET_AMD64)
    return _X("x64");
#elif defined(TARGET_X86)
    return _X("x86");
#elif defined(TARGET_ARM)
    return _X("arm");
#elif defined(TARGET_ARM64)
    return _X("arm64");
#else
#error "Unknown target"
#endif
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
        rid.append(get_arch());
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

bool get_global_shared_store_dirs(std::vector<pal::string_t>*  dirs, const pal::string_t& arch, const pal::string_t& tfm)
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

void get_framework_and_sdk_locations(const pal::string_t& dotnet_dir, std::vector<pal::string_t>* locations)
{
    bool multilevel_lookup = multilevel_lookup_enabled();

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
        remove_trailing_dir_seperator(&dotnet_dir_temp);

        locations->push_back(dotnet_dir_temp);
    }

    std::vector<pal::string_t> global_dirs;
    if (multilevel_lookup && pal::get_global_dotnet_dirs(&global_dirs))
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

size_t index_of_non_numeric(const pal::string_t& str, unsigned i)
{
    return str.find_first_not_of(_X("0123456789"), i);
}

bool try_stou(const pal::string_t& str, unsigned* num)
{
    if (str.empty())
    {
        return false;
    }
    if (index_of_non_numeric(str, 0) != pal::string_t::npos)
    {
        return false;
    }
    *num = std::stoul(str);
    return true;
}

pal::string_t get_dotnet_root_env_var_name()
{
    if (pal::is_running_in_wow64())
    {
        return pal::string_t(_X("DOTNET_ROOT(x86)"));
    }

    return pal::string_t(_X("DOTNET_ROOT"));
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

pal::string_t get_dotnet_root_from_fxr_path(const pal::string_t &fxr_path)
{
    // If coreclr exists next to hostfxr, assume everything is local (e.g. self-contained)
    pal::string_t fxr_dir = get_directory(fxr_path);
    if (coreclr_exists_in_dir(fxr_dir))
        return fxr_dir;

    // Path to hostfxr is: <dotnet_root>/host/fxr/<version>/<hostfxr_file>
    pal::string_t fxr_root = get_directory(fxr_dir);
    return get_directory(get_directory(fxr_root));
}

pal::string_t get_download_url(const pal::char_t *framework_name, const pal::char_t *framework_version)
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
    url.append(get_arch());
    pal::string_t rid = get_current_runtime_id(true /*use_fallback*/);
    url.append(_X("&rid="));
    url.append(rid);

    return url;
}

#define TEST_ONLY_MARKER "d38cc827-e34f-4453-9df4-1e796e9f1d07"

// Retrieves environment variable which is only used for testing.
// This will return the value of the variable only if the product binary is stamped
// with test-only marker.
bool test_only_getenv(const pal::char_t* name, pal::string_t* recv)
{
    // This is a static variable which is embeded in the product binary (somewhere).
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
