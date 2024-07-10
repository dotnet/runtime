// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>

#include "pal.h"
#include "fxr_resolver.h"
#include "trace.h"
#include "hostfxr_resolver.h"

namespace
{
    // SHA-256 of "dotnet-search" in UTF-8
    #define EMBED_DOTNET_SEARCH_HI_PART_UTF8 "19ff3e9c3602ae8e841925bb461a0adb"
    #define EMBED_DOTNET_SEARCH_LO_PART_UTF8 "064a1f1903667a5e0d87e8f608f425ac"

    // <fxr_resolver::search_location_default> \0 <app_relative_dotnet_placeholder>
    #define EMBED_DOTNET_SEARCH_FULL_UTF8    ("\0\0" EMBED_DOTNET_SEARCH_HI_PART_UTF8 EMBED_DOTNET_SEARCH_LO_PART_UTF8)

    bool try_get_dotnet_search_options(fxr_resolver::search_location& out_search_location, pal::string_t& out_app_relative_dotnet)
    {
        constexpr int EMBED_SIZE = 512;
        static_assert(sizeof(EMBED_DOTNET_SEARCH_FULL_UTF8) / sizeof(EMBED_DOTNET_SEARCH_FULL_UTF8[0]) < EMBED_SIZE, "Placeholder value for .NET search options longer than expected");

        // Contains the EMBED_DOTNET_SEARCH_FULL_UTF8 value at compile time or app-relative .NET path written by the SDK (dotnet publish).
        static char embed[EMBED_SIZE] = EMBED_DOTNET_SEARCH_FULL_UTF8;

        out_search_location = (fxr_resolver::search_location)embed[0];
        assert(embed[1] == 0); // NUL separates the search location and embedded .NET root value
        bool is_configured = out_search_location != fxr_resolver::search_location_default;
        if ((out_search_location & fxr_resolver::search_location_app_relative) != 0)
        {
            // Since the single static string is replaced by editing the executable, a reference string is needed to do the compare.
            // So use two parts of the string that will be unaffected by the edit.
            static const char hi_part[] = EMBED_DOTNET_SEARCH_HI_PART_UTF8;
            static const char lo_part[] = EMBED_DOTNET_SEARCH_LO_PART_UTF8;
            size_t hi_len = (sizeof(hi_part) / sizeof(hi_part[0])) - 1;
            size_t lo_len = (sizeof(lo_part) / sizeof(lo_part[0])) - 1;

            std::string binding(&embed[2]); // Embedded path is null-terminated
            if ((binding.size() >= (hi_len + lo_len)) &&
                binding.compare(0, hi_len, &hi_part[0]) == 0 &&
                binding.compare(hi_len, lo_len, &lo_part[0]) == 0)
            {
                trace::verbose(_X(".NET root is not embedded."));
                return is_configured;
            }

            pal::string_t app_relative_dotnet;
            if (!pal::clr_palstring(binding.c_str(), &app_relative_dotnet))
            {
                trace::error(_X("The app-relative .NET path could not be retrieved from the executable image."));
                return is_configured;
            }

            trace::info(_X("Embedded app-relative .NET path: '%s'"), app_relative_dotnet.c_str());
            out_app_relative_dotnet = std::move(app_relative_dotnet);
        }

        return is_configured;
    }
}

hostfxr_main_bundle_startupinfo_fn hostfxr_resolver_t::resolve_main_bundle_startupinfo()
{
    assert(m_hostfxr_dll != nullptr);
    return reinterpret_cast<hostfxr_main_bundle_startupinfo_fn>(pal::get_symbol(m_hostfxr_dll, "hostfxr_main_bundle_startupinfo"));
}

hostfxr_set_error_writer_fn hostfxr_resolver_t::resolve_set_error_writer()
{
    assert(m_hostfxr_dll != nullptr);
    return reinterpret_cast<hostfxr_set_error_writer_fn>(pal::get_symbol(m_hostfxr_dll, "hostfxr_set_error_writer"));
}

hostfxr_main_startupinfo_fn hostfxr_resolver_t::resolve_main_startupinfo()
{
    assert(m_hostfxr_dll != nullptr);
    return reinterpret_cast<hostfxr_main_startupinfo_fn>(pal::get_symbol(m_hostfxr_dll, "hostfxr_main_startupinfo"));
}

hostfxr_main_fn hostfxr_resolver_t::resolve_main_v1()
{
    assert(m_hostfxr_dll != nullptr);
    return reinterpret_cast<hostfxr_main_fn>(pal::get_symbol(m_hostfxr_dll, "hostfxr_main"));
}

hostfxr_resolver_t::hostfxr_resolver_t(const pal::string_t& app_root)
{
    fxr_resolver::search_location search_location = fxr_resolver::search_location_default;
    pal::string_t app_relative_dotnet;
    pal::string_t app_relative_dotnet_path;
    if (try_get_dotnet_search_options(search_location, app_relative_dotnet))
    {
        trace::info(_X(".NET root search location options: %d"), search_location);
        if (!app_relative_dotnet.empty())
        {
            app_relative_dotnet_path = app_root;
            append_path(&app_relative_dotnet_path, app_relative_dotnet.c_str());
        }
    }

    if (!fxr_resolver::try_get_path(app_root, search_location, &app_relative_dotnet_path, &m_dotnet_root, &m_fxr_path))
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

hostfxr_resolver_t::~hostfxr_resolver_t()
{
    if (m_hostfxr_dll != nullptr)
    {
        pal::unload_library(m_hostfxr_dll);
    }
}
