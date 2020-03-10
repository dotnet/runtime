// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <memory>
#include "extractor.h"
#include "runner.h"
#include "trace.h"
#include "header.h"
#include "marker.h"
#include "manifest.h"

using namespace bundle;

void runner_t::map_host()
{
    m_bundle_map = (int8_t *) pal::map_file_readonly(m_bundle_path, m_bundle_length);

    if (m_bundle_map == nullptr)
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Couldn't memory map the bundle file for reading."));
        throw StatusCode::BundleExtractionIOError;
    }
}

void runner_t::unmap_host()
{
    if (!pal::unmap_file(m_bundle_map, m_bundle_length))
    {
        trace::warning(_X("Failed to unmap bundle after extraction."));
    }
}

// Current support for executing single-file bundles involves 
// extraction of embedded files to actual files on disk. 
// This method implements the file extraction functionality at startup.
StatusCode runner_t::extract()
{
    try
    {
        map_host();
        reader_t reader(m_bundle_map, m_bundle_length);

        // Read the bundle header
        reader.set_offset(marker_t::header_offset());
        header_t header = header_t::read(reader);

        // Read the bundle manifest
        // Reader is at the correct offset
        manifest_t manifest = manifest_t::read(reader, header.num_embedded_files());

        // Extract the files 
        extractor_t extractor(header.bundle_id(), m_bundle_path, manifest);
        m_extraction_dir = extractor.extract(reader);

        unmap_host();
        return StatusCode::Success;
    }
    catch (StatusCode e)
    {
        return e;
    }
}

