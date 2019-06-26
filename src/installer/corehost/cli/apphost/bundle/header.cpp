// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "bundle_runner.h"
#include "error_codes.h"
#include "trace.h"
#include "utils.h"

using namespace bundle;

bool header_t::is_valid()
{
    return m_data.num_embedded_files > 0 &&
           ((m_data.major_version < current_major_version) ||
            (m_data.major_version == current_major_version && m_data.minor_version <= current_minor_version));
}

header_t* header_t::read(FILE* stream)
{
    header_t* header = new header_t();

    // First read the fixed size portion of the header
    bundle_runner_t::read(&header->m_data, sizeof(header->m_data), stream);
    if (!header->is_valid())
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Bundle header version compatibility check failed"));

        throw StatusCode::BundleExtractionFailure;
    }

    // bundle_id is a component of the extraction path
    size_t bundle_id_length = 
        bundle_runner_t::get_path_length(header->m_data.bundle_id_length_byte_1, stream);
     
    // Next read the bundle-ID string, given its length
    bundle_runner_t::read_string(header->m_bundle_id, bundle_id_length, stream);

    return header;
}
