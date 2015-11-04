//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    shmfilelockmgr.hpp

Abstract:
    Shared memory based file lock manager



--*/

#ifndef _PAL_SHMFILELOCKMGR_H_
#define _PAL_SHMFILELOCKMGR_H_

#include "pal/corunix.hpp"
#include "pal/shm.hpp"

namespace CorUnix
{
    #define SHARE_MODE_NOT_INITALIZED 0xFFFFFFFF

    typedef struct
    {
        SHMPTR unix_filename;
        SHMPTR fileLockedRgns;    
        UINT refCount;
        SHMPTR next;
        SHMPTR prev;
        DWORD  share_mode; /* FILE_SHARE_READ, FILE_SHARE_WRITE, 
                              FILE_SHARE_DELETE,  0 ( not shared ) or
                              SHARE_MODE_NOT_INITALIZED */
        int nbReadAccess;  /* used to keep track of the minimal
                              access permissions */
        int nbWriteAccess;
    } SHMFILELOCKS;

    typedef enum
    {
        USER_LOCK_RGN, /* Used only for user locks (LockFile or UnlockFile call) */
        RDWR_LOCK_RGN  /* Used to distinguish between the user locks and the internal 
                          locks made when  reading, writing or truncating file */
    } LOCK_TYPE;

    typedef struct
    {
        DWORD processId;
        PVOID pvControllerInstance;
        UINT64 lockRgnStart; 
        UINT64 nbBytesLocked;
        LOCK_TYPE lockType;

        SHMPTR next;
    } SHMFILELOCKRGNS;
    
    class CSharedMemoryFileLockMgr : public IFileLockManager
    {
    public:
        
        virtual
        PAL_ERROR
        GetLockControllerForFile(
            CPalThread *pThread,                // IN, OPTIONAL
            LPCSTR szFileName,
            DWORD dwAccessRights,
            DWORD dwShareMode,
            IFileLockController **ppLockController  // OUT
            );

        virtual
        PAL_ERROR
        GetFileShareModeForFile(
            LPCSTR szFileName,
            DWORD* pdwShareMode);
    };

    class CSharedMemoryFileLockController : public IFileLockController
    {
        template <class T> friend void InternalDelete(T *p);

    private:
        DWORD m_dwAccessRights;
        SHMPTR m_shmFileLocks;
    protected:
        virtual ~CSharedMemoryFileLockController()
        {
        };
        
    public:

        CSharedMemoryFileLockController(
            DWORD dwAccessRights,
            SHMPTR shmFileLocks
            )
            :
            m_dwAccessRights(dwAccessRights),
            m_shmFileLocks(shmFileLocks)
        {
        };

        virtual
        PAL_ERROR
        GetTransactionLock(
            CPalThread *pThread,                // IN, OPTIONAL
            FileTransactionLockType eLockType,
            DWORD dwOffsetLow,
            DWORD dwOffsetHigh,
            DWORD nNumberOfBytesToLockLow,
            DWORD nNumberOfBytesToLockHigh,
            IFileTransactionLock **ppTransactionLock    // OUT
            );

        virtual
        PAL_ERROR
        CreateFileLock(
            CPalThread *pThread,                // IN, OPTIONAL
            DWORD dwOffsetLow,
            DWORD dwOffsetHigh,
            DWORD nNumberOfBytesToLockLow,
            DWORD nNumberOfBytesToLockHigh,
            FileLockExclusivity eFileLockExclusivity,
            FileLockWaitMode eFileLockWaitMode
            );

        virtual
        PAL_ERROR
        ReleaseFileLock(
            CPalThread *pThread,                // IN, OPTIONAL
            DWORD dwOffsetLow,
            DWORD dwOffsetHigh,
            DWORD nNumberOfBytesToUnlockLow,
            DWORD nNumberOfBytesToUnlockHigh
            );

        virtual
        void
        ReleaseController();
    };

    class CSharedMemoryFileTransactionLock : public IFileTransactionLock
    {
        template <class T> friend void InternalDelete(T *p);
          
    private:

        SHMPTR m_shmFileLocks;
        PVOID m_pvControllerInstance;
        UINT64 m_lockRgnStart;
        UINT64 m_nbBytesToLock;
    protected:
        virtual ~CSharedMemoryFileTransactionLock()
        {
        };
        
    public:

        CSharedMemoryFileTransactionLock(
            SHMPTR shmFileLocks,
            PVOID pvControllerInstance,
            UINT64 lockRgnStart,
            UINT64 nbBytesToLock
            )
            :
            m_shmFileLocks(shmFileLocks),
            m_pvControllerInstance(pvControllerInstance),
            m_lockRgnStart(lockRgnStart),
            m_nbBytesToLock(nbBytesToLock)
        {
        };

        virtual
        void
        ReleaseLock();
    };
}

#endif /* _PAL_SHMFILELOCKMGR_H_ */

