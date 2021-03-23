// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "simpletimer.h"

// Class to implement method context hive reading and iterating.

class MethodContextIterator
{
public:
    MethodContextIterator(bool progressReport = false)
        : m_hFile(INVALID_HANDLE_VALUE)
        , m_fileSize(0)
        , m_methodContextNumber(0)
        , m_mc(nullptr)
        , m_indexCount(-1)
        , m_index(0)
        , m_indexes(nullptr)
        , m_progressReport(progressReport)
        , m_progressRate(1000)
        , m_timer(nullptr)
    {
        if (m_progressReport)
        {
            m_timer = new SimpleTimer();
        }
    }

    MethodContextIterator(const int indexCount, const int* indexes, bool progressReport = false)
        : m_hFile(INVALID_HANDLE_VALUE)
        , m_fileSize(0)
        , m_methodContextNumber(0)
        , m_mc(nullptr)
        , m_indexCount(indexCount)
        , m_index(0)
        , m_indexes(indexes)
        , m_progressReport(progressReport)
        , m_progressRate(1000)
        , m_timer(nullptr)
    {
        if (m_progressReport)
        {
            m_timer = new SimpleTimer();
        }
    }

    ~MethodContextIterator()
    {
        Destroy();
    }

    bool Initialize(const char* fileName);

    bool Destroy();

    bool MoveNext();

    // The iterator class owns the memory returned by Current(); the caller should not delete it.
    MethodContext* Current()
    {
        return m_mc;
    }

    // In this case, we are giving ownership of the MethodContext* to the caller. So, null out m_mc
    // before we return, so we don't attempt to delete it in this class.
    MethodContext* CurrentTakeOwnership()
    {
        MethodContext* ret = m_mc;
        m_mc               = nullptr;
        return ret;
    }

    // Return the file position offset of the current method context.
    __int64 CurrentPos()
    {
        return m_pos.QuadPart;
    }

    int MethodContextNumber()
    {
        return m_methodContextNumber;
    }

private:
    HANDLE         m_hFile;
    int64_t        m_fileSize;
    int            m_methodContextNumber;
    MethodContext* m_mc;
    LARGE_INTEGER  m_pos;

    // If m_indexCount==-1, use all method contexts. Otherwise, m_indexCount is the number of elements in the
    // m_indexes array, which contains a sorted set of method context indexes to return. In this case, m_index
    // is the index of the current element in m_indexes.
    const int  m_indexCount;
    int        m_index;
    const int* m_indexes;

    // Should we log a progress report as we are loading the method contexts?
    // The timer is only used when m_progressReport==true.
    bool         m_progressReport;
    const int    m_progressRate;    // Report progress every `m_progressRate` method contexts.
    SimpleTimer* m_timer;
};
