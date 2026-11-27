// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "minipalconfig.h"

#include <stdbool.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#include <windows.h>
#include "utf8.h"
#else
#include <errno.h>
#include <fcntl.h>
#include <sys/stat.h>
#include <unistd.h>
#endif

#include "fileio.h"

#ifdef _WIN32

static WCHAR* convert_path_to_utf16(const char* path)
{
    if (path == NULL)
    {
        return NULL;
    }

    size_t pathLength = strlen(path);
    size_t pathLengthUtf16 = minipal_get_length_utf8_to_utf16(path, pathLength, MINIPAL_MB_NO_REPLACE_INVALID_CHARS);
    if (pathLength != 0 && pathLengthUtf16 == 0)
    {
        return NULL;
    }

    if (pathLengthUtf16 >= SIZE_MAX / sizeof(WCHAR))
    {
        return NULL;
    }

    WCHAR* pathUtf16 = (WCHAR*)malloc((pathLengthUtf16 + 1) * sizeof(WCHAR));
    if (pathUtf16 == NULL)
    {
        return NULL;
    }

    if (minipal_convert_utf8_to_utf16(path, pathLength, (CHAR16_T*)pathUtf16, pathLengthUtf16, MINIPAL_MB_NO_REPLACE_INVALID_CHARS) != pathLengthUtf16)
    {
        free(pathUtf16);
        return NULL;
    }

    pathUtf16[pathLengthUtf16] = L'\0';
    return pathUtf16;
}

static bool initialize_owner_only_security_attributes(
    SECURITY_ATTRIBUTES* securityAttributes,
    SECURITY_DESCRIPTOR* securityDescriptor,
    HANDLE* token,
    TOKEN_USER** tokenUser,
    ACL** acl)
{
    if (!OpenThreadToken(GetCurrentThread(), TOKEN_QUERY, TRUE, token))
    {
        if (GetLastError() != ERROR_NO_TOKEN ||
            !OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, token))
        {
            return false;
        }
    }

    DWORD tokenUserLength = 0;
    if (GetTokenInformation(*token, TokenUser, NULL, 0, &tokenUserLength) ||
        GetLastError() != ERROR_INSUFFICIENT_BUFFER)
    {
        return false;
    }

    *tokenUser = (TOKEN_USER*)malloc(tokenUserLength);
    if (*tokenUser == NULL)
    {
        return false;
    }

    if (!GetTokenInformation(*token, TokenUser, *tokenUser, tokenUserLength, &tokenUserLength))
    {
        return false;
    }

    DWORD aclLength = sizeof(ACL) + sizeof(ACCESS_ALLOWED_ACE) - sizeof(DWORD) + GetLengthSid((*tokenUser)->User.Sid);
    *acl = (ACL*)malloc(aclLength);
    if (*acl == NULL)
    {
        return false;
    }

    if (!InitializeAcl(*acl, aclLength, ACL_REVISION) ||
        !AddAccessAllowedAce(*acl, ACL_REVISION, GENERIC_ALL, (*tokenUser)->User.Sid) ||
        !InitializeSecurityDescriptor(securityDescriptor, SECURITY_DESCRIPTOR_REVISION) ||
        !SetSecurityDescriptorOwner(securityDescriptor, (*tokenUser)->User.Sid, FALSE) ||
        !SetSecurityDescriptorDacl(securityDescriptor, TRUE, *acl, FALSE) ||
        !SetSecurityDescriptorControl(securityDescriptor, SE_DACL_PROTECTED, SE_DACL_PROTECTED))
    {
        return false;
    }

    securityAttributes->nLength = sizeof(*securityAttributes);
    securityAttributes->lpSecurityDescriptor = securityDescriptor;
    securityAttributes->bInheritHandle = FALSE;
    return true;
}

intptr_t minipal_create_owner_only_file_for_overwrite(const char* path)
{
    WCHAR* pathUtf16 = convert_path_to_utf16(path);
    if (pathUtf16 == NULL)
    {
        return -1;
    }

    HANDLE token = NULL;
    TOKEN_USER* tokenUser = NULL;
    ACL* acl = NULL;
    SECURITY_DESCRIPTOR securityDescriptor;
    SECURITY_ATTRIBUTES securityAttributes;
    HANDLE file = INVALID_HANDLE_VALUE;

    if (!initialize_owner_only_security_attributes(&securityAttributes, &securityDescriptor, &token, &tokenUser, &acl))
    {
        goto Exit;
    }

    DWORD attributes = GetFileAttributesW(pathUtf16);
    if (attributes != INVALID_FILE_ATTRIBUTES)
    {
        if ((attributes & (FILE_ATTRIBUTE_DIRECTORY | FILE_ATTRIBUTE_REPARSE_POINT)) != 0)
        {
            goto Exit;
        }

        if (!DeleteFileW(pathUtf16))
        {
            goto Exit;
        }
    }
    else
    {
        DWORD error = GetLastError();
        if (error != ERROR_FILE_NOT_FOUND && error != ERROR_PATH_NOT_FOUND)
        {
            goto Exit;
        }
    }

    file = CreateFileW(
        pathUtf16,
        GENERIC_WRITE,
        FILE_SHARE_READ,
        &securityAttributes,
        CREATE_NEW,
        FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OPEN_REPARSE_POINT,
        NULL);

Exit:
    free(acl);
    free(tokenUser);
    if (token != NULL)
    {
        CloseHandle(token);
    }
    free(pathUtf16);

    return (intptr_t)file;
}

#else

intptr_t minipal_create_owner_only_file_for_overwrite(const char* path)
{
    if (path == NULL)
    {
        return -1;
    }

    struct stat fileStatus;
    if (lstat(path, &fileStatus) == 0)
    {
        if (!S_ISREG(fileStatus.st_mode))
        {
            return -1;
        }

        if (unlink(path) == -1)
        {
            return -1;
        }
    }
    else if (errno != ENOENT)
    {
        return -1;
    }

    int flags = O_WRONLY | O_CREAT | O_EXCL;
#if HAVE_O_CLOEXEC
    flags |= O_CLOEXEC;
#endif

    int fd = open(path, flags, S_IRUSR | S_IWUSR);
#if !HAVE_O_CLOEXEC
    if (fd != -1 && fcntl(fd, F_SETFD, FD_CLOEXEC) == -1)
    {
        close(fd);
        return -1;
    }
#endif

    return (intptr_t)fd;
}

#endif
