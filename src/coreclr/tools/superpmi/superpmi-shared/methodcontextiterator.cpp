// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"

bool MethodContextIterator::Initialize(const char* fileName)
{
    m_hFile = fopen(fileName, "rb");
    if (m_hFile == nullptr)
    {
        LogError("Failed to open file '%s'. errno=%d", fileName, errno);
        return false;
    }

    m_fileSize = fgetsize(m_hFile);
    if (m_fileSize < 0)
    {
        LogError("fgetsize failed. errno=%d", errno);
        fclose(m_hFile);
        m_hFile = nullptr;
        return false;
    }

    if (m_progressReport)
    {
        m_timer->Start();
    }

    return true;
}

bool MethodContextIterator::Destroy()
{
    bool ret = true; // assume success
    if (m_hFile != nullptr)
    {
        if (fclose(m_hFile) != 0)
        {
            LogError("fclose failed. errno=%d", errno);
            ret = false;
        }
        m_hFile = nullptr;
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
        m_pos = ftell_64(m_hFile);
        if (m_pos < 0)
        {
            LogError("ftell_64 failed. errno=%d", errno);
            return false; // any failure causes us to bail out.
        }

        if (m_pos >= m_fileSize)
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

        if (!MethodContext::Initialize(m_methodContextNumber, m_hFile, /* readCompileResults */ true, &m_mc))
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
