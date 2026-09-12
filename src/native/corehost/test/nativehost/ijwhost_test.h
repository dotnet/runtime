// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal.h>

namespace ijwhost_test
{
    bool run(const pal::string_t &library_path, const pal::char_t *entry_point);

    bool load_context(const std::vector<pal::string_t> &library_paths, const std::vector<pal::string_t> &entry_points);
}
