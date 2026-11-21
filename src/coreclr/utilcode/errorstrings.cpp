// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// errorstrings.cpp
//
// Messages for Win32 error codes.
//*****************************************************************************

#include "stdafx.h"

#ifdef HOST_WINDOWS

bool GetSystemErrorMessage(DWORD code, WCHAR *buffer, size_t cchBuffer)
{
    return FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL, code, 0, buffer, (DWORD)cchBuffer, NULL) != 0;
}

#else // HOST_WINDOWS

struct ErrorString
{
    DWORD code;
    LPCWSTR const string;
};

static const ErrorString s_errorStrings[] =
{
    { ERROR_SUCCESS, W("The operation completed successfully.\n") },
    { ERROR_FILE_NOT_FOUND, W("The system cannot find the file specified.\n") },
    { ERROR_PATH_NOT_FOUND, W("The system cannot find the path specified.\n") },
    { ERROR_TOO_MANY_OPEN_FILES, W("The system cannot open the file.\n") },
    { ERROR_ACCESS_DENIED, W("Access is denied.\n") },
    { ERROR_INVALID_HANDLE, W("The handle is invalid.\n") },
    { ERROR_NOT_ENOUGH_MEMORY, W("Not enough storage is available to process this command.\n") },
    { ERROR_BAD_FORMAT, W("An attempt was made to load a program with an incorrect format.\n") },
    { ERROR_INVALID_DATA, W("The data is invalid.\n") },
    { ERROR_OUTOFMEMORY, W("Not enough storage is available to complete this operation.\n") },
    { ERROR_NO_MORE_FILES, W("There are no more files.\n") },
    { ERROR_NOT_READY, W("The device is not ready.\n") },
    { ERROR_BAD_COMMAND, W("The device does not recognize the command.\n") },
    { ERROR_WRITE_FAULT, W("The system cannot write to the specified device.\n") },
    { ERROR_READ_FAULT, W("The system cannot read from the specified device.\n") },
    { ERROR_GEN_FAILURE, W("A device attached to the system is not functioning.\n") },
    { ERROR_SHARING_VIOLATION, W("The process cannot access the file because it is being used by another process.\n") },
    { ERROR_LOCK_VIOLATION, W("The process cannot access the file because another process has locked a portion of the file.\n") },
    { ERROR_HANDLE_EOF, W("Reached the end of the file.\n") },
    { ERROR_NOT_SUPPORTED, W("The request is not supported.\n") },
    { ERROR_BAD_NETPATH, W("The network path was not found.\n") },
    { ERROR_BAD_NET_NAME, W("The network name cannot be found.\n") },
    { ERROR_INVALID_PARAMETER, W("The parameter is incorrect.\n") },
    { ERROR_OPEN_FAILED, W("The system cannot open the device or file specified.\n") },
    { ERROR_BUFFER_OVERFLOW, W("The file name is too long.\n") },
    { ERROR_DISK_FULL, W("There is not enough space on the disk.\n") },
    { ERROR_INSUFFICIENT_BUFFER, W("The data area passed to a system call is too small.\n") },
    { ERROR_INVALID_NAME, W("The filename, directory name, or volume label syntax is incorrect.\n") },
    { ERROR_MOD_NOT_FOUND, W("The specified module or one of its dependencies could not be found.\n") },
    { ERROR_PROC_NOT_FOUND, W("The specified procedure could not be found.\n") },
    { ERROR_DIR_NOT_EMPTY, W("The directory is not empty.\n") },
    { ERROR_BAD_PATHNAME, W("The specified path is invalid.\n") },
    { ERROR_BUSY, W("The requested resource is in use.\n") },
    { ERROR_INVALID_ORDINAL, W("The operating system cannot run %1.\n") },
    { ERROR_ALREADY_EXISTS, W("Cannot create a file when that file already exists.\n") },
    { ERROR_EXE_MARKED_INVALID, W("The operating system cannot run %1.\n") },
    { ERROR_BAD_EXE_FORMAT, W("%1 is not a valid Win32 application.\n") },
    { ERROR_ENVVAR_NOT_FOUND, W("The system could not find the environment option that was entered.\n") },
    { ERROR_FILENAME_EXCED_RANGE, W("The filename or extension is too long.\n") },
    { ERROR_PARTIAL_COPY, W("Only part of a ReadProcessMemory or WriteProcessMemory request was completed.\n") },
    { ERROR_INVALID_ADDRESS, W("Attempt to access invalid address.\n") },
    { ERROR_ARITHMETIC_OVERFLOW, W("Arithmetic result exceeded 32 bits.\n") },
    { ERROR_NOACCESS, W("Invalid access to memory location.\n") },
    { ERROR_STACK_OVERFLOW, W("Recursion too deep; the stack overflowed.\n") },
    { ERROR_INVALID_FLAGS, W("Invalid flags.\n") },
    { ERROR_UNRECOGNIZED_VOLUME, W("The volume does not contain a recognized file system.\nPlease make sure that all required file system drivers are loaded and that the volume is not corrupted.\n") },
    { ERROR_FILE_INVALID, W("The volume for a file has been externally altered so that the opened file is no longer valid.\n") },
    { ERROR_NO_UNICODE_TRANSLATION, W("No mapping for the Unicode character exists in the target multi-byte code page.\n") },
    { ERROR_DLL_INIT_FAILED, W("A dynamic link library (DLL) initialization routine failed.\n") },
    { ERROR_INVALID_DLL, W("One of the library files needed to run this application is damaged.\n") },
    { ERROR_DLL_NOT_FOUND, W("One of the library files needed to run this application cannot be found.\n") },
    { ERROR_NOT_FOUND, W("Element not found.\n") },
    { ERROR_CANCELLED, W("The operation was canceled by the user.\n") },
    { ERROR_INTERNAL_ERROR, W("An internal error occurred.\n") },
    { ERROR_FILE_CORRUPT, W("The file or directory is corrupted and unreadable.\n") },
    { ERROR_DISK_CORRUPT, W("The disk structure is corrupted and unreadable.\n") },
    { ERROR_WRONG_TARGET_NAME, W("Logon Failure: The target account name is incorrect.\n") },
    { ERROR_COMMITMENT_LIMIT, W("The paging file is too small for this operation to complete.\n") },
    { ERROR_TIMEOUT, W("This operation returned because the timeout period expired.\n") },
    { ERROR_EVENTLOG_FILE_CORRUPT, W("The event log file is corrupted.\n") },
    { ERROR_LOG_FILE_FULL, W("The event log file is full.\n") },
    { ERROR_RESOURCE_DATA_NOT_FOUND, W("The specified image file did not contain a resource section.\n") }
};

static int CompareErrorStrings(const void *a, const void *b)
{
    DWORD codeA = ((ErrorString *)a)->code;
    DWORD codeB = ((ErrorString *)b)->code;

    if (codeA < codeB)
    {
        return -1;
    }
    else if (codeA == codeB)
    {
        return 0;
    }
    else
    {
        return 1;
    }
}

bool GetSystemErrorMessage(DWORD code, WCHAR *buffer, size_t cchBuffer)
{
    if ((code & 0xFFFF0000) == 0x80070000)
    {
        // This message has been produced by HRESULT_FROM_WIN32.  Undo its work.
        code &= 0xFFFF;
    }

    // Search the sorted set of resources for the ID we're interested in.
    ErrorString searchEntry = {code, NULL};
    const ErrorString *stringEntry = (const ErrorString *)bsearch(
        &searchEntry,
        s_errorStrings,
        ARRAY_SIZE(s_errorStrings),
        sizeof(ErrorString),
        CompareErrorStrings);

    if (stringEntry != NULL)
    {
        return wcscpy_s(buffer, cchBuffer, stringEntry->string) == 0;
    }

    char errorString[sizeof("Error 4294967295")];
    int count = sprintf_s(errorString, ARRAY_SIZE(errorString), "Error %u", code);
    if (count < 0 || (size_t)count >= cchBuffer)
    {
        return false;
    }

    // Widening is fine since the string is ASCII.
    for (int i = 0; i <= count; i++)
    {
        buffer[i] = (WCHAR)errorString[i];
    }

    return true;
}

#endif // HOST_WINDOWS
