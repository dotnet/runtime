// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include "trace.h"
#include "hostfxr.h"
#include "hostfxr_resolver.h"

extern "C"
{
    int HOSTFXR_CALLTYPE hostfxr_main_bundle_startupinfo(const int argc, const pal::char_t* argv[], const pal::char_t* host_path, const pal::char_t* dotnet_root, const pal::char_t* app_path, int64_t bundle_header_offset);
    int HOSTFXR_CALLTYPE hostfxr_main_startupinfo(const int argc, const pal::char_t* argv[], const pal::char_t* host_path, const pal::char_t* dotnet_root, const pal::char_t* app_path);
    int HOSTFXR_CALLTYPE hostfxr_main(const int argc, const pal::char_t* argv[]);
    hostfxr_error_writer_fn HOSTFXR_CALLTYPE hostfxr_set_error_writer(hostfxr_error_writer_fn error_writer);
}

hostfxr_main_bundle_startupinfo_fn hostfxr_resolver_t::resolve_main_bundle_startupinfo()
{
    assert(m_hostfxr_dll == nullptr);
    return hostfxr_main_bundle_startupinfo;
}

hostfxr_set_error_writer_fn hostfxr_resolver_t::resolve_set_error_writer()
{
    assert(m_hostfxr_dll == nullptr);
    return hostfxr_set_error_writer;
}

hostfxr_main_startupinfo_fn hostfxr_resolver_t::resolve_main_startupinfo()
{
    assert(m_hostfxr_dll == nullptr);
    return hostfxr_main_startupinfo;
}

hostfxr_main_fn hostfxr_resolver_t::resolve_main_v1()
{
    assert(m_hostfxr_dll == nullptr);
    assert(!"This function should not be called in a static host");
    return nullptr; 
}

hostfxr_resolver_t::hostfxr_resolver_t(const pal::string_t& app_root)
{
    if (app_root.length() == 0)
    {
        trace::info(_X("Application root path is empty. This shouldn't happen"));
        m_status_code = StatusCode::CoreHostLibMissingFailure;
    }
    else
    {
        trace::info(_X("Using internal fxr"));

        m_dotnet_root.assign(app_root);
        m_fxr_path.assign(app_root);

        m_status_code = StatusCode::Success;
    }
}

hostfxr_resolver_t::~hostfxr_resolver_t()
{
}
