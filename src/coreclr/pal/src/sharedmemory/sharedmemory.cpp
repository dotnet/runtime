// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(SHMEM); // some headers have code with asserts, so do this first

#include "pal/sharedmemory.h"

#include "pal/file.hpp"
#include "pal/thread.hpp"
#include "pal/virtual.h"
#include "pal/process.h"
#include "pal/utils.h"

#include <sys/file.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/types.h>

#include <fcntl.h>
#include <pwd.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

using namespace CorUnix;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// AutoFreeBuffer

AutoFreeBuffer::AutoFreeBuffer(void *buffer) : m_buffer(buffer), m_cancel(false)
{
}

AutoFreeBuffer::~AutoFreeBuffer()
{
    if (!m_cancel && m_buffer != nullptr)
    {
        free(m_buffer);
    }
}

void AutoFreeBuffer::Cancel()
{
    m_cancel = true;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// SharedMemoryException

SharedMemoryException::SharedMemoryException(DWORD errorCode) : m_errorCode(errorCode)
{
}

DWORD SharedMemoryException::GetErrorCode() const
{
    return m_errorCode;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// SharedMemorySystemCallErrors

SharedMemorySystemCallErrors::SharedMemorySystemCallErrors(char *buffer, int bufferSize)
    : m_buffer(buffer), m_bufferSize(bufferSize), m_length(0), m_isTracking(bufferSize != 0)
{
    _ASSERTE((buffer == nullptr) == (bufferSize == 0));
    _ASSERTE(bufferSize >= 0);
}

void SharedMemorySystemCallErrors::Append(LPCSTR format, ...)
{
    if (!m_isTracking)
    {
        return;
    }

    char *buffer = m_buffer;
    _ASSERTE(buffer != nullptr);
    int bufferSize = m_bufferSize;
    _ASSERTE(bufferSize != 0);
    int length = m_length;
    _ASSERTE(length < bufferSize);
    _ASSERTE(buffer[length] == '\0');
    if (length >= bufferSize - 1)
    {
        return;
    }

    if (length != 0)
    {
        length++; // the previous null terminator will be changed to a space if the append succeeds
    }

    va_list args;
    va_start(args, format);
    int result = _vsnprintf_s(buffer + length, bufferSize - length, bufferSize - 1 - length, format, args);
    va_end(args);

    if (result == 0)
    {
        return;
    }

    if (result < 0 || result >= bufferSize - length)
    {
        // There's not enough space to append this error, discard the append and stop tracking
        if (length == 0)
        {
            buffer[0] = '\0';
        }
        m_isTracking = false;
        return;
    }

    if (length != 0)
    {
        buffer[length - 1] = ' '; // change the previous null terminator to a space
    }

    length += result;
    _ASSERTE(buffer[length] == '\0');
    m_length = length;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// SharedMemoryHelpers

PathCharString *SharedMemoryHelpers::s_userSpecificSharedFilesPath = nullptr;

const mode_t SharedMemoryHelpers::PermissionsMask_OwnerUser_ReadWrite = S_IRUSR | S_IWUSR;
const mode_t SharedMemoryHelpers::PermissionsMask_OwnerUser_ReadWriteExecute = S_IRUSR | S_IWUSR | S_IXUSR;
const mode_t SharedMemoryHelpers::PermissionsMask_NonOwnerUsers_Write = S_IWGRP | S_IWOTH;
const mode_t SharedMemoryHelpers::PermissionsMask_AllUsers_ReadWrite =
    S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH;
const mode_t SharedMemoryHelpers::PermissionsMask_AllUsers_ReadWriteExecute =
    PermissionsMask_AllUsers_ReadWrite | (S_IXUSR | S_IXGRP | S_IXOTH);
const mode_t SharedMemoryHelpers::PermissionsMask_Sticky = S_ISVTX;
const UINT32 SharedMemoryHelpers::InvalidProcessId = static_cast<UINT32>(-1);
const SIZE_T SharedMemoryHelpers::InvalidThreadId = static_cast<SIZE_T>(-1);
const UINT64 SharedMemoryHelpers::InvalidSharedThreadId = static_cast<UINT64>(-1);

void *SharedMemoryHelpers::Alloc(SIZE_T byteCount)
{
    void *buffer = malloc(byteCount != 0 ? byteCount : 1);
    if (buffer == nullptr)
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
    }
    return buffer;
}

SIZE_T SharedMemoryHelpers::AlignDown(SIZE_T value, SIZE_T alignment)
{
    _ASSERTE((alignment & (alignment - 1)) == 0); // must be a power of 2
    return value & ~(alignment - 1);
}

SIZE_T SharedMemoryHelpers::AlignUp(SIZE_T value, SIZE_T alignment)
{
    _ASSERTE((alignment & (alignment - 1)) == 0); // must be a power of 2
    return AlignDown(value + (alignment - 1), alignment);
}

bool SharedMemoryHelpers::AppendPathComponent(PathCharString& path, LPCSTR component)
{
    _ASSERTE(component != nullptr);
    _ASSERTE(component[0] != '\0');

    SIZE_T pathCharCount = path.GetCount();
    return
        (pathCharCount == 0 || path.GetString()[pathCharCount - 1] == '/' || path.Append('/')) &&
        path.Append(component, strlen(component));
}

void SharedMemoryHelpers::CreateDirectory(LPCSTR path, mode_t permissionsMask)
{
    int result;
    do
    {
        result = mkdir(path, permissionsMask);
    } while (result != 0 && errno == EINTR);

    if (result != 0 && errno != EEXIST)
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }
}

bool SharedMemoryHelpers::GetHomeDirectory(PathCharString& path)
{
    const char *homeDirectory = getenv("HOME");
    if (homeDirectory != nullptr && homeDirectory[0] != '\0')
    {
        return path.Set(homeDirectory, strlen(homeDirectory));
    }

    long suggestedBufferSize = sysconf(_SC_GETPW_R_SIZE_MAX);
    SIZE_T bufferSize = suggestedBufferSize > 0 ? static_cast<SIZE_T>(suggestedBufferSize) : 1024;
    while (true)
    {
        char *buffer = reinterpret_cast<char *>(Alloc(bufferSize));
        AutoFreeBuffer autoFreeBuffer(buffer);
        struct passwd passwdData;
        struct passwd *passwdResult;
        int errorCode = getpwuid_r(geteuid(), &passwdData, buffer, bufferSize, &passwdResult);
        if (errorCode == 0)
        {
            return
                passwdResult != nullptr &&
                passwdResult->pw_dir != nullptr &&
                passwdResult->pw_dir[0] != '\0' &&
                path.Set(passwdResult->pw_dir, strlen(passwdResult->pw_dir));
        }

        if (errorCode != ERANGE || bufferSize > static_cast<SIZE_T>(-1) / 2)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        bufferSize *= 2;
    }
}

bool SharedMemoryHelpers::InitializeUserSpecificSharedFilesPath()
{
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());

    if (s_userSpecificSharedFilesPath != nullptr)
    {
        return true;
    }

    PathCharString path;
#ifdef __APPLE__
    if (gApplicationGroupId != nullptr && gApplicationGroupId[0] != '\0')
    {
        if (!path.Set(*gSharedFilesPath))
        {
            return false;
        }
    }
    else
    {
        const char *tmpDirectory = getenv("TMPDIR");
        if (tmpDirectory != nullptr && tmpDirectory[0] != '\0' && !path.Set(tmpDirectory, strlen(tmpDirectory)))
        {
            return false;
        }
    }
#elif defined(__linux__)
    const char *xdgRuntimeDirectory = getenv("XDG_RUNTIME_DIR");
    if (xdgRuntimeDirectory != nullptr &&
        xdgRuntimeDirectory[0] != '\0' &&
        !path.Set(xdgRuntimeDirectory, strlen(xdgRuntimeDirectory)))
    {
        return false;
    }
#endif

    if (path.IsEmpty())
    {
        if (!GetHomeDirectory(path) || !AppendPathComponent(path, ".dotnet"))
        {
            return false;
        }

        CreateDirectory(path, PermissionsMask_OwnerUser_ReadWriteExecute);
        if (!AppendPathComponent(path, "corefx"))
        {
            return false;
        }

        CreateDirectory(path, PermissionsMask_OwnerUser_ReadWriteExecute);
        if (!AppendPathComponent(path, "Runtime"))
        {
            return false;
        }

        CreateDirectory(path, PermissionsMask_OwnerUser_ReadWriteExecute);
        if (!AppendPathComponent(path, "SharedMemory"))
        {
            return false;
        }

        // This is not a system-created directory, so guarantee that it exists.
        // Owner-only permissions are sufficient because it is under the user's home directory.
        CreateDirectory(path, PermissionsMask_OwnerUser_ReadWriteExecute);
    }

    PathCharString *userSpecificSharedFilesPath = new(std::nothrow) PathCharString();
    if (userSpecificSharedFilesPath == nullptr)
    {
        return false;
    }

    if (!userSpecificSharedFilesPath->Set(path))
    {
        delete userSpecificSharedFilesPath;
        return false;
    }

    s_userSpecificSharedFilesPath = userSpecificSharedFilesPath;
    return true;
}

bool SharedMemoryHelpers::BuildSharedFilesSystemPath(PathCharString& path, const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);

    if (!id->IsUserScope())
    {
        return path.Set(*gSharedFilesPath);
    }

    return InitializeUserSpecificSharedFilesPath() && path.Set(*s_userSpecificSharedFilesPath);
}

bool SharedMemoryHelpers::BuildSharedFilesRootPath(PathCharString& path, const SharedMemoryId *id)
{
    LPCSTR runtimeDirectoryName = SHARED_MEMORY_RUNTIME_TEMP_DIRECTORY_NAME;
#ifdef __APPLE__
    if (id->IsUserScope() && gApplicationGroupId != nullptr && gApplicationGroupId[0] != '\0')
    {
        // An Apple application-group container is already user-specific, so global-scoped and user-scoped mutexes would
        // otherwise use the same .dotnet directory and conflict. Give user-scoped mutexes a parallel directory to preserve
        // the distinction between the scopes.
        runtimeDirectoryName = SHARED_MEMORY_USER_RUNTIME_TEMP_DIRECTORY_NAME;
    }
#endif

    return
        BuildSharedFilesSystemPath(path, id) &&
        AppendPathComponent(path, runtimeDirectoryName);
}

bool SharedMemoryHelpers::BuildSharedMemoryDirectoryPath(PathCharString& path, const SharedMemoryId *id)
{
    return
        BuildSharedFilesRootPath(path, id) &&
        AppendPathComponent(path, SHARED_MEMORY_SHARED_MEMORY_DIRECTORY_NAME);
}

bool SharedMemoryHelpers::BuildSharedMemorySessionDirectoryPath(PathCharString& path, const SharedMemoryId *id)
{
    if (!BuildSharedMemoryDirectoryPath(path, id))
    {
        return false;
    }

    PathCharString name;
    return
        BuildSharedMemorySessionDirectoryName(name, id) &&
        AppendPathComponent(path, name);
}

bool SharedMemoryHelpers::BuildSharedMemorySessionDirectoryName(PathCharString& name, const SharedMemoryId *id)
{
    if (!id->IsSessionScope())
    {
        return name.Set(SHARED_MEMORY_GLOBAL_DIRECTORY_NAME);
    }

    return
        name.Set(SHARED_MEMORY_SESSION_DIRECTORY_NAME_PREFIX) &&
        AppendUInt32String(name, GetCurrentSessionId());
}

bool SharedMemoryHelpers::BuildSharedMemoryLockFilesDirectoryPath(PathCharString& path, const SharedMemoryId *id)
{
    return
        BuildSharedFilesRootPath(path, id) &&
        AppendPathComponent(path, SHARED_MEMORY_LOCK_FILES_DIRECTORY_NAME);
}

bool SharedMemoryHelpers::BuildSharedMemoryLockFilesSessionDirectoryPath(
    PathCharString& path,
    const SharedMemoryId *id)
{
    if (!BuildSharedMemoryLockFilesDirectoryPath(path, id))
    {
        return false;
    }

    if (!id->IsSessionScope())
    {
        return AppendPathComponent(path, SHARED_MEMORY_GLOBAL_DIRECTORY_NAME);
    }

    return
        AppendPathComponent(path, SHARED_MEMORY_SESSION_DIRECTORY_NAME_PREFIX) &&
        AppendUInt32String(path, GetCurrentSessionId());
}

bool SharedMemoryHelpers::EnsureDirectoryExists(
    SharedMemorySystemCallErrors *errors,
    const char *path,
    const SharedMemoryId *id,
    bool isGlobalLockAcquired,
    bool createIfNotExist)
{
    _ASSERTE(path != nullptr);
    _ASSERTE(id != nullptr);
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!isGlobalLockAcquired || SharedMemoryManager::IsCreationDeletionFileLockAcquired());

    mode_t permissionsMask =
        id->IsUserScope() ? PermissionsMask_OwnerUser_ReadWriteExecute : PermissionsMask_AllUsers_ReadWriteExecute;

    // Check if the directory already exists.
    // Use lstat so we don't automatically follow symlinks.
    struct stat statInfo;
    int statResult = lstat(path, &statInfo);
    if (statResult != 0 && errno == ENOENT)
    {
        if (!createIfNotExist)
        {
            return false;
        }

        // The path does not exist, create the directory. The permissions mask passed to mkdir() is filtered by the process'
        // permissions umask, so mkdir() may not set all of the requested permissions. We need to use chmod() to set the proper
        // permissions. That creates a race when there is no global lock acquired when creating the directory. Another user's
        // process may create the directory and this user's process may try to use it before the other process sets the full
        // permissions. In that case, create a temporary directory first, set the permissions, and rename it to the actual
        // directory name.
        if (isGlobalLockAcquired)
        {
            if (mkdir(path, permissionsMask) != 0)
            {
                if (errors != nullptr)
                {
                    int errorCode = errno;
                    errors->Append(
                        "mkdir(\"%s\", %s_ReadWriteExecute) == -1; errno == %s;",
                        path,
                        id->IsUserScope() ? "OwnerUser" : "AllUsers",
                        GetFriendlyErrorCodeString(errorCode));
                }

                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }

            int directoryFileDescriptor = -1;
            try
            {
                directoryFileDescriptor = Open(errors, path, O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC);
            }
            catch (SharedMemoryException)
            {
                // Avoid leaving a newly-created directory with permissions filtered by umask.
                rmdir(path);
                throw;
            }

            if (directoryFileDescriptor == -1)
            {
                if (errors != nullptr)
                {
                    errors->Append(
                        "open(\"%s\", O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC) == -1; errno == %s;",
                        path,
                        GetFriendlyErrorCodeString(errno));
                }

                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }

            int operationResult;
            do
            {
                operationResult = fchmod(directoryFileDescriptor, permissionsMask);
            } while (operationResult != 0 && errno == EINTR);

            int errorCode = errno;
            CloseFile(directoryFileDescriptor);
            if (operationResult != 0)
            {
                if (errors != nullptr)
                {
                    errors->Append(
                        "fchmod(directoryFileDescriptor, %s_ReadWriteExecute) == %d; errno == %s;",
                        id->IsUserScope() ? "OwnerUser" : "AllUsers",
                        operationResult,
                        GetFriendlyErrorCodeString(errorCode));
                }

                // Remove the newly-created directory to avoid leaving a directory with permissions filtered by umask.
                rmdir(path);

                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }

            return true;
        }

        PathCharString tempPath;
        VerifyStringOperation(
            BuildSharedFilesSystemPath(tempPath, id) &&
            AppendPathComponent(tempPath, SHARED_MEMORY_UNIQUE_TEMP_NAME_TEMPLATE));

        if (mkdtemp(tempPath.OpenStringBuffer()) == nullptr)
        {
            if (errors != nullptr)
            {
                int errorCode = errno;
                errors->Append(
                    "mkdtemp(\"%s\") == nullptr; errno == %s;",
                    (const char *)tempPath,
                    GetFriendlyErrorCodeString(errorCode));
            }

            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        int directoryFileDescriptor = Open(errors, tempPath, O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC);
        if (directoryFileDescriptor == -1)
        {
            rmdir(tempPath);
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        int operationResult = fchmod(directoryFileDescriptor, permissionsMask);
        int errorCode = errno;
        CloseFile(directoryFileDescriptor);
        if (operationResult != 0)
        {
            if (errors != nullptr)
            {
                errors->Append(
                    "fchmod(directoryFileDescriptor, %s_ReadWriteExecute) == %d; errno == %s;",
                    id->IsUserScope() ? "OwnerUser" : "AllUsers",
                    operationResult,
                    GetFriendlyErrorCodeString(errorCode));
            }

            rmdir(tempPath);
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        if (rename(tempPath, path) == 0)
        {
            return true;
        }

        rmdir(tempPath);
        statResult = lstat(path, &statInfo);
    }

    if (statResult == 0)
    {
        // Directory exists, no need to create it
        return true;
    }

    int errorCode = errno;
    if (errors != nullptr)
    {
        errors->Append(
            "lstat(\"%s\", ...) == %d; errno == %s;",
            path,
            statResult,
            GetFriendlyErrorCodeString(errorCode));
    }

    throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
}

int SharedMemoryHelpers::Open(SharedMemorySystemCallErrors *errors, LPCSTR path, int flags, mode_t mode)
{
    int openErrorCode;

    flags |= O_CLOEXEC;
    do
    {
        int fileDescriptor = InternalOpen(path, flags, mode);
        if (fileDescriptor != -1)
        {
            return fileDescriptor;
        }
        openErrorCode = errno;
    } while (openErrorCode == EINTR);

    SharedMemoryError sharedMemoryError;
    switch (openErrorCode)
    {
        case ENOENT:
            _ASSERTE(!(flags & O_CREAT));
            errno = openErrorCode;
            return -1;

        case ENAMETOOLONG:
            sharedMemoryError = SharedMemoryError::NameTooLong;
            break;

        case EMFILE:
        case ENFILE:
        case ENOMEM:
            sharedMemoryError = SharedMemoryError::OutOfMemory;
            break;

        default:
            sharedMemoryError = SharedMemoryError::IO;
            break;
    }

    if (sharedMemoryError != SharedMemoryError::NameTooLong && errors != nullptr)
    {
        errors->Append(
            "open(\"%s\", 0x%x, 0x%x) == -1; errno == %s;",
            path,
            flags,
            (int)mode,
            GetFriendlyErrorCodeString(openErrorCode));
    }

    throw SharedMemoryException(static_cast<DWORD>(sharedMemoryError));
}

int SharedMemoryHelpers::OpenAt(
    SharedMemorySystemCallErrors *errors,
    int directoryFileDescriptor,
    LPCSTR name,
    LPCSTR path,
    int flags,
    mode_t mode)
{
    _ASSERTE(directoryFileDescriptor != -1);
    _ASSERTE(name != nullptr);
    _ASSERTE(name[0] != '\0');
    _ASSERTE(path != nullptr);
    _ASSERTE(path[0] != '\0');

    int openErrorCode;

    flags |= O_CLOEXEC;
    do
    {
        int fileDescriptor = openat(directoryFileDescriptor, name, flags, mode);
        if (fileDescriptor != -1)
        {
            return fileDescriptor;
        }
        openErrorCode = errno;
    } while (openErrorCode == EINTR);

    SharedMemoryError sharedMemoryError;
    switch (openErrorCode)
    {
        case ENOENT:
            _ASSERTE(!(flags & O_CREAT));
            errno = openErrorCode;
            return -1;

        case ENAMETOOLONG:
            sharedMemoryError = SharedMemoryError::NameTooLong;
            break;

        case EMFILE:
        case ENFILE:
        case ENOMEM:
            sharedMemoryError = SharedMemoryError::OutOfMemory;
            break;

        default:
            sharedMemoryError = SharedMemoryError::IO;
            break;
    }

    if (sharedMemoryError != SharedMemoryError::NameTooLong && errors != nullptr)
    {
        errors->Append(
            "openat(%d, \"%s\", 0x%x, 0x%x) == -1; errno == %s;",
            directoryFileDescriptor,
            path,
            flags,
            (int)mode,
            GetFriendlyErrorCodeString(openErrorCode));
    }

    throw SharedMemoryException(static_cast<DWORD>(sharedMemoryError));
}

int SharedMemoryHelpers::OpenDirectory(SharedMemorySystemCallErrors *errors, LPCSTR path)
{
    _ASSERTE(path != nullptr);
    _ASSERTE(path[0] != '\0');

    int fileDescriptor = Open(errors, path, O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC);
    _ASSERTE(fileDescriptor != -1 || errno == ENOENT);
    return fileDescriptor;
}

int SharedMemoryHelpers::EnsureDirectoryExistsAt(
    SharedMemorySystemCallErrors *errors,
    int parentDirectoryFileDescriptor,
    LPCSTR name,
    LPCSTR path,
    const SharedMemoryId *id,
    bool createIfNotExist)
{
    _ASSERTE(parentDirectoryFileDescriptor != -1);
    _ASSERTE(name != nullptr);
    _ASSERTE(name[0] != '\0');
    _ASSERTE(path != nullptr);
    _ASSERTE(path[0] != '\0');
    _ASSERTE(id != nullptr);
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(SharedMemoryManager::IsCreationDeletionFileLockAcquired());

    int directoryFileDescriptor =
        OpenAt(errors, parentDirectoryFileDescriptor, name, path, O_RDONLY | O_NOFOLLOW | O_DIRECTORY);
    if (directoryFileDescriptor != -1)
    {
        return directoryFileDescriptor;
    }
    _ASSERTE(errno == ENOENT);
    if (!createIfNotExist)
    {
        return -1;
    }

    mode_t permissionsMask =
        id->IsUserScope() ? PermissionsMask_OwnerUser_ReadWriteExecute : PermissionsMask_AllUsers_ReadWriteExecute;
    int operationResult;
    do
    {
        operationResult = mkdirat(parentDirectoryFileDescriptor, name, permissionsMask);
    } while (operationResult != 0 && errno == EINTR);

    if (operationResult != 0)
    {
        if (errors != nullptr)
        {
            int errorCode = errno;
            errors->Append(
                "mkdirat(%d, \"%s\", %s_ReadWriteExecute) == -1; errno == %s;",
                parentDirectoryFileDescriptor,
                path,
                id->IsUserScope() ? "OwnerUser" : "AllUsers",
                GetFriendlyErrorCodeString(errorCode));
        }

        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }

    try
    {
        directoryFileDescriptor =
            OpenAt(errors, parentDirectoryFileDescriptor, name, path, O_RDONLY | O_NOFOLLOW | O_DIRECTORY);
    }
    catch (SharedMemoryException)
    {
        unlinkat(parentDirectoryFileDescriptor, name, AT_REMOVEDIR);
        throw;
    }

    _ASSERTE(directoryFileDescriptor != -1);
    do
    {
        operationResult = fchmod(directoryFileDescriptor, permissionsMask);
    } while (operationResult != 0 && errno == EINTR);

    if (operationResult != 0)
    {
        int errorCode = errno;
        if (errors != nullptr)
        {
            errors->Append(
                "fchmod(directoryFileDescriptor, %s_ReadWriteExecute) == %d; errno == %s;",
                id->IsUserScope() ? "OwnerUser" : "AllUsers",
                operationResult,
                GetFriendlyErrorCodeString(errorCode));
        }

        CloseFile(directoryFileDescriptor);
        unlinkat(parentDirectoryFileDescriptor, name, AT_REMOVEDIR);
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }

    return directoryFileDescriptor;
}

int SharedMemoryHelpers::CreateOrOpenFileAt(
    SharedMemorySystemCallErrors *errors,
    int directoryFileDescriptor,
    LPCSTR name,
    LPCSTR path,
    const SharedMemoryId *id,
    bool createIfNotExist,
    bool *createdRef)
{
    _ASSERTE(directoryFileDescriptor != -1);
    _ASSERTE(name != nullptr);
    _ASSERTE(name[0] != '\0');
    _ASSERTE(path != nullptr);
    _ASSERTE(path[0] != '\0');
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!createIfNotExist || SharedMemoryManager::IsCreationDeletionFileLockAcquired());

    // Try to open the file
    int openFlags = O_RDWR | O_NOFOLLOW | O_CLOEXEC;
    int fileDescriptor = OpenAt(errors, directoryFileDescriptor, name, path, openFlags);
    if (fileDescriptor != -1)
    {
        // For user-scoped files, verify the owner UID and permissions
        if (id->IsUserScope())
        {
            struct stat statInfo;
            int statResult = fstat(fileDescriptor, &statInfo);
            if (statResult != 0)
            {
                if (errors != nullptr)
                {
                    int errorCode = errno;
                    errors->Append(
                        "fstat(\"%s\", ...) == %d; errno == %s;",
                        path,
                        statResult,
                        GetFriendlyErrorCodeString(errorCode));
                }

                CloseFile(fileDescriptor);
                throw SharedMemoryException((DWORD)SharedMemoryError::IO);
            }

            if (statInfo.st_uid != id->GetUserScopeUid())
            {
                if (errors != nullptr)
                {
                    errors->Append(
                        "fstat(\"%s\", &info) == 0; info.st_uid == %u; info.st_uid != %u;",
                        path,
                        (int)statInfo.st_uid,
                        (int)id->GetUserScopeUid());
                }

                CloseFile(fileDescriptor);
                throw SharedMemoryException((DWORD)SharedMemoryError::IO);
            }

            if ((statInfo.st_mode & PermissionsMask_AllUsers_ReadWriteExecute) != PermissionsMask_OwnerUser_ReadWrite)
            {
                if (errors != nullptr)
                {
                    errors->Append(
                        "fstat(\"%s\", &info) == 0; info.st_mode == 0x%x; (info.st_mode & AllUsers_ReadWriteExecute) != OwnerUser_ReadWrite;",
                        path,
                        (int)statInfo.st_mode);
                }

                CloseFile(fileDescriptor);
                throw SharedMemoryException((DWORD)SharedMemoryError::IO);
            }
        }

        if (createdRef != nullptr)
        {
            *createdRef = false;
        }
        return fileDescriptor;
    }

    _ASSERTE(errno == ENOENT);
    if (!createIfNotExist)
    {
        if (createdRef != nullptr)
        {
            *createdRef = false;
        }
        return -1;
    }

    // File does not exist, create the file
    openFlags |= O_CREAT | O_EXCL | O_CLOEXEC;
    mode_t permissionsMask = id->IsUserScope() ? PermissionsMask_OwnerUser_ReadWrite : PermissionsMask_AllUsers_ReadWrite;
    fileDescriptor = OpenAt(errors, directoryFileDescriptor, name, path, openFlags, permissionsMask);
    _ASSERTE(fileDescriptor != -1);

    // The permissions mask passed to openat() is filtered by the process' permissions umask, so openat() may not set all of
    // the requested permissions. Use fchmod() to set the proper permissions.
    int operationResult;
    do
    {
        operationResult = fchmod(fileDescriptor, permissionsMask);
    } while (operationResult != 0 && errno == EINTR);

    if (operationResult != 0)
    {
        if (errors != nullptr)
        {
            int errorCode = errno;
            errors->Append(
                "fchmod(\"%s\", %s_ReadWrite) == %d; errno == %s;",
                path,
                id->IsUserScope() ? "OwnerUser" : "AllUsers",
                operationResult,
                GetFriendlyErrorCodeString(errorCode));
        }

        CloseFile(fileDescriptor);
        unlinkat(directoryFileDescriptor, name, 0);
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }

    if (createdRef != nullptr)
    {
        *createdRef = true;
    }
    return fileDescriptor;
}

void SharedMemoryHelpers::CloseFile(int fileDescriptor)
{
    _ASSERTE(fileDescriptor != -1);
    close(fileDescriptor);
}

SIZE_T SharedMemoryHelpers::GetFileSize(SharedMemorySystemCallErrors *errors, LPCSTR filePath, int fileDescriptor)
{
    _ASSERTE(filePath != nullptr);
    _ASSERTE(filePath[0] != '\0');
    _ASSERTE(fileDescriptor != -1);

    off_t endOffset = lseek(fileDescriptor, 0, SEEK_END);
    if (endOffset == static_cast<off_t>(-1) ||
        lseek(fileDescriptor, 0, SEEK_SET) == static_cast<off_t>(-1))
    {
        if (errors != nullptr)
        {
            int errorCode = errno;
            errors->Append(
                "lseek(\"%s\", 0, %s) == -1; errno == %s;",
                filePath,
                endOffset == (off_t)-1 ? "SEEK_END" : "SEEK_SET",
                GetFriendlyErrorCodeString(errorCode));
        }

        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }

    return endOffset;
}

void SharedMemoryHelpers::SetFileSize(
    SharedMemorySystemCallErrors *errors,
    LPCSTR filePath,
    int fileDescriptor,
    SIZE_T byteCount)
{
    _ASSERTE(filePath != nullptr);
    _ASSERTE(filePath[0] != '\0');
    _ASSERTE(fileDescriptor != -1);
    _ASSERTE(static_cast<SIZE_T>(byteCount) == byteCount);

    while (true)
    {
        int ftruncateResult = ftruncate(fileDescriptor, static_cast<off_t>(byteCount));
        if (ftruncateResult == 0)
        {
            break;
        }

        int errorCode = errno;
        if (errorCode != EINTR)
        {
            if (errors != nullptr)
            {
                errors->Append(
                    "ftruncate(\"%s\", %zu) == %d; errno == %s;",
                    filePath,
                    byteCount,
                    ftruncateResult,
                    GetFriendlyErrorCodeString(errorCode));
            }

            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }
    }
}

void *SharedMemoryHelpers::MemoryMapFile(
    SharedMemorySystemCallErrors *errors,
    LPCSTR filePath,
    int fileDescriptor,
    SIZE_T byteCount)
{
    _ASSERTE(filePath != nullptr);
    _ASSERTE(filePath[0] != '\0');
    _ASSERTE(fileDescriptor != -1);
    _ASSERTE(byteCount > sizeof(SharedMemorySharedDataHeader));
    _ASSERTE(AlignDown(byteCount, GetVirtualPageSize()) == byteCount);

    void *sharedMemoryBuffer = mmap(nullptr, byteCount, PROT_READ | PROT_WRITE, MAP_SHARED, fileDescriptor, 0);
    if (sharedMemoryBuffer != MAP_FAILED)
    {
        return sharedMemoryBuffer;
    }

    int errorCode = errno;
    SharedMemoryError sharedMemoryError;
    switch (errorCode)
    {
        case EMFILE:
        case ENFILE:
        case ENOMEM:
            sharedMemoryError = SharedMemoryError::OutOfMemory;
            break;

        default:
            sharedMemoryError = SharedMemoryError::IO;
            break;
    }

    if (errors != nullptr)
    {
        errors->Append(
            "mmap(nullptr, %zu, PROT_READ | PROT_WRITE, MAP_SHARED, \"%s\", 0) == MAP_FAILED; errno == %s;",
            byteCount,
            filePath,
            GetFriendlyErrorCodeString(errorCode));
    }

    throw SharedMemoryException(static_cast<DWORD>(sharedMemoryError));
}

bool SharedMemoryHelpers::TryAcquireFileLock(SharedMemorySystemCallErrors *errors, int fileDescriptor, int operation)
{
    // A file lock is acquired once per file descriptor, so the caller will need to synchronize threads of this process

    _ASSERTE(fileDescriptor != -1);
    _ASSERTE((operation & LOCK_EX) ^ (operation & LOCK_SH));
    _ASSERTE(!(operation & LOCK_UN));

    while (true)
    {
        int flockResult = flock(fileDescriptor, operation);
        if (flockResult == 0)
        {
            return true;
        }

        int flockError = errno;
        SharedMemoryError sharedMemoryError = SharedMemoryError::IO;
        switch (flockError)
        {
            case EWOULDBLOCK:
                return false;

            case EINTR:
                continue;

            case ENOLCK:
                sharedMemoryError = SharedMemoryError::OutOfMemory;
                break;
        }

        if (errors != nullptr)
        {
            errors->Append(
                "flock(%d, %s%s) == %d; errno == %s;",
                fileDescriptor,
                operation & LOCK_EX ? "LOCK_EX" : "LOCK_SH",
                operation & LOCK_NB ? " | LOCK_NB" : "",
                flockResult,
                GetFriendlyErrorCodeString(flockError));
        }

        throw SharedMemoryException(static_cast<DWORD>(sharedMemoryError));
    }
}

void SharedMemoryHelpers::ReleaseFileLock(int fileDescriptor)
{
    _ASSERTE(fileDescriptor != -1);

    int flockResult;
    do
    {
        flockResult = flock(fileDescriptor, LOCK_UN);
    } while (flockResult != 0 && errno == EINTR);
}

bool SharedMemoryHelpers::AppendUInt32String(
    PathCharString& destination,
    UINT32 value)
{
    char int32String[16];

    int valueCharCount =
        sprintf_s(int32String, sizeof(int32String), "%u", value);
    _ASSERTE(valueCharCount > 0);
    return destination.Append(int32String, valueCharCount) != FALSE;
}

void SharedMemoryHelpers::VerifyStringOperation(bool success)
{
    if (!success)
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
    }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// SharedMemoryId

SharedMemoryId::SharedMemoryId()
    : m_name(nullptr), m_nameCharCount(0), m_isSessionScope(false), m_isUserScope(false), m_userScopeUid((uid_t)0)
{
}

SharedMemoryId::SharedMemoryId(LPCSTR name, bool isUserScope)
{
    _ASSERTE(name != nullptr);

    // Look for "Global\" and "Local\" prefixes in the name, and determine the session ID
    if (strncmp(name, "Global\\", 7) == 0)
    {
        m_isSessionScope = false;
        name += STRING_LENGTH("Global\\");
    }
    else
    {
        if (strncmp(name, "Local\\", 6) == 0)
        {
            name += STRING_LENGTH("Local\\");
        }
        m_isSessionScope = true;
    }
    m_name = name;

    m_nameCharCount = strlen(name);
    if (m_nameCharCount == 0)
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::NameEmpty));
    }
    if (m_nameCharCount > SHARED_MEMORY_MAX_FILE_NAME_CHAR_COUNT)
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::NameTooLong));
    }

    // Look for invalid characters '\' and '/' in the name
    for (SIZE_T i = 0; i < m_nameCharCount; ++i)
    {
        char c = name[i];
        if (c == '\\' || c == '/')
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::NameInvalid));
        }
    }

    m_isUserScope = isUserScope;
    m_userScopeUid = isUserScope ? geteuid() : (uid_t)0;

    // The uid_t is converted to UINT32 to create a directory name, verify that it's valid
    static_assert_no_msg(sizeof(uid_t) <= sizeof(UINT32));
    if ((uid_t)(UINT32)m_userScopeUid != m_userScopeUid)
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }
}

LPCSTR SharedMemoryId::GetName() const
{
    _ASSERTE(m_name != nullptr);
    return m_name;
}

SIZE_T SharedMemoryId::GetNameCharCount() const
{
    _ASSERTE(m_name != nullptr);
    return m_nameCharCount;
}

void SharedMemoryId::ReplaceNamePtr(LPCSTR name)
{
    _ASSERTE(name != nullptr);
    _ASSERTE(m_nameCharCount != 0);
    _ASSERTE(strlen(name) == m_nameCharCount);

    m_name = name;
}

bool SharedMemoryId::IsSessionScope() const
{
    _ASSERTE(m_name != nullptr);
    return m_isSessionScope;
}

bool SharedMemoryId::IsUserScope() const
{
    _ASSERTE(m_name != nullptr);
    return m_isUserScope;
}

uid_t SharedMemoryId::GetUserScopeUid() const
{
    _ASSERTE(m_name != nullptr);
    _ASSERTE(m_isUserScope);
    return m_userScopeUid;
}

bool SharedMemoryId::Equals(const SharedMemoryId *other) const
{
    return
        GetNameCharCount() == other->GetNameCharCount() &&
        IsSessionScope() == other->IsSessionScope() &&
        IsUserScope() == other->IsUserScope() &&
        (!IsUserScope() || GetUserScopeUid() == other->GetUserScopeUid()) &&
        strcmp(GetName(), other->GetName()) == 0;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// SharedMemorySharedDataHeader

SIZE_T SharedMemorySharedDataHeader::GetUsedByteCount(SIZE_T dataByteCount)
{
    return sizeof(SharedMemorySharedDataHeader) + dataByteCount;
}

SIZE_T SharedMemorySharedDataHeader::GetTotalByteCount(SIZE_T dataByteCount)
{
    return SharedMemoryHelpers::AlignUp(GetUsedByteCount(dataByteCount), GetVirtualPageSize());
}

SharedMemorySharedDataHeader::SharedMemorySharedDataHeader(SharedMemoryType type, UINT8 version)
    : m_type(type), m_version(version)
{
}

SharedMemoryType SharedMemorySharedDataHeader::GetType() const
{
    return m_type;
}

UINT8 SharedMemorySharedDataHeader::GetVersion() const
{
    return m_version;
}

void *SharedMemorySharedDataHeader::GetData()
{
    return this + 1;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// SharedMemoryProcessDataHeader

SharedMemoryProcessDataHeader *SharedMemoryProcessDataHeader::CreateOrOpen(
    SharedMemorySystemCallErrors *errors,
    LPCSTR name,
    bool isUserScope,
    SharedMemorySharedDataHeader requiredSharedDataHeader,
    SIZE_T sharedDataByteCount,
    bool createIfNotExist,
    bool *createdRef)
{
    _ASSERTE(name != nullptr);
    _ASSERTE(sharedDataByteCount != 0);
    _ASSERTE(!createIfNotExist || createdRef != nullptr);
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!SharedMemoryManager::IsCreationDeletionFileLockAcquired());

    if (createdRef != nullptr)
    {
        *createdRef = false;
    }

    PathCharString filePath;
    PathCharString sessionDirectoryName;
    SharedMemoryId id(name, isUserScope);

    struct AutoCleanup
    {
        const SharedMemoryId *m_acquiredCreationDeletionFileLockForId;
        int m_sharedMemoryDirectoryFileDescriptor;
        int m_sessionDirectoryFileDescriptor;
        LPCSTR m_sessionDirectoryName;
        LPCSTR m_fileName;
        bool m_createdFile;
        int m_fileDescriptor;
        bool m_acquiredFileLock;
        void *m_mappedBuffer;
        SIZE_T m_mappedBufferByteCount;
        bool m_cancel;

        AutoCleanup()
            : m_acquiredCreationDeletionFileLockForId(nullptr),
            m_sharedMemoryDirectoryFileDescriptor(-1),
            m_sessionDirectoryFileDescriptor(-1),
            m_sessionDirectoryName(nullptr),
            m_fileName(nullptr),
            m_createdFile(false),
            m_fileDescriptor(-1),
            m_acquiredFileLock(false),
            m_mappedBuffer(nullptr),
            m_mappedBufferByteCount(0),
            m_cancel(false)
        {
        }

        ~AutoCleanup()
        {
            if (m_cancel)
            {
                return;
            }

            if (m_mappedBuffer != nullptr)
            {
                _ASSERTE(m_mappedBufferByteCount != 0);
                munmap(m_mappedBuffer, m_mappedBufferByteCount);
            }

            if (m_acquiredFileLock)
            {
                _ASSERTE(m_fileDescriptor != -1);
                SharedMemoryHelpers::ReleaseFileLock(m_fileDescriptor);
            }

            if (m_fileDescriptor != -1)
            {
                SharedMemoryHelpers::CloseFile(m_fileDescriptor);
            }

            if (m_createdFile)
            {
                _ASSERTE(m_sessionDirectoryFileDescriptor != -1);
                _ASSERTE(m_fileName != nullptr);
                unlinkat(m_sessionDirectoryFileDescriptor, m_fileName, 0);
            }

            if (m_sessionDirectoryFileDescriptor != -1)
            {
                SharedMemoryHelpers::CloseFile(m_sessionDirectoryFileDescriptor);
            }

            if (m_sessionDirectoryName != nullptr)
            {
                _ASSERTE(m_sharedMemoryDirectoryFileDescriptor != -1);
                unlinkat(m_sharedMemoryDirectoryFileDescriptor, m_sessionDirectoryName, AT_REMOVEDIR);
            }

            if (m_acquiredCreationDeletionFileLockForId != nullptr)
            {
                SharedMemoryManager::ReleaseCreationDeletionFileLock(m_acquiredCreationDeletionFileLockForId);
            }
        }
    } autoCleanup;

    SharedMemoryProcessDataHeader *processDataHeader = SharedMemoryManager::FindProcessDataHeader(&id);
    if (processDataHeader != nullptr)
    {
        _ASSERTE(
            processDataHeader->GetSharedDataTotalByteCount() ==
            SharedMemorySharedDataHeader::GetTotalByteCount(sharedDataByteCount));
        processDataHeader->IncRefCount();
        return processDataHeader;
    }

    SharedMemoryManager::AcquireCreationDeletionFileLock(errors, &id);
    autoCleanup.m_acquiredCreationDeletionFileLockForId = &id;
    int sharedMemoryDirectoryFileDescriptor =
        SharedMemoryManager::GetCreationDeletionLockFileDescriptor(&id);
    autoCleanup.m_sharedMemoryDirectoryFileDescriptor = sharedMemoryDirectoryFileDescriptor;

    // Create the session directory
    SharedMemoryHelpers::VerifyStringOperation(
        SharedMemoryHelpers::BuildSharedMemorySessionDirectoryName(sessionDirectoryName, &id) &&
        SharedMemoryHelpers::BuildSharedMemorySessionDirectoryPath(filePath, &id));
    int sessionDirectoryFileDescriptor = SharedMemoryHelpers::EnsureDirectoryExistsAt(
        errors,
        sharedMemoryDirectoryFileDescriptor,
        sessionDirectoryName,
        filePath,
        &id,
        createIfNotExist);
    if (sessionDirectoryFileDescriptor == -1)
    {
        _ASSERTE(!createIfNotExist);
        return nullptr;
    }
    autoCleanup.m_sessionDirectoryFileDescriptor = sessionDirectoryFileDescriptor;
    autoCleanup.m_sessionDirectoryName = sessionDirectoryName;

    // Create or open the shared memory file
    SharedMemoryHelpers::VerifyStringOperation(filePath.Append('/') && filePath.Append(id.GetName(), id.GetNameCharCount()));
    bool createdFile;
    int fileDescriptor = SharedMemoryHelpers::CreateOrOpenFileAt(
        errors,
        sessionDirectoryFileDescriptor,
        id.GetName(),
        filePath,
        &id,
        createIfNotExist,
        &createdFile);
    if (fileDescriptor == -1)
    {
        _ASSERTE(!createIfNotExist);
        return nullptr;
    }
    autoCleanup.m_fileName = id.GetName();
    autoCleanup.m_createdFile = createdFile;
    autoCleanup.m_fileDescriptor = fileDescriptor;

    bool clearContents = false;
    if (!createdFile)
    {
        // A shared file lock on the shared memory file would be held by any process that has opened the same file. Try to take
        // an exclusive lock on the file. Successfully acquiring an exclusive lock indicates that no process has a reference to
        // the shared memory file, and this process can reinitialize its contents.
        if (SharedMemoryHelpers::TryAcquireFileLock(errors, fileDescriptor, LOCK_EX | LOCK_NB))
        {
            // The shared memory file is not being used, flag it as created so that its contents will be reinitialized
            SharedMemoryHelpers::ReleaseFileLock(fileDescriptor);
            autoCleanup.m_createdFile = true;
            if (!createIfNotExist)
            {
                return nullptr;
            }
            createdFile = true;
            clearContents = true;
        }
    }

    // Set or validate the file length
    SIZE_T sharedDataUsedByteCount = SharedMemorySharedDataHeader::GetUsedByteCount(sharedDataByteCount);
    SIZE_T sharedDataTotalByteCount = SharedMemorySharedDataHeader::GetTotalByteCount(sharedDataByteCount);
    if (createdFile)
    {
        SharedMemoryHelpers::SetFileSize(errors, filePath, fileDescriptor, sharedDataTotalByteCount);
    }
    else
    {
        SIZE_T currentFileSize = SharedMemoryHelpers::GetFileSize(errors, filePath, fileDescriptor);
        if (currentFileSize < sharedDataUsedByteCount)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::HeaderMismatch));
        }
        if (currentFileSize < sharedDataTotalByteCount)
        {
            SharedMemoryHelpers::SetFileSize(errors, filePath, fileDescriptor, sharedDataTotalByteCount);
        }
    }

    // Acquire and hold a shared file lock on the shared memory file as long as it is open, to indicate that this process is
    // using the file. An exclusive file lock is attempted above to detect whether the file contents are valid, for the case
    // where a process crashes or is killed after the file is created. Since we already hold the creation/deletion locks, a
    // non-blocking file lock should succeed.
    if (!SharedMemoryHelpers::TryAcquireFileLock(errors, fileDescriptor, LOCK_SH | LOCK_NB))
    {
        if (errors != nullptr)
        {
            int errorCode = errno;
            errors->Append(
                "flock(\"%s\", LOCK_SH | LOCK_NB) == -1; errno == %s;",
                (const char *)filePath,
                GetFriendlyErrorCodeString(errorCode));
        }

        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }
    autoCleanup.m_acquiredFileLock = true;

    // Map the file into memory, and initialize or validate the header
    void *mappedBuffer = SharedMemoryHelpers::MemoryMapFile(errors, filePath, fileDescriptor, sharedDataTotalByteCount);
    autoCleanup.m_mappedBuffer = mappedBuffer;
    autoCleanup.m_mappedBufferByteCount = sharedDataTotalByteCount;
    SharedMemorySharedDataHeader *sharedDataHeader;
    if (createdFile)
    {
        if (clearContents)
        {
            memset(mappedBuffer, 0, sharedDataUsedByteCount);
        }
        sharedDataHeader = new(mappedBuffer) SharedMemorySharedDataHeader(requiredSharedDataHeader);
    }
    else
    {
        sharedDataHeader = reinterpret_cast<SharedMemorySharedDataHeader *>(mappedBuffer);
        if (sharedDataHeader->GetType() != requiredSharedDataHeader.GetType() ||
            sharedDataHeader->GetVersion() != requiredSharedDataHeader.GetVersion())
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::HeaderMismatch));
        }
    }

    // When *createdRef is true, the creation/deletion file lock will remain locked upon returning for the caller to initialize
    // the shared data. The caller must release the file lock afterwards.
    if (!createdFile)
    {
        autoCleanup.m_acquiredCreationDeletionFileLockForId = nullptr;
        SharedMemoryManager::ReleaseCreationDeletionFileLock(&id);
    }

    processDataHeader = SharedMemoryProcessDataHeader::New(&id, fileDescriptor, sharedDataHeader, sharedDataTotalByteCount);

    SharedMemoryHelpers::CloseFile(sessionDirectoryFileDescriptor);
    autoCleanup.m_sessionDirectoryFileDescriptor = -1;
    autoCleanup.m_sessionDirectoryName = nullptr;
    autoCleanup.m_cancel = true;
    if (createdFile)
    {
        _ASSERTE(createIfNotExist);
        _ASSERTE(createdRef != nullptr);
        *createdRef = true;
    }
    return processDataHeader;
}

SharedMemoryProcessDataHeader *SharedMemoryProcessDataHeader::PalObject_GetProcessDataHeader(CorUnix::IPalObject *object)
{
    _ASSERTE(object != nullptr);
    _ASSERTE(object->GetObjectType()->GetId() == otiNamedMutex);
    _ASSERTE(object->GetObjectType()->GetImmutableDataSize() == sizeof(SharedMemoryProcessDataHeader *));

    void *immutableDataBuffer;
    PAL_ERROR errorCode = object->GetImmutableData(&immutableDataBuffer);
    _ASSERTE(errorCode == NO_ERROR);
    _ASSERTE(immutableDataBuffer != nullptr);
    return *reinterpret_cast<SharedMemoryProcessDataHeader **>(immutableDataBuffer);
}

void SharedMemoryProcessDataHeader::PalObject_SetProcessDataHeader(
    CorUnix::IPalObject *object,
    SharedMemoryProcessDataHeader *processDataHeader)
{
    _ASSERTE(object != nullptr);
    _ASSERTE(object->GetObjectType()->GetId() == otiNamedMutex);
    _ASSERTE(object->GetObjectType()->GetImmutableDataSize() == sizeof(SharedMemoryProcessDataHeader *));
    _ASSERTE(processDataHeader != nullptr);

    void *immutableDataBuffer;
    PAL_ERROR errorCode = object->GetImmutableData(&immutableDataBuffer);
    _ASSERTE(errorCode == NO_ERROR);
    _ASSERTE(immutableDataBuffer != nullptr);
    *reinterpret_cast<SharedMemoryProcessDataHeader **>(immutableDataBuffer) = processDataHeader;
}

void SharedMemoryProcessDataHeader::PalObject_Close(
    CPalThread *thread,
    IPalObject *object,
    bool isShuttingDown)
{
    // This function's signature matches OBJECTCLEANUPROUTINE
    _ASSERTE(thread != nullptr);
    _ASSERTE(object != nullptr);
    _ASSERTE(object->GetObjectType()->GetId() == otiNamedMutex);
    _ASSERTE(object->GetObjectType()->GetImmutableDataSize() == sizeof(SharedMemoryProcessDataHeader *));

    SharedMemoryProcessDataHeader *processDataHeader = PalObject_GetProcessDataHeader(object);
    if (processDataHeader == nullptr)
    {
        // The object was created, but an error must have occurred before the process data was initialized
        return;
    }

    SharedMemoryManager::AcquireCreationDeletionProcessLock();
    processDataHeader->DecRefCount();
    SharedMemoryManager::ReleaseCreationDeletionProcessLock();
}

SharedMemoryProcessDataHeader::SharedMemoryProcessDataHeader(
    const SharedMemoryId *id,
    int fileDescriptor,
    SharedMemorySharedDataHeader *sharedDataHeader,
    SIZE_T sharedDataTotalByteCount)
    :
    m_refCount(1),
    m_id(*id),
    m_data(nullptr),
    m_fileDescriptor(fileDescriptor),
    m_sharedDataHeader(sharedDataHeader),
    m_sharedDataTotalByteCount(sharedDataTotalByteCount),
    m_nextInProcessDataHeaderList(nullptr)
{
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(id != nullptr);
    _ASSERTE(fileDescriptor != -1);
    _ASSERTE(sharedDataHeader != nullptr);
    _ASSERTE(sharedDataTotalByteCount > sizeof(SharedMemorySharedDataHeader));
    _ASSERTE(SharedMemoryHelpers::AlignDown(sharedDataTotalByteCount, GetVirtualPageSize()) == sharedDataTotalByteCount);

    // Copy the name and initialize the ID
    char *nameCopy = reinterpret_cast<char *>(this + 1);
    SIZE_T nameByteCount = id->GetNameCharCount() + 1;
    memcpy_s(nameCopy, nameByteCount, id->GetName(), nameByteCount);
    m_id.ReplaceNamePtr(nameCopy);

    SharedMemoryManager::AddProcessDataHeader(this);
}

SharedMemoryProcessDataHeader *SharedMemoryProcessDataHeader::New(
    const SharedMemoryId *id,
    int fileDescriptor,
    SharedMemorySharedDataHeader *sharedDataHeader,
    SIZE_T sharedDataTotalByteCount)
{
    _ASSERTE(id != nullptr);

    // Allocate space for the header and a copy of the name
    SIZE_T nameByteCount = id->GetNameCharCount() + 1;
    SIZE_T totalByteCount = sizeof(SharedMemoryProcessDataHeader) + nameByteCount;
    void *buffer = SharedMemoryHelpers::Alloc(totalByteCount);
    AutoFreeBuffer autoFreeBuffer(buffer);
    SharedMemoryProcessDataHeader *processDataHeader =
        new(buffer) SharedMemoryProcessDataHeader(id, fileDescriptor, sharedDataHeader, sharedDataTotalByteCount);
    autoFreeBuffer.Cancel();
    return processDataHeader;
}

SharedMemoryProcessDataHeader::~SharedMemoryProcessDataHeader()
{
    _ASSERTE(m_refCount == 0);
    Close();
}

void SharedMemoryProcessDataHeader::Close()
{
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!SharedMemoryManager::IsCreationDeletionFileLockAcquired());

    // If the ref count is nonzero, we are shutting down the process abruptly without having closed some shared memory objects.
    // There could still be threads running with active references to the shared memory object. So when the ref count is
    // nonzero, don't clean up any object or global process-local state.
    if (m_refCount == 0)
    {
        _ASSERTE(m_data == nullptr || m_data->CanClose());
        SharedMemoryManager::RemoveProcessDataHeader(this);
    }

    struct AutoReleaseCreationDeletionFileLock
    {
        const SharedMemoryId *m_acquiredForId;

        AutoReleaseCreationDeletionFileLock() : m_acquiredForId(nullptr)
        {
        }

        ~AutoReleaseCreationDeletionFileLock()
        {
            if (m_acquiredForId != nullptr)
            {
                SharedMemoryManager::ReleaseCreationDeletionFileLock(m_acquiredForId);
            }
        }
    } autoReleaseCreationDeletionFileLock;

    // A shared file lock on the shared memory file would be held by any process that has opened the same file. Try to take
    // an exclusive lock on the file. Successfully acquiring an exclusive lock indicates that no process has a reference to
    // the shared memory file, and this process can delete the file. File locks on the shared memory file are only ever acquired
    // or released while holding the creation/deletion locks, so holding the creation/deletion locks while trying an exclusive
    // lock on the shared memory file guarantees that another process cannot start using the shared memory file after this
    // process has decided to delete the file.
    bool releaseSharedData = false;
    try
    {
        SharedMemoryManager::AcquireCreationDeletionFileLock(nullptr, GetId());
        autoReleaseCreationDeletionFileLock.m_acquiredForId = GetId();

        SharedMemoryHelpers::ReleaseFileLock(m_fileDescriptor);
        if (SharedMemoryHelpers::TryAcquireFileLock(nullptr, m_fileDescriptor, LOCK_EX | LOCK_NB))
        {
            SharedMemoryHelpers::ReleaseFileLock(m_fileDescriptor);
            releaseSharedData = true;
        }
    }
    catch (SharedMemoryException)
    {
        // Ignore the error, just don't release shared data
    }

    if (m_data != nullptr)
    {
        m_data->Close(m_refCount != 0 /* isAbruptShutdown */, releaseSharedData);
    }

    if (m_refCount == 0)
    {
        if (m_data != nullptr)
        {
            delete m_data;
        }

        if (releaseSharedData)
        {
            m_sharedDataHeader->~SharedMemorySharedDataHeader();
        }

        munmap(m_sharedDataHeader, m_sharedDataTotalByteCount);
        SharedMemoryHelpers::CloseFile(m_fileDescriptor);
    }

    if (!releaseSharedData)
    {
        return;
    }

    try
    {
        // Delete the shared memory file, and the session directory if it's empty.
        PathCharString sessionDirectoryName;
        PathCharString sessionDirectoryPath;
        SharedMemoryHelpers::VerifyStringOperation(
            SharedMemoryHelpers::BuildSharedMemorySessionDirectoryName(sessionDirectoryName, &m_id) &&
            SharedMemoryHelpers::BuildSharedMemorySessionDirectoryPath(sessionDirectoryPath, &m_id));
        int sharedMemoryDirectoryFileDescriptor =
            SharedMemoryManager::GetCreationDeletionLockFileDescriptor(&m_id);
        int sessionDirectoryFileDescriptor = SharedMemoryHelpers::EnsureDirectoryExistsAt(
            nullptr,
            sharedMemoryDirectoryFileDescriptor,
            sessionDirectoryName,
            sessionDirectoryPath,
            &m_id,
            false /* createIfNotExist */);
        if (sessionDirectoryFileDescriptor != -1)
        {
            unlinkat(sessionDirectoryFileDescriptor, m_id.GetName(), 0);
            SharedMemoryHelpers::CloseFile(sessionDirectoryFileDescriptor);
            unlinkat(sharedMemoryDirectoryFileDescriptor, sessionDirectoryName, AT_REMOVEDIR);
        }
    }
    catch (SharedMemoryException)
    {
        // Ignore the error, just don't release shared data
    }
}

const SharedMemoryId *SharedMemoryProcessDataHeader::GetId() const
{
    return &m_id;
}

SharedMemoryProcessDataBase *SharedMemoryProcessDataHeader::GetData() const
{
    return m_data;
}

void SharedMemoryProcessDataHeader::SetData(SharedMemoryProcessDataBase *data)
{
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(m_data == nullptr);
    _ASSERTE(data != nullptr);

    m_data = data;
}

SharedMemorySharedDataHeader *SharedMemoryProcessDataHeader::GetSharedDataHeader() const
{
    return m_sharedDataHeader;
}

SIZE_T SharedMemoryProcessDataHeader::GetSharedDataTotalByteCount() const
{
    return m_sharedDataTotalByteCount;
}

SharedMemoryProcessDataHeader *SharedMemoryProcessDataHeader::GetNextInProcessDataHeaderList() const
{
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    return m_nextInProcessDataHeaderList;
}

void SharedMemoryProcessDataHeader::SetNextInProcessDataHeaderList(SharedMemoryProcessDataHeader *next)
{
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    m_nextInProcessDataHeaderList = next;
}

void SharedMemoryProcessDataHeader::IncRefCount()
{
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(m_refCount != 0);

    if (++m_refCount == 2 && m_data != nullptr && m_data->HasImplicitRef())
    {
        // The synchronization object got an explicit ref that will govern its lifetime, remove the implicit ref
        --m_refCount;
        m_data->SetHasImplicitRef(false);
    }
}

void SharedMemoryProcessDataHeader::DecRefCount()
{
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(m_refCount != 0);

    if (--m_refCount != 0)
    {
        return;
    }

    if (m_data != nullptr && !m_data->CanClose())
    {
        // Extend the lifetime of the synchronization object. The process data object is responsible for removing this extra ref
        // when the synchronization object transitions into a state where it can be closed.
        ++m_refCount;
        m_data->SetHasImplicitRef(true);
        return;
    }

    delete this;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// SharedMemoryManager

minipal_mutex SharedMemoryManager::s_creationDeletionProcessLock;
int SharedMemoryManager::s_runtimeDirectoryFileDescriptor = -1;
int SharedMemoryManager::s_creationDeletionLockFileDescriptor = -1;

SharedMemoryManager::UserScopeUidAndFileDescriptor *SharedMemoryManager::s_userScopeUidToCreationDeletionLockFDs;
int SharedMemoryManager::s_userScopeUidToCreationDeletionLockFDsCount;
int SharedMemoryManager::s_userScopeUidToCreationDeletionLockFDsCapacity;

SharedMemoryProcessDataHeader *SharedMemoryManager::s_processDataHeaderListHead = nullptr;

#ifdef _DEBUG
SIZE_T SharedMemoryManager::s_creationDeletionProcessLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
SIZE_T SharedMemoryManager::s_creationDeletionFileLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
#endif // _DEBUG

void SharedMemoryManager::StaticInitialize()
{
    minipal_mutex_init(&s_creationDeletionProcessLock);
}

void SharedMemoryManager::StaticClose()
{
    // This function could very well be running during abrupt shutdown, and there could still be user threads running.
    // Synchronize the deletion, and don't remove or delete items in the linked list.
    AcquireCreationDeletionProcessLock();
    for (SharedMemoryProcessDataHeader *current = s_processDataHeaderListHead;
        current != nullptr;
        current = current->GetNextInProcessDataHeaderList())
    {
        current->Close();
    }
    ReleaseCreationDeletionProcessLock();

    // This function could very well be running during abrupt shutdown, and there could still be user threads running. Don't
    // delete the creation/deletion process lock, the process is shutting down anyway.
}

void SharedMemoryManager::AcquireCreationDeletionProcessLock()
{
    _ASSERTE(!IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!IsCreationDeletionFileLockAcquired());

    minipal_mutex_enter(&s_creationDeletionProcessLock);
#ifdef _DEBUG
    s_creationDeletionProcessLockOwnerThreadId = THREADSilentGetCurrentThreadId();
#endif // _DEBUG
}

void SharedMemoryManager::ReleaseCreationDeletionProcessLock()
{
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!IsCreationDeletionFileLockAcquired());

#ifdef _DEBUG
    s_creationDeletionProcessLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
#endif // _DEBUG
    minipal_mutex_leave(&s_creationDeletionProcessLock);
}

void SharedMemoryManager::AcquireCreationDeletionFileLock(SharedMemorySystemCallErrors *errors, const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!IsCreationDeletionFileLockAcquired());

    int creationDeletionLockFD =
        id->IsUserScope() ? FindUserScopeCreationDeletionLockFD(id->GetUserScopeUid()) : s_creationDeletionLockFileDescriptor;
    if (creationDeletionLockFD == -1)
    {
        int runtimeDirectoryFileDescriptor = -1;

        // Validate the selected system directory
        PathCharString dirPath;
        SharedMemoryHelpers::VerifyStringOperation(SharedMemoryHelpers::BuildSharedFilesSystemPath(dirPath, id));
        if (!SharedMemoryHelpers::EnsureDirectoryExists(
                errors,
                dirPath,
                id,
                false /* isGlobalLockAcquired */,
                false /* createIfNotExist */))
        {
            _ASSERTE(errno == ENOENT);
            if (errors != nullptr)
            {
                errors->Append("lstat(\"%s\", ...) == -1; errno == ENOENT;", (const char *)dirPath);
            }

            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        // Create the runtime temp directory
        SharedMemoryHelpers::VerifyStringOperation(SharedMemoryHelpers::BuildSharedFilesRootPath(dirPath, id));
        SharedMemoryHelpers::EnsureDirectoryExists(errors, dirPath, id, false /* isGlobalLockAcquired */);
        runtimeDirectoryFileDescriptor = SharedMemoryHelpers::OpenDirectory(errors, dirPath);
        if (runtimeDirectoryFileDescriptor == -1)
        {
            if (errors != nullptr)
            {
                int errorCode = errno;
                errors->Append(
                    "open(\"%s\", O_RDONLY | O_CLOEXEC, 0) == -1; errno == %s;",
                    (const char *)dirPath,
                    GetFriendlyErrorCodeString(errorCode));
            }

            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        try
        {
            // Create the shared memory directory
            SharedMemoryHelpers::VerifyStringOperation(SharedMemoryHelpers::BuildSharedMemoryDirectoryPath(dirPath, id));
            SharedMemoryHelpers::EnsureDirectoryExists(errors, dirPath, id, false /* isGlobalLockAcquired */);

            // Open the shared memory directory
            creationDeletionLockFD = SharedMemoryHelpers::OpenDirectory(errors, dirPath);
            if (creationDeletionLockFD == -1)
            {
                if (errors != nullptr)
                {
                    int errorCode = errno;
                    errors->Append(
                        "open(\"%s\", O_RDONLY | O_CLOEXEC, 0) == -1; errno == %s;",
                        (const char *)dirPath,
                        GetFriendlyErrorCodeString(errorCode));
                }

                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }
        }
        catch (SharedMemoryException)
        {
            SharedMemoryHelpers::CloseFile(runtimeDirectoryFileDescriptor);
            throw;
        }

        if (id->IsUserScope())
        {
            try
            {
                AddUserScopeUidCreationDeletionLockFD(
                    id->GetUserScopeUid(),
                    runtimeDirectoryFileDescriptor,
                    creationDeletionLockFD);
            }
            catch (SharedMemoryException)
            {
                SharedMemoryHelpers::CloseFile(runtimeDirectoryFileDescriptor);
                SharedMemoryHelpers::CloseFile(creationDeletionLockFD);
                throw;
            }
        }
        else
        {
            s_runtimeDirectoryFileDescriptor = runtimeDirectoryFileDescriptor;
            s_creationDeletionLockFileDescriptor = creationDeletionLockFD;
        }
    }

    bool acquiredFileLock = SharedMemoryHelpers::TryAcquireFileLock(errors, creationDeletionLockFD, LOCK_EX);
    _ASSERTE(acquiredFileLock);
#ifdef _DEBUG
    s_creationDeletionFileLockOwnerThreadId = THREADSilentGetCurrentThreadId();
#endif // _DEBUG
}

void SharedMemoryManager::ReleaseCreationDeletionFileLock(const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(IsCreationDeletionFileLockAcquired());

    int creationDeletionLockFD =
        id->IsUserScope() ? FindUserScopeCreationDeletionLockFD(id->GetUserScopeUid()) : s_creationDeletionLockFileDescriptor;
    _ASSERTE(creationDeletionLockFD != -1);

#ifdef _DEBUG
    s_creationDeletionFileLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
#endif // _DEBUG
    SharedMemoryHelpers::ReleaseFileLock(creationDeletionLockFD);
}

int SharedMemoryManager::GetRuntimeDirectoryFileDescriptor(const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(IsCreationDeletionFileLockAcquired());

    int runtimeDirectoryFileDescriptor =
        id->IsUserScope() ? FindUserScopeRuntimeDirectoryFD(id->GetUserScopeUid()) : s_runtimeDirectoryFileDescriptor;
    _ASSERTE(runtimeDirectoryFileDescriptor != -1);
    return runtimeDirectoryFileDescriptor;
}

int SharedMemoryManager::GetCreationDeletionLockFileDescriptor(const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(IsCreationDeletionFileLockAcquired());

    int creationDeletionLockFD =
        id->IsUserScope() ? FindUserScopeCreationDeletionLockFD(id->GetUserScopeUid()) : s_creationDeletionLockFileDescriptor;
    _ASSERTE(creationDeletionLockFD != -1);
    return creationDeletionLockFD;
}

void SharedMemoryManager::AddUserScopeUidCreationDeletionLockFD(
    uid_t userScopeUid,
    int runtimeDirectoryFileDescriptor,
    int creationDeletionLockFD)
{
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(runtimeDirectoryFileDescriptor != -1);
    _ASSERTE(creationDeletionLockFD != -1);
    _ASSERTE(FindUserScopeCreationDeletionLockFD(userScopeUid) == -1);

    int count = s_userScopeUidToCreationDeletionLockFDsCount;
    int capacity = s_userScopeUidToCreationDeletionLockFDsCapacity;
    if (count >= capacity)
    {
        int newCapacity = capacity == 0 ? 1 : capacity * 2;
        if (newCapacity <= capacity ||
            newCapacity * sizeof(UserScopeUidAndFileDescriptor) / sizeof(UserScopeUidAndFileDescriptor) != (SIZE_T)newCapacity)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
        }

        UserScopeUidAndFileDescriptor *newArray = new(std::nothrow) UserScopeUidAndFileDescriptor[newCapacity];
        if (newArray == nullptr)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
        }

        if (count != 0)
        {
            UserScopeUidAndFileDescriptor *oldArray = s_userScopeUidToCreationDeletionLockFDs;
            CopyMemory(newArray, oldArray, count * sizeof(newArray[0]));
            delete[] oldArray;
        }

        s_userScopeUidToCreationDeletionLockFDs = newArray;
        s_userScopeUidToCreationDeletionLockFDsCapacity = newCapacity;
    }

    s_userScopeUidToCreationDeletionLockFDs[count] =
        UserScopeUidAndFileDescriptor(userScopeUid, runtimeDirectoryFileDescriptor, creationDeletionLockFD);
    s_userScopeUidToCreationDeletionLockFDsCount = count + 1;
}

int SharedMemoryManager::FindUserScopeRuntimeDirectoryFD(uid_t userScopeUid)
{
    _ASSERTE(IsCreationDeletionProcessLockAcquired());

    UserScopeUidAndFileDescriptor *arr = s_userScopeUidToCreationDeletionLockFDs;
    for (int i = 0; i < s_userScopeUidToCreationDeletionLockFDsCount; i++)
    {
        _ASSERTE(arr[i].runtimeDirectoryFileDescriptor != -1);
        if (arr[i].userScopeUid == userScopeUid)
        {
            return arr[i].runtimeDirectoryFileDescriptor;
        }
    }

    return -1;
}

int SharedMemoryManager::FindUserScopeCreationDeletionLockFD(uid_t userScopeUid)
{
    _ASSERTE(IsCreationDeletionProcessLockAcquired());

    UserScopeUidAndFileDescriptor *arr = s_userScopeUidToCreationDeletionLockFDs;
    for (int i = 0; i < s_userScopeUidToCreationDeletionLockFDsCount; i++)
    {
        _ASSERTE(arr[i].fileDescriptor != -1);
        if (arr[i].userScopeUid == userScopeUid)
        {
            return arr[i].fileDescriptor;
        }
    }

    return -1;
}

#ifdef _DEBUG
bool SharedMemoryManager::IsCreationDeletionProcessLockAcquired()
{
    return s_creationDeletionProcessLockOwnerThreadId == THREADSilentGetCurrentThreadId();
}

bool SharedMemoryManager::IsCreationDeletionFileLockAcquired()
{
    return s_creationDeletionFileLockOwnerThreadId == THREADSilentGetCurrentThreadId();
}
#endif // _DEBUG

void SharedMemoryManager::AddProcessDataHeader(SharedMemoryProcessDataHeader *processDataHeader)
{
    _ASSERTE(processDataHeader != nullptr);
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(processDataHeader->GetNextInProcessDataHeaderList() == nullptr);
    _ASSERTE(FindProcessDataHeader(processDataHeader->GetId()) == nullptr);

    processDataHeader->SetNextInProcessDataHeaderList(s_processDataHeaderListHead);
    s_processDataHeaderListHead = processDataHeader;
}

void SharedMemoryManager::RemoveProcessDataHeader(SharedMemoryProcessDataHeader *processDataHeader)
{
    _ASSERTE(processDataHeader != nullptr);
    _ASSERTE(IsCreationDeletionProcessLockAcquired());

    if (s_processDataHeaderListHead == processDataHeader)
    {
        s_processDataHeaderListHead = processDataHeader->GetNextInProcessDataHeaderList();
        processDataHeader->SetNextInProcessDataHeaderList(nullptr);
        return;
    }
    for (SharedMemoryProcessDataHeader
            *previous = s_processDataHeaderListHead,
            *current = previous->GetNextInProcessDataHeaderList();
        current != nullptr;
        previous = current, current = current->GetNextInProcessDataHeaderList())
    {
        if (current == processDataHeader)
        {
            previous->SetNextInProcessDataHeaderList(current->GetNextInProcessDataHeaderList());
            current->SetNextInProcessDataHeaderList(nullptr);
            return;
        }
    }
    _ASSERTE(false);
}

SharedMemoryProcessDataHeader *SharedMemoryManager::FindProcessDataHeader(const SharedMemoryId *id)
{
    _ASSERTE(IsCreationDeletionProcessLockAcquired());

    // TODO: Use a hash table
    for (SharedMemoryProcessDataHeader *current = s_processDataHeaderListHead;
        current != nullptr;
        current = current->GetNextInProcessDataHeaderList())
    {
        if (current->GetId()->Equals(id))
        {
            return current;
        }
    }
    return nullptr;
}
