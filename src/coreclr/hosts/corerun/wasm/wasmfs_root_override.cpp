// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <emscripten/wasmfs.h>

extern "C" backend_t wasmfs_create_root_dir(void)
{
    return wasmfs_create_node_backend("");
}
