// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipeeventpayload.h"

#ifdef FEATURE_PERFTRACING

EventPipeEventPayload::EventPipeEventPayload(EventData *pEventData, unsigned int eventDataCount)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pData = NULL;
    m_pEventData = pEventData;
    m_eventDataCount = eventDataCount;
    m_allocatedData = false;

    S_UINT32 tmp_size = S_UINT32(0);
    for (unsigned int i = 0; i < m_eventDataCount; i++)
    {
        tmp_size += S_UINT32(m_pEventData[i].Size);
    }

    if (tmp_size.IsOverflow())
    {
        // If there is an overflow, drop the data and create an empty payload
        m_pEventData = NULL;
        m_eventDataCount = 0;
        m_size = 0;
    }
    else
    {
        m_size = tmp_size.Value();
    }
}

EventPipeEventPayload::~EventPipeEventPayload()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_allocatedData && m_pData != NULL)
    {
        delete[] m_pData;
        m_pData = NULL;
    }
}

void EventPipeEventPayload::Flatten()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_size > 0)
    {
        if (!IsFlattened())
        {
            BYTE *tmp_pData = new (nothrow) BYTE[m_size];
            if (tmp_pData != NULL)
            {
                m_allocatedData = true;
                CopyData(tmp_pData);
                m_pData = tmp_pData;
            }
        }
    }
}

void EventPipeEventPayload::CopyData(BYTE *pDst)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_size > 0)
    {
        if (IsFlattened())
        {
            memcpy(pDst, m_pData, m_size);
        }

        else if (m_pEventData != NULL)
        {
            unsigned int offset = 0;
            for (unsigned int i = 0; i < m_eventDataCount; i++)
            {
                memcpy(pDst + offset, (BYTE *)m_pEventData[i].Ptr, m_pEventData[i].Size);
                offset += m_pEventData[i].Size;
            }
        }
    }
}

BYTE *EventPipeEventPayload::GetFlatData()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!IsFlattened())
    {
        Flatten();
    }
    return m_pData;
}

#endif // FEATURE_PERFTRACING
