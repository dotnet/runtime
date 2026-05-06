// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __RUNNER_H__
#define __RUNNER_H__

#include "error_codes.h"
#include "header.h"
#include "manifest.h"
#include "info.h"

// bundle::runner extends bundle::info to supports:
// * Reading the bundle manifest and identifying file locations for the runtime
// * Extracting bundled files to disk when necessary
// bundle::runner is used by HostPolicy.

namespace bundle
{
    class runner_t : public info_t
    {
    public:
        runner_t(const pal::char_t* bundle_path,
            const pal::char_t* app_path,
            int64_t header_offset)
            : info_t(bundle_path, app_path, header_offset) {}

        const pal::string_t& extraction_path() const { return m_extraction_path; }
        bool has_base(const pal::string_t& base) const { return base.compare(base_path()) == 0; }

        bool probe(const pal::string_t& relative_path, int64_t* offset, int64_t* size, int64_t* compressedSize) const;
        const file_entry_t* probe(const pal::string_t& relative_path) const;
        bool locate(const pal::string_t& relative_path, pal::string_t& full_path, bool& extracted_to_disk) const;
        bool locate(const pal::string_t& relative_path, pal::string_t& full_path) const
        {
            bool extracted_to_disk;
            return locate(relative_path, full_path, extracted_to_disk);
        }
        bool disable(const pal::string_t& relative_path);

        static StatusCode process_manifest_and_extract()
        {
            return mutable_app()->extract();
        }
        
        static const runner_t* app() { return (const runner_t*)the_app; }
        static runner_t* mutable_app() { return (runner_t*)the_app; }

    private:

        StatusCode extract();

        manifest_t m_manifest;
        pal::string_t m_extraction_path;
    };
}

#endif // __RUNNER_H__
