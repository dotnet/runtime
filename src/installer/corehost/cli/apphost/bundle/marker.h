// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __MARKER_H__
#define __MARKER_H__

#include <cstdint>

namespace bundle
{
#pragma pack(push, 1)
    union marker_t
    {
    public:
        uint8_t placeholder[40];
        struct
        {
            int64_t bundle_header_offset;
            uint8_t signature[32];
        } locator;

        static int64_t header_offset();
        static bool is_bundle()
        {
            return header_offset() != 0;
        }
    };
#pragma pack(pop)

}
#endif // __MARKER_H__
