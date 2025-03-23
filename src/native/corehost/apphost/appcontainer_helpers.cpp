// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define WINRT_NO_SOURCE_LOCATION

#include "appcontainer_helpers.h"

#include <windows.h>
#include <appmodel.h>
#include <winrt/windows.foundation.collections.h>
#include <winrt/windows.system.h>
#include <winrt/windows.ui.core.h>
#include <winrt/windows.applicationmodel.core.h>

namespace appcontainer_helpers
{
    HANDLE app_created_event = nullptr;

    struct UriLauncherApp : winrt::implements<UriLauncherApp, winrt::Windows::ApplicationModel::Core::IFrameworkViewSource, winrt::Windows::ApplicationModel::Core::IFrameworkView>
    {
    private:
        winrt::hstring m_url;
        winrt::Windows::UI::Core::CoreWindow m_window { nullptr };

    public:
        UriLauncherApp(winrt::hstring url) : m_url(url) { }

        winrt::Windows::ApplicationModel::Core::IFrameworkView CreateView() { return *this; }
        void Initialize(winrt::Windows::ApplicationModel::Core::CoreApplicationView const&) { }
        void Load(winrt::hstring const&) { }
        void Uninitialize() { }
        void SetWindow(winrt::Windows::UI::Core::CoreWindow const& window) { m_window = window; }
        void Run();
    };

    bool is_appcontainer()
    {
        DWORD is_appcontainer = false;
        DWORD return_length = 0;
        ::GetTokenInformation(::GetCurrentThreadEffectiveToken(), ::TokenIsAppContainer, &is_appcontainer, sizeof(is_appcontainer), &return_length);

        return is_appcontainer;
    }

    bool is_uwp()
    {
        const static auto AppPolicyGetWindowingModel = reinterpret_cast<decltype(&::AppPolicyGetWindowingModel)>(::GetProcAddress(::GetModuleHandleW(L"kernelbase.dll"), "AppPolicyGetWindowingModel"));

        if (AppPolicyGetWindowingModel)
        {
            ::AppPolicyWindowingModel windowingModel;
            if(!AppPolicyGetWindowingModel(::GetCurrentProcessToken(), &windowingModel))
            {
                return windowingModel == ::AppPolicyWindowingModel::AppPolicyWindowingModel_Universal;
            }
        }

        return false;
    }

    inline bool is_corewindow_initialized()
    {
        return winrt::Windows::ApplicationModel::Core::CoreApplication::Views().Size();
    }

    __declspec(noinline) void open_url(winrt::hstring url)
    {
        auto launcher_factory = winrt::try_get_activation_factory<winrt::Windows::System::Launcher, winrt::Windows::System::ILauncherStatics>();
        if (launcher_factory)
        {
            auto uri_factory = winrt::try_get_activation_factory<winrt::Windows::Foundation::Uri, winrt::Windows::Foundation::IUriRuntimeClassFactory>();
            if (uri_factory) launcher_factory.LaunchUriAsync(uri_factory.CreateUri(url)).get();
        }
    }

    void UriLauncherApp::Run()
    {
        open_url(m_url);
        SetEvent(app_created_event);

        m_window.Dispatcher().ProcessEvents(winrt::Windows::UI::Core::CoreProcessEventsOption::ProcessUntilQuit);
    }

    void open_url_for_appcontainer(const wchar_t* url)
    {
        auto uwp = is_uwp();
        auto corewindow_initialized = uwp && is_corewindow_initialized();
        auto should_run_directly = !uwp || corewindow_initialized;

        struct THREAD_PARAMETERS
        {
            const bool uwp;
            const bool corewindow_initialized;
            const wchar_t* url;
        };

        THREAD_PARAMETERS parameters = { uwp, corewindow_initialized, url };

        auto thread = CreateThread(NULL, NULL, [](void* parameters) -> DWORD
        {
            auto thread_parameters = static_cast<THREAD_PARAMETERS*>(parameters);

            winrt::init_apartment(winrt::apartment_type::multi_threaded);

            if (thread_parameters->uwp)
            {
                if (thread_parameters->corewindow_initialized)
                {
                    auto dispatcher = winrt::Windows::ApplicationModel::Core::CoreApplication::MainView().Dispatcher();
                    dispatcher.RunAsync(winrt::Windows::UI::Core::CoreDispatcherPriority::High, [=]()
                    {
                        open_url(thread_parameters->url);
                    }).get();
                }
                else
                {
                    winrt::Windows::ApplicationModel::Core::CoreApplication::Run(winrt::make<UriLauncherApp>(thread_parameters->url));
                }
            }
            else
            {
                open_url(thread_parameters->url);
            }

            return 0;
        }, &parameters, should_run_directly ? NULL : CREATE_SUSPENDED, NULL);

        if (thread)
        {
            if (should_run_directly)
            {
                ::WaitForSingleObject(thread, INFINITE);
                ::CloseHandle(thread);
            }
            else
            {
                app_created_event = ::CreateEventW(nullptr, false, false, nullptr);

                ResumeThread(thread);

                if (app_created_event)
                {
                    ::WaitForSingleObject(app_created_event, INFINITE);
                    ::CloseHandle(app_created_event);
                    app_created_event = nullptr;
                }
            }
        }
    }
}
