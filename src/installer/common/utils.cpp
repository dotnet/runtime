// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "utils.h"
#include "trace.h"

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
        if (path1->empty() || path1->back() != DIR_SEPARATOR)
        {
            path1->push_back(DIR_SEPARATOR);
        }
        path1->append(path2);
    }
}

pal::string_t get_executable(const pal::string_t& filename)
{
    pal::string_t result(filename);

    if (ends_with(result, _X(".exe"), false))
    {
        // We need to strip off the old extension
        result.erase(result.size() - 4);
    }

    return result;
}

pal::string_t strip_file_ext(const pal::string_t& path)
{
    if (path.empty())
    {
        return path;
    }
    return path.substr(0, path.rfind(_X('.')));
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
    size_t count = (dot_pos == pal::string_t::npos) ? pal::string_t::npos : (dot_pos - start_pos);
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
    // Find the last dir separator
    auto path_sep = path.find_last_of(DIR_SEPARATOR);
    if (path_sep == pal::string_t::npos)
    {
        return pal::string_t(path);
    }

    return path.substr(0, path_sep);
}

void replace_char(pal::string_t* path, pal::char_t match, pal::char_t repl)
{
    int pos = 0;
    while ((pos = path->find(match, pos)) != pal::string_t::npos)
    {
        (*path)[pos] = repl;
    }
}

const pal::char_t* get_arch()
{
#if _TARGET_AMD64_
    return _X("x64");
#elif _TARGET_X86_
    return _X("x86");
#else
#error "Unknown target"
#endif
}

bool parse_known_args(
    const int argc,
    const pal::char_t* argv[],
    const std::vector<pal::string_t>& known_opts,
    std::unordered_map<pal::string_t, pal::string_t>* opts,
    int* num_args)
{
    int arg_i = *num_args;
    while (arg_i < argc)
    {
        pal::string_t arg = argv[arg_i];
        if (std::find(known_opts.begin(), known_opts.end(), pal::to_lower(arg)) == known_opts.end())
        {
            // Unknown argument.
            break;
        }

        // Known argument, so expect one more arg (value) to be present.
        if (arg_i + 1 >= argc)
        {
            return false;
        }

        (*opts)[arg] = argv[arg_i + 1];

        // Increment for both the option and its value.
        arg_i += 2;
    }

    *num_args = arg_i;

    return true;
}

