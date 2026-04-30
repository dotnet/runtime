// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/process.h>

#include <stdlib.h>
#include <string.h>
#include <minipal/utf8.h>
#include <minipal/strings.h>

#ifdef _WIN32

#include <windows.h>

bool minipal_create_process(
    const char* command_line,
    const char* working_dir,
    bool inherit_handles,
    minipal_process_info* out_info)
{
    if (command_line == NULL)
        return false;

    // Convert command_line to UTF-16.
    size_t cmd_len = strlen(command_line);
    size_t cmd_wide_len = minipal_get_length_utf8_to_utf16(command_line, cmd_len, 0);
    if (cmd_wide_len == 0)
        return false;

    // +1 for null terminator
    WCHAR* cmd_wide = (WCHAR*)malloc((cmd_wide_len + 1) * sizeof(WCHAR));
    if (cmd_wide == NULL)
        return false;

    minipal_convert_utf8_to_utf16(command_line, cmd_len, (CHAR16_T*)cmd_wide, cmd_wide_len + 1, 0);
    cmd_wide[cmd_wide_len] = L'\0';

    // Convert working_dir if provided.
    WCHAR* dir_wide = NULL;
    if (working_dir != NULL)
    {
        size_t dir_len = strlen(working_dir);
        size_t dir_wide_len = minipal_get_length_utf8_to_utf16(working_dir, dir_len, 0);
        if (dir_wide_len == 0)
        {
            free(cmd_wide);
            return false;
        }
        dir_wide = (WCHAR*)malloc((dir_wide_len + 1) * sizeof(WCHAR));
        if (dir_wide == NULL)
        {
            free(cmd_wide);
            return false;
        }
        minipal_convert_utf8_to_utf16(working_dir, dir_len, (CHAR16_T*)dir_wide, dir_wide_len + 1, 0);
        dir_wide[dir_wide_len] = L'\0';
    }

    STARTUPINFOW si;
    memset(&si, 0, sizeof(si));
    si.cb = sizeof(si);

    PROCESS_INFORMATION pi;
    memset(&pi, 0, sizeof(pi));

    BOOL result = CreateProcessW(
        NULL,
        cmd_wide,
        NULL,
        NULL,
        inherit_handles ? TRUE : FALSE,
        0,
        NULL,
        dir_wide,
        &si,
        &pi);

    free(cmd_wide);
    free(dir_wide);

    if (!result)
        return false;

    if (out_info != NULL)
    {
        out_info->process_handle = (intptr_t)pi.hProcess;
        out_info->thread_handle = (intptr_t)pi.hThread;
        out_info->process_id = (int32_t)pi.dwProcessId;
    }
    else
    {
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }

    return true;
}

void minipal_close_process_handle(intptr_t handle)
{
    if (handle != 0)
        CloseHandle((HANDLE)handle);
}

bool minipal_create_process_w(
    const CHAR16_T* command_line,
    const CHAR16_T* working_dir,
    bool inherit_handles,
    minipal_process_info* out_info)
{
    if (command_line == NULL)
        return false;

    // On Windows, CHAR16_T is wchar_t so we can pass directly to CreateProcessW.
    // CreateProcessW may modify the command line buffer, so make a mutable copy.
    size_t cmd_len = wcslen((const WCHAR*)command_line);
    WCHAR* cmd_copy = (WCHAR*)malloc((cmd_len + 1) * sizeof(WCHAR));
    if (cmd_copy == NULL)
        return false;
    memcpy(cmd_copy, command_line, (cmd_len + 1) * sizeof(WCHAR));

    STARTUPINFOW si;
    memset(&si, 0, sizeof(si));
    si.cb = sizeof(si);

    PROCESS_INFORMATION pi;
    memset(&pi, 0, sizeof(pi));

    BOOL result = CreateProcessW(
        NULL,
        cmd_copy,
        NULL,
        NULL,
        inherit_handles ? TRUE : FALSE,
        0,
        NULL,
        (LPCWSTR)working_dir,
        &si,
        &pi);

    free(cmd_copy);

    if (!result)
        return false;

    if (out_info != NULL)
    {
        out_info->process_handle = (intptr_t)pi.hProcess;
        out_info->thread_handle = (intptr_t)pi.hThread;
        out_info->process_id = (int32_t)pi.dwProcessId;
    }
    else
    {
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }

    return true;
}

#else // !_WIN32

#include <errno.h>
#include <fcntl.h>
#include <signal.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <unistd.h>

// Command line splitting matching Win32 CreateProcessW semantics:
// 1) Whitespace splits arguments (space, tab)
// 2) Double quotes group text (whitespace inside quotes doesn't split)
// 3) \" is an escaped double quote (produces literal " in output)
// 4) Backslash followed by anything other than " is literal (kept as-is)
// 5) Bare double quotes are stripped from output
// Returns heap-allocated argv array (caller frees each element and the array).
static char** split_command_line(const char* cmd, int* out_argc)
{
    int capacity = 8;
    int count = 0;
    char** argv = (char**)malloc(capacity * sizeof(char*));
    if (argv == NULL)
        return NULL;

    const char* p = cmd;
    while (*p != '\0')
    {
        // Skip whitespace
        while (*p == ' ' || *p == '\t')
            p++;
        if (*p == '\0')
            break;

        // Find the end of this argument (first pass: determine boundaries)
        const char* arg_start = p;
        bool in_quotes = false;
        while (*p != '\0')
        {
            if (!in_quotes && (*p == ' ' || *p == '\t'))
                break;

            if (*p == '"')
            {
                // Check for escaped quote: \"
                if (p > arg_start && *(p - 1) == '\\')
                {
                    // This is an escaped quote, not a real quote toggle
                    p++;
                    continue;
                }
                in_quotes = !in_quotes;
                p++;
            }
            else
            {
                p++;
            }
        }

        // Second pass: copy the argument, stripping bare quotes and handling \"
        size_t arg_len = (size_t)(p - arg_start);
        char* buf = (char*)malloc(arg_len + 1);
        if (buf == NULL)
        {
            for (int i = 0; i < count; i++) free(argv[i]);
            free(argv);
            return NULL;
        }

        size_t j = 0;
        const char* s = arg_start;
        while (s < p)
        {
            if (*s == '"')
            {
                // Skip bare double quotes (they're grouping characters)
                s++;
            }
            else if (*s == '\\' && (s + 1) < p && *(s + 1) == '"')
            {
                // Escaped double quote: \\" -> produce literal "
                buf[j++] = '"';
                s += 2;
            }
            else
            {
                buf[j++] = *s++;
            }
        }
        buf[j] = '\0';

        if (count + 2 > capacity)
        {
            capacity *= 2;
            argv = (char**)realloc(argv, capacity * sizeof(char*));
        }
        argv[count++] = buf;
    }

    argv[count] = NULL;
    *out_argc = count;
    return argv;
}

bool minipal_create_process(
    const char* command_line,
    const char* working_dir,
    bool inherit_handles,
    minipal_process_info* out_info)
{
    if (command_line == NULL)
        return false;

    int argc = 0;
    char** argv = split_command_line(command_line, &argc);
    if (argv == NULL || argc == 0)
    {
        free(argv);
        return false;
    }

    pid_t pid = fork();
    if (pid < 0)
    {
        for (int i = 0; i < argc; i++) free(argv[i]);
        free(argv);
        return false;
    }

    if (pid == 0)
    {
        // Child process
        if (working_dir != NULL)
        {
            if (chdir(working_dir) != 0)
                _exit(127);
        }

        if (!inherit_handles)
        {
            // Close file descriptors > 2
            int max_fd = (int)sysconf(_SC_OPEN_MAX);
            if (max_fd < 0) max_fd = 1024;
            for (int fd = 3; fd < max_fd; fd++)
                close(fd);
        }

        execvp(argv[0], argv);
        _exit(127); // exec failed
    }

    // Parent
    for (int i = 0; i < argc; i++) free(argv[i]);
    free(argv);

    if (out_info != NULL)
    {
        out_info->process_handle = (intptr_t)pid;
        out_info->thread_handle = 0;
        out_info->process_id = (int32_t)pid;
    }

    return true;
}

void minipal_close_process_handle(intptr_t handle)
{
    // On Unix, PIDs don't need to be "closed". No-op.
    (void)handle;
}

bool minipal_create_process_w(
    const CHAR16_T* command_line,
    const CHAR16_T* working_dir,
    bool inherit_handles,
    minipal_process_info* out_info)
{
    if (command_line == NULL)
        return false;

    // Convert command_line from UTF-16 to UTF-8.
    size_t cmd_u16_len = minipal_u16_strlen(command_line);
    size_t cmd_u8_len = minipal_get_length_utf16_to_utf8(command_line, cmd_u16_len, 0);
    if (cmd_u8_len == 0)
        return false;

    char* cmd_utf8 = (char*)malloc(cmd_u8_len + 1);
    if (cmd_utf8 == NULL)
        return false;
    minipal_convert_utf16_to_utf8(command_line, cmd_u16_len, cmd_utf8, cmd_u8_len + 1, 0);
    cmd_utf8[cmd_u8_len] = '\0';

    // Convert working_dir if provided.
    char* dir_utf8 = NULL;
    if (working_dir != NULL)
    {
        size_t dir_u16_len = minipal_u16_strlen(working_dir);
        size_t dir_u8_len = minipal_get_length_utf16_to_utf8(working_dir, dir_u16_len, 0);
        if (dir_u8_len > 0)
        {
            dir_utf8 = (char*)malloc(dir_u8_len + 1);
            if (dir_utf8 != NULL)
            {
                minipal_convert_utf16_to_utf8(working_dir, dir_u16_len, dir_utf8, dir_u8_len + 1, 0);
                dir_utf8[dir_u8_len] = '\0';
            }
        }
    }

    bool ok = minipal_create_process(cmd_utf8, dir_utf8, inherit_handles, out_info);
    free(cmd_utf8);
    free(dir_utf8);
    return ok;
}

#endif // !_WIN32
