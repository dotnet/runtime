// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "test.h"
#include <minipal/fileio.h>

#ifdef _WIN32
#include <windows.h>
#include <aclapi.h>
#undef FAILED
#else
#include <fcntl.h>
#include <sys/stat.h>
#include <unistd.h>
#endif

static bool write_file(const char* path, const char* text)
{
    FILE* file = fopen(path, "wb");
    if (file == NULL)
        return false;
    bool success = fputs(text, file) >= 0;
    return fclose(file) == 0 && success;
}

static bool file_contains(const char* path, const char* text)
{
    FILE* file = fopen(path, "rb");
    if (file == NULL)
        return false;
    char buffer[32] = {0};
    size_t count = fread(buffer, 1, sizeof(buffer), file);
    bool success = !ferror(file) && count == strlen(text) && memcmp(buffer, text, count) == 0;
    return fclose(file) == 0 && success;
}

static void close_file(intptr_t file)
{
#ifdef _WIN32
    CloseHandle((HANDLE)file);
#else
    close((int)file);
#endif
}

static RESULT check_file_security(intptr_t file)
{
#ifdef _WIN32
    HANDLE token = NULL;
    TOKEN_USER* token_user = NULL;
    PSECURITY_DESCRIPTOR descriptor = NULL;
    PSID owner = NULL;
    PACL acl = NULL;
    DWORD length = 0;
    SECURITY_DESCRIPTOR_CONTROL control;
    DWORD revision;
    void* ace = NULL;
    RESULT result = OK;

    if (!OpenThreadToken(GetCurrentThread(), TOKEN_QUERY, TRUE, &token) &&
        (GetLastError() != ERROR_NO_TOKEN || !OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &token)))
    {
        return FAILED("unable to open effective token");
    }

    GetTokenInformation(token, TokenUser, NULL, 0, &length);
    token_user = (TOKEN_USER*)malloc(length);
    if (token_user == NULL ||
        !GetTokenInformation(token, TokenUser, token_user, length, &length) ||
        GetSecurityInfo((HANDLE)file, SE_FILE_OBJECT, OWNER_SECURITY_INFORMATION | DACL_SECURITY_INFORMATION,
            &owner, NULL, &acl, NULL, &descriptor) != ERROR_SUCCESS ||
        !GetSecurityDescriptorControl(descriptor, &control, &revision) ||
        !(control & SE_DACL_PROTECTED) ||
        !EqualSid(owner, token_user->User.Sid) ||
        acl == NULL || acl->AceCount != 1 || !GetAce(acl, 0, &ace) ||
        ((ACE_HEADER*)ace)->AceType != ACCESS_ALLOWED_ACE_TYPE ||
        ((((ACCESS_ALLOWED_ACE*)ace)->Mask & FILE_ALL_ACCESS) != FILE_ALL_ACCESS &&
         (((ACCESS_ALLOWED_ACE*)ace)->Mask & GENERIC_ALL) == 0) ||
        !EqualSid(&((ACCESS_ALLOWED_ACE*)ace)->SidStart, owner))
    {
        result = FAILED("file owner or protected owner-only DACL is incorrect");
    }

    DWORD flags;
    if (result == OK && (!GetHandleInformation((HANDLE)file, &flags) || (flags & HANDLE_FLAG_INHERIT)))
        result = FAILED("file handle is inheritable");

    if (descriptor != NULL)
        LocalFree(descriptor);
    free(token_user);
    CloseHandle(token);
    return result;
#else
    struct stat status;
    if (fstat((int)file, &status) != 0 || (status.st_mode & 0777) != 0600 || status.st_uid != geteuid())
        return FAILED("file owner or mode is incorrect");
    int flags = fcntl((int)file, F_GETFD);
    if (flags == -1 || !(flags & FD_CLOEXEC))
        return FAILED("file descriptor is inheritable");
    return OK;
#endif
}

static RESULT test_fileio(void)
{
    RESULT result = OK;
    intptr_t file = -1;
#ifdef _WIN32
    WCHAR original[MAX_PATH];
    WCHAR temp[MAX_PATH];
    WCHAR directory[MAX_PATH];
    DWORD original_length = GetCurrentDirectoryW(MAX_PATH, original);
    DWORD temp_length = GetTempPathW(MAX_PATH, temp);
    if (original_length == 0 || original_length >= MAX_PATH || temp_length == 0 || temp_length >= MAX_PATH ||
        !GetTempFileNameW(temp, L"mpt", 0, directory))
        return FAILED("unable to create test directory path");
    if (!DeleteFileW(directory) || !CreateDirectoryW(directory, NULL))
        return FAILED("unable to create test directory");
    if (!SetCurrentDirectoryW(directory))
    {
        RemoveDirectoryW(directory);
        return FAILED("unable to enter test directory");
    }
#else
    char original[4096];
    char directory[4096];
    const char* temp = getenv("TMPDIR");
    if (temp == NULL || *temp == '\0')
        temp = "/tmp";
    if (getcwd(original, sizeof(original)) == NULL ||
        snprintf(directory, sizeof(directory), "%s/minipal-fileio-XXXXXX", temp) >= (int)sizeof(directory) ||
        mkdtemp(directory) == NULL)
        return FAILED("unable to create test directory");
    if (chdir(directory) != 0)
    {
        rmdir(directory);
        return FAILED("unable to enter test directory");
    }
    mode_t old_umask = umask(0);
#endif

#define CHECK(condition, message) do { if (!(condition)) { result = FAILED("%s", message); goto Cleanup; } } while (0)

    CHECK(minipal_create_owner_only_file_for_overwrite(NULL) == -1, "null path was accepted");
    CHECK(minipal_create_owner_only_file_for_overwrite("") == -1, "empty path was accepted");
    CHECK(minipal_create_owner_only_file_for_overwrite("missing/file") == -1, "missing parent was accepted");

    file = minipal_create_owner_only_file_for_overwrite("trace");
    CHECK(file != -1, "fresh file creation failed");
    result = check_file_security(file);
    if (result != OK)
        goto Cleanup;
#ifdef _WIN32
    HANDLE concurrent_reader = CreateFileW(L"trace", GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL,
        OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    CHECK(concurrent_reader != INVALID_HANDLE_VALUE, "unable to read while the writer is open");
    CloseHandle(concurrent_reader);
#endif
    close_file(file);
    file = -1;
    CHECK(write_file("trace", "old"), "unable to write initial file");

#ifdef _WIN32
    CHECK(CreateHardLinkW(L"original", L"trace", NULL), "unable to create hard link");
#else
    CHECK(chmod("trace", 0666) == 0 && link("trace", "original") == 0, "unable to create permissive hard link");
#endif

    file = minipal_create_owner_only_file_for_overwrite("trace");
    CHECK(file != -1, "replacement failed");
    result = check_file_security(file);
    if (result != OK)
        goto Cleanup;
    close_file(file);
    file = -1;
    CHECK(file_contains("trace", ""), "replacement was not empty");
    CHECK(file_contains("original", "old"), "replacement modified the hard link target");

    CHECK(minipal_create_owner_only_file_for_overwrite(".") == -1, "directory was accepted");
#ifdef _WIN32
    BOOL symlink_created = CreateSymbolicLinkW(L"link", L"original", SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE);
    if (!symlink_created && GetLastError() == ERROR_INVALID_PARAMETER)
        symlink_created = CreateSymbolicLinkW(L"link", L"original", 0);
    if (symlink_created)
    {
        CHECK(minipal_create_owner_only_file_for_overwrite("link") == -1, "symlink was accepted");
        CHECK(file_contains("original", "old"), "symlink target was modified");
        DWORD attributes = GetFileAttributesW(L"link");
        CHECK(attributes != INVALID_FILE_ATTRIBUTES && (attributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0,
            "symlink was modified");
        CHECK(DeleteFileW(L"link"), "unable to remove symlink");
    }
    else
    {
        DWORD error = GetLastError();
        CHECK(error == ERROR_PRIVILEGE_NOT_HELD || error == ERROR_NOT_SUPPORTED || error == ERROR_ACCESS_DENIED,
            "unable to create symlink");
        printf("symlink test skipped (error %lu) ", error);
    }
#else
    CHECK(symlink("original", "link") == 0, "unable to create symlink");
    CHECK(minipal_create_owner_only_file_for_overwrite("link") == -1, "symlink was accepted");
    CHECK(file_contains("original", "old"), "symlink target was modified");
    struct stat link_status;
    CHECK(lstat("link", &link_status) == 0 && S_ISLNK(link_status.st_mode), "symlink was modified");
    CHECK(mkfifo("fifo", 0600) == 0, "unable to create FIFO");
    CHECK(minipal_create_owner_only_file_for_overwrite("fifo") == -1, "FIFO was accepted");
    CHECK(lstat("fifo", &link_status) == 0 && S_ISFIFO(link_status.st_mode), "FIFO was modified");
#endif

#ifdef _WIN32
    CHECK(write_file("trace", "reader"), "unable to write reader file");
    HANDLE reader = CreateFileW(L"trace", GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL,
        OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    CHECK(reader != INVALID_HANDLE_VALUE, "unable to open reader");
    file = minipal_create_owner_only_file_for_overwrite("trace");
    DWORD sharing_error = GetLastError();
    CloseHandle(reader);
    CHECK(file == -1, "replacement succeeded without delete sharing");
    CHECK(file_contains("trace", "reader"), "sharing failure modified the file");

    CHECK(SetFileAttributesW(L"trace", FILE_ATTRIBUTE_READONLY), "unable to set read-only attribute");
    file = minipal_create_owner_only_file_for_overwrite("trace");
    CHECK(file == -1, "read-only file was replaced");
    CHECK(file_contains("trace", "reader"), "read-only file was modified");
    CHECK(SetFileAttributesW(L"trace", FILE_ATTRIBUTE_NORMAL), "unable to clear read-only attribute");

    reader = CreateFileW(L"trace", GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL,
        OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    CHECK(reader != INVALID_HANDLE_VALUE, "unable to open reader with delete sharing");
    file = minipal_create_owner_only_file_for_overwrite("trace");
    DWORD replacement_error = file == -1 ? GetLastError() : ERROR_SUCCESS;
    char reader_buffer[6];
    DWORD bytes_read = 0;
    bool old_file_preserved = ReadFile(reader, reader_buffer, sizeof(reader_buffer), &bytes_read, NULL) &&
        bytes_read == sizeof(reader_buffer) && memcmp(reader_buffer, "reader", sizeof(reader_buffer)) == 0;
    CloseHandle(reader);
    if (file == -1)
    {
        result = FAILED("replacement failed with delete sharing (Win32 error %lu)", replacement_error);
        goto Cleanup;
    }
    CHECK(old_file_preserved, "open reader did not retain the old file");
    close_file(file);
    file = -1;
    CHECK(file_contains("trace", ""), "replacement was not empty");
    CHECK(sharing_error == ERROR_SHARING_VIOLATION, "replacement failed for an unexpected reason");
#else
    CHECK(write_file("trace", "reader"), "unable to write reader file");
    int reader = open("trace", O_RDONLY);
    CHECK(reader != -1, "unable to open reader");
    file = minipal_create_owner_only_file_for_overwrite("trace");
    char reader_buffer[6];
    bool old_file_preserved = read(reader, reader_buffer, sizeof(reader_buffer)) == sizeof(reader_buffer) &&
        memcmp(reader_buffer, "reader", sizeof(reader_buffer)) == 0;
    close(reader);
    CHECK(file != -1, "replacement failed with an open reader");
    CHECK(old_file_preserved, "open reader did not retain the old file");
    close_file(file);
    file = -1;
    CHECK(file_contains("trace", ""), "replacement was not empty");
#endif

Cleanup:
    if (file != -1)
        close_file(file);
#ifdef _WIN32
    SetFileAttributesW(L"trace", FILE_ATTRIBUTE_NORMAL);
    DeleteFileW(L"link");
#endif
    remove("trace");
    remove("original");
#ifdef _WIN32
    if (!SetCurrentDirectoryW(original) || !RemoveDirectoryW(directory))
    {
        if (result == OK)
            result = FAILED("unable to remove test directory");
    }
#else
    remove("link");
    remove("fifo");
    umask(old_umask);
    if (chdir(original) != 0 || rmdir(directory) != 0)
    {
        if (result == OK)
            result = FAILED("unable to remove test directory");
    }
#endif
#undef CHECK
    return result;
}

static Test minipal_fileio_tests[] =
{
    {"test_fileio", test_fileio},
    {NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(minipal_fileio_tests_init, minipal_fileio_tests)
