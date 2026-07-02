// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Static version of the hostfxr resolver for singlefilehost.
// Functions are statically linked, so no dynamic library loading is needed.
// This is a .cpp file providing extern "C" implementations of the C resolver
// interface, so it can also reference C++-linked functions like
// initialize_static_createdump and the statically-linked hostfxr functions.

#include "pal.h"
#include "apphost_hostfxr_resolver.h"
#include "trace.h"

#include <assert.h>
#include <cstdlib>
#include <cstring>

// Statically linked hostfxr functions
extern "C"
{
    int HOSTFXR_CALLTYPE hostfxr_main_bundle_startupinfo(const int argc, const pal_char_t* argv[], const pal_char_t* host_path, const pal_char_t* dotnet_root, const pal_char_t* app_path, int64_t bundle_header_offset);
    int HOSTFXR_CALLTYPE hostfxr_main_startupinfo(const int argc, const pal_char_t* argv[], const pal_char_t* host_path, const pal_char_t* dotnet_root, const pal_char_t* app_path);
    int HOSTFXR_CALLTYPE hostfxr_main(const int argc, const pal_char_t* argv[]);
    hostfxr_error_writer_fn HOSTFXR_CALLTYPE hostfxr_set_error_writer(hostfxr_error_writer_fn error_writer);
}

extern "C" hostfxr_main_bundle_startupinfo_fn hostfxr_resolver_resolve_main_bundle_startupinfo(const hostfxr_resolver_t* resolver)
{
    assert(resolver->hostfxr_dll == nullptr);
    return hostfxr_main_bundle_startupinfo;
}

extern "C" hostfxr_set_error_writer_fn hostfxr_resolver_resolve_set_error_writer(const hostfxr_resolver_t* resolver)
{
    assert(resolver->hostfxr_dll == nullptr);
    return hostfxr_set_error_writer;
}

extern "C" hostfxr_main_startupinfo_fn hostfxr_resolver_resolve_main_startupinfo(const hostfxr_resolver_t* resolver)
{
    assert(resolver->hostfxr_dll == nullptr);
    return hostfxr_main_startupinfo;
}

extern "C" hostfxr_main_fn hostfxr_resolver_resolve_main_v1(const hostfxr_resolver_t* resolver)
{
    assert(resolver->hostfxr_dll == nullptr);
    assert(!"This function should not be called in a static host");
    return nullptr;
}

extern "C" void hostfxr_resolver_init(hostfxr_resolver_t* resolver, const pal_char_t* app_root)
{
    resolver->hostfxr_dll = nullptr;
    resolver->dotnet_root = nullptr;
    resolver->fxr_path = nullptr;
    resolver->status_code = Success;

    if (app_root == nullptr || app_root[0] == _X('\0'))
    {
        trace_info(_X("Application root path is empty. This shouldn't happen"));
        resolver->status_code = CoreHostLibMissingFailure;
        return;
    }

    trace_info(_X("Using internal fxr"));

    size_t root_len = pal_strlen(app_root);
    resolver->dotnet_root = static_cast<pal_char_t*>(malloc((root_len + 1) * sizeof(pal_char_t)));
    if (resolver->dotnet_root == nullptr)
    {
        resolver->status_code = CoreHostLibMissingFailure;
        return;
    }
    memcpy(resolver->dotnet_root, app_root, (root_len + 1) * sizeof(pal_char_t));

    resolver->fxr_path = static_cast<pal_char_t*>(malloc((root_len + 1) * sizeof(pal_char_t)));
    if (resolver->fxr_path == nullptr)
    {
        resolver->status_code = CoreHostLibMissingFailure;
        return;
    }
    memcpy(resolver->fxr_path, app_root, (root_len + 1) * sizeof(pal_char_t));
}

extern "C" void hostfxr_resolver_cleanup(hostfxr_resolver_t* resolver)
{
    // No library to unload in a static host
    free(resolver->dotnet_root);
    resolver->dotnet_root = nullptr;
    free(resolver->fxr_path);
    resolver->fxr_path = nullptr;
}

#if defined(FEATURE_STATIC_CREATEDUMP)
extern void initialize_static_createdump();
#endif

extern "C" void apphost_static_init(void)
{
#if defined(FEATURE_STATIC_CREATEDUMP)
    initialize_static_createdump();
#endif
}
