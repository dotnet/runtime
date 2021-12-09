// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __RWLock_h__
#define __RWLock_h__

class ReaderWriterLock
{
    volatile int32_t  m_RWLock;       // lock used for R/W synchronization
    int32_t           m_spinCount;    // spin count for a reader waiting for a writer to release the lock
    bool            m_fBlockOnGc;   // True if the spinning writers should block when GC is in progress


#if 0
    // used to prevent writers from being starved by readers
    // we currently do not prevent writers from starving readers since writers
    // are supposed to be rare.
    bool            m_WriterWaiting;
#endif

    bool TryAcquireReadLock();
    bool TryAcquireWriteLock();

public:
    class ReadHolder
    {
        ReaderWriterLock * m_pLock;
        bool               m_fLockAcquired;
    public:
        ReadHolder(ReaderWriterLock * pLock, bool fAcquireLock = true);
        ~ReadHolder();
    };

    class WriteHolder
    {
        ReaderWriterLock * m_pLock;
        bool               m_fLockAcquired;
    public:
        WriteHolder(ReaderWriterLock * pLock, bool fAcquireLock = true);
        ~WriteHolder();
    };

    ReaderWriterLock(bool fBlockOnGc = false);

    void AcquireReadLock();
    void ReleaseReadLock();

    bool DangerousTryPulseReadLock();

protected:
    void AcquireWriteLock();
    void ReleaseWriteLock();

    void AcquireReadLockWorker();

};

#endif // __RWLock_h__
