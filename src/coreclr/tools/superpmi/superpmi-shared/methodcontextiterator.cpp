// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include <dn-stdio.h>

bool MethodContextIterator::Initialize(const char* fileName)
{
    if (fopen_s(&m_fp, fileName, "rb") != 0)
    {
        LogError("Failed to open file '%s'. errno=%d", fileName, errno);
        return false;
    }

    m_fileSize = fgetsize(m_fp);
    if (m_fileSize <= 0)
    {
        LogError("Getting file size failed. errno=%d", errno);
        fclose(m_fp);
        m_fp = NULL;
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
    if (m_fp != NULL)
    {
        if (fclose(m_fp) != 0)
        {
            LogError("Closing file failed. errno=%d", errno);
            ret = false;
        }
        m_fp = NULL;
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
        int64_t m_pos = ftell(m_fp);
        if (m_pos <= 0)
        {
            LogError("Getting file position failed. errno=%d", errno);
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

        if (!MethodContext::Initialize(m_methodContextNumber, m_fp, &m_mc))
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
