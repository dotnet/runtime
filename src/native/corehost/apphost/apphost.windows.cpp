// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "apphost.windows.h"
#include "error_codes.h"
#include "pal.h"
#include "trace.h"
#include "utils.h"
#include <shellapi.h>

namespace
{
    pal::string_t g_buffered_errors;

    void __cdecl buffering_trace_writer(const pal::char_t* message)
    {
        // Add to buffer for later use.
        g_buffered_errors.append(message).append(_X("\n"));
        // Also write to stderr immediately
        pal::err_fputs(message);
    }

    // Determines if the current module (apphost executable) is marked as a Windows GUI application
    bool is_gui_application()
    {
        HMODULE module = ::GetModuleHandleW(nullptr);
        assert(module != nullptr);

        // https://docs.microsoft.com/en-us/windows/win32/debug/pe-format
        BYTE *bytes = reinterpret_cast<BYTE *>(module);
        UINT32 pe_header_offset = reinterpret_cast<IMAGE_DOS_HEADER *>(bytes)->e_lfanew;
        UINT16 subsystem = reinterpret_cast<IMAGE_NT_HEADERS *>(bytes + pe_header_offset)->OptionalHeader.Subsystem;

        return subsystem == IMAGE_SUBSYSTEM_WINDOWS_GUI;
    }

    void write_errors_to_event_log(const pal::char_t *executable_path, const pal::char_t *executable_name)
    {
        // Report errors to the Windows Event Log.
        auto eventSource = ::RegisterEventSourceW(nullptr, _X(".NET Runtime"));
        const DWORD traceErrorID = 1023; // Matches CoreCLR ERT_UnmanagedFailFast
        pal::string_t message;
        message.append(_X("Description: A .NET application failed.\n"));
        message.append(_X("Application: ")).append(executable_name).append(_X("\n"));
        message.append(_X("Path: ")).append(executable_path).append(_X("\n"));
        message.append(_X("Message: ")).append(g_buffered_errors).append(_X("\n"));

        LPCWSTR messages[] = {message.c_str()};
        ::ReportEventW(eventSource, EVENTLOG_ERROR_TYPE, 0, traceErrorID, nullptr, 1, 0, messages, nullptr);
        ::DeregisterEventSource(eventSource);
    }

    bool try_get_url_from_line(const pal::string_t& line, pal::string_t& url)
    {
        const pal::char_t url_prefix[] = DOTNET_CORE_APPLAUNCH_URL _X("?");
        if (utils::starts_with(line, url_prefix, true))
        {
            url.assign(line);
            return true;
        }

        const pal::char_t url_prefix_before_7_0[] = _X("  - ") DOTNET_CORE_APPLAUNCH_URL _X("?");
        if (utils::starts_with(line, url_prefix_before_7_0, true))
        {
            size_t offset = utils::strlen(url_prefix_before_7_0) - utils::strlen(DOTNET_CORE_APPLAUNCH_URL) - 1;
            url.assign(line.substr(offset, line.length() - offset));
            return true;
        }

        return false;
    }

    pal::string_t get_runtime_not_found_message()
    {
        pal::string_t msg = INSTALL_NET_DESKTOP_ERROR_MESSAGE _X("\n\n")
            _X("Architecture: ");
        msg.append(get_arch());
        msg.append(_X("\n")
            _X("App host version: ") _STRINGIFY(COMMON_HOST_PKG_VER) _X("\n\n"));
        return msg;
    }

    void show_error_dialog(const pal::char_t *executable_name, int error_code)
    {
        pal::string_t gui_errors_disabled;
        if (pal::getenv(_X("DOTNET_DISABLE_GUI_ERRORS"), &gui_errors_disabled) && pal::xtoi(gui_errors_disabled.c_str()) == 1)
            return;

        pal::string_t dialogMsg;
        pal::string_t url;
        if (error_code == StatusCode::CoreHostLibMissingFailure)
        {
            dialogMsg = get_runtime_not_found_message();
            pal::string_t line;
            pal::stringstream_t ss(g_buffered_errors);
            while (std::getline(ss, line, _X('\n')))
            {
                if (try_get_url_from_line(line, url))
                {
                    break;
                }
            }
        }
        else if (error_code == StatusCode::FrameworkMissingFailure)
        {
            // We don't have a great way of passing out different kinds of detailed error info across components, so
            // just match the expected error string. See fx_resolver.messages.cpp.
            dialogMsg = pal::string_t(INSTALL_OR_UPDATE_NET_ERROR_MESSAGE _X("\n\n"));
            pal::string_t line;
            pal::stringstream_t ss(g_buffered_errors);
            while (std::getline(ss, line, _X('\n')))
            {
                const pal::char_t prefix[] = _X("Framework: '");
                const pal::char_t prefix_before_7_0[] = _X("The framework '");
                const pal::char_t suffix_before_7_0[] = _X(" was not found.");
                const pal::char_t custom_prefix[] = _X("  _ ");
                if (utils::starts_with(line, prefix, true)
                    || (utils::starts_with(line, prefix_before_7_0, true) && utils::ends_with(line, suffix_before_7_0, true)))
                {
                    dialogMsg.append(line);
                    dialogMsg.append(_X("\n\n"));
                }
                else if (utils::starts_with(line, custom_prefix, true))
                {
                    dialogMsg.erase();
                    dialogMsg.append(line.substr(utils::strlen(custom_prefix)));
                    dialogMsg.append(_X("\n\n"));
                }
                else if (try_get_url_from_line(line, url))
                {
                    break;
                }
            }
        }
        else if (error_code == StatusCode::BundleExtractionFailure)
        {
            pal::string_t line;
            pal::stringstream_t ss(g_buffered_errors);
            while (std::getline(ss, line, _X('\n')))
            {
                if (utils::starts_with(line, _X("Bundle header version compatibility check failed."), true))
                {
                    dialogMsg = get_runtime_not_found_message();
                    url = get_download_url();
                    url.append(_X("&apphost_version="));
                    url.append(_STRINGIFY(COMMON_HOST_PKG_VER));
                }
            }

            if (dialogMsg.empty())
                return;
        }
        else
        {
            return;
        }

        dialogMsg.append(
            _X("Would you like to download it now?\n\n")
            _X("Learn about "));
        dialogMsg.append(error_code == StatusCode::FrameworkMissingFailure ? _X("framework resolution:") : _X("runtime installation:"));
        dialogMsg.append(_X("\n") DOTNET_APP_LAUNCH_FAILED_URL);

        assert(url.length() > 0);
        assert(is_gui_application());
        url.append(_X("&gui=true"));

        trace::verbose(_X("Showing error dialog for application: '%s' - error code: 0x%x - url: '%s' - dialog message: %s"), executable_name, error_code, url.c_str(), dialogMsg.c_str());
        if (::MessageBoxW(nullptr, dialogMsg.c_str(), executable_name, MB_ICONERROR | MB_YESNO) == IDYES)
        {
            // Open the URL in default browser
            ::ShellExecuteW(
                nullptr,
                _X("open"),
                url.c_str(),
                nullptr,
                nullptr,
                SW_SHOWNORMAL);
        }
    }
}

void apphost::buffer_errors()
{
    trace::verbose(_X("Redirecting errors to custom writer."));
    trace::set_error_writer(buffering_trace_writer);
}

void apphost::write_buffered_errors(int error_code)
{
    if (g_buffered_errors.empty())
        return;

    pal::string_t executable_path;
    pal::string_t executable_name;
    if (pal::get_own_executable_path(&executable_path))
    {
        executable_name = get_filename(executable_path);
    }

    write_errors_to_event_log(executable_path.c_str(), executable_name.c_str());

    if (is_gui_application())
        show_error_dialog(executable_name.c_str(), error_code);
}
