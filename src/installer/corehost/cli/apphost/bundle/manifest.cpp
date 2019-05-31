// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "bundle_runner.h"
#include "pal.h"
#include "error_codes.h"
#include "trace.h"
#include "utils.h"

using namespace bundle;

bool manifest_header_t::is_valid()
{
    return m_data.major_version == m_current_major_version &&
           m_data.minor_version == m_current_minor_version &&
           m_data.num_embedded_files > 0;
}

manifest_header_t* manifest_header_t::read(FILE* stream)
{
    manifest_header_t* header = new manifest_header_t();

    // First read the fixed size portion of the header
    bundle_runner_t::read(&header->m_data, sizeof(header->m_data), stream);
    if (!header->is_valid())
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Manifest header version compatibility check failed"));

        throw StatusCode::BundleExtractionFailure;
    }

    // bundle_id is a component of the extraction path
    size_t bundle_id_length = 
        bundle_runner_t::get_path_length(header->m_data.bundle_id_length_byte_1, stream);
     
    // Next read the bundle-ID string, given its length
    bundle_runner_t::read_string(header->m_bundle_id, bundle_id_length, stream);

    return header;
}

const char* manifest_footer_t::m_expected_signature = ".NetCoreBundle";

bool manifest_footer_t::is_valid()
{
    return m_header_offset > 0 &&
        m_signature_length == 14 &&
        strcmp(m_signature, m_expected_signature) == 0;
}

manifest_footer_t* manifest_footer_t::read(FILE* stream)
{
    manifest_footer_t* footer = new manifest_footer_t();

    bundle_runner_t::read(footer, num_bytes_read(), stream);

    return footer;
}

manifest_t* manifest_t::read(FILE* stream, int32_t num_files)
{
    manifest_t* manifest = new manifest_t();

    for (int32_t i = 0; i < num_files; i++)
    {
        file_entry_t* entry = file_entry_t::read(stream);
        if (entry == nullptr)
        {
            return nullptr;
        }

        manifest->files.push_back(entry);
    }

    return manifest;
}
