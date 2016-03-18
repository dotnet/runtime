// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "trace.h"
#include "servicing_index.h"

static const pal::char_t* DOTNET_SERVICING_INDEX_TXT = _X("dotnet_servicing_index.txt");

servicing_index_t::servicing_index_t(const pal::string_t& svc_dir)
{
    m_patch_root = svc_dir;
    if (!m_patch_root.empty())
    {
        m_index_file.assign(m_patch_root);
        append_path(&m_index_file, DOTNET_SERVICING_INDEX_TXT);
    }
    m_parsed = m_index_file.empty() || !pal::file_exists(m_index_file);
}

bool servicing_index_t::find_redirection(
        const pal::string_t& package_name,
        const pal::string_t& package_version,
        const pal::string_t& package_relative,
        pal::string_t* redirection)
{
    ensure_redirections();

    redirection->clear();

    if (m_redirections.empty())
    {
        return false;
    }

    pal::stringstream_t stream;
    stream << package_name << _X("|") << package_version << _X("|") << package_relative;

    auto iter = m_redirections.find(stream.str());
    if (iter != m_redirections.end())
    {
        pal::string_t ni_root = m_patch_root;
        append_path(&ni_root, get_arch());

        // First prefer the architecture specific NI image.
        pal::string_t paths[2] = { ni_root, m_patch_root };
        for (pal::string_t& full_path : paths)
        {
            append_path(&full_path, iter->second.c_str());
            if (pal::file_exists(full_path))
            {
                *redirection = full_path;
                if (trace::is_enabled())
                {
                    pal::string_t stream_str = stream.str();
                    trace::verbose(_X("Servicing %s with %s"), stream_str.c_str(), redirection->c_str());
                }
                return true;
            }
            trace::verbose(_X("Serviced file %s doesn't exist"), full_path.c_str());
        }
    }

    if (trace::is_enabled())
    {
        auto stream_str = stream.str();
        trace::verbose(_X("Entry %s not serviced or file doesn't exist"), stream_str.c_str());
    }
    return false;
}

void servicing_index_t::ensure_redirections()
{
    if (m_parsed)
    {
        return;
    }

    pal::ifstream_t fstream(m_index_file);
    if (!fstream.good())
    {
        return;
    }

    pal::stringstream_t sstream;
    std::string line;
    while (std::getline(fstream, line))
    {
        pal::string_t str;
        pal::to_palstring(line.c_str(), &str);

        // Can interpret line as "package"?
        pal::string_t prefix = _X("package|");
        if (str.find(prefix) != 0)
        {
            continue;
        }

        pal::string_t name, version, relative;
        pal::string_t* tokens[] = { &name, &version, &relative };
        pal::string_t delim[] = { pal::string_t(_X("|")), pal::string_t(_X("|")), pal::string_t(_X("=")) };

        bool bad_line = false;

        size_t from = prefix.length();
        for (size_t cur = 0; cur < (sizeof(delim) / sizeof(delim[0])); ++cur)
        {
            size_t pos = str.find(delim[cur], from);
            if (pos == pal::string_t::npos)
            {
                bad_line = true;
                break;
            }

            tokens[cur]->assign(str.substr(from, pos - from));
            from = pos + 1;
        }

        if (bad_line)
        {
            trace::error(_X("Invalid line in servicing index. Skipping..."));
            continue;
        }

        // Save redirection for this package.
        sstream.str(_X(""));
        sstream << name << _X("|") << version << _X("|") << relative;

        if (trace::is_enabled())
        {
            auto stream_str = sstream.str();
            trace::verbose(_X("Adding servicing entry %s => %s"), stream_str.c_str(), str.substr(from).c_str());
        }

        // Store just the filename.
        pal::string_t redir = str.substr(from);
        if (_X('/') != DIR_SEPARATOR)
        {
            replace_char(&redir, _X('/'), DIR_SEPARATOR);
        }
        m_redirections.emplace(sstream.str(), redir);
    }

    m_parsed = true;
}
