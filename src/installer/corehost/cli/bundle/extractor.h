// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __EXTRACTOR_H__
#define __EXTRACTOR_H__

#include "reader.h"
#include "manifest.h"

namespace bundle
{
    class extractor_t
    {
    public:
        extractor_t(const pal::string_t &bundle_id,
                    const pal::string_t& bundle_path,
                    const manifest_t &manifest)
            :m_extraction_dir(),
             m_working_extraction_dir(),
             m_manifest(manifest)
        {
            m_bundle_id = bundle_id;
            m_bundle_path = bundle_path;
        }

        pal::string_t& extract(reader_t& reader);

    private:
        pal::string_t& extraction_dir();
        pal::string_t& working_extraction_dir();

        void extract_new(reader_t& reader);
        void verify_recover_extraction(reader_t& reader);

        FILE* create_extraction_file(const pal::string_t& relative_path);
        void extract(const file_entry_t& entry, reader_t& reader);

        void begin();
        void commit_file(const pal::string_t& relative_path);
        void commit_dir();
        void clean();

        pal::string_t m_bundle_id;
        pal::string_t m_bundle_path;
        pal::string_t m_extraction_dir;
        pal::string_t m_working_extraction_dir;
        const manifest_t& m_manifest;
    };
}

#endif // __EXTRACTOR_H__
