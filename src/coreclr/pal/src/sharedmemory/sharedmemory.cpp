// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal/sharedmemory.h"

#include "pal/dbgmsg.h"
#include "pal/file.hpp"
#include "pal/malloc.hpp"
#include "pal/thread.hpp"
#include "pal/virtual.h"
#include "pal/process.h"

#include <sys/file.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/types.h>

#include <fcntl.h>
#include <limits.h>
#include <pwd.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(SHMEM);

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
// SharedMemoryHelpers

const mode_t SharedMemoryHelpers::PermissionsMask_OwnerUser_ReadWrite = S_IRUSR | S_IWUSR;
const mode_t SharedMemoryHelpers::PermissionsMask_OwnerUser_ReadWriteExecute =
    PermissionsMask_OwnerUser_ReadWrite | S_IXUSR;
const mode_t SharedMemoryHelpers::PermissionsMask_AllUsers_ReadWrite =
    S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH;
const mode_t SharedMemoryHelpers::PermissionsMask_AllUsers_ReadWriteExecute =
    PermissionsMask_AllUsers_ReadWrite | (S_IXUSR | S_IXGRP | S_IXOTH);
const int SharedMemoryHelpers::TransientRetryCount = 10;
const UINT32 SharedMemoryHelpers::InvalidProcessId = static_cast<UINT32>(-1);
const SIZE_T SharedMemoryHelpers::InvalidThreadId = static_cast<SIZE_T>(-1);
const UINT64 SharedMemoryHelpers::InvalidSharedThreadId = static_cast<UINT64>(-1);

static PathCharString *s_userSharedFilesPath = nullptr;
static bool s_userSharedFilesPathInitialized = false;
static bool s_userSharedFilesPathUsesFallback = false;

void *SharedMemoryHelpers::Alloc(SIZE_T byteCount)
{
    void *buffer = InternalMalloc(byteCount);
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

void SharedMemoryHelpers::BuildUserHomePath(PathCharString& destination, uid_t userScopeUid)
{
    size_t bufferSize = 1024;
    long suggestedBufferSize = sysconf(_SC_GETPW_R_SIZE_MAX);
    if (suggestedBufferSize > 0 && static_cast<size_t>(suggestedBufferSize) > bufferSize)
    {
        bufferSize =
            static_cast<size_t>(suggestedBufferSize) <= 1024 * 1024
                ? static_cast<size_t>(suggestedBufferSize)
                : 1024 * 1024;
    }

    while (bufferSize <= 1024 * 1024)
    {
        char *buffer = static_cast<char *>(InternalMalloc(bufferSize));
        if (buffer == nullptr)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
        }

        struct passwd passwordEntry;
        struct passwd *passwordEntryResult = nullptr;
        int errorCode;
        do
        {
            errorCode = getpwuid_r(userScopeUid, &passwordEntry, buffer, bufferSize, &passwordEntryResult);
        } while (errorCode == EINTR);

        if (errorCode == 0 && passwordEntryResult != nullptr &&
            passwordEntryResult->pw_dir != nullptr && passwordEntryResult->pw_dir[0] != '\0')
        {
            bool success =
                destination.Set(passwordEntryResult->pw_dir, strlen(passwordEntryResult->pw_dir)) != FALSE;
            free(buffer);
            VerifyStringOperation(success);
            return;
        }

        free(buffer);
        if (errorCode != ERANGE)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        bufferSize *= 2;
    }

    throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
}

void SharedMemoryHelpers::BuildUserFallbackBasePath(PathCharString& destination, uid_t userScopeUid)
{
    BuildUserHomePath(destination, userScopeUid);
    VerifyStringOperation(
        AppendPathComponent(destination, ".dotnet") &&
        AppendPathComponent(destination, "corefx") &&
        AppendPathComponent(destination, "Runtime") &&
        AppendPathComponent(destination, "SharedMemory"));
    VerifyPathLength(destination);
}

void SharedMemoryHelpers::BuildUserSpecificSharedFilesPath(
    PathCharString& destination,
    const SharedMemoryId *id,
    bool *isUserFallbackPath)
{
    _ASSERTE(id != nullptr);
    _ASSERTE(id->IsUserScope());

    if (!s_userSharedFilesPathInitialized)
    {
        const char *userSharedFilesPath = nullptr;
#ifdef __APPLE__
        if (gApplicationGroupId != nullptr && gApplicationGroupId[0] != '\0')
        {
            userSharedFilesPath = *gSharedFilesPath;
        }
        else
        {
            userSharedFilesPath = getenv("TMPDIR");
        }
#elif defined(__linux__)
        userSharedFilesPath = getenv("XDG_RUNTIME_DIR");
#else
        s_userSharedFilesPathUsesFallback = true;
#endif

        if (userSharedFilesPath == nullptr || userSharedFilesPath[0] == '\0')
        {
            s_userSharedFilesPathUsesFallback = true;
        }
        else
        {
            s_userSharedFilesPath = InternalNew<PathCharString>();
            if (s_userSharedFilesPath == nullptr)
            {
                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
            }
            VerifyStringOperation(s_userSharedFilesPath->Set(userSharedFilesPath, strlen(userSharedFilesPath)));
        }

        s_userSharedFilesPathInitialized = true;
    }

    if (s_userSharedFilesPathUsesFallback)
    {
        BuildUserFallbackBasePath(destination, id->GetUserScopeUid());
    }
    else
    {
        _ASSERTE(s_userSharedFilesPath != nullptr);
        VerifyStringOperation(destination.Set(*s_userSharedFilesPath));
    }

    if (isUserFallbackPath != nullptr)
    {
        *isUserFallbackPath = s_userSharedFilesPathUsesFallback;
    }
}

void SharedMemoryHelpers::BuildSharedFilesPath(
    PathCharString& destination,
    const SharedMemoryId *id,
    bool *isUserFallbackPath)
{
    _ASSERTE(id != nullptr);

    if (id->IsUserScope())
    {
        BuildUserSpecificSharedFilesPath(destination, id, isUserFallbackPath);
    }
    else
    {
        VerifyStringOperation(destination.Set(*gSharedFilesPath));
        if (isUserFallbackPath != nullptr)
        {
            *isUserFallbackPath = false;
        }
    }

    VerifyPathLength(destination);
}

void SharedMemoryHelpers::VerifyPathLength(const PathCharString& path)
{
    if (path.GetCount() >= MAX_LONGPATH)
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::NameTooLong));
    }
}

LPCSTR SharedMemoryHelpers::GetRuntimeTempDirectoryName(const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);

#ifdef __APPLE__
    if (id->IsUserScope() && gApplicationGroupId != nullptr && gApplicationGroupId[0] != '\0')
    {
        // An Apple application-group container is already user-specific, so both global-scoped and user-scoped mutexes
        // would otherwise use the same .dotnet directory and conflict. Give user-scoped mutexes a parallel directory to
        // preserve the distinction between the scopes.
        return SHARED_MEMORY_USER_RUNTIME_TEMP_DIRECTORY_NAME;
    }
#endif

    return SHARED_MEMORY_RUNTIME_TEMP_DIRECTORY_NAME;
}

void SharedMemoryHelpers::BuildRuntimeTempDirectoryPath(PathCharString& destination, const SharedMemoryId *id)
{
    BuildSharedFilesPath(destination, id);
    LPCSTR runtimeTempDirectoryName = GetRuntimeTempDirectoryName(id);
    VerifyStringOperation(AppendPathComponent(destination, runtimeTempDirectoryName));
    VerifyPathLength(destination);
}

void SharedMemoryHelpers::BuildSharedMemoryDirectoryPath(PathCharString& destination, const SharedMemoryId *id)
{
    BuildRuntimeTempDirectoryPath(destination, id);
    VerifyStringOperation(AppendPathComponent(destination, SHARED_MEMORY_SHARED_MEMORY_DIRECTORY_NAME));
    VerifyPathLength(destination);
}

void SharedMemoryHelpers::BuildSessionDirectoryPath(
    PathCharString& destination,
    const SharedMemoryId *id,
    LPCSTR contentsDirectoryName)
{
    BuildRuntimeTempDirectoryPath(destination, id);
    VerifyStringOperation(AppendPathComponent(destination, contentsDirectoryName));

    PathCharString sessionDirectoryName;
    BuildSessionDirectoryName(sessionDirectoryName, id);
    VerifyStringOperation(AppendPathComponent(destination, sessionDirectoryName));
    VerifyPathLength(destination);
}

void SharedMemoryHelpers::BuildSessionDirectoryName(PathCharString& destination, const SharedMemoryId *id)
{
    if (id->IsSessionScope())
    {
        VerifyStringOperation(
            destination.Set(
                SHARED_MEMORY_SESSION_DIRECTORY_NAME_PREFIX,
                STRING_LENGTH(SHARED_MEMORY_SESSION_DIRECTORY_NAME_PREFIX)) &&
            AppendUInt32String(destination, GetCurrentSessionId()));
    }
    else
    {
        VerifyStringOperation(
            destination.Set(
                SHARED_MEMORY_GLOBAL_DIRECTORY_NAME,
                STRING_LENGTH(SHARED_MEMORY_GLOBAL_DIRECTORY_NAME)));
    }
}

void SharedMemoryHelpers::BuildSharedMemorySessionDirectoryPath(
    PathCharString& destination,
    const SharedMemoryId *id)
{
    BuildSessionDirectoryPath(destination, id, SHARED_MEMORY_SHARED_MEMORY_DIRECTORY_NAME);
}

void SharedMemoryHelpers::BuildSharedMemoryFilePath(PathCharString& destination, const SharedMemoryId *id)
{
    BuildSharedMemorySessionDirectoryPath(destination, id);
    VerifyStringOperation(AppendPathComponent(destination, id->GetName()));
    VerifyPathLength(destination);
}

void SharedMemoryHelpers::BuildLockFilesDirectoryPath(PathCharString& destination, const SharedMemoryId *id)
{
    BuildRuntimeTempDirectoryPath(destination, id);
    VerifyStringOperation(AppendPathComponent(destination, SHARED_MEMORY_LOCK_FILES_DIRECTORY_NAME));
    VerifyPathLength(destination);
}

void SharedMemoryHelpers::BuildLockFilesSessionDirectoryPath(
    PathCharString& destination,
    const SharedMemoryId *id)
{
    BuildSessionDirectoryPath(destination, id, SHARED_MEMORY_LOCK_FILES_DIRECTORY_NAME);
}

void SharedMemoryHelpers::BuildLockFilePath(PathCharString& destination, const SharedMemoryId *id)
{
    BuildLockFilesSessionDirectoryPath(destination, id);
    VerifyStringOperation(AppendPathComponent(destination, id->GetName()));
    VerifyPathLength(destination);
}

void SharedMemoryHelpers::BuildUniqueTempDirectoryPath(PathCharString& destination, const SharedMemoryId *id)
{
    BuildSharedFilesPath(destination, id);
    VerifyStringOperation(AppendPathComponent(destination, SHARED_MEMORY_UNIQUE_TEMP_NAME_TEMPLATE));
    VerifyPathLength(destination);
}

void SharedMemoryHelpers::EnsureUserFallbackBasePathExists(const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);
    _ASSERTE(id->IsUserScope());

    PathCharString path;
    BuildUserHomePath(path, id->GetUserScopeUid());
    if (!EnsureDirectoryExists(
            path,
            id,
            false /* isGlobalLockAcquired */,
            false /* createIfNotExist */,
            true /* isSystemDirectory */))
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }

    const char *components[] = { ".dotnet", "corefx", "Runtime", "SharedMemory" };
    for (SIZE_T componentIndex = 0; componentIndex < ARRAY_SIZE(components); componentIndex++)
    {
        LPCSTR component = components[componentIndex];
        VerifyStringOperation(AppendPathComponent(path, component));
        VerifyPathLength(path);

        int transientRetryCount = 0;
        while (true)
        {
            struct stat statInfo;
            int statResult = lstat(path, &statInfo);
            if (statResult != 0 && errno == ENOENT)
            {
                if (mkdir(path, PermissionsMask_OwnerUser_ReadWriteExecute) == 0)
                {
                    int directoryFileDescriptor = -1;
                    try
                    {
                        directoryFileDescriptor = Open(path, O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC);
                    }
                    catch (SharedMemoryException)
                    {
                        rmdir(path);
                        throw;
                    }
                    if (directoryFileDescriptor == -1)
                    {
                        rmdir(path);
                        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                    }

                    int operationResult;
                    do
                    {
                        operationResult = fchmod(directoryFileDescriptor, PermissionsMask_OwnerUser_ReadWriteExecute);
                    } while (operationResult != 0 && errno == EINTR);

                    CloseFile(directoryFileDescriptor);
                    if (operationResult != 0)
                    {
                        rmdir(path);
                        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                    }
                    break;
                }

                if (errno != EEXIST)
                {
                    throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                }
                statResult = lstat(path, &statInfo);
            }

            if (statResult == 0 && S_ISDIR(statInfo.st_mode))
            {
                break;
            }

            if (transientRetryCount >= TransientRetryCount)
            {
                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }

            transientRetryCount++;
            Sleep(1);
        }
    }
}

bool SharedMemoryHelpers::EnsureDirectoryExists(
    const char *path,
    const SharedMemoryId *id,
    bool isGlobalLockAcquired,
    bool createIfNotExist,
    bool isSystemDirectory)
{
    _ASSERTE(path != nullptr);
    _ASSERTE(id != nullptr);
    _ASSERTE(!(isSystemDirectory && createIfNotExist)); // should not create or change permissions on system directories
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!isGlobalLockAcquired || SharedMemoryManager::IsCreationDeletionFileLockAcquired());

    mode_t permissionsMask =
        id->IsUserScope() ? PermissionsMask_OwnerUser_ReadWriteExecute : PermissionsMask_AllUsers_ReadWriteExecute;
    int transientRetryCount = 0;

    while (true)
    {
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
            // permissions umask, so mkdir() may not set all of the requested permissions. We need to use fchmod() to set the
            // proper permissions without following a replacement symlink. When the global lock is not yet available, create a
            // temporary directory with its final permissions and atomically rename it into place.
            if (!isGlobalLockAcquired)
            {
                PathCharString tempPath;
                BuildUniqueTempDirectoryPath(tempPath, id);

                if (mkdtemp(tempPath.OpenStringBuffer()) == nullptr)
                {
                    throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                }

                int directoryFileDescriptor = -1;
                try
                {
                    directoryFileDescriptor = Open(tempPath, O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC);
                }
                catch (SharedMemoryException)
                {
                    rmdir(tempPath);
                    throw;
                }
                if (directoryFileDescriptor == -1)
                {
                    rmdir(tempPath);
                    throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                }

                int operationResult;
                do
                {
                    operationResult = fchmod(directoryFileDescriptor, permissionsMask);
                } while (operationResult != 0 && errno == EINTR);

                CloseFile(directoryFileDescriptor);
                if (operationResult != 0)
                {
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
            else
            {
                if (mkdir(path, permissionsMask) == 0)
                {
                    int directoryFileDescriptor = -1;
                    try
                    {
                        directoryFileDescriptor = Open(path, O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC);
                    }
                    catch (SharedMemoryException)
                    {
                        // Avoid leaving a newly-created directory with permissions filtered by umask.
                        rmdir(path);
                        throw;
                    }
                    if (directoryFileDescriptor == -1)
                    {
                        rmdir(path);
                        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                    }

                    int operationResult;
                    do
                    {
                        operationResult = fchmod(directoryFileDescriptor, permissionsMask);
                    } while (operationResult != 0 && errno == EINTR);

                    CloseFile(directoryFileDescriptor);
                    if (operationResult != 0)
                    {
                        // Remove the newly-created directory to avoid leaving a directory with permissions filtered by umask.
                        rmdir(path);
                        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                    }
                    return true;
                }

                if (errno != EEXIST)
                {
                    throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                }
                statResult = lstat(path, &statInfo);
            }
        }

        if (statResult != 0)
        {
            int errorCode = errno;
            if ((errorCode == ENOENT || errorCode == EACCES) && transientRetryCount < TransientRetryCount)
            {
                transientRetryCount++;
                Sleep(1);
                continue;
            }
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        bool isValid;
        if (isSystemDirectory)
        {
            isValid = S_ISDIR(statInfo.st_mode);
        }
        else if (id->IsUserScope())
        {
            isValid =
                statInfo.st_uid == id->GetUserScopeUid() &&
                (statInfo.st_mode & PermissionsMask_AllUsers_ReadWriteExecute) ==
                    PermissionsMask_OwnerUser_ReadWriteExecute;
        }
        else
        {
            isValid =
                (statInfo.st_mode & PermissionsMask_AllUsers_ReadWriteExecute) ==
                    PermissionsMask_AllUsers_ReadWriteExecute;
        }

        if (isValid)
        {
            return true;
        }

        if (transientRetryCount >= TransientRetryCount)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        transientRetryCount++;
        Sleep(1);
    }
}

int SharedMemoryHelpers::Open(LPCSTR path, int flags, mode_t mode)
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

    switch (openErrorCode)
    {
        case ENOENT:
            if (!(flags & O_CREAT))
            {
                errno = openErrorCode;
                return -1;
            }
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));

        case ENAMETOOLONG:
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::NameTooLong));

        case EMFILE:
        case ENFILE:
        case ENOMEM:
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));

        default:
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }
}

int SharedMemoryHelpers::OpenAt(int directoryFileDescriptor, LPCSTR path, int flags, mode_t mode)
{
    _ASSERTE(directoryFileDescriptor != -1);
    _ASSERTE(path != nullptr);
    _ASSERTE(path[0] != '\0');

    int openErrorCode;

    flags |= O_CLOEXEC;
    do
    {
        int fileDescriptor = openat(directoryFileDescriptor, path, flags, mode);
        if (fileDescriptor != -1)
        {
            return fileDescriptor;
        }
        openErrorCode = errno;
    } while (openErrorCode == EINTR);

    switch (openErrorCode)
    {
        case ENOENT:
            if (!(flags & O_CREAT))
            {
                errno = openErrorCode;
                return -1;
            }
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));

        case ENAMETOOLONG:
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::NameTooLong));

        case EMFILE:
        case ENFILE:
        case ENOMEM:
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));

        default:
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }
}

int SharedMemoryHelpers::OpenSharedFilesSystemDirectory(const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);

    PathCharString sharedFilesPath;
    bool isUserFallbackPath;
    BuildSharedFilesPath(sharedFilesPath, id, &isUserFallbackPath);
    if (isUserFallbackPath)
    {
        EnsureUserFallbackBasePathExists(id);
    }

    int transientRetryCount = 0;
    while (true)
    {
        int directoryFileDescriptor;
        try
        {
            directoryFileDescriptor = Open(
                sharedFilesPath,
                O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC);
        }
        catch (SharedMemoryException)
        {
            int errorCode = errno;
            if ((errorCode == ENOENT || errorCode == EACCES) && transientRetryCount < TransientRetryCount)
            {
                transientRetryCount++;
                Sleep(1);
                continue;
            }
            throw;
        }

        if (directoryFileDescriptor == -1)
        {
            if (transientRetryCount < TransientRetryCount)
            {
                transientRetryCount++;
                Sleep(1);
                continue;
            }
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        return directoryFileDescriptor;
    }
}

int SharedMemoryHelpers::EnsureDirectoryExists(
    int parentDirectoryFileDescriptor,
    LPCSTR directoryName,
    LPCSTR directoryPathForError,
    const SharedMemoryId *id,
    bool createIfNotExist)
{
    _ASSERTE(parentDirectoryFileDescriptor != -1);
    _ASSERTE(directoryName != nullptr);
    _ASSERTE(directoryName[0] != '\0');
    _ASSERTE(directoryPathForError != nullptr);
    _ASSERTE(id != nullptr);
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());

    mode_t permissionsMask =
        id->IsUserScope() ? PermissionsMask_OwnerUser_ReadWriteExecute : PermissionsMask_AllUsers_ReadWriteExecute;
    int transientRetryCount = 0;

    while (true)
    {
        int directoryFileDescriptor;
        try
        {
            directoryFileDescriptor = OpenAt(
                parentDirectoryFileDescriptor,
                directoryName,
                O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC);
        }
        catch (SharedMemoryException)
        {
            int errorCode = errno;
            if ((errorCode == ENOENT || errorCode == EACCES) && transientRetryCount < TransientRetryCount)
            {
                transientRetryCount++;
                Sleep(1);
                continue;
            }
            throw;
        }

        if (directoryFileDescriptor == -1)
        {
            if (!createIfNotExist)
            {
                return -1;
            }

            int mkdirResult;
            do
            {
                mkdirResult = mkdirat(parentDirectoryFileDescriptor, directoryName, permissionsMask);
            } while (mkdirResult != 0 && errno == EINTR);

            if (mkdirResult == 0)
            {
                try
                {
                    directoryFileDescriptor = OpenAt(
                        parentDirectoryFileDescriptor,
                        directoryName,
                        O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC);
                }
                catch (SharedMemoryException)
                {
                    unlinkat(parentDirectoryFileDescriptor, directoryName, AT_REMOVEDIR);
                    throw;
                }

                if (directoryFileDescriptor == -1)
                {
                    unlinkat(parentDirectoryFileDescriptor, directoryName, AT_REMOVEDIR);
                    throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                }

                int operationResult;
                do
                {
                    operationResult = fchmod(directoryFileDescriptor, permissionsMask);
                } while (operationResult != 0 && errno == EINTR);

                if (operationResult != 0)
                {
                    CloseFile(directoryFileDescriptor);
                    unlinkat(parentDirectoryFileDescriptor, directoryName, AT_REMOVEDIR);
                    throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                }

                return directoryFileDescriptor;
            }

            int mkdirErrorCode = errno;
            if (mkdirErrorCode != EEXIST)
            {
                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }

            if (transientRetryCount < TransientRetryCount)
            {
                transientRetryCount++;
                Sleep(1);
                continue;
            }

            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }
        
        return directoryFileDescriptor;
    }
}

int SharedMemoryHelpers::CreateOrOpenFile(
    int directoryFileDescriptor,
    LPCSTR fileName,
    LPCSTR filePathForError,
    const SharedMemoryId *id,
    bool createIfNotExist,
    bool *createdRef)
{
    _ASSERTE(directoryFileDescriptor != -1);
    _ASSERTE(fileName != nullptr);
    _ASSERTE(fileName[0] != '\0');
    _ASSERTE(filePathForError != nullptr);
    _ASSERTE(id != nullptr);
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!createIfNotExist || SharedMemoryManager::IsCreationDeletionFileLockAcquired());

    int transientRetryCount = 0;
    while (true)
    {
        int fileDescriptor;
        try
        {
            fileDescriptor = OpenAt(directoryFileDescriptor, fileName, O_RDWR | O_NOFOLLOW | O_CLOEXEC);
        }
        catch (SharedMemoryException)
        {
            if (errno == EACCES && transientRetryCount < TransientRetryCount)
            {
                transientRetryCount++;
                Sleep(1);
                continue;
            }
            throw;
        }

        if (fileDescriptor != -1)
        {
            struct stat statInfo;
            if (fstat(fileDescriptor, &statInfo) != 0 || !S_ISREG(statInfo.st_mode))
            {
                CloseFile(fileDescriptor);
                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }

            if (id->IsUserScope())
            {
                bool ownerAndPermissionsAreValid =
                    statInfo.st_uid == id->GetUserScopeUid() &&
                    (statInfo.st_mode & PermissionsMask_AllUsers_ReadWriteExecute) ==
                        PermissionsMask_OwnerUser_ReadWrite;
                if (!ownerAndPermissionsAreValid)
                {
                    CloseFile(fileDescriptor);
                    if (transientRetryCount < TransientRetryCount)
                    {
                        transientRetryCount++;
                        Sleep(1);
                        continue;
                    }
                    throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
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

        mode_t permissionsMask =
            id->IsUserScope() ? PermissionsMask_OwnerUser_ReadWrite : PermissionsMask_AllUsers_ReadWrite;
        try
        {
            fileDescriptor = OpenAt(
                directoryFileDescriptor,
                fileName,
                O_RDWR | O_NOFOLLOW | O_CREAT | O_EXCL | O_CLOEXEC,
                permissionsMask);
        }
        catch (SharedMemoryException)
        {
            if (errno == EEXIST && transientRetryCount < TransientRetryCount)
            {
                transientRetryCount++;
                continue;
            }
            throw;
        }
        _ASSERTE(fileDescriptor != -1);

        // The permissions mask passed to open() is filtered by the process' permissions umask. Set the exact mode through the
        // descriptor so a replacement path cannot redirect the permission change.
        int operationResult;
        do
        {
            operationResult = fchmod(fileDescriptor, permissionsMask);
        } while (operationResult != 0 && errno == EINTR);

        if (operationResult != 0)
        {
            CloseFile(fileDescriptor);
            unlinkat(directoryFileDescriptor, fileName, 0);
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        if (createdRef != nullptr)
        {
            *createdRef = true;
        }
        return fileDescriptor;
    }
}

void SharedMemoryHelpers::CloseFile(int fileDescriptor)
{
    _ASSERTE(fileDescriptor != -1);

    int closeResult;
    do
    {
        closeResult = close(fileDescriptor);
    } while (closeResult != 0 && errno == EINTR);
}

SIZE_T SharedMemoryHelpers::GetFileSize(int fileDescriptor)
{
    _ASSERTE(fileDescriptor != -1);

    off_t endOffset = lseek(fileDescriptor, 0, SEEK_END);
    if (endOffset == static_cast<off_t>(-1) ||
        lseek(fileDescriptor, 0, SEEK_SET) == static_cast<off_t>(-1))
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }
    return endOffset;
}

void SharedMemoryHelpers::SetFileSize(int fileDescriptor, SIZE_T byteCount)
{
    _ASSERTE(fileDescriptor != -1);
    _ASSERTE(static_cast<SIZE_T>(byteCount) == byteCount);

    while (true)
    {
        int ftruncateResult = ftruncate(fileDescriptor, static_cast<off_t>(byteCount));
        if (ftruncateResult == 0)
        {
            break;
        }
        if (errno != EINTR)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }
    }
}

void *SharedMemoryHelpers::MemoryMapFile(int fileDescriptor, SIZE_T byteCount)
{
    _ASSERTE(fileDescriptor != -1);
    _ASSERTE(byteCount > sizeof(SharedMemorySharedDataHeader));
    _ASSERTE(AlignDown(byteCount, GetVirtualPageSize()) == byteCount);

    void *sharedMemoryBuffer = mmap(nullptr, byteCount, PROT_READ | PROT_WRITE, MAP_SHARED, fileDescriptor, 0);
    if (sharedMemoryBuffer != MAP_FAILED)
    {
        return sharedMemoryBuffer;
    }
    switch (errno)
    {
        case ENFILE:
        case ENOMEM:
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));

        default:
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }
}

bool SharedMemoryHelpers::TryAcquireFileLock(int fileDescriptor, int operation)
{
    // A file lock is acquired once per file descriptor, so the caller will need to synchronize threads of this process

    _ASSERTE(fileDescriptor != -1);
    _ASSERTE(!(operation & LOCK_UN));

    while (true)
    {
        if (flock(fileDescriptor, operation) == 0)
        {
            return true;
        }

        int flockError = errno;
        switch (flockError)
        {
            case EWOULDBLOCK:
                return false;

            case EINTR:
                continue;

            default:
                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
        }
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
    : m_name(nullptr),
    m_nameCharCount(0),
    m_isSessionScope(false),
    m_isUserScope(false),
    m_userScopeUid(static_cast<uid_t>(0))
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
    m_userScopeUid = isUserScope ? geteuid() : static_cast<uid_t>(0);
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
    LPCSTR name,
    bool isUserScope,
    SharedMemorySharedDataHeader requiredSharedDataHeader,
    SIZE_T sharedDataByteCount,
    bool createIfNotExist,
    bool *createdRef,
    int *runtimeDirectoryFileDescriptorRef)
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
    if (runtimeDirectoryFileDescriptorRef != nullptr)
    {
        *runtimeDirectoryFileDescriptorRef = -1;
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
                _ASSERTE(m_sharedMemoryDirectoryFileDescriptor != -1);
                _ASSERTE(m_sessionDirectoryName != nullptr);
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

    int runtimeDirectoryFileDescriptor;
    int sharedMemoryDirectoryFileDescriptor;
    SharedMemoryManager::AcquireCreationDeletionFileLock(
        &id,
        &runtimeDirectoryFileDescriptor,
        &sharedMemoryDirectoryFileDescriptor);
    autoCleanup.m_acquiredCreationDeletionFileLockForId = &id;

    // Create the session directory
    SharedMemoryHelpers::BuildSessionDirectoryName(sessionDirectoryName, &id);
    SharedMemoryHelpers::BuildSharedMemorySessionDirectoryPath(filePath, &id);
    int sessionDirectoryFileDescriptor = SharedMemoryHelpers::EnsureDirectoryExists(
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
    autoCleanup.m_sharedMemoryDirectoryFileDescriptor = sharedMemoryDirectoryFileDescriptor;
    autoCleanup.m_sessionDirectoryFileDescriptor = sessionDirectoryFileDescriptor;
    autoCleanup.m_sessionDirectoryName = sessionDirectoryName;

    // Create or open the shared memory file
    SharedMemoryHelpers::BuildSharedMemoryFilePath(filePath, &id);

    bool createdFile;
    int fileDescriptor = SharedMemoryHelpers::CreateOrOpenFile(
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
        if (SharedMemoryHelpers::TryAcquireFileLock(fileDescriptor, LOCK_EX | LOCK_NB))
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
        SharedMemoryHelpers::SetFileSize(fileDescriptor, sharedDataTotalByteCount);
    }
    else
    {
        SIZE_T currentFileSize = SharedMemoryHelpers::GetFileSize(fileDescriptor);
        if (currentFileSize < sharedDataUsedByteCount)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::HeaderMismatch));
        }
        if (currentFileSize < sharedDataTotalByteCount)
        {
            SharedMemoryHelpers::SetFileSize(fileDescriptor, sharedDataTotalByteCount);
        }
    }

    // Acquire and hold a shared file lock on the shared memory file as long as it is open, to indicate that this process is
    // using the file. An exclusive file lock is attempted above to detect whether the file contents are valid, for the case
    // where a process crashes or is killed after the file is created. Since we already hold the creation/deletion locks, a
    // non-blocking file lock should succeed.
    if (!SharedMemoryHelpers::TryAcquireFileLock(fileDescriptor, LOCK_SH | LOCK_NB))
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }
    autoCleanup.m_acquiredFileLock = true;

    // Map the file into memory, and initialize or validate the header
    void *mappedBuffer = SharedMemoryHelpers::MemoryMapFile(fileDescriptor, sharedDataTotalByteCount);
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

    autoCleanup.m_cancel = true;
    if (runtimeDirectoryFileDescriptorRef != nullptr)
    {
        *runtimeDirectoryFileDescriptorRef = runtimeDirectoryFileDescriptor;
    }
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
    bool isShuttingDown,
    bool cleanUpPalSharedState)
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
    int runtimeDirectoryFileDescriptor = -1;
    int sharedMemoryDirectoryFileDescriptor = -1;
    try
    {
        SharedMemoryManager::AcquireCreationDeletionFileLock(
            GetId(),
            &runtimeDirectoryFileDescriptor,
            &sharedMemoryDirectoryFileDescriptor);
        autoReleaseCreationDeletionFileLock.m_acquiredForId = GetId();

        SharedMemoryHelpers::ReleaseFileLock(m_fileDescriptor);
        if (SharedMemoryHelpers::TryAcquireFileLock(m_fileDescriptor, LOCK_EX | LOCK_NB))
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
        m_data->Close(
            m_refCount != 0 /* isAbruptShutdown */,
            releaseSharedData,
            runtimeDirectoryFileDescriptor);
    }

    if (m_refCount == 0)
    {
        if (m_data != nullptr)
        {
            InternalDelete(m_data);
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
        // Delete the shared memory file, and the session directory if it's not empty
        PathCharString sessionDirectoryName;
        PathCharString sessionDirectoryPath;
        SharedMemoryHelpers::BuildSessionDirectoryName(sessionDirectoryName, &m_id);
        SharedMemoryHelpers::BuildSharedMemorySessionDirectoryPath(sessionDirectoryPath, &m_id);
        int sessionDirectoryFileDescriptor = SharedMemoryHelpers::EnsureDirectoryExists(
            sharedMemoryDirectoryFileDescriptor,
            sessionDirectoryName,
            sessionDirectoryPath,
            &m_id,
            false /* createIfNotExist */);
        if (sessionDirectoryFileDescriptor != -1)
        {
            unlinkat(sessionDirectoryFileDescriptor, m_id.GetName(), 0);
            SharedMemoryHelpers::CloseFile(sessionDirectoryFileDescriptor);
        }
        unlinkat(sharedMemoryDirectoryFileDescriptor, sessionDirectoryName, AT_REMOVEDIR);
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

    InternalDelete(this);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// SharedMemoryManager

CRITICAL_SECTION SharedMemoryManager::s_creationDeletionProcessLock;
int SharedMemoryManager::s_runtimeDirectoryFileDescriptor = -1;
int SharedMemoryManager::s_creationDeletionLockFileDescriptor = -1;

SharedMemoryManager::UserScopeUidAndDirectoryFileDescriptors *SharedMemoryManager::s_userScopeUidToDirectoryFileDescriptors =
    nullptr;
int SharedMemoryManager::s_userScopeUidToDirectoryFileDescriptorsCount = 0;
int SharedMemoryManager::s_userScopeUidToDirectoryFileDescriptorsCapacity = 0;

SharedMemoryProcessDataHeader *SharedMemoryManager::s_processDataHeaderListHead = nullptr;

#ifdef _DEBUG
SIZE_T SharedMemoryManager::s_creationDeletionProcessLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
SIZE_T SharedMemoryManager::s_creationDeletionFileLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
#endif // _DEBUG

bool SharedMemoryManager::StaticInitialize()
{
    InitializeCriticalSection(&s_creationDeletionProcessLock);
    return true;
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

    EnterCriticalSection(&s_creationDeletionProcessLock);
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
    LeaveCriticalSection(&s_creationDeletionProcessLock);
}

void SharedMemoryManager::AcquireCreationDeletionFileLock(
    const SharedMemoryId *id,
    int *runtimeDirectoryFileDescriptorRef,
    int *sharedMemoryDirectoryFileDescriptorRef)
{
    _ASSERTE(id != nullptr);
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!IsCreationDeletionFileLockAcquired());

    UserScopeUidAndDirectoryFileDescriptors *userScopeDirectoryFileDescriptors =
        id->IsUserScope() ? FindUserScopeUidDirectoryFileDescriptors(id->GetUserScopeUid()) : nullptr;
    int runtimeDirectoryFileDescriptor =
        id->IsUserScope()
            ? (userScopeDirectoryFileDescriptors == nullptr
                ? -1
                : userScopeDirectoryFileDescriptors->runtimeDirectoryFileDescriptor)
            : s_runtimeDirectoryFileDescriptor;
    int creationDeletionLockFileDescriptor =
        id->IsUserScope()
            ? (userScopeDirectoryFileDescriptors == nullptr
                ? -1
                : userScopeDirectoryFileDescriptors->sharedMemoryDirectoryFileDescriptor)
            : s_creationDeletionLockFileDescriptor;
    if (creationDeletionLockFileDescriptor == -1)
    {
        int systemDirectoryFileDescriptor = SharedMemoryHelpers::OpenSharedFilesSystemDirectory(id);
        try
        {
            PathCharString runtimeDirectoryPath;
            SharedMemoryHelpers::BuildRuntimeTempDirectoryPath(runtimeDirectoryPath, id);
            runtimeDirectoryFileDescriptor = SharedMemoryHelpers::EnsureDirectoryExists(
                systemDirectoryFileDescriptor,
                SharedMemoryHelpers::GetRuntimeTempDirectoryName(id),
                runtimeDirectoryPath,
                id);

            PathCharString sharedMemoryDirectoryPath;
            SharedMemoryHelpers::BuildSharedMemoryDirectoryPath(sharedMemoryDirectoryPath, id);
            creationDeletionLockFileDescriptor = SharedMemoryHelpers::EnsureDirectoryExists(
                runtimeDirectoryFileDescriptor,
                SHARED_MEMORY_SHARED_MEMORY_DIRECTORY_NAME,
                sharedMemoryDirectoryPath,
                id);
        }
        catch (SharedMemoryException)
        {
            if (runtimeDirectoryFileDescriptor != -1)
            {
                SharedMemoryHelpers::CloseFile(runtimeDirectoryFileDescriptor);
            }
            SharedMemoryHelpers::CloseFile(systemDirectoryFileDescriptor);
            throw;
        }
        SharedMemoryHelpers::CloseFile(systemDirectoryFileDescriptor);

        if (id->IsUserScope())
        {
            try
            {
                AddUserScopeUidDirectoryFileDescriptors(
                    id->GetUserScopeUid(),
                    runtimeDirectoryFileDescriptor,
                    creationDeletionLockFileDescriptor);
            }
            catch (SharedMemoryException)
            {
                SharedMemoryHelpers::CloseFile(runtimeDirectoryFileDescriptor);
                SharedMemoryHelpers::CloseFile(creationDeletionLockFileDescriptor);
                throw;
            }
        }
        else
        {
            s_runtimeDirectoryFileDescriptor = runtimeDirectoryFileDescriptor;
            s_creationDeletionLockFileDescriptor = creationDeletionLockFileDescriptor;
        }
    }

    bool acquiredFileLock = SharedMemoryHelpers::TryAcquireFileLock(creationDeletionLockFileDescriptor, LOCK_EX);
    _ASSERTE(acquiredFileLock);
#ifdef _DEBUG
    s_creationDeletionFileLockOwnerThreadId = THREADSilentGetCurrentThreadId();
#endif // _DEBUG
    if (runtimeDirectoryFileDescriptorRef != nullptr)
    {
        *runtimeDirectoryFileDescriptorRef = runtimeDirectoryFileDescriptor;
    }
    if (sharedMemoryDirectoryFileDescriptorRef != nullptr)
    {
        *sharedMemoryDirectoryFileDescriptorRef = creationDeletionLockFileDescriptor;
    }
}

void SharedMemoryManager::ReleaseCreationDeletionFileLock(const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(IsCreationDeletionFileLockAcquired());

    UserScopeUidAndDirectoryFileDescriptors *userScopeDirectoryFileDescriptors =
        id->IsUserScope() ? FindUserScopeUidDirectoryFileDescriptors(id->GetUserScopeUid()) : nullptr;
    int creationDeletionLockFileDescriptor =
        id->IsUserScope()
            ? (userScopeDirectoryFileDescriptors == nullptr
                ? -1
                : userScopeDirectoryFileDescriptors->sharedMemoryDirectoryFileDescriptor)
            : s_creationDeletionLockFileDescriptor;
    _ASSERTE(creationDeletionLockFileDescriptor != -1);

#ifdef _DEBUG
    s_creationDeletionFileLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
#endif // _DEBUG
    SharedMemoryHelpers::ReleaseFileLock(creationDeletionLockFileDescriptor);
}

void SharedMemoryManager::AddUserScopeUidDirectoryFileDescriptors(
    uid_t userScopeUid,
    int runtimeDirectoryFileDescriptor,
    int sharedMemoryDirectoryFileDescriptor)
{
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(runtimeDirectoryFileDescriptor != -1);
    _ASSERTE(sharedMemoryDirectoryFileDescriptor != -1);
    _ASSERTE(FindUserScopeUidDirectoryFileDescriptors(userScopeUid) == nullptr);

    int count = s_userScopeUidToDirectoryFileDescriptorsCount;
    int capacity = s_userScopeUidToDirectoryFileDescriptorsCapacity;
    if (count >= capacity)
    {
        if (capacity > INT_MAX / 2)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
        }

        int newCapacity = capacity == 0 ? 1 : capacity * 2;
        SIZE_T newByteCount = static_cast<SIZE_T>(newCapacity) * sizeof(UserScopeUidAndDirectoryFileDescriptors);
        if (newCapacity <= capacity ||
            newByteCount / sizeof(UserScopeUidAndDirectoryFileDescriptors) != static_cast<SIZE_T>(newCapacity))
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
        }

        UserScopeUidAndDirectoryFileDescriptors *newArray =
            static_cast<UserScopeUidAndDirectoryFileDescriptors *>(
                InternalRealloc(s_userScopeUidToDirectoryFileDescriptors, newByteCount));
        if (newArray == nullptr)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
        }

        s_userScopeUidToDirectoryFileDescriptors = newArray;
        s_userScopeUidToDirectoryFileDescriptorsCapacity = newCapacity;
    }

    s_userScopeUidToDirectoryFileDescriptors[count].userScopeUid = userScopeUid;
    s_userScopeUidToDirectoryFileDescriptors[count].runtimeDirectoryFileDescriptor = runtimeDirectoryFileDescriptor;
    s_userScopeUidToDirectoryFileDescriptors[count].sharedMemoryDirectoryFileDescriptor =
        sharedMemoryDirectoryFileDescriptor;
    s_userScopeUidToDirectoryFileDescriptorsCount = count + 1;
}

SharedMemoryManager::UserScopeUidAndDirectoryFileDescriptors *
SharedMemoryManager::FindUserScopeUidDirectoryFileDescriptors(uid_t userScopeUid)
{
    _ASSERTE(IsCreationDeletionProcessLockAcquired());

    for (int i = 0; i < s_userScopeUidToDirectoryFileDescriptorsCount; i++)
    {
        UserScopeUidAndDirectoryFileDescriptors *entry = &s_userScopeUidToDirectoryFileDescriptors[i];
        _ASSERTE(entry->runtimeDirectoryFileDescriptor != -1);
        _ASSERTE(entry->sharedMemoryDirectoryFileDescriptor != -1);
        if (entry->userScopeUid == userScopeUid)
        {
            return entry;
        }
    }

    return nullptr;
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
