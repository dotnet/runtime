// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"

bool MethodContextIterator::Initialize(const char* fileName)
{
    m_hFile = CreateFileA(fileName, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING,
                          FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (m_hFile == INVALID_HANDLE_VALUE)
    {
        LogError("Failed to open file '%s'. GetLastError()=%u", fileName, GetLastError());
        return false;
    }

    LARGE_INTEGER DataTemp;
    if (GetFileSizeEx(m_hFile, &DataTemp) == 0)
    {
        LogError("GetFileSizeEx failed. GetLastError()=%u", GetLastError());
        CloseHandle(m_hFile);
        m_hFile = INVALID_HANDLE_VALUE;
        return false;
    }

    m_fileSize = DataTemp.QuadPart;

    if (m_progressReport)
    {
        m_timer->Start();
    }

    return true;
}

bool MethodContextIterator::Destroy()
{
    bool ret = true; // assume success
    if (m_hFile != INVALID_HANDLE_VALUE)
    {
        if (!CloseHandle(m_hFile))
        {
            LogError("CloseHandle failed. GetLastError()=%u", GetLastError());
            ret = false;
        }
        m_hFile = INVALID_HANDLE_VALUE;
    }
    delete m_mc;
    m_mc = nullptr;

    if (m_index < m_indexCount)
    {
        LogWarning("Didn't use all of index count input: %d < %d (i.e., didn't see MC #%d)", m_index, m_indexCount,
                   m_indexes[m_index]);
    }

    delete m_timer;
    m_timer = nullptr;

    return ret;
}

bool MethodContextIterator::MoveNext()
{
    if (m_mc != nullptr)
    {
        delete m_mc;
        m_mc = nullptr;
    }

    while (true)
    {
        // Figure out where the pointer is currently.
        LARGE_INTEGER pos;
        pos.QuadPart = 0;
        if (SetFilePointerEx(m_hFile, pos, &m_pos, FILE_CURRENT) == 0)
        {
            LogError("SetFilePointerEx failed. GetLastError()=%u", GetLastError());
            return false; // any failure causes us to bail out.
        }

        if (m_pos.QuadPart >= m_fileSize)
        {
            return false;
        }

        // Load the current method context.
        m_methodContextNumber++;

        if (m_progressReport)
        {
            if ((m_methodContextNumber % m_progressRate) == 0)
            {
                m_timer->Stop();
                LogVerbose("Loaded %d at %d per second", m_methodContextNumber,
                           (int)((double)m_progressRate / m_timer->GetSeconds()));
                m_timer->Start();
            }
        }

        if (!MethodContext::Initialize(m_methodContextNumber, m_hFile, &m_mc))
            return false;

        // If we have an array of indexes, skip the loaded indexes that have not been specified.

        if (m_indexCount == -1)
        {
            break;
        }
        else if (m_index == m_indexCount)
        {
            return false; // we're beyond the array of indexes
        }
        else if (m_index < m_indexCount)
        {
            if (m_indexes[m_index] == m_methodContextNumber)
            {
                m_index++;
                break;
            }
        }
    }

    return true;
}
