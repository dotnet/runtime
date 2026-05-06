// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <memory>
#include "extractor.h"
#include "runner.h"
#include "trace.h"
#include "header.h"
#include "manifest.h"
#include "utils.h"

using namespace bundle;

// This method processes the bundle manifest.
// It also implements the extraction of files that cannot be directly processed from the bundle.
StatusCode runner_t::extract()
{
    try
    {
        const char* addr = map_bundle();

        // Set the Reader at header_offset
        reader_t reader(addr, m_bundle_size, m_header_offset);
        m_offset_in_file = reader.offset_in_file();

        // Read the bundle header
        m_header = header_t::read(reader);
        m_deps_json.set_location(&m_header.deps_json_location());
        m_runtimeconfig_json.set_location(&m_header.runtimeconfig_json_location());

        // Read the bundle manifest
        m_manifest = manifest_t::read(reader, m_header);

        // Extract the files if necessary
        if (m_manifest.files_need_extraction())
        {
            extractor_t extractor(m_header.bundle_id(), m_bundle_path, m_manifest);
            m_extraction_path = extractor.extract(reader);
        }

        unmap_bundle(addr);

        return StatusCode::Success;
    }
    catch (StatusCode e)
    {
        return e;
    }
}

const file_entry_t*  runner_t::probe(const pal::string_t &relative_path) const
{
    for (const file_entry_t& entry : m_manifest.files)
    {
        if (entry.matches(relative_path))
        {
            assert(!entry.is_disabled());
            return &entry;
        }
    }

    return nullptr;
}

bool runner_t::probe(const pal::string_t& relative_path, int64_t* offset, int64_t* size, int64_t* compressedSize) const
{
    const bundle::file_entry_t* entry = probe(relative_path);

    // Do not report extracted entries - those should be reported through either TPA or resource paths
    if (entry == nullptr || entry->needs_extraction())
    {
        return false;
    }

    assert(!entry->is_disabled());
    assert(entry->offset() != 0);

    *offset = entry->offset() + m_offset_in_file;
    *size = entry->size();
    *compressedSize = entry->compressedSize();

    return true;
}

bool runner_t::locate(const pal::string_t& relative_path, pal::string_t& full_path, bool& extracted_to_disk) const
{
    const bundle::file_entry_t* entry = probe(relative_path);

    if (entry == nullptr)
    {
        full_path.clear();
        return false;
    }

    assert(!entry->is_disabled());

    extracted_to_disk = entry->needs_extraction();
    full_path.assign(extracted_to_disk ? extraction_path() : base_path());

    append_path(&full_path, relative_path.c_str());

    return true;
}

bool runner_t::disable(const pal::string_t& relative_path)
{
    for (file_entry_t& entry : m_manifest.files)
    {
        if (entry.matches(relative_path))
        {
            entry.disable();
            return true;
        }
    }

    return false;
}

