// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __MANIFEST_H__
#define __MANIFEST_H__

#include <cstdint>
#include <list>
#include "file_entry.h"

namespace bundle
{
    // Bundle Manifest contains:
    //     Series of file entries (for each embedded file)

    class manifest_t
    {
    public:
        manifest_t()
            :files()
        {}

        std::list<file_entry_t*> files;

        static manifest_t* read(FILE* host, int32_t num_files);
    };
}
#endif // __MANIFEST_H__
