// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "utils.h"
#include "trace.h"

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
#if defined(_TARGET_AMD64_)
    return _X("x64");
#elif defined(_TARGET_X86_)
    return _X("x86");
#elif defined(_TARGET_ARM_)
    return _X("arm");
#elif defined(_TARGET_ARM64_)
    return _X("arm64");
#else
#error "Unknown target"
#endif
}

pal::string_t get_last_known_arg(
    const opt_map_t& opts,
    const pal::string_t& opt_key,
    const pal::string_t& de_fault)
{
    if (opts.count(opt_key))
    {
        const auto& val = opts.find(opt_key)->second;
        return val[val.size() - 1];
    }
    return de_fault;
}

bool parse_known_args(
    const int argc,
    const pal::char_t* argv[],
    const std::vector<host_option>& known_opts,
    // Although multimap would provide this functionality the order of kv, values are
    // not preserved in C++ < C++0x
    opt_map_t* opts,
    int* num_args)
{
    int arg_i = *num_args;
    while (arg_i < argc)
    {
        pal::string_t arg = argv[arg_i];
        pal::string_t arg_lower = pal::to_lower(arg);
        if (std::find_if(known_opts.begin(), known_opts.end(),
            [&](const host_option& hostoption) { return arg_lower == hostoption.option; })
            == known_opts.end())
        {
            // Unknown argument.
            break;
        }

        // Known argument, so expect one more arg (value) to be present.
        if (arg_i + 1 >= argc)
        {
            return false;
        }

        trace::verbose(_X("Parsed known arg %s = %s"), arg.c_str(), argv[arg_i + 1]);
        (*opts)[arg_lower].push_back(argv[arg_i + 1]);

        // Increment for both the option and its value.
        arg_i += 2;
    }

    *num_args = arg_i;

    return true;
}

// Try to match 0xEF 0xBB 0xBF byte sequence (no endianness here.)
bool skip_utf8_bom(pal::istream_t* stream)
{
    if (stream->eof() || !stream->good())
    {
        return false;
    }

    int peeked = stream->peek();
    if (peeked == EOF || ((peeked & 0xFF) != 0xEF))
    {
        return false;
    }

    unsigned char bytes[3];
    stream->read(reinterpret_cast<char*>(bytes), 3);
    if ((stream->gcount() < 3) ||
            (bytes[1] != 0xBB) ||
            (bytes[2] != 0xBF))
    {
        // Reset to 0 if returning false.
        stream->seekg(0, stream->beg);
        return false;
    }

    return true;
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
    }
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

void get_runtime_config_paths(const pal::string_t& path, const pal::string_t& name, pal::string_t* cfg, pal::string_t* dev_cfg)
{
    auto json_path = path;
    auto json_name = name + _X(".runtimeconfig.json");
    append_path(&json_path, json_name.c_str());
    cfg->assign(json_path);

    auto dev_json_path = path;
    auto dev_json_name = name + _X(".runtimeconfig.dev.json");
    append_path(&dev_json_path, dev_json_name.c_str());
    dev_cfg->assign(dev_json_path);

    trace::verbose(_X("Runtime config is cfg=%s dev=%s"), json_path.c_str(), dev_json_path.c_str());
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
