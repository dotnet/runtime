// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __FILE_TYPE_H__
#define __FILE_TYPE_H__

#include <cstdint>

namespace bundle
{
    // FileType: Identifies the type of file embedded into the bundle.
    // 
    // The bundler differentiates a few kinds of files via the manifest,
    // with respect to the way in which they'll be used by the runtime.
    //
    // Currently all files are extracted out to the disk, but future 
    // implementations will process certain file_types directly from the bundle.

    enum file_type_t : uint8_t
    {
        unknown,
        assembly,
        native_binary,
        deps_json,
        runtime_config_json,
        symbols,
        __last
    };
}

#endif // __FILE_TYPE_H__
