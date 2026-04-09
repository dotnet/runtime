// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#ifndef __GCENV_SYNC_H__
#define __GCENV_SYNC_H__

// -----------------------------------------------------------------------------------------------------------
//
// Helper classes expected by the GC
//
#define CRST_REENTRANCY         0
#define CRST_UNSAFE_SAMELEVEL   0
#define CRST_UNSAFE_ANYMODE     0
#define CRST_DEBUGGER_THREAD    0
#define CRST_DEFAULT            0

#define CrstHandleTable         0

typedef int CrstFlags;
typedef int CrstType;

class CrstStatic
{
    CLRCriticalSection m_cs;
#ifdef _DEBUG
    EEThreadId m_holderThreadId;
#endif

public:
    bool InitNoThrow(CrstType eType, CrstFlags eFlags = CRST_DEFAULT)
    {
        return m_cs.Initialize();
    }

    void Destroy()
    {
        m_cs.Destroy();
    }

    void Enter()
    {
        m_cs.Enter();
#ifdef _DEBUG
        m_holderThreadId.SetToCurrentThread();
#endif
    }

    void Leave()
    {
#ifdef _DEBUG
        m_holderThreadId.Clear();
#endif
        m_cs.Leave();
    }

#ifdef _DEBUG
    EEThreadId GetHolderThreadId()
    {
        return m_holderThreadId;
    }

    bool OwnedByCurrentThread()
    {
        return GetHolderThreadId().IsCurrentThread();
    }
#endif
};

class CrstHolder
{
    CrstStatic * m_pLock;

public:
    CrstHolder(CrstStatic * pLock)
        : m_pLock(pLock)
    {
        m_pLock->Enter();
    }

    ~CrstHolder()
    {
        m_pLock->Leave();
    }
};

class CrstHolderWithState
{
    CrstStatic * m_pLock;
    bool m_fAcquired;

public:
    CrstHolderWithState(CrstStatic * pLock, bool fAcquire = true)
        : m_pLock(pLock), m_fAcquired(fAcquire)
    {
        if (fAcquire)
            m_pLock->Enter();
    }

    ~CrstHolderWithState()
    {
        if (m_fAcquired)
            m_pLock->Leave();
    }

    void Acquire()
    {
        if (!m_fAcquired)
        {
            m_pLock->Enter();
            m_fAcquired = true;
        }
    }

    void Release()
    {
        if (m_fAcquired)
        {
            m_pLock->Leave();
            m_fAcquired = false;
        }
    }

    CrstStatic * GetValue()
    {
        return m_pLock;
    }
};

class CLREventStatic
{
public:
    bool CreateAutoEventNoThrow(bool bInitialState);
    bool CreateManualEventNoThrow(bool bInitialState);
    bool CreateOSAutoEventNoThrow(bool bInitialState);
    bool CreateOSManualEventNoThrow(bool bInitialState);

    void CloseEvent();
    bool IsValid() const;
    bool Set();
    bool Reset();
    uint32_t Wait(uint32_t dwMilliseconds, bool bAlertable);

private:
    HANDLE  m_hEvent;
    bool    m_fInitialized;
};

#endif // __GCENV_SYNC_H__
