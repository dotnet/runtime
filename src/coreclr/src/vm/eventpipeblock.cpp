// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipeblock.h"
#include "eventpipeeventinstance.h"
#include "fastserializableobject.h"
#include "fastserializer.h"

#ifdef FEATURE_PERFTRACING

EventPipeBlock::EventPipeBlock(unsigned int maxBlockSize) :
    FastSerializableObject(1, 0)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pBlock = new (nothrow) BYTE[maxBlockSize];
    if (m_pBlock == NULL)
    {
        return;
    }

    memset(m_pBlock, 0, maxBlockSize);
    m_pWritePointer = m_pBlock;
    m_pEndOfTheBuffer = m_pBlock + maxBlockSize;
}

EventPipeBlock::~EventPipeBlock()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    delete[] m_pBlock;
}

bool EventPipeBlock::WriteEvent(EventPipeEventInstance &instance)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pBlock == NULL)
    {
        return false;
    }

    unsigned int totalSize = instance.GetAlignedTotalSize();
    if (m_pWritePointer + totalSize >= m_pEndOfTheBuffer)
    {
        return false;
    }

    BYTE* alignedEnd = m_pWritePointer + totalSize + sizeof(totalSize);

    memcpy(m_pWritePointer, &totalSize, sizeof(totalSize));
    m_pWritePointer += sizeof(totalSize);

    unsigned int metadataId = instance.GetMetadataId();
    memcpy(m_pWritePointer, &metadataId, sizeof(metadataId));
    m_pWritePointer += sizeof(metadataId);

    DWORD threadId = instance.GetThreadId();
    memcpy(m_pWritePointer, &threadId, sizeof(threadId));
    m_pWritePointer += sizeof(threadId);

    const LARGE_INTEGER* timeStamp = instance.GetTimeStamp();
    memcpy(m_pWritePointer, timeStamp, sizeof(*timeStamp));
    m_pWritePointer += sizeof(*timeStamp);

    const GUID* activityId = instance.GetActivityId();
    memcpy(m_pWritePointer, activityId, sizeof(*activityId));
    m_pWritePointer += sizeof(*activityId);

    const GUID* relatedActivityId = instance.GetRelatedActivityId();
    memcpy(m_pWritePointer, relatedActivityId, sizeof(*relatedActivityId));
    m_pWritePointer += sizeof(*relatedActivityId);

    unsigned int dataLength = instance.GetDataLength();
    memcpy(m_pWritePointer, &dataLength, sizeof(dataLength));
    m_pWritePointer += sizeof(dataLength);

    if (dataLength > 0)
    {
        memcpy(m_pWritePointer, instance.GetData(), dataLength);
        m_pWritePointer += dataLength;
    }

    unsigned int stackSize = instance.GetStackSize();
    memcpy(m_pWritePointer, &stackSize, sizeof(stackSize));
    m_pWritePointer += sizeof(stackSize);

    if (stackSize > 0)
    {
        memcpy(m_pWritePointer, instance.GetStack(), stackSize);
        m_pWritePointer += stackSize;
    }

    while (m_pWritePointer < alignedEnd)
    {
        *m_pWritePointer++ = (BYTE)0; // put padding at the end to get 4 bytes alignment of the payload
    }

    return true;
}

void EventPipeBlock::Clear()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pBlock == NULL)
    {
        return;
    }

    memset(m_pBlock, 0, GetSize());
    m_pWritePointer = m_pBlock;
}

#endif // FEATURE_PERFTRACING
