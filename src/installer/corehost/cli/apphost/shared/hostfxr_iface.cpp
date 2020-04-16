// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <assert.h>

#include "pal.h"
#include "fxr_resolver.h"
#include "trace.h"
#include "hostfxr_iface.h"

hostfxr_main_bundle_startupinfo_fn hostfxr::resolve_main_bundle_startupinfo()
{
    assert(m_hostfxr_dll != nullptr);
    return reinterpret_cast<hostfxr_main_bundle_startupinfo_fn>(pal::get_symbol(m_hostfxr_dll, "hostfxr_main_bundle_startupinfo"));
}

hostfxr_set_error_writer_fn hostfxr::resolve_set_error_writer()
{
    assert(m_hostfxr_dll != nullptr);
    return reinterpret_cast<hostfxr_set_error_writer_fn>(pal::get_symbol(m_hostfxr_dll, "hostfxr_set_error_writer"));
}

hostfxr_main_startupinfo_fn hostfxr::resolve_main_startupinfo()
{
    assert(m_hostfxr_dll != nullptr);
    return reinterpret_cast<hostfxr_main_startupinfo_fn>(pal::get_symbol(m_hostfxr_dll, "hostfxr_main_startupinfo"));
}

hostfxr_main_fn hostfxr::resolve_main_v1()
{
    assert(m_hostfxr_dll != nullptr);
    return reinterpret_cast<hostfxr_main_fn>(pal::get_symbol(m_hostfxr_dll, "hostfxr_main"));
}

hostfxr::hostfxr(const pal::string_t& app_root)
{
    if (!fxr_resolver::try_get_path(app_root, &m_dotnet_root, &m_fxr_path))
    {
        m_status_code = StatusCode::CoreHostLibMissingFailure;
    }
    else if (pal::load_library(&m_fxr_path, &m_hostfxr_dll))
    {
        m_status_code = StatusCode::Success;
    }
    else
    {
        trace::error(_X("The library %s was found, but loading it from %s failed"), LIBFXR_NAME, m_fxr_path.c_str());
        trace::error(_X("  - Installing .NET prerequisites might help resolve this problem."));
        trace::error(_X("     %s"), DOTNET_CORE_INSTALL_PREREQUISITES_URL);
        m_status_code = StatusCode::CoreHostLibLoadFailure;
    }
}

hostfxr::~hostfxr()
{
    if (m_hostfxr_dll != nullptr)
    {
        pal::unload_library(m_hostfxr_dll);
    }
}
