// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <assert.h>
#include "trace.h"
#include "hostfxr.h"
#include "hostfxr_resolver_t.h"

extern "C"
{
    int HOSTFXR_CALLTYPE hostfxr_main_bundle_startupinfo(const int argc, const pal::char_t* argv[], const pal::char_t* host_path, const pal::char_t* dotnet_root, const pal::char_t* app_path, int64_t bundle_header_offset);
    int HOSTFXR_CALLTYPE hostfxr_main_startupinfo(const int argc, const pal::char_t* argv[], const pal::char_t* host_path, const pal::char_t* dotnet_root, const pal::char_t* app_path);
    int HOSTFXR_CALLTYPE hostfxr_main(const int argc, const pal::char_t* argv[]);
    hostfxr_error_writer_fn HOSTFXR_CALLTYPE hostfxr_set_error_writer(hostfxr_error_writer_fn error_writer);
}

extern "C"
{
    using host_handle_t = void*;
    using domain_id_t = std::uint32_t;

    pal::hresult_t STDMETHODCALLTYPE coreclr_initialize(
        const char* exePath,
        const char* appDomainFriendlyName,
        int propertyCount,
        const char** propertyKeys,
        const char** propertyValues,
        host_handle_t* hostHandle,
        unsigned int* domainId);

    pal::hresult_t STDMETHODCALLTYPE coreclr_shutdown(
        host_handle_t hostHandle,
        unsigned int domainId,
        int* latchedExitCode);

    pal::hresult_t STDMETHODCALLTYPE coreclr_execute_assembly(
        host_handle_t hostHandle,
        unsigned int domainId,
        int argc,
        const char** argv,
        const char* managedAssemblyPath,
        unsigned int* exitCode);

    pal::hresult_t STDMETHODCALLTYPE coreclr_create_delegate(
        host_handle_t hostHandle,
        unsigned int domainId,
        const char* entryPointAssemblyName,
        const char* entryPointTypeName,
        const char* entryPointMethodName,
        void** delegate);
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
    // TODO: WIP this is just to make coreclr stuff _used_
    //       to see how linker handles this.
    if (app_root.length() == 100000)
    {
        coreclr_initialize(nullptr, nullptr, 0, nullptr, nullptr, nullptr, nullptr);
        coreclr_execute_assembly(0, 0, 0, nullptr, nullptr, nullptr);
    }

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
