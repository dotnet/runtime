// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _PAL_SHARED_MEMORY_H_
#define _PAL_SHARED_MEMORY_H_

#include "corunix.hpp"

#ifndef static_assert_no_msg
#define static_assert_no_msg( cond ) static_assert( cond, #cond )
#endif // !static_assert_no_msg

#ifndef _countof
#define _countof(a) (sizeof(a) / sizeof(a[0]))
#endif // !_countof

// - Global shared memory files go in:
//     /tmp/.dotnet/shm/global/<fileName>
// - Session-scoped shared memory files go in:
//     /tmp/.dotnet/shm/session<sessionId>/<fileName>
// - Lock files associated with global shared memory files go in:
//     /tmp/.dotnet/lockfiles/global/<fileName>
// - Lock files associated with session-scoped shared memory files go in:
//     /tmp/.dotnet/lockfiles/session<sessionId>/<fileName>

#define SHARED_MEMORY_MAX_FILE_NAME_CHAR_COUNT (_MAX_FNAME - 1)
#define SHARED_MEMORY_MAX_NAME_CHAR_COUNT (_countof("Global\\") - 1 + SHARED_MEMORY_MAX_FILE_NAME_CHAR_COUNT)

#define SHARED_MEMORY_TEMP_DIRECTORY_PATH "/tmp"
#define SHARED_MEMORY_RUNTIME_TEMP_DIRECTORY_PATH "/tmp/.dotnet"

#define SHARED_MEMORY_SHARED_MEMORY_DIRECTORY_PATH "/tmp/.dotnet/shm"
#define SHARED_MEMORY_LOCK_FILES_DIRECTORY_PATH "/tmp/.dotnet/lockfiles"
static_assert_no_msg(_countof(SHARED_MEMORY_LOCK_FILES_DIRECTORY_PATH) >= _countof(SHARED_MEMORY_SHARED_MEMORY_DIRECTORY_PATH));

#define SHARED_MEMORY_GLOBAL_DIRECTORY_NAME "global"
#define SHARED_MEMORY_SESSION_DIRECTORY_NAME_PREFIX "session"
static_assert_no_msg(_countof(SHARED_MEMORY_SESSION_DIRECTORY_NAME_PREFIX) >= _countof(SHARED_MEMORY_GLOBAL_DIRECTORY_NAME));

#define SHARED_MEMORY_UNIQUE_TEMP_NAME_TEMPLATE "/tmp/.coreclr.XXXXXX"

#define SHARED_MEMORY_MAX_SESSION_ID_CHAR_COUNT (10)

#define SHARED_MEMORY_MAX_FILE_PATH_CHAR_COUNT \
    ( \
        _countof(SHARED_MEMORY_LOCK_FILES_DIRECTORY_PATH) - 1 + \
        1 /* path separator */ + \
        _countof(SHARED_MEMORY_SESSION_DIRECTORY_NAME_PREFIX) - 1 + \
        SHARED_MEMORY_MAX_SESSION_ID_CHAR_COUNT + \
        1 /* path separator */ + \
        SHARED_MEMORY_MAX_FILE_NAME_CHAR_COUNT \
    )
static_assert_no_msg(SHARED_MEMORY_MAX_FILE_PATH_CHAR_COUNT + 1 /* null terminator */ <= MAX_LONGPATH);

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

class SharedMemoryHelpers
{
private:
    static const mode_t PermissionsMask_AllUsers_ReadWrite;
    static const mode_t PermissionsMask_AllUsers_ReadWriteExecute;
public:
    static const UINT32 InvalidProcessId;
    static const SIZE_T InvalidThreadId;
    static const UINT64 InvalidSharedThreadId;

public:
    static SIZE_T AlignDown(SIZE_T value, SIZE_T alignment);
    static SIZE_T AlignUp(SIZE_T value, SIZE_T alignment);

    static void *Alloc(SIZE_T byteCount);

    template<SIZE_T DestinationByteCount, SIZE_T SourceByteCount> static SIZE_T CopyString(char (&destination)[DestinationByteCount], SIZE_T destinationStartOffset, const char (&source)[SourceByteCount]);
    template<SIZE_T DestinationByteCount> static SIZE_T CopyString(char (&destination)[DestinationByteCount], SIZE_T destinationStartOffset, LPCSTR source, SIZE_T sourceCharCount);
    template<SIZE_T DestinationByteCount> static SIZE_T AppendUInt32String(char (&destination)[DestinationByteCount], SIZE_T destinationStartOffset, UINT32 value);

    static bool EnsureDirectoryExists(const char *path, bool isGlobalLockAcquired, bool createIfNotExist = true);
private:
    static int Open(LPCSTR path, int flags, mode_t mode = static_cast<mode_t>(0));
public:
    static int OpenDirectory(LPCSTR path);
    static int CreateOrOpenFile(LPCSTR path, bool createIfNotExist = true, bool *createdRef = nullptr);
    static void CloseFile(int fileDescriptor);

    static SIZE_T GetFileSize(int fileDescriptor);
    static void SetFileSize(int fileDescriptor, SIZE_T byteCount);

    static void *MemoryMapFile(int fileDescriptor, SIZE_T byteCount);

    static bool TryAcquireFileLock(int fileDescriptor, int operation);
    static void ReleaseFileLock(int fileDescriptor);
};

class SharedMemoryId
{
private:
    LPCSTR m_name;
    SIZE_T m_nameCharCount;
    bool m_isSessionScope; // false indicates global scope

public:
    SharedMemoryId();
    SharedMemoryId(LPCSTR name, SIZE_T nameCharCount, bool isSessionScope);
    SharedMemoryId(LPCSTR name);

public:
    LPCSTR GetName() const;
    SIZE_T GetNameCharCount() const;
    bool IsSessionScope() const;
    bool Equals(SharedMemoryId *other) const;

public:
    SIZE_T AppendSessionDirectoryName(char (&path)[SHARED_MEMORY_MAX_FILE_PATH_CHAR_COUNT + 1], SIZE_T pathCharCount) const;
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
    static SIZE_T DetermineTotalByteCount(SIZE_T dataByteCount);

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
    virtual void Close(bool isAbruptShutdown, bool releaseSharedData)
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
    static SharedMemoryProcessDataHeader *CreateOrOpen(LPCSTR name, SharedMemorySharedDataHeader requiredSharedDataHeader, SIZE_T sharedDataByteCount, bool createIfNotExist, bool *createdRef);

public:
    static SharedMemoryProcessDataHeader *PalObject_GetProcessDataHeader(CorUnix::IPalObject *object);
    static void PalObject_SetProcessDataHeader(CorUnix::IPalObject *object, SharedMemoryProcessDataHeader *processDataHeader);
    static void PalObject_Close(CorUnix::CPalThread *thread, CorUnix::IPalObject *object, bool isShuttingDown, bool cleanUpPalSharedState);

private:
    SharedMemoryProcessDataHeader(SharedMemoryId *id, int fileDescriptor, SharedMemorySharedDataHeader *sharedDataHeader, SIZE_T sharedDataTotalByteCount);
public:
    static SharedMemoryProcessDataHeader *New(SharedMemoryId *id, int fileDescriptor, SharedMemorySharedDataHeader *sharedDataHeader, SIZE_T sharedDataTotalByteCount);
    ~SharedMemoryProcessDataHeader();
    void Close();

public:
    SharedMemoryId *GetId();
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
    static int s_creationDeletionLockFileDescriptor;

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
    static void AcquireCreationDeletionFileLock();
    static void ReleaseCreationDeletionFileLock();

#ifdef _DEBUG
public:
    static bool IsCreationDeletionProcessLockAcquired();
    static bool IsCreationDeletionFileLockAcquired();
#endif // _DEBUG

public:
    static void AddProcessDataHeader(SharedMemoryProcessDataHeader *processDataHeader);
    static void RemoveProcessDataHeader(SharedMemoryProcessDataHeader *processDataHeader);
    static SharedMemoryProcessDataHeader *FindProcessDataHeader(SharedMemoryId *id);
};

#endif // !_PAL_SHARED_MEMORY_H_
