// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "redirected_error_writer.h"
#include "hostfxr.h"
#include "fxr_resolver.h"
#include "pal.h"
#include "trace.h"
#include "error_codes.h"
#include "utils.h"
#include <hstring.h>
#include <activation.h>
#include <roerrorapi.h>


#if defined(_WIN32)

// WinRT entry points are defined without the __declspec(dllexport) attribute.
// The issue here is that the compiler will throw an error regarding linkage
// redefinion. The solution here is to the use a .def file on Windows.
#define WINRT_API extern "C"

#else

#define WINRT_API SHARED_API

#endif // _WIN32

using winrt_activation_fn = pal::hresult_t(STDMETHODCALLTYPE*)(const pal::char_t* appPath, HSTRING activatableClassId, IActivationFactory** factory);

namespace
{
    int get_winrt_activation_delegate(pal::string_t* app_path, winrt_activation_fn *delegate)
    {
        return load_fxr_and_get_delegate(
            hostfxr_delegate_type::hdt_winrt_activation,
            [app_path](const pal::string_t& host_path, pal::string_t* config_path_out)
            {
                // Change the extension to get the 'app' and config
                size_t idx = host_path.rfind(_X(".dll"));
                assert(idx != pal::string_t::npos);

                pal::string_t app_path_local{ host_path };
                app_path_local.replace(app_path_local.begin() + idx, app_path_local.end(), _X(".winmd"));
                *app_path = std::move(app_path_local);

                pal::string_t config_path_local { host_path };
                config_path_local.replace(config_path_local.begin() + idx, config_path_local.end(), _X(".runtimeconfig.json"));
                *config_path_out = std::move(config_path_local);

                return StatusCode::Success;
            },
            delegate
        );
    }
}


WINRT_API HRESULT STDMETHODCALLTYPE DllGetActivationFactory(_In_ HSTRING activatableClassId, _Out_ IActivationFactory** factory)
{
    HRESULT hr;
    pal::string_t app_path;
    winrt_activation_fn activator;
    {
        trace::setup();
        reset_redirected_error_writer();
        error_writer_scope_t writer_scope(redirected_error_writer);

        int ec = get_winrt_activation_delegate(&app_path, &activator);
        if (ec != StatusCode::Success)
        {
            RoOriginateErrorW(__HRESULT_FROM_WIN32(ec), 0 /* message is null-terminated */, get_redirected_error_string().c_str());
            return __HRESULT_FROM_WIN32(ec);
        }
    }

    return activator(app_path.c_str(), activatableClassId, factory);
}

WINRT_API HRESULT STDMETHODCALLTYPE DllCanUnloadNow(void)
{
    return S_FALSE;
}
