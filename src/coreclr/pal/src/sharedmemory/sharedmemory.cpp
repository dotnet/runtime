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
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

using namespace CorUnix;

#include "pal/sharedmemory.inl"

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

const mode_t SharedMemoryHelpers::PermissionsMask_CurrentUser_ReadWriteExecute = S_IRUSR | S_IWUSR | S_IXUSR;
const mode_t SharedMemoryHelpers::PermissionsMask_AllUsers_ReadWrite =
    S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH;
const mode_t SharedMemoryHelpers::PermissionsMask_AllUsers_ReadWriteExecute =
    PermissionsMask_AllUsers_ReadWrite | (S_IXUSR | S_IXGRP | S_IXOTH);
const UINT32 SharedMemoryHelpers::InvalidProcessId = static_cast<UINT32>(-1);
const SIZE_T SharedMemoryHelpers::InvalidThreadId = static_cast<SIZE_T>(-1);
const UINT64 SharedMemoryHelpers::InvalidSharedThreadId = static_cast<UINT64>(-1);

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

bool SharedMemoryHelpers::EnsureDirectoryExists(
    SharedMemorySystemCallErrors *errors,
    const char *path,
    bool isGlobalLockAcquired,
    bool createIfNotExist,
    bool isSystemDirectory)
{
    _ASSERTE(path != nullptr);
    _ASSERTE(!(isSystemDirectory && createIfNotExist)); // should not create or change permissions on system directories
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!isGlobalLockAcquired || SharedMemoryManager::IsCreationDeletionFileLockAcquired());

    // Check if the path already exists
    struct stat statInfo;
    int statResult = stat(path, &statInfo);
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
            int operationResult = mkdir(path, PermissionsMask_AllUsers_ReadWriteExecute);
            if (operationResult != 0)
            {
                if (errors != nullptr)
                {
                    int errorCode = errno;
                    errors->Append(
                        "mkdir(\"%s\", AllUsers_ReadWriteExecute) == %d; errno == %s;",
                        path,
                        operationResult,
                        GetFriendlyErrorCodeString(errorCode));
                }

                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }

            operationResult = ChangeMode(path, PermissionsMask_AllUsers_ReadWriteExecute);
            if (operationResult != 0)
            {
                if (errors != nullptr)
                {
                    int errorCode = errno;
                    errors->Append(
                        "chmod(\"%s\", AllUsers_ReadWriteExecute) == %d; errno == %s;",
                        path,
                        operationResult,
                        GetFriendlyErrorCodeString(errorCode));
                }

                rmdir(path);
                throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
            }

            return true;
        }

        PathCharString tempPath;
        BuildSharedFilesPath(tempPath, SHARED_MEMORY_UNIQUE_TEMP_NAME_TEMPLATE);

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

        int operationResult = ChangeMode(tempPath, PermissionsMask_AllUsers_ReadWriteExecute);
        if (operationResult != 0)
        {
            if (errors != nullptr)
            {
                int errorCode = errno;
                errors->Append(
                    "chmod(\"%s\", AllUsers_ReadWriteExecute) == %d; errno == %s;",
                    (const char *)tempPath,
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

        // Another process may have beaten us to it. Delete the temp directory and continue to check the requested directory to
        // see if it meets our needs.
        rmdir(tempPath);
        statResult = stat(path, &statInfo);
    }

    // If the path exists, check that it's a directory
    if (statResult != 0 || !(statInfo.st_mode & S_IFDIR))
    {
        if (errors != nullptr)
        {
            if (statResult != 0)
            {
                int errorCode = errno;
                errors->Append(
                    "stat(\"%s\", ...) == %d; errno == %s;",
                    path,
                    statResult,
                    GetFriendlyErrorCodeString(errorCode));
            }
            else
            {
                errors->Append(
                    "stat(\"%s\", &info) == 0; info.st_mode == 0x%x; (info.st_mode & 0x%x) == 0;",
                    path,
                    (int)statInfo.st_mode,
                    (int)S_IFDIR);
            }
        }

        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }

    if (isSystemDirectory)
    {
        // For system directories (such as TEMP_DIRECTORY_PATH), require sufficient permissions only for the
        // current user. For instance, "docker run --mount ..." to mount /tmp to some directory on the host mounts the
        // destination directory with the same permissions as the source directory, which may not include some permissions for
        // other users. In the docker container, other user permissions are typically not relevant and relaxing the permissions
        // requirement allows for that scenario to work without having to work around it by first giving sufficient permissions
        // for all users.
        if ((statInfo.st_mode & PermissionsMask_CurrentUser_ReadWriteExecute) == PermissionsMask_CurrentUser_ReadWriteExecute)
        {
            return true;
        }

        if (errors != nullptr)
        {
            errors->Append(
                "stat(\"%s\", &info) == 0; info.st_mode == 0x%x; (info.st_mode & CurrentUser_ReadWriteExecute) != CurrentUser_ReadWriteExecute;",
                path,
                (int)statInfo.st_mode);
        }

        throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
    }

    // For non-system directories (such as gSharedFilesPath/SHARED_MEMORY_RUNTIME_TEMP_DIRECTORY_NAME),
    // require sufficient permissions for all users and try to update them if requested to create the directory, so that
    // shared memory files may be shared by all processes on the system.
    if ((statInfo.st_mode & PermissionsMask_AllUsers_ReadWriteExecute) == PermissionsMask_AllUsers_ReadWriteExecute)
    {
        return true;
    }
    if (!createIfNotExist || ChangeMode(path, PermissionsMask_AllUsers_ReadWriteExecute) != 0)
    {
        // We were not asked to create the path or we weren't able to set the new permissions.
        // As a last resort, check that at least the current user has full access.
        if ((statInfo.st_mode & PermissionsMask_CurrentUser_ReadWriteExecute) != PermissionsMask_CurrentUser_ReadWriteExecute)
        {
            if (errors != nullptr)
            {
                errors->Append(
                    "stat(\"%s\", &info) == 0; info.st_mode == 0x%x; (info.st_mode & CurrentUser_ReadWriteExecute) != CurrentUser_ReadWriteExecute;",
                    path,
                    (int)statInfo.st_mode);
            }

            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }
    }
    return true;
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

int SharedMemoryHelpers::OpenDirectory(SharedMemorySystemCallErrors *errors, LPCSTR path)
{
    _ASSERTE(path != nullptr);
    _ASSERTE(path[0] != '\0');

    int fileDescriptor = Open(errors, path, O_RDONLY);
    _ASSERTE(fileDescriptor != -1 || errno == ENOENT);
    return fileDescriptor;
}

int SharedMemoryHelpers::CreateOrOpenFile(
    SharedMemorySystemCallErrors *errors,
    LPCSTR path,
    bool createIfNotExist,
    bool *createdRef)
{
    _ASSERTE(path != nullptr);
    _ASSERTE(path[0] != '\0');
    _ASSERTE(SharedMemoryManager::IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!createIfNotExist || SharedMemoryManager::IsCreationDeletionFileLockAcquired());

    // Try to open the file
    int openFlags = O_RDWR;
    int fileDescriptor = Open(errors, path, openFlags);
    if (fileDescriptor != -1)
    {
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
    openFlags |= O_CREAT | O_EXCL;
    fileDescriptor = Open(errors, path, openFlags, PermissionsMask_AllUsers_ReadWrite);
    _ASSERTE(fileDescriptor != -1);

    // The permissions mask passed to open() is filtered by the process' permissions umask, so open() may not set all of
    // the requested permissions. Use chmod() to set the proper permissions.
    int operationResult = ChangeMode(path, PermissionsMask_AllUsers_ReadWrite);
    if (operationResult != 0)
    {
        if (errors != nullptr)
        {
            int errorCode = errno;
            errors->Append(
                "chmod(\"%s\", AllUsers_ReadWrite) == %d; errno == %s;",
                path,
                operationResult,
                GetFriendlyErrorCodeString(errorCode));
        }

        CloseFile(fileDescriptor);
        unlink(path);
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

    int closeResult;
    do
    {
        closeResult = close(fileDescriptor);
    } while (closeResult != 0 && errno == EINTR);
}

int SharedMemoryHelpers::ChangeMode(LPCSTR path, mode_t mode)
{
    _ASSERTE(path != nullptr);
    _ASSERTE(path[0] != '\0');

    int chmodResult;
    do
    {
        chmodResult = chmod(path, mode);
    } while (chmodResult != 0 && errno == EINTR);

    return chmodResult;
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

void SharedMemoryHelpers::BuildSharedFilesPath(PathCharString& destination, const char *suffix, int suffixCharCount)
{
    _ASSERTE((int)strlen(suffix) == suffixCharCount);

    VerifyStringOperation(destination.Set(*gSharedFilesPath));
    VerifyStringOperation(destination.Append(suffix, suffixCharCount));
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

SharedMemoryId::SharedMemoryId() : m_name(nullptr)
{
}

SharedMemoryId::SharedMemoryId(LPCSTR name, SIZE_T nameCharCount, bool isSessionScope)
    : m_name(name), m_nameCharCount(nameCharCount), m_isSessionScope(isSessionScope)
{
    _ASSERTE(name != nullptr);
    _ASSERTE(nameCharCount != 0);
    _ASSERTE(nameCharCount <= SHARED_MEMORY_MAX_FILE_NAME_CHAR_COUNT);
    _ASSERTE(strlen(name) == nameCharCount);
}

SharedMemoryId::SharedMemoryId(LPCSTR name)
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

bool SharedMemoryId::IsSessionScope() const
{
    _ASSERTE(m_name != nullptr);
    return m_isSessionScope;
}

bool SharedMemoryId::Equals(SharedMemoryId *other) const
{
    return
        GetNameCharCount() == other->GetNameCharCount() &&
        IsSessionScope() == other->IsSessionScope() &&
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
    SharedMemoryId id(name);

    struct AutoCleanup
    {
        bool m_acquiredCreationDeletionFileLock;
        PathCharString *m_filePath;
        SIZE_T m_sessionDirectoryPathCharCount;
        bool m_createdFile;
        int m_fileDescriptor;
        bool m_acquiredFileLock;
        void *m_mappedBuffer;
        SIZE_T m_mappedBufferByteCount;
        bool m_cancel;

        AutoCleanup()
            : m_acquiredCreationDeletionFileLock(false),
            m_filePath(nullptr),
            m_sessionDirectoryPathCharCount(0),
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
                _ASSERTE(m_filePath != nullptr);
                unlink(*m_filePath);
            }

            if (m_sessionDirectoryPathCharCount != 0)
            {
                _ASSERTE(*m_filePath != nullptr);
                m_filePath->CloseBuffer(m_sessionDirectoryPathCharCount);
                rmdir(*m_filePath);
            }

            if (m_acquiredCreationDeletionFileLock)
            {
                SharedMemoryManager::ReleaseCreationDeletionFileLock();
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

    SharedMemoryManager::AcquireCreationDeletionFileLock(errors);
    autoCleanup.m_acquiredCreationDeletionFileLock = true;

    // Create the session directory
    SharedMemoryHelpers::VerifyStringOperation(SharedMemoryManager::CopySharedMemoryBasePath(filePath));
    SharedMemoryHelpers::VerifyStringOperation(filePath.Append('/'));
    SharedMemoryHelpers::VerifyStringOperation(id.AppendSessionDirectoryName(filePath));
    if (!SharedMemoryHelpers::EnsureDirectoryExists(errors, filePath, true /* isGlobalLockAcquired */, createIfNotExist))
    {
        _ASSERTE(!createIfNotExist);
        return nullptr;
    }
    autoCleanup.m_filePath = &filePath;
    autoCleanup.m_sessionDirectoryPathCharCount = filePath.GetCount();

    // Create or open the shared memory file
    SharedMemoryHelpers::VerifyStringOperation(filePath.Append('/'));
    SharedMemoryHelpers::VerifyStringOperation(filePath.Append(id.GetName(), id.GetNameCharCount()));

    bool createdFile;
    int fileDescriptor = SharedMemoryHelpers::CreateOrOpenFile(errors, filePath, createIfNotExist, &createdFile);
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
        autoCleanup.m_acquiredCreationDeletionFileLock = false;
        SharedMemoryManager::ReleaseCreationDeletionFileLock();
    }

    processDataHeader = SharedMemoryProcessDataHeader::New(&id, fileDescriptor, sharedDataHeader, sharedDataTotalByteCount);

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
    SharedMemoryId *id,
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
    m_id = SharedMemoryId(nameCopy, id->GetNameCharCount(), id->IsSessionScope());

    SharedMemoryManager::AddProcessDataHeader(this);
}

SharedMemoryProcessDataHeader *SharedMemoryProcessDataHeader::New(
    SharedMemoryId *id,
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
        bool m_acquired;

        AutoReleaseCreationDeletionFileLock() : m_acquired(false)
        {
        }

        ~AutoReleaseCreationDeletionFileLock()
        {
            if (m_acquired)
            {
                SharedMemoryManager::ReleaseCreationDeletionFileLock();
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
        SharedMemoryManager::AcquireCreationDeletionFileLock(nullptr);
        autoReleaseCreationDeletionFileLock.m_acquired = true;

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
        // Delete the shared memory file, and the session directory if it's not empty
        PathCharString path;
        SharedMemoryHelpers::VerifyStringOperation(SharedMemoryManager::CopySharedMemoryBasePath(path));
        SharedMemoryHelpers::VerifyStringOperation(path.Append('/'));
        SharedMemoryHelpers::VerifyStringOperation(m_id.AppendSessionDirectoryName(path));
        SharedMemoryHelpers::VerifyStringOperation(path.Append('/'));

        SIZE_T sessionDirectoryPathCharCount = path.GetCount();
        SharedMemoryHelpers::VerifyStringOperation(path.Append(m_id.GetName(), m_id.GetNameCharCount()));
        unlink(path);
        path.CloseBuffer(sessionDirectoryPathCharCount);
        rmdir(path);
    }
    catch (SharedMemoryException)
    {
        // Ignore the error, just don't release shared data
    }
}

SharedMemoryId *SharedMemoryProcessDataHeader::GetId()
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
int SharedMemoryManager::s_creationDeletionLockFileDescriptor = -1;

SharedMemoryProcessDataHeader *SharedMemoryManager::s_processDataHeaderListHead = nullptr;
PathCharString* SharedMemoryManager::s_runtimeTempDirectoryPath;
PathCharString* SharedMemoryManager::s_sharedMemoryDirectoryPath;

#ifdef _DEBUG
SIZE_T SharedMemoryManager::s_creationDeletionProcessLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
SIZE_T SharedMemoryManager::s_creationDeletionFileLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
#endif // _DEBUG

bool SharedMemoryManager::StaticInitialize()
{
    InitializeCriticalSection(&s_creationDeletionProcessLock);

    s_runtimeTempDirectoryPath = InternalNew<PathCharString>();
    s_sharedMemoryDirectoryPath = InternalNew<PathCharString>();

    if (s_runtimeTempDirectoryPath && s_sharedMemoryDirectoryPath)
    {
        try
        {
            SharedMemoryHelpers::BuildSharedFilesPath(*s_runtimeTempDirectoryPath, SHARED_MEMORY_RUNTIME_TEMP_DIRECTORY_NAME);
            SharedMemoryHelpers::BuildSharedFilesPath(*s_sharedMemoryDirectoryPath, SHARED_MEMORY_SHARED_MEMORY_DIRECTORY_NAME);

            return true;
        }
        catch (SharedMemoryException)
        {
            // Ignore and let function fail
        }
    }
    return false;
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

void SharedMemoryManager::AcquireCreationDeletionFileLock(SharedMemorySystemCallErrors *errors)
{
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(!IsCreationDeletionFileLockAcquired());

    if (s_creationDeletionLockFileDescriptor == -1)
    {
        if (!SharedMemoryHelpers::EnsureDirectoryExists(
                errors,
                *gSharedFilesPath,
                false /* isGlobalLockAcquired */,
                false /* createIfNotExist */,
                true /* isSystemDirectory */))
        {
            _ASSERTE(errno == ENOENT);
            if (errors != nullptr)
            {
                errors->Append("stat(\"%s\", ...) == -1; errno == ENOENT;", (const char *)*gSharedFilesPath);
            }

            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }

        SharedMemoryHelpers::EnsureDirectoryExists(
            errors,
            *s_runtimeTempDirectoryPath,
            false /* isGlobalLockAcquired */);

        SharedMemoryHelpers::EnsureDirectoryExists(
            errors,
            *s_sharedMemoryDirectoryPath,
            false /* isGlobalLockAcquired */);

        s_creationDeletionLockFileDescriptor = SharedMemoryHelpers::OpenDirectory(errors, *s_sharedMemoryDirectoryPath);
        if (s_creationDeletionLockFileDescriptor == -1)
        {
            if (errors != nullptr)
            {
                int errorCode = errno;
                errors->Append(
                    "open(\"%s\", O_RDONLY | O_CLOEXEC, 0) == -1; errno == %s;",
                    (const char *)*s_sharedMemoryDirectoryPath,
                    GetFriendlyErrorCodeString(errorCode));
            }

            throw SharedMemoryException(static_cast<DWORD>(SharedMemoryError::IO));
        }
    }

    bool acquiredFileLock = SharedMemoryHelpers::TryAcquireFileLock(errors, s_creationDeletionLockFileDescriptor, LOCK_EX);
    _ASSERTE(acquiredFileLock);
#ifdef _DEBUG
    s_creationDeletionFileLockOwnerThreadId = THREADSilentGetCurrentThreadId();
#endif // _DEBUG
}

void SharedMemoryManager::ReleaseCreationDeletionFileLock()
{
    _ASSERTE(IsCreationDeletionProcessLockAcquired());
    _ASSERTE(IsCreationDeletionFileLockAcquired());
    _ASSERTE(s_creationDeletionLockFileDescriptor != -1);

#ifdef _DEBUG
    s_creationDeletionFileLockOwnerThreadId = SharedMemoryHelpers::InvalidThreadId;
#endif // _DEBUG
    SharedMemoryHelpers::ReleaseFileLock(s_creationDeletionLockFileDescriptor);
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

SharedMemoryProcessDataHeader *SharedMemoryManager::FindProcessDataHeader(SharedMemoryId *id)
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

bool SharedMemoryManager::CopySharedMemoryBasePath(PathCharString& destination)
{
    return destination.Set(*s_sharedMemoryDirectoryPath) != FALSE;
}
