// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __HOSTFXR_RESOLVER_T_H__
#define __HOSTFXR_RESOLVER_T_H__

#include "hostfxr.h"
#include "pal.h"
#include "error_codes.h"

class hostfxr_resolver_t
{
    public:
        hostfxr_resolver_t(const pal::string_t& app_root);
        ~hostfxr_resolver_t();

        StatusCode status_code() const { return m_status_code; }

        const pal::string_t& host_path() const { return m_host_path; }
        const pal::string_t& dotnet_root() const { return m_dotnet_root; }
        const pal::string_t& fxr_path() const { return m_fxr_path; }

        hostfxr_main_bundle_startupinfo_fn resolve_main_bundle_startupinfo();
        hostfxr_set_error_writer_fn resolve_set_error_writer();
        hostfxr_main_startupinfo_fn resolve_main_startupinfo();
        hostfxr_main_fn resolve_main_v1();

    private:
        pal::dll_t m_hostfxr_dll{nullptr};

        pal::string_t m_host_path;
        pal::string_t m_dotnet_root;
        pal::string_t m_fxr_path;

        bool m_requires_startupinfo_iface{false};
        StatusCode m_status_code;
};

#endif // __HOSTFXR_RESOLVER_T_H__
