// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipeblock.h"
#include "eventpipeeventinstance.h"
#include "fastserializableobject.h"
#include "fastserializer.h"

// my attempts to include limits.h were hitting missing headers on Linux
// This might be resolvable with more effort but I chose not to head
// down the rabbit hole when a perfectly decent 60 second fix was available:
#ifndef LLONG_MIN
#define LLONG_MIN 0x8000000000000000
#endif
#ifndef LLONG_MAX 
#define LLONG_MAX 0x7FFFFFFFFFFFFFFF
#endif

#ifdef FEATURE_PERFTRACING



DWORD GetBlockVersion(EventPipeSerializationFormat format)
{
    LIMITED_METHOD_CONTRACT;
    switch (format)
    {
    case EventPipeSerializationFormat::NetPerfV3:
        return 1;
    case EventPipeSerializationFormat::NetTraceV4:
        return 2;
    default:
        _ASSERTE(!"Unrecognized EventPipeSerializationFormat");
        return 0;
    }
}

DWORD GetBlockMinVersion(EventPipeSerializationFormat format)
{
    LIMITED_METHOD_CONTRACT;
    switch (format)
    {
    case EventPipeSerializationFormat::NetPerfV3:
        return 0;
    case EventPipeSerializationFormat::NetTraceV4:
        return 2;
    default:
        _ASSERTE(!"Unrecognized EventPipeSerializationFormat");
        return 0;
    }
}

EventPipeBlock::EventPipeBlock(unsigned int maxBlockSize, EventPipeSerializationFormat format) :
    FastSerializableObject(GetBlockVersion(format), GetBlockMinVersion(format), format >= EventPipeSerializationFormat::NetTraceV4)
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
    m_format = format;
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

    _ASSERTE(m_pWritePointer <= m_pEndOfTheBuffer);

    memset(m_pBlock, 0, GetSize());
    m_pWritePointer = m_pBlock;
}

EventPipeEventBlockBase::EventPipeEventBlockBase(unsigned int maxBlockSize, EventPipeSerializationFormat format, bool fUseHeaderCompression) :
    EventPipeBlock(maxBlockSize, format), m_fUseHeaderCompression(fUseHeaderCompression)
{
    memset(m_compressedHeader, 0, 100);
    Clear();
}

void EventPipeEventBlockBase::Clear()
{
    EventPipeBlock::Clear();
    m_lastHeader.MetadataId = 0;
    m_lastHeader.SequenceNumber = 0;
    m_lastHeader.ThreadId = 0;
    m_lastHeader.CaptureThreadId = 0;
    m_lastHeader.StackId = 0;
    m_lastHeader.TimeStamp.QuadPart = 0;
    m_lastHeader.ActivityId = { 0 };
    m_lastHeader.RelatedActivityId = { 0 };
    m_lastHeader.DataLength = 0;

    m_minTimeStamp.QuadPart = LLONG_MAX;
    m_maxTimeStamp.QuadPart = LLONG_MIN;
}

void WriteVarUInt32(BYTE* & pWritePointer, unsigned int value)
{
    while (value >= 0x80)
    {
        *pWritePointer = (BYTE)(value | 0x80);
        pWritePointer++;
        value >>= 7;
    }
    *pWritePointer = (BYTE)value;
    pWritePointer++;
}

void WriteVarUInt64(BYTE* & pWritePointer, ULONGLONG value)
{
    while (value >= 0x80)
    {
        *pWritePointer = (BYTE)(value | 0x80);
        pWritePointer++;
        value >>= 7;
    }
    *pWritePointer = (BYTE)value;
    pWritePointer++;
}

bool EventPipeEventBlockBase::WriteEvent(EventPipeEventInstance &instance, 
                                         ULONGLONG captureThreadId,
                                         unsigned int sequenceNumber,
                                         DWORD stackId,
                                         BOOL isSortedEvent)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(isSortedEvent || m_format >= EventPipeSerializationFormat::NetTraceV4);
    }
    CONTRACTL_END;

    if (m_pBlock == NULL)
    {
        return false;
    }

    unsigned int dataLength = 0;
    BYTE* alignedEnd = NULL;
    unsigned int captureProcNumber = instance.GetProcNumber();

    if (!m_fUseHeaderCompression)
    {
        unsigned int totalSize = instance.GetAlignedTotalSize(m_format);
        if (m_pWritePointer + totalSize >= m_pEndOfTheBuffer)
        {
            return false;
        }

        alignedEnd = m_pWritePointer + totalSize + sizeof(totalSize);

        memcpy(m_pWritePointer, &totalSize, sizeof(totalSize));
        m_pWritePointer += sizeof(totalSize);

        unsigned int metadataId = instance.GetMetadataId();
        _ASSERTE((metadataId & (1 << 31)) == 0);
        metadataId |= (!isSortedEvent ? 1 << 31 : 0);
        memcpy(m_pWritePointer, &metadataId, sizeof(metadataId));
        m_pWritePointer += sizeof(metadataId);

        if (m_format == EventPipeSerializationFormat::NetPerfV3)
        {
            DWORD threadId = instance.GetThreadId32();
            memcpy(m_pWritePointer, &threadId, sizeof(threadId));
            m_pWritePointer += sizeof(threadId);
        }
        else if (m_format == EventPipeSerializationFormat::NetTraceV4)
        {
            memcpy(m_pWritePointer, &sequenceNumber, sizeof(sequenceNumber));
            m_pWritePointer += sizeof(sequenceNumber);

            ULONGLONG threadId = instance.GetThreadId64();
            memcpy(m_pWritePointer, &threadId, sizeof(threadId));
            m_pWritePointer += sizeof(threadId);

            memcpy(m_pWritePointer, &captureThreadId, sizeof(captureThreadId));
            m_pWritePointer += sizeof(captureThreadId);

            memcpy(m_pWritePointer, &captureProcNumber, sizeof(captureProcNumber));
            m_pWritePointer += sizeof(captureProcNumber);

            memcpy(m_pWritePointer, &stackId, sizeof(stackId));
            m_pWritePointer += sizeof(stackId);
        }

        const LARGE_INTEGER* timeStamp = instance.GetTimeStamp();
        memcpy(m_pWritePointer, timeStamp, sizeof(*timeStamp));
        m_pWritePointer += sizeof(*timeStamp);

        const GUID* activityId = instance.GetActivityId();
        memcpy(m_pWritePointer, activityId, sizeof(*activityId));
        m_pWritePointer += sizeof(*activityId);

        const GUID* relatedActivityId = instance.GetRelatedActivityId();
        memcpy(m_pWritePointer, relatedActivityId, sizeof(*relatedActivityId));
        m_pWritePointer += sizeof(*relatedActivityId);

        dataLength = instance.GetDataLength();
        memcpy(m_pWritePointer, &dataLength, sizeof(dataLength));
        m_pWritePointer += sizeof(dataLength);
    }
    else // using header compression
    {
        BYTE flags = 0;
        BYTE* pWritePointer = m_compressedHeader;

        if (instance.GetMetadataId() != m_lastHeader.MetadataId)
        {
            WriteVarUInt32(pWritePointer, instance.GetMetadataId());
            flags |= 1;
        }
        if (isSortedEvent)
        {
            flags |= (1 << 6);
        }
        
        if (m_lastHeader.SequenceNumber + (instance.GetMetadataId() != 0 ? 1 : 0) != sequenceNumber ||
            m_lastHeader.CaptureThreadId != captureThreadId ||
            m_lastHeader.CaptureProcNumber != captureProcNumber)
        {
            WriteVarUInt32(pWritePointer, sequenceNumber - m_lastHeader.SequenceNumber - 1);
            WriteVarUInt64(pWritePointer, captureThreadId);
            WriteVarUInt32(pWritePointer, captureProcNumber);
            flags |= (1 << 1);
        }

        if (m_lastHeader.ThreadId != instance.GetThreadId64())
        {
            WriteVarUInt64(pWritePointer, instance.GetThreadId64());
            flags |= (1 << 2);
        }

        if (m_lastHeader.StackId != stackId)
        {
            WriteVarUInt32(pWritePointer, stackId);
            flags |= (1 << 3);
        }

        const LARGE_INTEGER* timeStamp = instance.GetTimeStamp();
        WriteVarUInt64(pWritePointer, timeStamp->QuadPart - m_lastHeader.TimeStamp.QuadPart);

        if (memcmp(&m_lastHeader.ActivityId, instance.GetActivityId(), sizeof(GUID)) != 0)
        {
            memcpy(pWritePointer, instance.GetActivityId(), sizeof(GUID));
            pWritePointer += sizeof(GUID);
            flags |= (1 << 4);
        }

        if (memcmp(&m_lastHeader.RelatedActivityId, instance.GetRelatedActivityId(), sizeof(GUID)) != 0)
        {
            memcpy(pWritePointer, instance.GetRelatedActivityId(), sizeof(GUID));
            pWritePointer += sizeof(GUID);
            flags |= (1 << 5);
        }

        dataLength = instance.GetDataLength();
        if (m_lastHeader.DataLength != dataLength)
        {
            WriteVarUInt32(pWritePointer, dataLength);
            flags |= (1 << 7);
        }

        unsigned int bytesWritten = (unsigned int)(pWritePointer - m_compressedHeader);
        unsigned int totalSize = 1 + bytesWritten + dataLength;
        if (m_pWritePointer + totalSize >= m_pEndOfTheBuffer)
        {
            return false;
        }

        m_lastHeader.MetadataId = instance.GetMetadataId();
        m_lastHeader.SequenceNumber = sequenceNumber;
        m_lastHeader.ThreadId = instance.GetThreadId64();
        m_lastHeader.CaptureThreadId = captureThreadId;
        m_lastHeader.CaptureProcNumber = captureProcNumber;
        m_lastHeader.StackId = stackId;
        m_lastHeader.TimeStamp.QuadPart = timeStamp->QuadPart;
        memcpy(&m_lastHeader.ActivityId, instance.GetActivityId(), sizeof(GUID));
        memcpy(&m_lastHeader.RelatedActivityId, instance.GetRelatedActivityId(), sizeof(GUID));
        m_lastHeader.DataLength = dataLength;

        alignedEnd = m_pWritePointer + totalSize;
        *m_pWritePointer = flags;
        m_pWritePointer++;
        memcpy(m_pWritePointer, m_compressedHeader, bytesWritten);
        m_pWritePointer += bytesWritten;
    }

    if (dataLength > 0)
    {
        memcpy(m_pWritePointer, instance.GetData(), dataLength);
        m_pWritePointer += dataLength;
    }

    if (m_format == EventPipeSerializationFormat::NetPerfV3)
    {
        unsigned int stackSize = instance.GetStackSize();
        memcpy(m_pWritePointer, &stackSize, sizeof(stackSize));
        m_pWritePointer += sizeof(stackSize);

        if (stackSize > 0)
        {
            memcpy(m_pWritePointer, instance.GetStack(), stackSize);
            m_pWritePointer += stackSize;
        }
    }

    while (m_pWritePointer < alignedEnd)
    {
        *m_pWritePointer++ = (BYTE)0; // put padding at the end to get 4 bytes alignment of the payload
    }
    _ASSERTE(m_pWritePointer == alignedEnd);

    if (m_minTimeStamp.QuadPart > instance.GetTimeStamp()->QuadPart)
    {
        m_minTimeStamp.QuadPart = instance.GetTimeStamp()->QuadPart;
    }
    if (m_maxTimeStamp.QuadPart < instance.GetTimeStamp()->QuadPart)
    {
        m_maxTimeStamp.QuadPart = instance.GetTimeStamp()->QuadPart;
    }

    return true;
}

EventPipeEventBlock::EventPipeEventBlock(unsigned int maxBlockSize, EventPipeSerializationFormat format) :
    EventPipeEventBlockBase(maxBlockSize, format, format >= EventPipeSerializationFormat::NetTraceV4)
{}


EventPipeMetadataBlock::EventPipeMetadataBlock(unsigned int maxBlockSize) :
    EventPipeEventBlockBase(maxBlockSize, EventPipeSerializationFormat::NetTraceV4)
{}

unsigned int GetSequencePointBlockSize(EventPipeSequencePoint* pSequencePoint)
{
    const unsigned int sizeOfSequenceNumber =
        sizeof(ULONGLONG) +    // thread id
        sizeof(unsigned int);  // sequence number
    return sizeof(pSequencePoint->TimeStamp) +
        sizeof(unsigned int) + // thread count
        pSequencePoint->ThreadSequenceNumbers.GetCount() * sizeOfSequenceNumber;
}

EventPipeSequencePointBlock::EventPipeSequencePointBlock(EventPipeSequencePoint* pSequencePoint) :
    EventPipeBlock(GetSequencePointBlockSize(pSequencePoint))
{
    const LARGE_INTEGER timeStamp = pSequencePoint->TimeStamp;
    memcpy(m_pWritePointer, &timeStamp, sizeof(timeStamp));
    m_pWritePointer += sizeof(timeStamp);

    const unsigned int threadCount = pSequencePoint->ThreadSequenceNumbers.GetCount();
    memcpy(m_pWritePointer, &threadCount, sizeof(threadCount));
    m_pWritePointer += sizeof(threadCount);

    for (ThreadSequenceNumberMap::Iterator pCur = pSequencePoint->ThreadSequenceNumbers.Begin();
        pCur != pSequencePoint->ThreadSequenceNumbers.End();
        pCur++)
    {
        const ULONGLONG threadId = pCur->Key()->GetThread()->GetOSThreadId();
        memcpy(m_pWritePointer, &threadId, sizeof(threadId));
        m_pWritePointer += sizeof(threadId);

        const unsigned int sequenceNumber = pCur->Value();
        memcpy(m_pWritePointer, &sequenceNumber, sizeof(sequenceNumber));
        m_pWritePointer += sizeof(sequenceNumber);
    }
}

EventPipeStackBlock::EventPipeStackBlock(unsigned int maxBlockSize) :
    EventPipeBlock(maxBlockSize)
{
    Clear();
}

void EventPipeStackBlock::Clear()
{
    m_hasInitialIndex = false;
    m_initialIndex = 0;
    m_count = 0;
    EventPipeBlock::Clear();
}

bool EventPipeStackBlock::WriteStack(DWORD stackId, StackContents* pStack)
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

    unsigned int stackSize = pStack->GetSize();
    unsigned int totalSize = sizeof(stackSize) + stackSize;
    if (m_pWritePointer + totalSize >= m_pEndOfTheBuffer)
    {
        return false;
    }

    if (!m_hasInitialIndex)
    {
        m_hasInitialIndex = true;
        m_initialIndex = stackId;
    }
    m_count++;

    memcpy(m_pWritePointer, &stackSize, sizeof(stackSize));
    m_pWritePointer += sizeof(stackSize);

    if (stackSize > 0)
    {
        memcpy(m_pWritePointer, pStack->GetPointer(), stackSize);
        m_pWritePointer += stackSize;
    }

    return true;
}


#endif // FEATURE_PERFTRACING
