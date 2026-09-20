// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _PAL_SHARED_MEMORY_H_
#define _PAL_SHARED_MEMORY_H_

#include "corunix.hpp"
#include <minipal/utils.h>

#ifndef static_assert_no_msg
#define static_assert_no_msg( cond ) static_assert( cond, #cond )
#endif // !static_assert_no_msg

// User-unscoped files use gSharedFilesPath as their system base. On Apple, user-scoped files use the configured
// application-group container when present, otherwise a non-empty TMPDIR. On Linux, they use a non-empty XDG_RUNTIME_DIR.
// All other cases use ~/.dotnet/corefx/Runtime/SharedMemory.
// In an Apple application-group container, user-scoped files use .dotnet-uid to keep them separate from user-unscoped
// files in .dotnet. In all other cases, both scopes use .dotnet under their respective system bases.
// - Shared memory files go in:
//     <system base>/{.dotnet|.dotnet-uid}/shm/{global|session<sessionId>}/<fileName>
// - Lock files go in:
//     <system base>/{.dotnet|.dotnet-uid}/lockfiles/{global|session<sessionId>}/<fileName>

#define SHARED_MEMORY_MAX_FILE_NAME_CHAR_COUNT (_MAX_FNAME - 1)
#define SHARED_MEMORY_MAX_NAME_CHAR_COUNT (STRING_LENGTH("Global\\") + SHARED_MEMORY_MAX_FILE_NAME_CHAR_COUNT)

#define SHARED_MEMORY_RUNTIME_TEMP_DIRECTORY_NAME ".dotnet"
#define SHARED_MEMORY_USER_RUNTIME_TEMP_DIRECTORY_NAME ".dotnet-uid"
#define SHARED_MEMORY_SHARED_MEMORY_DIRECTORY_NAME "shm"
#define SHARED_MEMORY_LOCK_FILES_DIRECTORY_NAME "lockfiles"
static_assert_no_msg(STRING_LENGTH(SHARED_MEMORY_LOCK_FILES_DIRECTORY_NAME) >= STRING_LENGTH(SHARED_MEMORY_SHARED_MEMORY_DIRECTORY_NAME));

#define SHARED_MEMORY_GLOBAL_DIRECTORY_NAME "global"
#define SHARED_MEMORY_SESSION_DIRECTORY_NAME_PREFIX "session"

#define SHARED_MEMORY_PERSISTED_USER_DIRECTORY_NAME_0 ".dotnet"
#define SHARED_MEMORY_PERSISTED_USER_DIRECTORY_NAME_1 "corefx"
#define SHARED_MEMORY_PERSISTED_USER_DIRECTORY_NAME_2 "Runtime"
#define SHARED_MEMORY_PERSISTED_USER_DIRECTORY_NAME_3 "SharedMemory"
#define SHARED_MEMORY_PERSISTED_USER_DIRECTORY_SUFFIX \
    SHARED_MEMORY_PERSISTED_USER_DIRECTORY_NAME_0 "/" \
    SHARED_MEMORY_PERSISTED_USER_DIRECTORY_NAME_1 "/" \
    SHARED_MEMORY_PERSISTED_USER_DIRECTORY_NAME_2 "/" \
    SHARED_MEMORY_PERSISTED_USER_DIRECTORY_NAME_3

#define SHARED_MEMORY_MAX_SESSION_ID_CHAR_COUNT (10)

// Note that this Max size does not include the prefix folder path size which is unknown (in the case of sandbox) until runtime
#define SHARED_MEMORY_MAX_FILE_PATH_CHAR_COUNT \
    ( \
        STRING_LENGTH(SHARED_MEMORY_USER_RUNTIME_TEMP_DIRECTORY_NAME) + \
        1 /* path separator */ + \
        STRING_LENGTH(SHARED_MEMORY_LOCK_FILES_DIRECTORY_NAME) + \
        1 /* path separator */ + \
        STRING_LENGTH(SHARED_MEMORY_SESSION_DIRECTORY_NAME_PREFIX) + \
        SHARED_MEMORY_MAX_SESSION_ID_CHAR_COUNT + \
        1 /* path separator */ + \
        SHARED_MEMORY_MAX_FILE_NAME_CHAR_COUNT \
    )

class AutoFreeBuffer
{
private:
    void *m_buffer;
    bool m_cancel;

public:
    AutoFreeBuffer(void *buffer);
    ~AutoFreeBuffer();

public:
    void Cancel();
};

enum class SharedMemoryError : DWORD
{
    NameEmpty = ERROR_INVALID_PARAMETER,
    NameTooLong = ERROR_FILENAME_EXCED_RANGE,
    NameInvalid = ERROR_INVALID_NAME,
    HeaderMismatch = ERROR_INVALID_HANDLE,
    OutOfMemory = ERROR_NOT_ENOUGH_MEMORY,
    IO = ERROR_OPEN_FAILED
};

class SharedMemoryException
{
private:
    DWORD m_errorCode;

public:
    SharedMemoryException(DWORD errorCode);
    DWORD GetErrorCode() const;
};

class SharedMemorySystemCallErrors
{
private:
    char *m_buffer;
    int m_bufferSize;
    int m_length;
    bool m_isTracking;

public:
    SharedMemorySystemCallErrors(char *buffer, int bufferSize);
    void Append(LPCSTR format, ...);
};

class SharedMemoryId;

class SharedMemoryHelpers
{
    friend class SharedMemoryManager;

private:
    static const mode_t PermissionsMask_OwnerUser_ReadWrite;
    static const mode_t PermissionsMask_OwnerUser_ReadWriteExecute;
    static const mode_t PermissionsMask_NonOwnerUsers_Write;
    static const mode_t PermissionsMask_AllUsers_ReadWrite;
    static const mode_t PermissionsMask_AllUsers_ReadWriteExecute;
    static const mode_t PermissionsMask_Sticky;
    static const int TransientRetryCount;
public:
    static const UINT32 InvalidProcessId;
    static const SIZE_T InvalidThreadId;
    static const UINT64 InvalidSharedThreadId;

public:
    static SIZE_T AlignDown(SIZE_T value, SIZE_T alignment);
    static SIZE_T AlignUp(SIZE_T value, SIZE_T alignment);

    static void *Alloc(SIZE_T byteCount);

    static void BuildSharedFilesSystemPath(PathCharString& destination, const SharedMemoryId *id);
    static void BuildRuntimeRootPath(PathCharString& destination, const SharedMemoryId *id);
    static void BuildSharedMemoryDirectoryPath(PathCharString& destination, const SharedMemoryId *id);
    static void BuildLockFilesDirectoryPath(PathCharString& destination, const SharedMemoryId *id);
    static void BuildSessionDirectoryPath(PathCharString& destination, const SharedMemoryId *id, bool isLockFile);
    static void BuildPath(PathCharString& destination, const SharedMemoryId *id, bool isLockFile);
    static bool AppendUInt32String(PathCharString& destination, UINT32 value);

    static int OpenSharedFilesSystemDirectory(SharedMemorySystemCallErrors *errors, const SharedMemoryId *id);
    static int EnsureDirectoryExists(
        SharedMemorySystemCallErrors *errors,
        int parentDirectoryFileDescriptor,
        LPCSTR directoryName,
        LPCSTR directoryPathForError,
        const SharedMemoryId *id,
        bool createIfNotExist = true);
private:
    static bool AppendPathComponent(PathCharString& path, LPCSTR component);
    static void BuildUserSpecificSharedFilesPath(
        PathCharString& destination,
        uid_t uid,
        SIZE_T *persistedUserHomeDirectoryPathCharCountRef,
        bool *isApplicationContainerRef);
    static void EnsurePersistedUserDirectoryExists(
        SharedMemorySystemCallErrors *errors,
        LPCSTR path,
        SIZE_T homeDirectoryPathCharCount,
        const SharedMemoryId *id);
    static int Open(SharedMemorySystemCallErrors *errors, LPCSTR path, int flags, mode_t mode = static_cast<mode_t>(0));
    static int OpenAt(
        SharedMemorySystemCallErrors *errors,
        int directoryFileDescriptor,
        LPCSTR path,
        int flags,
        mode_t mode = static_cast<mode_t>(0));
public:
    static int OpenDirectory(SharedMemorySystemCallErrors *errors, LPCSTR path);
    static int CreateOrOpenFile(
        SharedMemorySystemCallErrors *errors,
        int directoryFileDescriptor,
        LPCSTR fileName,
        LPCSTR filePathForError,
        const SharedMemoryId *id,
        bool createIfNotExist = true,
        bool *createdRef = nullptr);
    static void CloseFile(int fileDescriptor);

    static SIZE_T GetFileSize(SharedMemorySystemCallErrors *errors, LPCSTR filePath, int fileDescriptor);
    static void SetFileSize(SharedMemorySystemCallErrors *errors, LPCSTR filePath, int fileDescriptor, SIZE_T byteCount);

    static void *MemoryMapFile(SharedMemorySystemCallErrors *errors, LPCSTR filePath, int fileDescriptor, SIZE_T byteCount);

    static bool TryAcquireFileLock(SharedMemorySystemCallErrors *errors, int fileDescriptor, int operation);
    static void ReleaseFileLock(int fileDescriptor);

    static void VerifyStringOperation(bool success);
    static void VerifyStringOperation(BOOL success)
    {
        VerifyStringOperation(success != FALSE);
    }
};

class SharedMemoryId
{
private:
    LPCSTR m_name;
    SIZE_T m_nameCharCount;
    bool m_isSessionScope; // false indicates global scope
    bool m_isUserScope;
    uid_t m_userScopeUid;

public:
    SharedMemoryId();
    SharedMemoryId(LPCSTR name, bool isUserScope);

public:
    LPCSTR GetName() const;
    SIZE_T GetNameCharCount() const;
    void ReplaceNamePtr(LPCSTR name);
    bool IsSessionScope() const;
    bool IsUserScope() const;
    uid_t GetUserScopeUid() const;
    bool Equals(const SharedMemoryId *other) const;

public:
    bool AppendSessionDirectoryName(PathCharString& path) const;
};

enum class SharedMemoryType : UINT8
{
    Mutex
};

class SharedMemorySharedDataHeader
{
private:
    union
    {
        struct
        {
            SharedMemoryType m_type;
            UINT8 m_version;
        };
        UINT64 _raw; // use the same size for the header on all archs, and align the data to a pointer
    };

public:
    static SIZE_T GetUsedByteCount(SIZE_T dataByteCount);
    static SIZE_T GetTotalByteCount(SIZE_T dataByteCount);

public:
    SharedMemorySharedDataHeader(SharedMemoryType type, UINT8 version);

public:
    SharedMemoryType GetType() const;
    UINT8 GetVersion() const;
    void *GetData();
};

class SharedMemoryProcessDataBase
{
public:
    virtual bool CanClose() const = 0;
    virtual bool HasImplicitRef() const = 0;
    virtual void SetHasImplicitRef(bool value) = 0;
    virtual void Close(bool isAbruptShutdown, bool releaseSharedData) = 0;

    virtual ~SharedMemoryProcessDataBase()
    {
    }
};

class SharedMemoryProcessDataHeader
{
private:
    SIZE_T m_refCount;
    SharedMemoryId m_id;
    SharedMemoryProcessDataBase *m_data;
    int m_fileDescriptor;
    SharedMemorySharedDataHeader *m_sharedDataHeader;
    SIZE_T m_sharedDataTotalByteCount;
    SharedMemoryProcessDataHeader *m_nextInProcessDataHeaderList;

public:
    static SharedMemoryProcessDataHeader *CreateOrOpen(SharedMemorySystemCallErrors *errors, LPCSTR name, bool isUserScope, SharedMemorySharedDataHeader requiredSharedDataHeader, SIZE_T sharedDataByteCount, bool createIfNotExist, bool *createdRef);

public:
    static SharedMemoryProcessDataHeader *PalObject_GetProcessDataHeader(CorUnix::IPalObject *object);
    static void PalObject_SetProcessDataHeader(CorUnix::IPalObject *object, SharedMemoryProcessDataHeader *processDataHeader);
    static void PalObject_Close(CorUnix::CPalThread *thread, CorUnix::IPalObject *object, bool isShuttingDown, bool cleanUpPalSharedState);

private:
    SharedMemoryProcessDataHeader(const SharedMemoryId *id, int fileDescriptor, SharedMemorySharedDataHeader *sharedDataHeader, SIZE_T sharedDataTotalByteCount);
public:
    static SharedMemoryProcessDataHeader *New(const SharedMemoryId *id, int fileDescriptor, SharedMemorySharedDataHeader *sharedDataHeader, SIZE_T sharedDataTotalByteCount);
    ~SharedMemoryProcessDataHeader();
    void Close();

public:
    const SharedMemoryId *GetId() const;
    SharedMemoryProcessDataBase *GetData() const;
    void SetData(SharedMemoryProcessDataBase *data);
    SharedMemorySharedDataHeader *GetSharedDataHeader() const;
    SIZE_T GetSharedDataTotalByteCount() const;
    SharedMemoryProcessDataHeader *GetNextInProcessDataHeaderList() const;
    void SetNextInProcessDataHeaderList(SharedMemoryProcessDataHeader *next);

public:
    void IncRefCount();
    void DecRefCount();
};

class SharedMemoryManager
{
private:
    static CRITICAL_SECTION s_creationDeletionProcessLock;
    static int s_runtimeDirectoryFileDescriptor;
    static int s_creationDeletionLockFileDescriptor;

    struct UserScopeUidData
    {
        uid_t userScopeUid;
        LPCSTR sharedFilesPath;
        SIZE_T persistedUserHomeDirectoryPathCharCount;
        bool isApplicationContainer;
        int runtimeDirectoryFileDescriptor;
        int sharedMemoryDirectoryFileDescriptor;

        UserScopeUidData()
            : userScopeUid((uid_t)0),
            sharedFilesPath(nullptr),
            persistedUserHomeDirectoryPathCharCount(0),
            isApplicationContainer(false),
            runtimeDirectoryFileDescriptor(-1),
            sharedMemoryDirectoryFileDescriptor(-1)
        {
        }

        UserScopeUidData(
            uid_t userScopeUid,
            LPCSTR sharedFilesPath,
            SIZE_T persistedUserHomeDirectoryPathCharCount,
            bool isApplicationContainer)
            : userScopeUid(userScopeUid),
            sharedFilesPath(sharedFilesPath),
            persistedUserHomeDirectoryPathCharCount(persistedUserHomeDirectoryPathCharCount),
            isApplicationContainer(isApplicationContainer),
            runtimeDirectoryFileDescriptor(-1),
            sharedMemoryDirectoryFileDescriptor(-1)
        {
        }
    };

    static UserScopeUidData *s_userScopeUidData;
    static int s_userScopeUidDataCount;
    static int s_userScopeUidDataCapacity;

private:
    static SharedMemoryProcessDataHeader *s_processDataHeaderListHead;

#ifdef _DEBUG
private:
    static SIZE_T s_creationDeletionProcessLockOwnerThreadId;
    static SIZE_T s_creationDeletionFileLockOwnerThreadId;
#endif // _DEBUG

public:
    static void StaticInitialize();
    static void StaticClose();

public:
    static void AcquireCreationDeletionProcessLock();
    static void ReleaseCreationDeletionProcessLock();
    static void AcquireCreationDeletionFileLock(SharedMemorySystemCallErrors *errors, const SharedMemoryId *id);
    static void ReleaseCreationDeletionFileLock(const SharedMemoryId *id);
    static int GetRuntimeDirectoryFileDescriptor(const SharedMemoryId *id);
    static int GetSharedMemoryDirectoryFileDescriptor(const SharedMemoryId *id);
    static LPCSTR GetUserScopeSharedFilesPath(uid_t userScopeUid);
    static SIZE_T GetUserScopePersistedHomeDirectoryPathCharCount(uid_t userScopeUid);
    static LPCSTR GetRuntimeTempDirectoryName(const SharedMemoryId *id);
private:
    static UserScopeUidData *GetOrCreateUserScopeUidData(uid_t userScopeUid);
    static UserScopeUidData *FindUserScopeUidData(uid_t userScopeUid);
    static void SetUserScopeUidDirectoryFileDescriptors(
        uid_t userScopeUid,
        int runtimeDirectoryFileDescriptor,
        int sharedMemoryDirectoryFileDescriptor);

#ifdef _DEBUG
public:
    static bool IsCreationDeletionProcessLockAcquired();
    static bool IsCreationDeletionFileLockAcquired();
#endif // _DEBUG

public:
    static void AddProcessDataHeader(SharedMemoryProcessDataHeader *processDataHeader);
    static void RemoveProcessDataHeader(SharedMemoryProcessDataHeader *processDataHeader);
    static SharedMemoryProcessDataHeader *FindProcessDataHeader(const SharedMemoryId *id);
};

#endif // !_PAL_SHARED_MEMORY_H_
