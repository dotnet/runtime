// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __MANIFEST_H__
#define __MANIFEST_H__

#include <list>
#include "file_entry.h"
#include "header.h"

namespace bundle
{
    // Bundle Manifest contains:
    //     Series of file entries (for each embedded file)

    class manifest_t
    {
    public:
        manifest_t()
            : m_files_need_extraction(false)
            , m_netcoreapp3_compat_mode(false)
        {
        }

        std::vector<file_entry_t> files;

        static manifest_t read(reader_t &reader, header_t &header);

        bool files_need_extraction() const
        {
            return m_files_need_extraction;
        }

        bool is_netcoreapp3_compat_mode() const
        {
            return m_netcoreapp3_compat_mode;
        }

    private:
        bool m_files_need_extraction;
        bool m_netcoreapp3_compat_mode;
    };
}
#endif // __MANIFEST_H__
