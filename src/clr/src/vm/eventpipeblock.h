// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_BLOCK_H__
#define __EVENTPIPE_BLOCK_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipeeventinstance.h"
#include "fastserializableobject.h"
#include "fastserializer.h"

struct EventPipeSequencePoint;

// The base class for all file blocks in the Nettrace file format
// This class handles memory management to buffer the block data,
// bookkeeping, block version numbers, and serializing the data 
// to the file with correct alignment.
// Sub-classes decide the format of the block contents and how
// the blocks are named.
class EventPipeBlock : public FastSerializableObject
{
public:
    EventPipeBlock(unsigned int maxBlockSize, EventPipeSerializationFormat format = EventPipeSerializationFormat::NetTraceV4);
    ~EventPipeBlock();

    virtual void Clear();

    unsigned int GetBytesWritten() const
    {
        return m_pBlock == nullptr ? 0 : (unsigned int)(m_pWritePointer - m_pBlock);
    }

    // The size of the header for this block, if any
    virtual unsigned int GetHeaderSize()
    {
        return 0;
    }

    // Write the header to the stream
    virtual void SerializeHeader(FastSerializer *pSerializer)
    {
    }

    void FastSerialize(FastSerializer *pSerializer) override
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_PREEMPTIVE;
            PRECONDITION(pSerializer != NULL);
        }
        CONTRACTL_END;

        if (m_pBlock == NULL)
            return;

        unsigned int dataSize = GetBytesWritten();
        // We shouldn't attempt to write blocks that have no data
        _ASSERTE(dataSize != 0);
        unsigned int headerSize = GetHeaderSize();
        unsigned int totalSize = dataSize + headerSize;
        pSerializer->WriteBuffer((BYTE *)&totalSize, sizeof(totalSize));

        unsigned int requiredPadding = pSerializer->GetRequiredPadding();
        if (requiredPadding != 0)
        {
            BYTE maxPadding[ALIGNMENT_SIZE - 1] = {}; // it's longest possible padding, we are going to use only part of it
            pSerializer->WriteBuffer(maxPadding, requiredPadding); // we write zeros here, the reader is going to always read from the first aligned address of the serialized content

            _ASSERTE(pSerializer->HasWriteErrors() || (pSerializer->GetRequiredPadding() == 0));
        }

        SerializeHeader(pSerializer);
        pSerializer->WriteBuffer(m_pBlock, dataSize);
    }

protected:
    BYTE *m_pBlock;
    BYTE *m_pWritePointer;
    BYTE *m_pEndOfTheBuffer;
    EventPipeSerializationFormat m_format;

    unsigned int GetSize() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pBlock == nullptr ? 0 : (unsigned int)(m_pEndOfTheBuffer - m_pBlock);
    }
};

struct EventPipeEventHeader
{
    DWORD MetadataId;
    DWORD SequenceNumber;
    ULONGLONG ThreadId;
    ULONGLONG CaptureThreadId;
    DWORD CaptureProcNumber;
    DWORD StackId;
    LARGE_INTEGER TimeStamp;
    GUID ActivityId;
    GUID RelatedActivityId;
    DWORD DataLength;
}; 

// The base type for blocks that contain events (EventBlock and EventMetadataBlock)
class EventPipeEventBlockBase : public EventPipeBlock
{
public:
    EventPipeEventBlockBase(unsigned int maxBlockSize, EventPipeSerializationFormat format, bool fUseHeaderCompression = true);

    void Clear() override;

    unsigned int GetHeaderSize() override
    {
        if(m_format == EventPipeSerializationFormat::NetPerfV3)
        {
            return 0;
        }
        else
        {
            return sizeof(unsigned short) + // header size
                   sizeof(unsigned short) + // flags
                   sizeof(LARGE_INTEGER)  + // min timestamp
                   sizeof(LARGE_INTEGER);   // max timestamp
        }
    }

    void SerializeHeader(FastSerializer* pSerializer) override
    {
        if(m_format == EventPipeSerializationFormat::NetPerfV3)
        {
            return;
        }
        else
        {
            const unsigned short headerSize = GetHeaderSize();
            pSerializer->WriteBuffer((BYTE *)&headerSize, sizeof(headerSize));
            const unsigned short flags = m_fUseHeaderCompression ? 1 : 0;
            pSerializer->WriteBuffer((BYTE *)&flags, sizeof(flags));
            pSerializer->WriteBuffer((BYTE *)&m_minTimeStamp, sizeof(m_minTimeStamp));
            pSerializer->WriteBuffer((BYTE *)&m_maxTimeStamp, sizeof(m_maxTimeStamp));
        }
    }

    // Write an event to the block.
    // Returns:
    //  - true: The write succeeded.
    //  - false: The write failed.  In this case, the block should be considered full.
    bool WriteEvent(EventPipeEventInstance &instance, ULONGLONG captureThreadId, unsigned int sequenceNumber, DWORD stackId, BOOL isSortedEvent);

private:
    EventPipeEventHeader m_lastHeader;
    BYTE m_compressedHeader[100];
    bool m_fUseHeaderCompression;
    LARGE_INTEGER m_minTimeStamp;
    LARGE_INTEGER m_maxTimeStamp;
};

class EventPipeEventBlock : public EventPipeEventBlockBase
{
public:
    EventPipeEventBlock(unsigned int maxBlockSize, EventPipeSerializationFormat format);

    const char *GetTypeName() override
    {
        LIMITED_METHOD_CONTRACT;
        return "EventBlock";
    }
};

class EventPipeMetadataBlock : public EventPipeEventBlockBase
{
public:
    EventPipeMetadataBlock(unsigned int maxBlockSize);

    const char *GetTypeName() override
    {
        LIMITED_METHOD_CONTRACT;
        return "MetadataBlock";
    }
};

class EventPipeSequencePointBlock : public EventPipeBlock
{
public:
    EventPipeSequencePointBlock(EventPipeSequencePoint* sequencePoint);

    const char *GetTypeName() override
    {
        LIMITED_METHOD_CONTRACT;
        return "SPBlock";
    }
};

// The block that contains interned stacks
class EventPipeStackBlock : public EventPipeBlock
{
public:
    EventPipeStackBlock(unsigned int maxBlockSize);

    unsigned int GetHeaderSize() override
    {
        return sizeof(unsigned int) + // start index 
               sizeof(unsigned int);  // count of indices
    }

    void SerializeHeader(FastSerializer* pSerializer) override
    {
        pSerializer->WriteBuffer((BYTE *)&m_initialIndex, sizeof(m_initialIndex));
        pSerializer->WriteBuffer((BYTE *)&m_count, sizeof(m_count));
    }

    void Clear() override;

    // Write a stack to the block
    // Returns:
    //  - true: The write succeeded.
    //  - false: The write failed.  In this case, the block should be considered full.
    bool WriteStack(DWORD stackId, StackContents* pStack);

    const char *GetTypeName() override
    {
        LIMITED_METHOD_CONTRACT;
        return "StackBlock";
    }

private:
    bool m_hasInitialIndex;
    unsigned int m_initialIndex;
    unsigned int m_count;
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_BLOCK_H__
