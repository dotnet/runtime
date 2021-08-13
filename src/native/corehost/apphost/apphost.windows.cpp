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

    void show_error_dialog(const pal::char_t *executable_name, int error_code)
    {
        // Show message dialog for UI apps with actionable errors
        if (error_code != StatusCode::CoreHostLibMissingFailure  // missing hostfxr
            && error_code != StatusCode::FrameworkMissingFailure) // missing framework
            return;

        pal::string_t gui_errors_disabled;
        if (pal::getenv(_X("DOTNET_DISABLE_GUI_ERRORS"), &gui_errors_disabled) && pal::xtoi(gui_errors_disabled.c_str()) == 1)
            return;

        pal::string_t dialogMsg;
        pal::string_t url;
        const pal::string_t url_prefix = _X("  - ") DOTNET_CORE_APPLAUNCH_URL _X("?");
        if (error_code == StatusCode::CoreHostLibMissingFailure)
        {
            dialogMsg = pal::string_t(_X("To run this application, you must install .NET Desktop Runtime ")) + _STRINGIFY(COMMON_HOST_PKG_VER) + _X(" (") + get_arch() + _X(").\n\n");
            pal::string_t line;
            pal::stringstream_t ss(g_buffered_errors);
            while (std::getline(ss, line, _X('\n'))) {
                if (starts_with(line, url_prefix, true))
                {
                    size_t offset = url_prefix.length() - pal::strlen(DOTNET_CORE_APPLAUNCH_URL) - 1;
                    url = line.substr(offset, line.length() - offset);
                    break;
                }
            }
        }
        else if (error_code == StatusCode::FrameworkMissingFailure)
        {
            // We don't have a great way of passing out different kinds of detailed error info across components, so
            // just match the expected error string. See fx_resolver.messages.cpp.
            dialogMsg = pal::string_t(_X("To run this application, you must install missing frameworks for .NET.\n\n"));
            pal::string_t line;
            pal::stringstream_t ss(g_buffered_errors);
            while (std::getline(ss, line, _X('\n'))){
                const pal::string_t prefix = _X("The framework '");
                const pal::string_t suffix = _X("' was not found.");
                const pal::string_t custom_prefix = _X("  _ ");
                if (starts_with(line, prefix, true) && ends_with(line, suffix, true))
                {
                    dialogMsg.append(line);
                    dialogMsg.append(_X("\n\n"));
                }
                else if (starts_with(line, custom_prefix, true))
                {
                    dialogMsg.erase();
                    dialogMsg.append(line.substr(custom_prefix.length()));
                    dialogMsg.append(_X("\n\n"));
                }
                else if (starts_with(line, url_prefix, true))
                {
                    size_t offset = url_prefix.length() - pal::strlen(DOTNET_CORE_APPLAUNCH_URL) - 1;
                    url = line.substr(offset, line.length() - offset);
                    break;
                }
            }
        }

        dialogMsg.append(_X("Would you like to download it now?"));

        assert(url.length() > 0);
        assert(is_gui_application());
        url.append(_X("&gui=true"));

        trace::verbose(_X("Showing error dialog for application: '%s' - error code: 0x%x - url: '%s'"), executable_name, error_code, url.c_str());
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
