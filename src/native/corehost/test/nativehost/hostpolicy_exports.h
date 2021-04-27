// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal.h>
#include <hostpolicy.h>

typedef void(HOSTPOLICY_CALLTYPE* corehost_resolve_component_dependencies_result_fn)(
    const pal::char_t* assembly_paths,
    const pal::char_t* native_search_paths,
    const pal::char_t* resource_search_paths);
typedef int(HOSTPOLICY_CALLTYPE* corehost_resolve_component_dependencies_fn) (
    const pal::char_t* component_main_assembly_path,
    corehost_resolve_component_dependencies_result_fn result);

class hostpolicy_exports
{
public:
    pal::string_t path;

    corehost_resolve_component_dependencies_fn resolve_component_dependencies;
    corehost_set_error_writer_fn set_error_writer;

public:
    hostpolicy_exports();

private:
    pal::dll_t _dll;
};
