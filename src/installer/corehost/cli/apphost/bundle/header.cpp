// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "header.h"
#include "reader.h"
#include "error_codes.h"
#include "trace.h"

using namespace bundle;

// The AppHost expects the bundle_header to be an exact_match for which it was built.
// The framework accepts backwards compatible header versions.
bool header_fixed_t::is_valid(bool exact_match) const
{
    if (num_embedded_files <= 0)
    {
        return false;
    }

    if (exact_match)
    {
        return (major_version == header_t::major_version) && (minor_version == header_t::minor_version);
    }

    return ((major_version < header_t::major_version) ||
            (major_version == header_t::major_version && minor_version <= header_t::minor_version));
}

header_t header_t::read(reader_t& reader, bool need_exact_version)
{
    const header_fixed_t* fixed_header = reinterpret_cast<const header_fixed_t*>(reader.read_direct(sizeof(header_fixed_t)));

    if (!fixed_header->is_valid(need_exact_version))
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Bundle header version compatibility check failed."));

        throw StatusCode::BundleExtractionFailure;
    }

    header_t header(fixed_header->num_embedded_files);

    // bundle_id is a component of the extraction path
    reader.read_path_string(header.m_bundle_id);

    if (fixed_header->major_version > 1)
    {
        header.m_v2_header = reinterpret_cast<const header_fixed_v2_t*>(reader.read_direct(sizeof(header_fixed_v2_t)));
    }

    return header;
}
