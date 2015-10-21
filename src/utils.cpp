#include "utils.h"

void append_path(pal::string_t& path1, const pal::char_t* path2)
{
    if (pal::is_path_rooted(path2))
    {
        path1.assign(path2);
    }
    else
    {
        if (path1.back() != DIR_SEPARATOR)
        {
            path1.push_back(DIR_SEPARATOR);
        }
        path1.append(path2);
    }
}

pal::string_t change_extension(const pal::string_t& filename, const pal::char_t* new_extension)
{
    pal::string_t result(filename);

    auto ext_sep = result.find_last_of('.');
    if (ext_sep != pal::string_t::npos)
    {
        // We need to strip off the old extension
        result.erase(ext_sep);
    }

    // Append the new extension
    result.append(new_extension);
    return result;
}

pal::string_t get_filename(const pal::string_t& path)
{
    // Find the last dir separator
    auto path_sep = path.find_last_of(DIR_SEPARATOR);
    if (path_sep == pal::string_t::npos)
    {
        return pal::string_t(path);
    }

    return path.substr(path_sep + 1);
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
