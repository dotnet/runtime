// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "header.h"
#include "reader.h"
#include "error_codes.h"
#include "trace.h"

using namespace bundle;

bool header_fixed_t::is_valid() const
{
    if (num_embedded_files <= 0)
    {
        return false;
    }

    // .net 6 host expects the version information to be 6.0
    // .net 5 host expects the version information to be 2.0
    // .net core 3 single-file bundles are handled within the netcoreapp3.x apphost, and are not processed here in the framework.
    return ((major_version == 6) && (minor_version == 0)) ||
           ((major_version == 2) && (minor_version == 0));
}

header_t header_t::read(reader_t& reader)
{
    const header_fixed_t* fixed_header = reinterpret_cast<const header_fixed_t*>(reader.read_direct(sizeof(header_fixed_t)));

    if (!fixed_header->is_valid())
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Bundle header version compatibility check failed."));

        throw StatusCode::BundleExtractionFailure;
    }

    header_t header(fixed_header->major_version, fixed_header->minor_version, fixed_header->num_embedded_files);

    // bundle_id is a component of the extraction path
    reader.read_path_string(header.m_bundle_id);

    const header_fixed_v2_t *v2_header = reinterpret_cast<const header_fixed_v2_t*>(reader.read_direct(sizeof(header_fixed_v2_t)));
    header.m_v2_header = *v2_header;

    return header;
}
