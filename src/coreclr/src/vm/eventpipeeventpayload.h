// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_EVENTPAYLOAD_H__
#define __EVENTPIPE_EVENTPAYLOAD_H__

#ifdef FEATURE_PERFTRACING
#include "common.h"

struct EventData
{
    UINT64 Ptr;
    unsigned int Size;
    unsigned int Reserved;
};

class EventPipeEventPayload
{
private:
    BYTE *m_pData;
    EventData *m_pEventData;
    unsigned int m_eventDataCount;
    unsigned int m_size;
    bool m_allocatedData;

    // If the data is stored only as an array of EventData objects, create a flat buffer and copy into it
    void Flatten();

public:
    // Build this payload with a flat buffer inside
    EventPipeEventPayload(BYTE *pData, unsigned int length) :
        m_pData(pData),
        m_pEventData(nullptr),
        m_eventDataCount(0),
        m_size(length),
        m_allocatedData(false)
    {
        LIMITED_METHOD_CONTRACT;
    }

    // Build this payload to contain an array of EventData objects
    EventPipeEventPayload(EventData *pEventData, unsigned int eventDataCount);

    // If a buffer was allocated internally, delete it
    ~EventPipeEventPayload();

    // Copy the data (whether flat or array of objects) into a flat buffer at pDst
    // Assumes that pDst points to an appropriatly sized buffer
    void CopyData(BYTE *pDst);

    // Get the flat formatted data in this payload
    // This method will allocate a buffer if it does not already contain flattened data
    // This method will return NULL on OOM if a buffer needed to be allocated
    BYTE *GetFlatData();

    // Return true is the data is stored in a flat buffer
    bool IsFlattened() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pData != NULL;
    }

    // The the size of buffer needed to contain the stored data
    unsigned int GetSize() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_size;
    }

    EventData *GetEventDataArray() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pEventData;
    }
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_EVENTPAYLOAD_H__
