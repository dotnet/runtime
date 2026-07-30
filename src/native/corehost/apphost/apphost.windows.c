// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "apphost.windows.h"
#include "error_codes.h"
#include "pal.h"
#include "trace.h"
#include "utils.h"

#include <assert.h>
#include <commctrl.h>
#include <shellapi.h>

#define APPHOST_DETAILS_MESSAGE \
    _X("Architecture: ") _STRINGIFY(CURRENT_ARCH_NAME) _X("\n") \
    _X("App host version: ") _STRINGIFY(HOST_VERSION) _X("\n\n")

// Allocate and format a string. Caller must free() the returned pointer.
static pal_char_t* format_alloc(const pal_char_t* format, ...)
{
    va_list args;
    va_start(args, format);
    int len = pal_strlen_vprintf(format, args);
    va_end(args);
    if (len < 0)
        return NULL;

    pal_char_t* buffer = (pal_char_t*)malloc((size_t)(len + 1) * sizeof(pal_char_t));
    if (buffer == NULL)
        return NULL;

    va_start(args, format);
    pal_str_vprintf(buffer, (size_t)(len + 1), format, args);
    va_end(args);
    return buffer;
}

// Return the next '\n'-delimited line at *cursor and advance *cursor past it,
// setting *line_len to the line length (excluding the newline). Returns NULL
// when the buffer is exhausted. The line is a slice of the buffer and is not
// null-terminated at *line_len.
static const pal_char_t* get_next_line(const pal_char_t** cursor, size_t* line_len)
{
    const pal_char_t* start = *cursor;
    if (start == NULL || *start == _X('\0'))
        return NULL;

    const pal_char_t* nl = pal_strchr(start, _X('\n'));
    *line_len = (nl != NULL) ? (size_t)(nl - start) : pal_strlen(start);
    *cursor = (nl != NULL) ? nl + 1 : start + *line_len;
    return start;
}

static pal_char_t* g_buffered_errors;

static void __cdecl buffering_trace_writer(const pal_char_t* message)
{
    // Append the message and a trailing newline to the buffer for later use.
    size_t existing_len = (g_buffered_errors != NULL) ? pal_strlen(g_buffered_errors) : 0;
    size_t message_len = pal_strlen(message);
    pal_char_t* grown = (pal_char_t*)realloc(g_buffered_errors, (existing_len + message_len + 2) * sizeof(pal_char_t));
    if (grown != NULL)
    {
        memcpy(grown + existing_len, message, message_len * sizeof(pal_char_t));
        grown[existing_len + message_len] = _X('\n');
        grown[existing_len + message_len + 1] = _X('\0');
        g_buffered_errors = grown;
    }

    // Also write to stderr immediately
    pal_err_print_line(message);
}

// Determines if the current module (apphost executable) is marked as a Windows GUI application
static bool is_gui_application(void)
{
    HMODULE module = GetModuleHandleW(NULL);
    assert(module != NULL);

    // https://learn.microsoft.com/windows/win32/debug/pe-format
    BYTE* bytes = (BYTE*)module;
    UINT32 pe_header_offset = (UINT32)((IMAGE_DOS_HEADER*)bytes)->e_lfanew;
    UINT16 subsystem = ((IMAGE_NT_HEADERS*)(bytes + pe_header_offset))->OptionalHeader.Subsystem;

    return subsystem == IMAGE_SUBSYSTEM_WINDOWS_GUI;
}

static void write_errors_to_event_log(const pal_char_t* executable_path, const pal_char_t* executable_name)
{
    // Report errors to the Windows Event Log.
    HANDLE eventSource = RegisterEventSourceW(NULL, _X(".NET Runtime"));
    const DWORD traceErrorID = 1023; // Matches CoreCLR ERT_UnmanagedFailFast
    pal_char_t* message = format_alloc(
        _X("Description: A .NET application failed.\n")
        _X("Application: %s\n")
        _X("Path: %s\n")
        _X("Message: %s\n"),
        executable_name,
        executable_path,
        g_buffered_errors != NULL ? g_buffered_errors : _X(""));

    if (message != NULL)
    {
        LPCWSTR messages[] = { message };
        ReportEventW(eventSource, EVENTLOG_ERROR_TYPE, 0, traceErrorID, NULL, 1, 0, messages, NULL);
        free(message);
    }

    DeregisterEventSource(eventSource);
}

// Extract the applaunch URL from a buffered error line, if present. On success
// writes the URL into url (size url_len, safely truncated) and returns true.
static bool try_get_url_from_line(const pal_char_t* line, size_t line_len, pal_char_t* url, size_t url_len)
{
    const pal_char_t url_prefix[] = DOTNET_CORE_APPLAUNCH_URL _X("?");
    if (utils_starts_with(line, line_len, url_prefix, STRING_LENGTH(url_prefix), true))
    {
        pal_str_printf(url, url_len, _X("%.*s"), (int)line_len, line);
        return true;
    }

    const pal_char_t url_prefix_before_7_0[] = _X("  - ") DOTNET_CORE_APPLAUNCH_URL _X("?");
    if (utils_starts_with(line, line_len, url_prefix_before_7_0, STRING_LENGTH(url_prefix_before_7_0), true))
    {
        // Strip the "  - " indent so the stored URL begins at the applaunch URL.
        size_t offset = STRING_LENGTH(_X("  - "));
        pal_str_printf(url, url_len, _X("%.*s"), (int)(line_len - offset), line + offset);
        return true;
    }

    return false;
}

static void open_url(const pal_char_t* url)
{
    // Open the URL in default browser
    ShellExecuteW(
        NULL,
        _X("open"),
        url,
        NULL,
        NULL,
        SW_SHOWNORMAL);
}

static bool enable_visual_styles(void)
{
    // Create an activation context using a manifest that enables visual styles
    // See https://learn.microsoft.com/windows/win32/controls/cookbook-overview
    // To avoid increasing the size of all applications by embedding a manifest,
    // we just use the WindowsShell manifest.
    const pal_char_t manifest_name[] = _X("WindowsShell.Manifest");

    // GetWindowsDirectoryW writes at most MAX_PATH chars; reserve room to append
    // a separator and the manifest file name.
    pal_char_t manifest[MAX_PATH + ARRAY_SIZE(manifest_name)];
    UINT len = GetWindowsDirectoryW(manifest, MAX_PATH);
    if (len == 0 || len >= MAX_PATH)
    {
        trace_verbose(_X("GetWindowsDirectory failed. Error code: %d"), GetLastError());
        return false;
    }

    utils_append_path(manifest, ARRAY_SIZE(manifest), manifest_name);

    // Since this is only for errors shown when the process is about to exit, we
    // skip releasing/deactivating the context to minimize impact on apphost size
    ACTCTXW actctx = { sizeof(ACTCTXW), 0, manifest };
    HANDLE context_handle = CreateActCtxW(&actctx);
    if (context_handle == INVALID_HANDLE_VALUE)
    {
        trace_verbose(_X("CreateActCtxW failed using manifest '%s'. Error code: %d"), manifest, GetLastError());
        return false;
    }

    ULONG_PTR cookie;
    if (ActivateActCtx(context_handle, &cookie) == FALSE)
    {
        trace_verbose(_X("ActivateActCtx failed. Error code: %d"), GetLastError());
        return false;
    }

    return true;
}

// Build a hyperlink for display in a task dialog.
static pal_char_t* format_hyperlink(const pal_char_t* url)
{
    size_t url_len = pal_strlen(url);
    pal_char_t* display = (pal_char_t*)malloc((url_len * 2 + 1) * sizeof(pal_char_t));
    if (display == NULL)
        return NULL;

    // & indicates an accelerator key when in hyperlink text.
    // Replace & with && such that the single ampersand is shown.
    size_t j = 0;
    for (size_t i = 0; i < url_len; ++i)
    {
        display[j++] = url[i];
        if (url[i] == _X('&'))
            display[j++] = _X('&');
    }
    display[j] = _X('\0');

    pal_char_t* result = format_alloc(_X("<A HREF=\"%s\">%s</A>"), url, display);
    free(display);
    return result;
}

static HRESULT CALLBACK task_dialog_callback(HWND hwnd, UINT uNotification, WPARAM wParam, LPARAM lParam, LONG_PTR lpRefData)
{
    (void)hwnd;
    (void)wParam;
    (void)lpRefData;

    if (uNotification == TDN_HYPERLINK_CLICKED && lParam != 0)
        open_url((LPCWSTR)lParam);

    return S_OK;
}

static bool try_show_error_with_task_dialog(
    const pal_char_t* executable_name,
    const pal_char_t* instruction,
    const pal_char_t* details,
    const pal_char_t* url)
{
    HMODULE comctl32 = LoadLibraryExW(L"comctl32.dll", NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (comctl32 == NULL)
        return false;

    typedef HRESULT (WINAPI* task_dialog_indirect)(
        const TASKDIALOGCONFIG* pTaskConfig,
        int* pnButton,
        int* pnRadioButton,
        BOOL* pfVerificationFlagChecked);

    task_dialog_indirect task_dialog_indirect_func = (task_dialog_indirect)GetProcAddress(comctl32, "TaskDialogIndirect");
    if (task_dialog_indirect_func == NULL)
    {
        FreeLibrary(comctl32);
        return false;
    }

    TASKDIALOGCONFIG config = { 0 };
    config.cbSize = sizeof(TASKDIALOGCONFIG);
    config.dwFlags = TDF_ALLOW_DIALOG_CANCELLATION | TDF_ENABLE_HYPERLINKS | TDF_SIZE_TO_CONTENT | TDF_USE_COMMAND_LINKS;
    config.dwCommonButtons = TDCBF_CLOSE_BUTTON;
    config.pszWindowTitle = executable_name;
    config.pszMainInstruction = instruction;

    // Use the application's icon if available
    HMODULE exe_module = GetModuleHandleW(NULL);
    assert(exe_module != NULL);
    if (FindResourceW(exe_module, IDI_APPLICATION, RT_GROUP_ICON) != NULL)
    {
        config.hInstance = exe_module;
        config.pszMainIcon = IDI_APPLICATION;
    }
    else
    {
        config.pszMainIcon = TD_ERROR_ICON;
    }

    int download_button_id = 1000;
    TASKDIALOG_BUTTON download_button = { download_button_id, _X("Download it now\n") _X("You will need to run the downloaded installer") };
    config.cButtons = 1;
    config.pButtons = &download_button;
    config.nDefaultButton = download_button_id;

    pal_char_t* app_launch_link = format_hyperlink(DOTNET_APP_LAUNCH_FAILED_URL);
    pal_char_t* download_link = format_hyperlink(url);
    pal_char_t* expanded_info = format_alloc(
        _X("%s") DOC_LINK_INTRO _X("\n%s\n\nDownload link:\n%s"),
        details,
        app_launch_link != NULL ? app_launch_link : _X(""),
        download_link != NULL ? download_link : _X(""));
    config.pszExpandedInformation = expanded_info;

    // Callback to handle hyperlink clicks
    config.pfCallback = task_dialog_callback;

    int clicked_button;
    bool succeeded = SUCCEEDED(task_dialog_indirect_func(&config, &clicked_button, NULL, NULL));
    if (succeeded && clicked_button == download_button_id)
        open_url(url);

    FreeLibrary(comctl32);
    free(app_launch_link);
    free(download_link);
    free(expanded_info);
    return succeeded;
}

static void show_error_dialog(const pal_char_t* executable_name, int error_code)
{
    pal_char_t* gui_errors_disabled = pal_getenv(_X("DOTNET_DISABLE_GUI_ERRORS"));
    if (gui_errors_disabled != NULL)
    {
        bool disabled = pal_xtoi(gui_errors_disabled) == 1;
        free(gui_errors_disabled);
        if (disabled)
            return;
    }

    const pal_char_t* instruction = NULL;
    pal_char_t* details = NULL;
    pal_char_t url[MAX_DOWNLOAD_URL_LEN];
    url[0] = _X('\0');

    if (error_code == CoreHostLibMissingFailure)
    {
        instruction = INSTALL_NET_DESKTOP_ERROR_MESSAGE;

        const pal_char_t* cursor = g_buffered_errors;
        const pal_char_t* line;
        size_t line_len;
        while ((line = get_next_line(&cursor, &line_len)) != NULL)
        {
            if (try_get_url_from_line(line, line_len, url, ARRAY_SIZE(url)))
                break;
        }
    }
    else if (error_code == FrameworkMissingFailure)
    {
        // We don't have a great way of passing out different kinds of detailed error info across components, so
        // just match the expected error string. See fx_resolver.messages.cpp.
        instruction = INSTALL_OR_UPDATE_NET_ERROR_MESSAGE;

        const pal_char_t prefix[] = _X("Framework: '");
        const pal_char_t prefix_before_7_0[] = _X("The framework '");
        const pal_char_t suffix_before_7_0[] = _X(" was not found.");
        const pal_char_t custom_prefix[] = _X("  _ ");

        const pal_char_t* cursor = g_buffered_errors;
        const pal_char_t* line;
        size_t line_len;
        while ((line = get_next_line(&cursor, &line_len)) != NULL)
        {
            bool has_prefix = utils_starts_with(line, line_len, prefix, STRING_LENGTH(prefix), true);
            if (has_prefix
                || (utils_starts_with(line, line_len, prefix_before_7_0, STRING_LENGTH(prefix_before_7_0), true)
                    && utils_ends_with(line, line_len, suffix_before_7_0, STRING_LENGTH(suffix_before_7_0), true)))
            {
                free(details);
                if (has_prefix)
                {
                    size_t offset = STRING_LENGTH(prefix) - 1;
                    details = format_alloc(_X("Required: %.*s\n\n"), (int)(line_len - offset), line + offset);
                }
                else
                {
                    size_t prefix_len = STRING_LENGTH(prefix_before_7_0) - 1;
                    size_t suffix_len = STRING_LENGTH(suffix_before_7_0);
                    size_t len = (line_len > prefix_len + suffix_len) ? line_len - prefix_len - suffix_len : 0;
                    details = format_alloc(_X("Required: %.*s\n\n"), (int)len, line + prefix_len);
                }
            }
            else if (utils_starts_with(line, line_len, custom_prefix, STRING_LENGTH(custom_prefix), true))
            {
                size_t offset = STRING_LENGTH(custom_prefix);
                free(details);
                details = format_alloc(_X("%.*s\n\n"), (int)(line_len - offset), line + offset);
            }
            else if (try_get_url_from_line(line, line_len, url, ARRAY_SIZE(url)))
            {
                break;
            }
        }
    }
    else if (error_code == BundleExtractionFailure)
    {
        const pal_char_t bundle_error_prefix[] = _X("Bundle header version compatibility check failed.");
        const pal_char_t* cursor = g_buffered_errors;
        const pal_char_t* line;
        size_t line_len;
        while ((line = get_next_line(&cursor, &line_len)) != NULL)
        {
            if (utils_starts_with(line, line_len, bundle_error_prefix, STRING_LENGTH(bundle_error_prefix), true))
            {
                instruction = INSTALL_NET_DESKTOP_ERROR_MESSAGE;

                utils_get_download_url(url, ARRAY_SIZE(url), NULL, NULL);
                size_t len = pal_strlen(url);
                pal_str_printf(url + len, ARRAY_SIZE(url) - len, _X("&apphost_version=") _STRINGIFY(HOST_VERSION));
                break;
            }
        }

        if (instruction == NULL)
            return;
    }
    else
    {
        return;
    }

    assert(url[0] != _X('\0'));
    assert(is_gui_application());

    size_t url_len = pal_strlen(url);
    pal_str_printf(url + url_len, ARRAY_SIZE(url) - url_len, _X("&gui=true"));
    const pal_char_t* details_text = details != NULL ? details : APPHOST_DETAILS_MESSAGE;

    trace_verbose(_X("Showing error dialog for application: '%s' - error code: 0x%x - url: '%s' - details: %s"),
        executable_name, error_code, url, details_text);

    // Prefer the rich task dialog (requires enabling visual styles).
    if (enable_visual_styles() && try_show_error_with_task_dialog(executable_name, instruction, details_text, url))
    {
        free(details);
        return;
    }

    // Fall back to a plain message box if the task dialog can't be shown.
    pal_char_t* dialog_message = format_alloc(
        _X("%s\n\n%s") DOC_LINK_INTRO _X("\n") DOTNET_APP_LAUNCH_FAILED_URL _X("\n\n")
        _X("Would you like to download it now?"),
        instruction,
        details_text);
    if (dialog_message != NULL
        && MessageBoxW(NULL, dialog_message, executable_name, MB_ICONERROR | MB_YESNO) == IDYES)
    {
        open_url(url);
    }

    free(dialog_message);
    free(details);
}

void apphost_buffer_errors(void)
{
    trace_verbose(_X("Redirecting errors to custom writer."));
    trace_set_error_writer(buffering_trace_writer);
}

void apphost_write_buffered_errors(int error_code)
{
    if (g_buffered_errors == NULL)
        return;

    pal_char_t* executable_path = pal_get_own_executable_path();
    pal_char_t executable_name[MAX_PATH] = { 0 };
    if (executable_path != NULL)
    {
        utils_get_filename(executable_path, executable_name, ARRAY_SIZE(executable_name));
    }

    write_errors_to_event_log(executable_path != NULL ? executable_path : _X(""), executable_name);

    if (is_gui_application())
        show_error_dialog(executable_name, error_code);

    free(executable_path);
    free(g_buffered_errors);
    g_buffered_errors = NULL;
}
