#include <set>
#include <functional>

#include "trace.h"
#include "tpafile.h"
#include "utils.h"

bool read_field(pal::string_t line, int& offset, pal::string_t& value_recv)
{
    // The first character should be a '"'
    if (line[offset] != '"')
    {
        trace::error(_X("error reading TPA file"));
        return false;
    }
    offset++;

    // Set up destination buffer (it can't be bigger than the original line)
    pal::char_t buf[PATH_MAX];
    auto buf_offset = 0;

    // Iterate through characters in the string
    for (; offset < line.length(); offset++)
    {
        // Is this a '\'?
        if (line[offset] == '\\')
        {
            // Skip this character and read the next character into the buffer
            offset++;
            buf[buf_offset] = line[offset];
        }
        // Is this a '"'?
        else if (line[offset] == '\"')
        {
            // Done! Advance to the pointer after the input
            offset++;
            break;
        }
        else
        {
            // Take the character
            buf[buf_offset] = line[offset];
        }
        buf_offset++;
    }
    buf[buf_offset] = '\0';
    value_recv.assign(buf);

    // Consume the ',' if we have one
    if (line[offset] == ',')
    {
        offset++;
    }
    return true;
}

bool tpafile::load(pal::string_t path)
{
    // Check if the file exists, if not, there is nothing to add
    if (!pal::file_exists(path))
    {
        return true;
    }

    // Open the file
    pal::ifstream_t file(path);
    if (!file.good())
    {
        // Failed to open the file!
        return false;
    }

    // Read lines from the file
    while (true)
    {
        std::string line;
        std::getline(file, line);
        auto line_palstr = pal::to_palstring(line);
        if (file.eof())
        {
            break;
        }

        auto offset = 0;

        tpaentry_t entry;

        // Read fields
        if (!(read_field(line_palstr, offset, entry.library_type))) return false;
        if (!(read_field(line_palstr, offset, entry.library_name))) return false;
        if (!(read_field(line_palstr, offset, entry.library_version))) return false;
        if (!(read_field(line_palstr, offset, entry.library_hash))) return false;
        if (!(read_field(line_palstr, offset, entry.asset_type))) return false;
        if (!(read_field(line_palstr, offset, entry.asset_name))) return false;
        if (!(read_field(line_palstr, offset, entry.relative_path))) return false;

        m_entries.push_back(entry);
    }

    return true;
}

void tpafile::add_from_local_dir(const pal::string_t& dir)
{
    trace::verbose(_X("adding files from %s to TPA"), dir.c_str());
    const pal::char_t * const tpa_extensions[] = {
        _X(".ni.dll"),      // Probe for .ni.dll first so that it's preferred if ni and il coexist in the same dir
        _X(".dll"),
        _X(".ni.exe"),
        _X(".exe"),
    };

    std::set<pal::string_t> added_assemblies;

    // Get directory entries
    auto files = pal::readdir(dir);
    for (auto ext : tpa_extensions)
    {
        auto len = pal::strlen(ext);
        for (auto file : files)
        {
            // Can't be a match if it's the same length as the extension :)
            if (file.length() > len)
            {
                // Extract the same amount of text from the end of file name
                auto file_ext = file.substr(file.length() - len, len);

                // Check if this file name matches
                if (pal::strcasecmp(ext, file_ext.c_str()) == 0)
                {
                    // Get the assembly name by stripping the extension
                    // and add it to the set so we can de-dupe
                    auto asm_name = file.substr(0, file.length() - len);

                    // TODO(anurse): Also check if already in TPA file
                    if (added_assemblies.find(asm_name) == added_assemblies.end())
                    {
                        added_assemblies.insert(asm_name);

                        tpaentry_t entry;
                        entry.asset_type = pal::string_t(_X("runtime"));
                        entry.library_name = pal::string_t(asm_name);
                        entry.library_version = pal::string_t(_X(""));

                        pal::string_t relpath(dir);
                        relpath.push_back(DIR_SEPARATOR);
                        relpath.append(file);
                        entry.relative_path = relpath;
                        entry.asset_name = asm_name;

                        trace::verbose(_X("adding %s to TPA list from %s"), asm_name.c_str(), relpath.c_str());
                        m_entries.push_back(entry);
                    }
                }
            }
        }
    }
}

void tpafile::write_tpa_list(pal::string_t& output)
{
    std::set<pal::string_t> items;
    for (auto entry : m_entries)
    {
        if (pal::strcmp(entry.asset_type.c_str(), _X("runtime")) == 0 && items.find(entry.asset_name) == items.end())
        {
            // Resolve the full path
            for (auto search_path : m_package_search_paths)
            {
                pal::string_t candidate;
                candidate.reserve(search_path.length() +
                    entry.library_name.length() +
                    entry.library_version.length() +
                    entry.relative_path.length() + 3);
                candidate.append(search_path);

                append_path(candidate, entry.library_name.c_str());
                append_path(candidate, entry.library_version.c_str());
                append_path(candidate, entry.relative_path.c_str());
                if (pal::file_exists(candidate))
                {
                    trace::verbose(_X("adding tpa entry: %s"), candidate.c_str());

                    output.append(candidate);
                    output.push_back(PATH_SEPARATOR);
                    items.insert(entry.asset_name);
                    break;
                }
            }
        }
    }
}

void tpafile::write_native_paths(pal::string_t& output)
{
    std::set<pal::string_t> items;
    for (auto search_path : m_native_search_paths)
    {
        if (items.find(search_path) == items.end())
        {
            trace::verbose(_X("adding native search path: %s"), search_path.c_str());
            output.append(search_path);
            output.push_back(PATH_SEPARATOR);
            items.insert(search_path);
        }
    }

    for (auto entry : m_entries)
    {
        auto dir = entry.relative_path.substr(0, entry.relative_path.find_last_of(DIR_SEPARATOR));
        if (pal::strcmp(entry.asset_type.c_str(), _X("native")) == 0 && items.find(dir) == items.end())
        {
            // Resolve the full path
            for (auto search_path : m_package_search_paths)
            {
                pal::string_t candidate;
                candidate.reserve(search_path.length() +
                    entry.library_name.length() +
                    entry.library_version.length() +
                    dir.length() + 3);
                candidate.append(search_path);

                append_path(candidate, entry.library_name.c_str());
                append_path(candidate, entry.library_version.c_str());
                append_path(candidate, entry.relative_path.c_str());

                if (pal::file_exists(candidate))
                {
                    trace::verbose(_X("adding native search path: %s"), candidate.c_str());
                    output.append(candidate);
                    output.push_back(PATH_SEPARATOR);
                    items.insert(dir);
                    break;
                }
            }
        }
    }
}

void tpafile::add_package_dir(pal::string_t dir)
{
    m_package_search_paths.push_back(dir);
}

void tpafile::add_native_search_path(pal::string_t dir)
{
    m_native_search_paths.push_back(dir);
}
