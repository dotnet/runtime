// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <pal.h>

namespace comhost_test
{
    bool synchronous(const pal::string_t &comhost_path, const pal::string_t &clsid_str, int count);

    bool concurrent(const pal::string_t &comhost_path, const pal::string_t &clsid_str, int count);
}
