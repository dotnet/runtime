// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(SHMEM); // some headers have code with asserts, so do this first

#include "pal/sharedmemory.h"

#include "pal/file.hpp"
#include "pal/malloc.hpp"
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

const mode_t SharedMemoryHelpers::PermissionsMask_OwnerUser_ReadWrite = S_IRUSR | S_IWUSR;
const mode_t SharedMemoryHelpers::PermissionsMask_OwnerUser_ReadWriteExecute = S_IRUSR | S_IWUSR | S_IXUSR;
const mode_t SharedMemoryHelpers::PermissionsMask_NonOwnerUsers_Write = S_IWGRP | S_IWOTH;
const mode_t SharedMemoryHelpers::PermissionsMask_AllUsers_ReadWrite =
    S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH;
const mode_t SharedMemoryHelpers::PermissionsMask_AllUsers_ReadWriteExecute =
    PermissionsMask_AllUsers_ReadWrite | (S_IXUSR | S_IXGRP | S_IXOTH);
const mode_t SharedMemoryHelpers::PermissionsMask_Sticky = S_ISVTX;
const int SharedMemoryHelpers::TransientRetryCount = 10;
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

int SharedMemoryHelpers::OpenSharedFilesSystemDirectory(
    SharedMemorySystemCallErrors *errors,
    const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());

    PathCharString path;
    BuildSharedFilesSystemPath(path, id);
    int transientRetryCount = 0;

    while (true)
    {
        int directoryFileDescriptor;
        do
        {
            directoryFileDescriptor = InternalOpen(path, O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC, 0);
        } while (directoryFileDescriptor == -1 && errno == EINTR);

        if (directoryFileDescriptor != -1)
        {
            struct stat statInfo;
            int statResult;
            do
            {
                statResult = fstat(directoryFileDescriptor, &statInfo);
            } while (statResult != 0 && errno == EINTR);

            if (statResult == 0)
            {
                // For system directories (such as TEMP_DIRECTORY_PATH), require sufficient permissions only for the
                // owner user. For instance, "docker run --mount ..." to mount /tmp to some directory on the host mounts the
                // destination directory with the same permissions as the source directory, which may not include some
                // permissions for other users. In the docker container, other user permissions are typically not relevant and
                // relaxing the permissions requirement allows for that scenario to work without having to work around it by
                // first giving sufficient permissions for all users.
                //
                // If the directory is being used for user-scoped shared memory data, also ensure that either it has the sticky
                // bit or it's owned by the current user and without write access for other users.
                mode_t ownerPermissions = statInfo.st_mode & PermissionsMask_OwnerUser_ReadWriteExecute;
                bool safeForUserScope =
                    !id->IsUserScope() ||
                    (statInfo.st_mode & PermissionsMask_Sticky) != 0 ||
                    (statInfo.st_uid == id->GetUserScopeUid() &&
                        (statInfo.st_mode & PermissionsMask_NonOwnerUsers_Write) == 0);
                if (ownerPermissions == PermissionsMask_OwnerUser_ReadWriteExecute && safeForUserScope)
                {
                    return directoryFileDescriptor;
                }
            }

            int errorCode = errno;
            CloseFile(directoryFileDescriptor);
            if (statResult != 0 && (errorCode == ENOENT || errorCode == EACCES) &&
                transientRetryCount++ < TransientRetryCount)
            {
                usleep(1000);
                continue;
            }

            if (statResult == 0 && transientRetryCount++ < TransientRetryCount)
            {
                usleep(1000);
                continue;
            }

            if (errors != nullptr)
            {
                if (statResult != 0)
                {
                    errors->Append(
                        "fstat(directoryFileDescriptor for \"%s\", ...) == %d; errno == %s;",
                        (const char *)path,
                        statResult,
                        GetFriendlyErrorCodeString(errorCode));
                }
                else
                {
                    errors->Append(
                        "fstat(directoryFileDescriptor for \"%s\", &info) == 0; info.st_mode == 0x%x; info.st_uid == %u;",
                        (const char *)path,
                        (int)statInfo.st_mode,
                        (unsigned int)statInfo.st_uid);
                }
            }
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        int errorCode = errno;
        if ((errorCode == ENOENT || errorCode == EACCES) && transientRetryCount++ < TransientRetryCount)
        {
            usleep(1000);
            continue;
        }

        if (errors != nullptr)
        {
            errors->Append(
                "open(\"%s\", O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC, 0) == -1; errno == %s;",
                (const char *)path,
                GetFriendlyErrorCodeString(errorCode));
        }
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }
}

int SharedMemoryHelpers::EnsureDirectoryExists(
    SharedMemorySystemCallErrors *errors,
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
        do
        {
            directoryFileDescriptor =
                openat(parentDirectoryFileDescriptor, directoryName, O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC);
        } while (directoryFileDescriptor == -1 && errno == EINTR);

        bool created = false;
        if (directoryFileDescriptor == -1)
        {
            int openErrorCode = errno;
            if (openErrorCode == ENOENT)
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
                    created = true;
                    do
                    {
                        directoryFileDescriptor =
                            openat(parentDirectoryFileDescriptor, directoryName, O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC);
                    } while (directoryFileDescriptor == -1 && errno == EINTR);

                    if (directoryFileDescriptor == -1)
                    {
                        int errorCode = errno;
                        unlinkat(parentDirectoryFileDescriptor, directoryName, AT_REMOVEDIR);
                        if (errors != nullptr)
                        {
                            errors->Append(
                                "openat(%d, \"%s\", O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC) == -1; errno == %s;",
                                parentDirectoryFileDescriptor,
                                directoryName,
                                GetFriendlyErrorCodeString(errorCode));
                        }
                        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                    }

                    int chmodResult;
                    do
                    {
                        chmodResult = fchmod(directoryFileDescriptor, permissionsMask);
                    } while (chmodResult != 0 && errno == EINTR);

                    if (chmodResult != 0)
                    {
                        int errorCode = errno;
                        CloseFile(directoryFileDescriptor);
                        unlinkat(parentDirectoryFileDescriptor, directoryName, AT_REMOVEDIR);
                        if (errors != nullptr)
                        {
                            errors->Append(
                                "fchmod(directoryFileDescriptor for \"%s\", 0x%x) == %d; errno == %s;",
                                directoryPathForError,
                                (int)permissionsMask,
                                chmodResult,
                                GetFriendlyErrorCodeString(errorCode));
                        }
                        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                    }
                }
                else
                {
                    int mkdirErrorCode = errno;
                    if (mkdirErrorCode == EEXIST && transientRetryCount++ < TransientRetryCount)
                    {
                        usleep(1000);
                        continue;
                    }

                    if (errors != nullptr)
                    {
                        errors->Append(
                            "mkdirat(%d, \"%s\", 0x%x) == -1; errno == %s;",
                            parentDirectoryFileDescriptor,
                            directoryName,
                            (int)permissionsMask,
                            GetFriendlyErrorCodeString(mkdirErrorCode));
                    }
                    throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                }
            }
            else
            {
                if ((openErrorCode == ENOENT || openErrorCode == EACCES) &&
                    transientRetryCount++ < TransientRetryCount)
                {
                    usleep(1000);
                    continue;
                }

                if (errors != nullptr)
                {
                    errors->Append(
                        "openat(%d, \"%s\", O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC) == -1; errno == %s;",
                        parentDirectoryFileDescriptor,
                        directoryName,
                        GetFriendlyErrorCodeString(openErrorCode));
                }
                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }
        }

        struct stat statInfo;
        int statResult;
        do
        {
            statResult = fstat(directoryFileDescriptor, &statInfo);
        } while (statResult != 0 && errno == EINTR);

        if (statResult == 0 &&
            (!id->IsUserScope() || statInfo.st_uid == id->GetUserScopeUid()) &&
            (statInfo.st_mode & PermissionsMask_AllUsers_ReadWriteExecute) == permissionsMask)
        {
            return directoryFileDescriptor;
        }

        int errorCode = errno;
        CloseFile(directoryFileDescriptor);
        if (!created && transientRetryCount++ < TransientRetryCount)
        {
            usleep(1000);
            continue;
        }

        if (created)
        {
            unlinkat(parentDirectoryFileDescriptor, directoryName, AT_REMOVEDIR);
        }

        if (errors != nullptr)
        {
            if (statResult != 0)
            {
                errors->Append(
                    "fstat(directoryFileDescriptor for \"%s\", ...) == %d; errno == %s;",
                    directoryPathForError,
                    statResult,
                    GetFriendlyErrorCodeString(errorCode));
            }
            else
            {
                errors->Append(
                    "fstat(directoryFileDescriptor for \"%s\", &info) == 0; info.st_mode == 0x%x; info.st_uid == %u; "
                    "expected mode == 0x%x, uid == %u;",
                    directoryPathForError,
                    (int)statInfo.st_mode,
                    (unsigned int)statInfo.st_uid,
                    (int)permissionsMask,
                    (unsigned int)(id->IsUserScope() ? id->GetUserScopeUid() : statInfo.st_uid));
            }
        }
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }
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
    LPCSTR path,
    int flags,
    mode_t mode)
{
    _ASSERTE(directoryFileDescriptor != -1);
    _ASSERTE(path != nullptr);

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

int SharedMemoryHelpers::CreateOrOpenFile(
    SharedMemorySystemCallErrors *errors,
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
    _ASSERTE(filePathForError[0] != '\0');
    _ASSERTE(id != nullptr);
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!createIfNotExist || SharedMemoryManager::IsCreationDeletionFileLockAcquired());

    int transientRetryCount = 0;
    while (true)
    {
        int openFlags = O_RDWR | O_NOFOLLOW | O_CLOEXEC;
        int fileDescriptor = -1;
        try
        {
            fileDescriptor = OpenAt(errors, directoryFileDescriptor, fileName, openFlags);
        }
        catch (SharedMemoryException)
        {
            if (errno == EACCES && transientRetryCount++ < TransientRetryCount)
            {
                usleep(1000);
                continue;
            }
            throw;
        }

        if (fileDescriptor != -1)
        {
            if (id->IsUserScope())
            {
                struct stat statInfo;
                int statResult = fstat(fileDescriptor, &statInfo);
                int errorCode = errno;
                if (statResult != 0)
                {
                    if (errors != nullptr)
                    {
                        errors->Append(
                            "fstat(\"%s\", ...) == %d; errno == %s;",
                            filePathForError,
                            statResult,
                            GetFriendlyErrorCodeString(errorCode));
                    }
                    CloseFile(fileDescriptor);
                    throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
                }

                if (statInfo.st_uid != id->GetUserScopeUid() ||
                    (statInfo.st_mode & PermissionsMask_AllUsers_ReadWriteExecute) != PermissionsMask_OwnerUser_ReadWrite)
                {
                    CloseFile(fileDescriptor);
                    if (transientRetryCount++ < TransientRetryCount)
                    {
                        usleep(1000);
                        continue;
                    }

                    if (errors != nullptr)
                    {
                        errors->Append(
                            "fstat(\"%s\", &info) == 0; info.st_mode == 0x%x; info.st_uid == %u; expected mode == 0x%x, uid == %u;",
                            filePathForError,
                            (int)statInfo.st_mode,
                            (unsigned int)statInfo.st_uid,
                            (int)PermissionsMask_OwnerUser_ReadWrite,
                            (unsigned int)id->GetUserScopeUid());
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

        openFlags |= O_CREAT | O_EXCL | O_CLOEXEC;
        mode_t permissionsMask =
            id->IsUserScope() ? PermissionsMask_OwnerUser_ReadWrite : PermissionsMask_AllUsers_ReadWrite;
        try
        {
            fileDescriptor = OpenAt(errors, directoryFileDescriptor, fileName, openFlags, permissionsMask);
        }
        catch (SharedMemoryException)
        {
            if (errno == EEXIST && transientRetryCount++ < TransientRetryCount)
            {
                continue;
            }
            throw;
        }
        _ASSERTE(fileDescriptor != -1);

        // The creation mode is filtered by umask. Set the exact mode through the descriptor so a replaced path is not followed.
        int operationResult;
        do
        {
            operationResult = fchmod(fileDescriptor, permissionsMask);
        } while (operationResult != 0 && errno == EINTR);

        if (operationResult != 0)
        {
            int errorCode = errno;
            if (errors != nullptr)
            {
                errors->Append(
                    "fchmod(fileDescriptor, 0x%x) == %d; errno == %s;",
                    (int)permissionsMask,
                    operationResult,
                    GetFriendlyErrorCodeString(errorCode));
            }

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

void SharedMemoryHelpers::BuildUserSpecificSharedFilesPath(
    PathCharString& destination,
    uid_t uid,
    SIZE_T *persistedUserHomeDirectoryPathCharCountRef,
    bool *isApplicationContainerRef)
{
    _ASSERTE(persistedUserHomeDirectoryPathCharCountRef != nullptr);
    _ASSERTE(isApplicationContainerRef != nullptr);
    *persistedUserHomeDirectoryPathCharCountRef = 0;
    *isApplicationContainerRef = false;

#ifdef __APPLE__
    const char *sharedMemoryApplicationGroupId = PAL_getenv("DOTNET_SHARED_MEMORY_APPLICATION_GROUP_ID");
    if (sharedMemoryApplicationGroupId != nullptr && sharedMemoryApplicationGroupId[0] != '\0')
    {
        SIZE_T applicationGroupIdLength = strlen(sharedMemoryApplicationGroupId);
        if (applicationGroupIdLength > MAX_APPLICATION_GROUP_ID_LENGTH ||
            !GetApplicationContainerFolder(
                destination,
                sharedMemoryApplicationGroupId,
                static_cast<int>(applicationGroupIdLength)))
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }
        *isApplicationContainerRef = true;
        return;
    }

    if (gApplicationGroupId != nullptr && gApplicationGroupId[0] != '\0')
    {
        VerifyStringOperation(destination.Set(*gSharedFilesPath));
        *isApplicationContainerRef = true;
        return;
    }

    const char *tmpDirectory = PAL_getenv("TMPDIR");
    if (tmpDirectory != nullptr && tmpDirectory[0] != '\0')
    {
        VerifyStringOperation(destination.Set(tmpDirectory, strlen(tmpDirectory)));
        return;
    }
#elif defined(__linux__)
    const char *xdgRuntimeDirectory = PAL_getenv("XDG_RUNTIME_DIR");
    if (xdgRuntimeDirectory != nullptr && xdgRuntimeDirectory[0] != '\0')
    {
        VerifyStringOperation(destination.Set(xdgRuntimeDirectory, strlen(xdgRuntimeDirectory)));
        return;
    }
#endif

    const char *homeDirectory = uid == geteuid() ? PAL_getenv("HOME") : nullptr;
    if (homeDirectory != nullptr && homeDirectory[0] != '\0')
    {
        SIZE_T homeDirectoryLength = strlen(homeDirectory);
        VerifyStringOperation(destination.Set(homeDirectory, homeDirectoryLength));
        while (homeDirectoryLength > 1 && homeDirectory[homeDirectoryLength - 1] == '/')
        {
            destination.CloseBuffer(--homeDirectoryLength);
        }
        *persistedUserHomeDirectoryPathCharCountRef = homeDirectoryLength;
        if (homeDirectory[homeDirectoryLength - 1] != '/')
        {
            VerifyStringOperation(destination.Append('/'));
        }
        VerifyStringOperation(destination.Append(SHARED_MEMORY_PERSISTED_USER_DIRECTORY_SUFFIX));
        return;
    }

    SIZE_T bufferSize = 1024;
    while (true)
    {
        char *buffer = reinterpret_cast<char *>(Alloc(bufferSize));
        AutoFreeBuffer autoFreeBuffer(buffer);
        struct passwd pwd;
        struct passwd *result = nullptr;
        int error = getpwuid_r(uid, &pwd, buffer, bufferSize, &result);
        if (error == 0)
        {
            if (result == nullptr || result->pw_dir == nullptr || result->pw_dir[0] == '\0')
            {
                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }

            SIZE_T homeDirectoryLength = strlen(result->pw_dir);
            VerifyStringOperation(destination.Set(result->pw_dir, homeDirectoryLength));
            while (homeDirectoryLength > 1 && result->pw_dir[homeDirectoryLength - 1] == '/')
            {
                destination.CloseBuffer(--homeDirectoryLength);
            }
            *persistedUserHomeDirectoryPathCharCountRef = homeDirectoryLength;
            if (result->pw_dir[homeDirectoryLength - 1] != '/')
            {
                VerifyStringOperation(destination.Append('/'));
            }
            VerifyStringOperation(destination.Append(SHARED_MEMORY_PERSISTED_USER_DIRECTORY_SUFFIX));
            return;
        }

        if (error != ERANGE || bufferSize > static_cast<SIZE_T>(-1) / 2)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }
        bufferSize *= 2;
    }
}

void SharedMemoryHelpers::EnsurePersistedUserDirectoryExists(
    SharedMemorySystemCallErrors *errors,
    LPCSTR path,
    SIZE_T homeDirectoryPathCharCount,
    const SharedMemoryId *id)
{
    _ASSERTE(path != nullptr);
    _ASSERTE(homeDirectoryPathCharCount != 0);
    _ASSERTE(id != nullptr);
    _ASSERTE(id->IsUserScope());
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!SharedMemoryManager::IsCreationDeletionFileLockAcquired());

    PathCharString homeDirectoryPath;
    VerifyStringOperation(homeDirectoryPath.Set(path, homeDirectoryPathCharCount));

    int homeDirectoryFileDescriptor = OpenDirectory(errors, homeDirectoryPath);
    if (homeDirectoryFileDescriptor == -1)
    {
        if (errors != nullptr)
        {
            errors->Append("The persisted user home directory \"%s\" does not exist;", (const char *)homeDirectoryPath);
        }
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }

    static const char *const directoryNames[] =
    {
        SHARED_MEMORY_PERSISTED_USER_DIRECTORY_NAME_0,
        SHARED_MEMORY_PERSISTED_USER_DIRECTORY_NAME_1,
        SHARED_MEMORY_PERSISTED_USER_DIRECTORY_NAME_2,
        SHARED_MEMORY_PERSISTED_USER_DIRECTORY_NAME_3
    };

    struct AutoCleanup
    {
        int m_directoryFileDescriptors[ARRAY_SIZE(directoryNames) + 1];
        bool m_createdDirectories[ARRAY_SIZE(directoryNames)];
        int m_directoryCount;
        bool m_cancel;

        AutoCleanup(int homeDirectoryFileDescriptor)
            : m_directoryCount(0), m_cancel(false)
        {
            for (SIZE_T i = 0; i < ARRAY_SIZE(m_directoryFileDescriptors); i++)
            {
                m_directoryFileDescriptors[i] = -1;
            }
            for (SIZE_T i = 0; i < ARRAY_SIZE(m_createdDirectories); i++)
            {
                m_createdDirectories[i] = false;
            }
            m_directoryFileDescriptors[0] = homeDirectoryFileDescriptor;
        }

        ~AutoCleanup()
        {
            for (int i = m_directoryCount; i >= 0; i--)
            {
                int fileDescriptor = m_directoryFileDescriptors[i];
                if (fileDescriptor != -1)
                {
                    SharedMemoryHelpers::CloseFile(fileDescriptor);
                }

                if (!m_cancel && i > 0 && m_createdDirectories[i - 1])
                {
                    unlinkat(m_directoryFileDescriptors[i - 1], directoryNames[i - 1], AT_REMOVEDIR);
                }
            }
        }
    } autoCleanup(homeDirectoryFileDescriptor);

    auto validateDirectory = [errors, id](int fileDescriptor, LPCSTR directoryName, bool requireExactMode)
    {
        struct stat statInfo;
        int statResult;
        do
        {
            statResult = fstat(fileDescriptor, &statInfo);
        } while (statResult != 0 && errno == EINTR);

        mode_t permissions =
            statResult == 0
                ? statInfo.st_mode & PermissionsMask_AllUsers_ReadWriteExecute
                : static_cast<mode_t>(0);
        bool valid =
            statResult == 0 &&
            statInfo.st_uid == id->GetUserScopeUid() &&
            (permissions & PermissionsMask_OwnerUser_ReadWriteExecute) ==
                PermissionsMask_OwnerUser_ReadWriteExecute &&
            (permissions & PermissionsMask_NonOwnerUsers_Write) == 0 &&
            (!requireExactMode || permissions == PermissionsMask_OwnerUser_ReadWriteExecute);
        if (valid)
        {
            return;
        }

        if (errors != nullptr)
        {
            if (statResult != 0)
            {
                int errorCode = errno;
                errors->Append(
                    "fstat(directoryFileDescriptor for \"%s\", ...) == %d; errno == %s;",
                    directoryName,
                    statResult,
                    GetFriendlyErrorCodeString(errorCode));
            }
            else
            {
                errors->Append(
                    "fstat(directoryFileDescriptor for \"%s\", &info) == 0; info.st_mode == 0x%x; "
                    "info.st_uid == %u; expected uid == %u and safe owner permissions;",
                    directoryName,
                    (int)statInfo.st_mode,
                    (unsigned int)statInfo.st_uid,
                    (unsigned int)id->GetUserScopeUid());
            }
        }
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    };

    validateDirectory(homeDirectoryFileDescriptor, homeDirectoryPath, false);

    for (SIZE_T directoryIndex = 0; directoryIndex < ARRAY_SIZE(directoryNames); directoryIndex++)
    {
        int parentDirectoryFileDescriptor = autoCleanup.m_directoryFileDescriptors[directoryIndex];
        const char *directoryName = directoryNames[directoryIndex];
        bool created = false;
        int directoryFileDescriptor = -1;
        int transientRetryCount = 0;

        while (true)
        {
            do
            {
                directoryFileDescriptor =
                    openat(parentDirectoryFileDescriptor, directoryName, O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC);
            } while (directoryFileDescriptor == -1 && errno == EINTR);

            if (directoryFileDescriptor != -1)
            {
                break;
            }

            int openErrorCode = errno;
            if (openErrorCode != ENOENT)
            {
                if (created)
                {
                    unlinkat(parentDirectoryFileDescriptor, directoryName, AT_REMOVEDIR);
                }
                if (errors != nullptr)
                {
                    errors->Append(
                        "openat(%d, \"%s\", O_RDONLY | O_NOFOLLOW | O_DIRECTORY | O_CLOEXEC) == -1; errno == %s;",
                        parentDirectoryFileDescriptor,
                        directoryName,
                        GetFriendlyErrorCodeString(openErrorCode));
                }
                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }

            int mkdirResult;
            do
            {
                mkdirResult =
                    mkdirat(parentDirectoryFileDescriptor, directoryName, PermissionsMask_OwnerUser_ReadWriteExecute);
            } while (mkdirResult != 0 && errno == EINTR);

            if (mkdirResult == 0)
            {
                created = true;
                continue;
            }

            int mkdirErrorCode = errno;
            if (mkdirErrorCode == EEXIST && transientRetryCount++ < TransientRetryCount)
            {
                usleep(1000);
                continue;
            }

            if (errors != nullptr)
            {
                errors->Append(
                    "mkdirat(%d, \"%s\", 0x%x) == -1; errno == %s;",
                    parentDirectoryFileDescriptor,
                    directoryName,
                    (int)PermissionsMask_OwnerUser_ReadWriteExecute,
                    GetFriendlyErrorCodeString(mkdirErrorCode));
            }
            if (created)
            {
                unlinkat(parentDirectoryFileDescriptor, directoryName, AT_REMOVEDIR);
            }
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        if (created)
        {
            int chmodResult;
            do
            {
                chmodResult = fchmod(directoryFileDescriptor, PermissionsMask_OwnerUser_ReadWriteExecute);
            } while (chmodResult != 0 && errno == EINTR);

            if (chmodResult != 0)
            {
                int chmodErrorCode = errno;
                CloseFile(directoryFileDescriptor);
                unlinkat(parentDirectoryFileDescriptor, directoryName, AT_REMOVEDIR);
                if (errors != nullptr)
                {
                    errors->Append(
                        "fchmod(directoryFileDescriptor for \"%s\", 0x%x) == %d; errno == %s;",
                        directoryName,
                        (int)PermissionsMask_OwnerUser_ReadWriteExecute,
                        chmodResult,
                        GetFriendlyErrorCodeString(chmodErrorCode));
                }
                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }
        }

        autoCleanup.m_directoryFileDescriptors[directoryIndex + 1] = directoryFileDescriptor;
        autoCleanup.m_createdDirectories[directoryIndex] = created;
        autoCleanup.m_directoryCount++;
        validateDirectory(directoryFileDescriptor, directoryName, created);
    }

    autoCleanup.m_cancel = true;
}

void SharedMemoryHelpers::BuildSharedFilesSystemPath(PathCharString& destination, const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);

    if (id->IsUserScope())
    {
        LPCSTR sharedFilesPath = SharedMemoryManager::GetUserScopeSharedFilesPath(id->GetUserScopeUid());
        VerifyStringOperation(destination.Set(sharedFilesPath, strlen(sharedFilesPath)));
    }
    else
    {
        VerifyStringOperation(destination.Set(*gSharedFilesPath));
    }

    SIZE_T pathCharCount = destination.GetCount();
    _ASSERTE(pathCharCount != 0);
    const char *path = destination;
    while (pathCharCount > 1 && path[pathCharCount - 1] == '/')
    {
        destination.CloseBuffer(--pathCharCount);
        path = destination;
    }
}

void SharedMemoryHelpers::BuildRuntimeRootPath(PathCharString& destination, const SharedMemoryId *id)
{
    BuildSharedFilesSystemPath(destination, id);
    LPCSTR runtimeTempDirectoryName = SharedMemoryManager::GetRuntimeTempDirectoryName(id);
    VerifyStringOperation(AppendPathComponent(destination, runtimeTempDirectoryName));
}

void SharedMemoryHelpers::BuildSharedMemoryDirectoryPath(PathCharString& destination, const SharedMemoryId *id)
{
    BuildRuntimeRootPath(destination, id);
    VerifyStringOperation(AppendPathComponent(destination, SHARED_MEMORY_SHARED_MEMORY_DIRECTORY_NAME));
}

void SharedMemoryHelpers::BuildLockFilesDirectoryPath(PathCharString& destination, const SharedMemoryId *id)
{
    BuildRuntimeRootPath(destination, id);
    VerifyStringOperation(AppendPathComponent(destination, SHARED_MEMORY_LOCK_FILES_DIRECTORY_NAME));
}

void SharedMemoryHelpers::BuildSessionDirectoryPath(
    PathCharString& destination,
    const SharedMemoryId *id,
    bool isLockFile)
{
    if (isLockFile)
    {
        BuildLockFilesDirectoryPath(destination, id);
    }
    else
    {
        BuildSharedMemoryDirectoryPath(destination, id);
    }

    PathCharString sessionDirectoryName;
    VerifyStringOperation(id->AppendSessionDirectoryName(sessionDirectoryName));
    VerifyStringOperation(AppendPathComponent(destination, sessionDirectoryName));
}

void SharedMemoryHelpers::BuildPath(PathCharString& destination, const SharedMemoryId *id, bool isLockFile)
{
    BuildSessionDirectoryPath(destination, id, isLockFile);
    VerifyStringOperation(AppendPathComponent(destination, id->GetName()));
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

bool SharedMemoryId::AppendSessionDirectoryName(PathCharString& path) const
{
    if (IsSessionScope())
    {
        return path.Append(SHARED_MEMORY_SESSION_DIRECTORY_NAME_PREFIX) != FALSE
            && SharedMemoryHelpers::AppendUInt32String(path, GetCurrentSessionId());
    }
    else
    {
        return path.Append(SHARED_MEMORY_GLOBAL_DIRECTORY_NAME) != FALSE;
    }
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
    SharedMemoryId id(name, isUserScope);

    struct AutoCleanup
    {
        const SharedMemoryId *m_acquiredCreationDeletionFileLockForId;
        int m_sharedMemoryDirectoryFileDescriptor;
        PathCharString *m_sessionDirectoryName;
        int m_sessionDirectoryFileDescriptor;
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
            m_sessionDirectoryName(nullptr),
            m_sessionDirectoryFileDescriptor(-1),
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
                unlinkat(
                    m_sharedMemoryDirectoryFileDescriptor,
                    *m_sessionDirectoryName,
                    AT_REMOVEDIR);
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
        SharedMemoryManager::GetSharedMemoryDirectoryFileDescriptor(&id);

    // Create the session directory
    PathCharString sessionDirectoryName;
    SharedMemoryHelpers::VerifyStringOperation(id.AppendSessionDirectoryName(sessionDirectoryName));
    SharedMemoryHelpers::BuildSessionDirectoryPath(filePath, &id, false /* isLockFile */);
    int sessionDirectoryFileDescriptor = SharedMemoryHelpers::EnsureDirectoryExists(
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
    autoCleanup.m_sharedMemoryDirectoryFileDescriptor = sharedMemoryDirectoryFileDescriptor;
    autoCleanup.m_sessionDirectoryName = &sessionDirectoryName;
    autoCleanup.m_sessionDirectoryFileDescriptor = sessionDirectoryFileDescriptor;
    autoCleanup.m_fileName = id.GetName();

    // Create or open the shared memory file
    SharedMemoryHelpers::BuildPath(filePath, &id, false /* isLockFile */);

    bool createdFile;
    int fileDescriptor =
        SharedMemoryHelpers::CreateOrOpenFile(
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
    m_id = *id;
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
        // Delete the shared memory file, and the session directory if it's empty.
        int sharedMemoryDirectoryFileDescriptor =
            SharedMemoryManager::GetSharedMemoryDirectoryFileDescriptor(&m_id);
        PathCharString sessionDirectoryName;
        SharedMemoryHelpers::VerifyStringOperation(m_id.AppendSessionDirectoryName(sessionDirectoryName));
        PathCharString sessionDirectoryPath;
        SharedMemoryHelpers::BuildSessionDirectoryPath(
            sessionDirectoryPath,
            &m_id,
            false /* isLockFile */);
        int sessionDirectoryFileDescriptor =
            SharedMemoryHelpers::EnsureDirectoryExists(
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

SharedMemoryManager::UserScopeUidData *SharedMemoryManager::s_userScopeUidData = nullptr;
int SharedMemoryManager::s_userScopeUidDataCount = 0;
int SharedMemoryManager::s_userScopeUidDataCapacity = 0;

SharedMemoryProcessDataHeader *SharedMemoryManager::s_processDataHeaderListHead = nullptr;

#ifdef _DEBUG
SIZE_T SharedMemoryManager::s_creationDeletionProcessLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
SIZE_T SharedMemoryManager::s_creationDeletionFileLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
#endif // _DEBUG

void SharedMemoryManager::StaticInitialize()
{
    InitializeCriticalSection(&s_creationDeletionProcessLock);
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
    SharedMemorySystemCallErrors *errors,
    const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!IsCreationDeletionFileLockAcquired());

    int creationDeletionLockFileDescriptor = GetSharedMemoryDirectoryFileDescriptor(id);
    if (creationDeletionLockFileDescriptor == -1)
    {
        PathCharString sharedFilesSystemPath;
        SharedMemoryHelpers::BuildSharedFilesSystemPath(sharedFilesSystemPath, id);
        if (id->IsUserScope())
        {
            SIZE_T homeDirectoryPathCharCount =
                GetUserScopePersistedHomeDirectoryPathCharCount(id->GetUserScopeUid());
            if (homeDirectoryPathCharCount != 0)
            {
                SharedMemoryHelpers::EnsurePersistedUserDirectoryExists(
                    errors,
                    sharedFilesSystemPath,
                    homeDirectoryPathCharCount,
                    id);
            }
        }

        int sharedFilesSystemDirectoryFileDescriptor =
            SharedMemoryHelpers::OpenSharedFilesSystemDirectory(errors, id);
        int runtimeDirectoryFileDescriptor = -1;
        try
        {
            PathCharString runtimeDirectoryPath;
            SharedMemoryHelpers::BuildRuntimeRootPath(runtimeDirectoryPath, id);
            runtimeDirectoryFileDescriptor =
                SharedMemoryHelpers::EnsureDirectoryExists(
                    errors,
                    sharedFilesSystemDirectoryFileDescriptor,
                    GetRuntimeTempDirectoryName(id),
                    runtimeDirectoryPath,
                    id);

            PathCharString sharedMemoryDirectoryPath;
            SharedMemoryHelpers::BuildSharedMemoryDirectoryPath(sharedMemoryDirectoryPath, id);
            creationDeletionLockFileDescriptor =
                SharedMemoryHelpers::EnsureDirectoryExists(
                    errors,
                    runtimeDirectoryFileDescriptor,
                    SHARED_MEMORY_SHARED_MEMORY_DIRECTORY_NAME,
                    sharedMemoryDirectoryPath,
                    id);
        }
        catch (SharedMemoryException)
        {
            SharedMemoryHelpers::CloseFile(sharedFilesSystemDirectoryFileDescriptor);
            if (runtimeDirectoryFileDescriptor != -1)
            {
                SharedMemoryHelpers::CloseFile(runtimeDirectoryFileDescriptor);
            }
            throw;
        }
        SharedMemoryHelpers::CloseFile(sharedFilesSystemDirectoryFileDescriptor);

        if (id->IsUserScope())
        {
            try
            {
                SetUserScopeUidDirectoryFileDescriptors(
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

    bool acquiredFileLock =
        SharedMemoryHelpers::TryAcquireFileLock(errors, creationDeletionLockFileDescriptor, LOCK_EX);
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

    int creationDeletionLockFileDescriptor = GetSharedMemoryDirectoryFileDescriptor(id);
    _ASSERTE(creationDeletionLockFileDescriptor != -1);

#ifdef _DEBUG
    s_creationDeletionFileLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
#endif // _DEBUG
    SharedMemoryHelpers::ReleaseFileLock(creationDeletionLockFileDescriptor);
}

SharedMemoryManager::UserScopeUidData *SharedMemoryManager::GetOrCreateUserScopeUidData(uid_t userScopeUid)
{
    _ASSERTE(IsCreationDeletionProcessLockAcquired());

    // Retain the selected base together with this UID's creation/deletion lock so environment changes cannot split them.
    UserScopeUidData *entry = FindUserScopeUidData(userScopeUid);
    if (entry != nullptr)
    {
        return entry;
    }

    PathCharString sharedFilesPath;
    SIZE_T persistedUserHomeDirectoryPathCharCount;
    bool isApplicationContainer;
    SharedMemoryHelpers::BuildUserSpecificSharedFilesPath(
        sharedFilesPath,
        userScopeUid,
        &persistedUserHomeDirectoryPathCharCount,
        &isApplicationContainer);

    SIZE_T pathCharCount = sharedFilesPath.GetCount();
    _ASSERTE(pathCharCount != 0);
    const char *path = sharedFilesPath;
    while (pathCharCount > 1 && path[pathCharCount - 1] == '/')
    {
        sharedFilesPath.CloseBuffer(--pathCharCount);
        path = sharedFilesPath;
    }
    if (pathCharCount == static_cast<SIZE_T>(-1))
    {
        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
    }

    char *sharedFilesPathCopy = reinterpret_cast<char *>(SharedMemoryHelpers::Alloc(pathCharCount + 1));
    AutoFreeBuffer autoFreeSharedFilesPathCopy(sharedFilesPathCopy);
    memcpy_s(sharedFilesPathCopy, pathCharCount + 1, path, pathCharCount + 1);

    int count = s_userScopeUidDataCount;
    int capacity = s_userScopeUidDataCapacity;
    if (count >= capacity)
    {
        int newCapacity = capacity == 0 ? 1 : capacity * 2;
        if (newCapacity <= capacity ||
            static_cast<SIZE_T>(newCapacity) >
                static_cast<SIZE_T>(-1) / sizeof(UserScopeUidData))
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
        }

        UserScopeUidData *newArray = InternalNewArray<UserScopeUidData>(newCapacity);
        if (newArray == nullptr)
        {
            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::OutOfMemory));
        }

        if (count != 0)
        {
            CopyMemory(
                newArray,
                s_userScopeUidData,
                count * sizeof(UserScopeUidData));
            InternalDeleteArray(s_userScopeUidData);
        }

        s_userScopeUidData = newArray;
        s_userScopeUidDataCapacity = newCapacity;
    }

    entry = &s_userScopeUidData[count];
    *entry =
        UserScopeUidData(
            userScopeUid,
            sharedFilesPathCopy,
            persistedUserHomeDirectoryPathCharCount,
            isApplicationContainer);
    s_userScopeUidDataCount = count + 1;
    autoFreeSharedFilesPathCopy.Cancel();
    return entry;
}

SharedMemoryManager::UserScopeUidData *SharedMemoryManager::FindUserScopeUidData(uid_t userScopeUid)
{
    _ASSERTE(IsCreationDeletionProcessLockAcquired());

    for (int i = 0; i < s_userScopeUidDataCount; i++)
    {
        UserScopeUidData *entry = &s_userScopeUidData[i];
        _ASSERTE(entry->sharedFilesPath != nullptr);
        if (entry->userScopeUid == userScopeUid)
        {
            return entry;
        }
    }

    return nullptr;
}

LPCSTR SharedMemoryManager::GetUserScopeSharedFilesPath(uid_t userScopeUid)
{
    return GetOrCreateUserScopeUidData(userScopeUid)->sharedFilesPath;
}

SIZE_T SharedMemoryManager::GetUserScopePersistedHomeDirectoryPathCharCount(uid_t userScopeUid)
{
    return GetOrCreateUserScopeUidData(userScopeUid)->persistedUserHomeDirectoryPathCharCount;
}

LPCSTR SharedMemoryManager::GetRuntimeTempDirectoryName(const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);

    // An Apple application-group container is already user-specific, so user-unscoped and user-scoped mutexes would
    // otherwise use the same .dotnet directory and conflict. Give user-scoped mutexes a parallel directory to preserve
    // the distinction between the scopes.
    return id->IsUserScope() && GetOrCreateUserScopeUidData(id->GetUserScopeUid())->isApplicationContainer
        ? SHARED_MEMORY_USER_RUNTIME_TEMP_DIRECTORY_NAME
        : SHARED_MEMORY_RUNTIME_TEMP_DIRECTORY_NAME;
}

void SharedMemoryManager::SetUserScopeUidDirectoryFileDescriptors(
    uid_t userScopeUid,
    int runtimeDirectoryFileDescriptor,
    int sharedMemoryDirectoryFileDescriptor)
{
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(runtimeDirectoryFileDescriptor != -1);
    _ASSERTE(sharedMemoryDirectoryFileDescriptor != -1);

    UserScopeUidData *entry = GetOrCreateUserScopeUidData(userScopeUid);
    _ASSERTE(entry->runtimeDirectoryFileDescriptor == -1);
    _ASSERTE(entry->sharedMemoryDirectoryFileDescriptor == -1);
    entry->runtimeDirectoryFileDescriptor = runtimeDirectoryFileDescriptor;
    entry->sharedMemoryDirectoryFileDescriptor = sharedMemoryDirectoryFileDescriptor;
}

int SharedMemoryManager::GetRuntimeDirectoryFileDescriptor(const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);
    _ASSERTE(IsCreationDeletionProcessLockAcquired());

    if (!id->IsUserScope())
    {
        return s_runtimeDirectoryFileDescriptor;
    }

    UserScopeUidData *entry = FindUserScopeUidData(id->GetUserScopeUid());
    return entry == nullptr ? -1 : entry->runtimeDirectoryFileDescriptor;
}

int SharedMemoryManager::GetSharedMemoryDirectoryFileDescriptor(const SharedMemoryId *id)
{
    _ASSERTE(id != nullptr);
    _ASSERTE(IsCreationDeletionProcessLockAcquired());

    if (!id->IsUserScope())
    {
        return s_creationDeletionLockFileDescriptor;
    }

    UserScopeUidData *entry = FindUserScopeUidData(id->GetUserScopeUid());
    return entry == nullptr ? -1 : entry->sharedMemoryDirectoryFileDescriptor;
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
