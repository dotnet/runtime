// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal.h>

namespace get_native_search_directories_test
{
    bool get_for_command_line(
        const pal::string_t& hostfxr_path,
        int argc,
        const pal::char_t* argv[],
        pal::stringstream_t& test_output);
}
