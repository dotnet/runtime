// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "hostpolicy_exports.h"
#include <error_codes.h>

hostpolicy_exports::hostpolicy_exports()
{
    if (!pal::get_loaded_library(LIBHOSTPOLICY_NAME, "corehost_set_error_writer", &_dll, &path))
    {
        std::cout << "Failed to find hostpolicy library already loaded in the process." << std::endl;
        throw StatusCode::CoreHostLibMissingFailure;
    }

    resolve_component_dependencies = (corehost_resolve_component_dependencies_fn)pal::get_symbol(_dll, "corehost_resolve_component_dependencies");
    set_error_writer = (corehost_set_error_writer_fn)pal::get_symbol(_dll, "corehost_set_error_writer");

    if (resolve_component_dependencies == nullptr || set_error_writer == nullptr)
    {
        std::cout << "Failed to get hostpolicy entry points" << std::endl;
        throw StatusCode::CoreHostEntryPointFailure;
    }
}
