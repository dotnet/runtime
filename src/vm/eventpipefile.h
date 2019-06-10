// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_FILE_H__
#define __EVENTPIPE_FILE_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipe.h"
#include "eventpipeblock.h"
#include "fastserializableobject.h"

class EventPipeConfiguration;
class EventPipeEventInstance;
class FastSerializer;
struct EventPipeSequencePoint;

struct StackHashKey
{
    BYTE* pStackBytes;
    ULONG Hash;
    ULONG StackSizeInBytes;

    StackHashKey(StackContents* pStack);
    StackHashKey(BYTE* pStack, ULONG stackSizeInBytes, ULONG hash);
};

struct StackHashEntry
{
    ULONG Id;
    ULONG Hash;
    ULONG StackSizeInBytes;
    // This is the first byte of StackSizeInBytes bytes of stack data
    BYTE StackBytes[1];

    static StackHashEntry* CreateNew(StackContents* pStack, ULONG id, ULONG hash);
    StackHashKey GetKey() const;
};

class EventPipeStackHashTraits : public NoRemoveSHashTraits<DefaultSHashTraits<StackHashEntry*>>
{
public:
    typedef typename DefaultSHashTraits<StackHashEntry*>::element_t element_t;
    typedef typename DefaultSHashTraits<StackHashEntry*>::count_t count_t;

    typedef const StackHashKey key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return e->GetKey();
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return k1.StackSizeInBytes == k2.StackSizeInBytes &&
            memcmp(k1.pStackBytes, k2.pStackBytes, k1.StackSizeInBytes) == 0;
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t)(size_t)k.Hash;
    }

    static element_t Null() { LIMITED_METHOD_CONTRACT; return nullptr; }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == nullptr; }
};

typedef SHash<EventPipeStackHashTraits> EventPipeStackHash;

class EventPipeFile final : public FastSerializableObject
{
public:
    EventPipeFile(StreamWriter *pStreamWriter, EventPipeSerializationFormat format);
    ~EventPipeFile();

    EventPipeSerializationFormat GetSerializationFormat() const;
    void WriteEvent(EventPipeEventInstance &instance, ULONGLONG captureThreadId, unsigned int sequenceNumber, BOOL isSortedEvent);
    void WriteSequencePoint(EventPipeSequencePoint* pSequencePoint);
    enum FlushFlags
    {
        FlushEventBlock = 1,
        FlushMetadataBlock = 2,
        FlushStackBlock = 4,
        FlushAllBlocks = FlushEventBlock | FlushMetadataBlock | FlushStackBlock
    };
    void Flush(FlushFlags flags = FlushAllBlocks);
    bool HasErrors() const;

    const char *GetTypeName() override
    {
        LIMITED_METHOD_CONTRACT;
        return "Trace";
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

        pSerializer->WriteBuffer((BYTE *)&m_fileOpenSystemTime, sizeof(m_fileOpenSystemTime));
        pSerializer->WriteBuffer((BYTE *)&m_fileOpenTimeStamp, sizeof(m_fileOpenTimeStamp));
        pSerializer->WriteBuffer((BYTE *)&m_timeStampFrequency, sizeof(m_timeStampFrequency));

        // the beginning of V3
        pSerializer->WriteBuffer((BYTE *)&m_pointerSize, sizeof(m_pointerSize));
        pSerializer->WriteBuffer((BYTE *)&m_currentProcessId, sizeof(m_currentProcessId));
        pSerializer->WriteBuffer((BYTE *)&m_numberOfProcessors, sizeof(m_numberOfProcessors));
        pSerializer->WriteBuffer((BYTE *)&m_samplingRateInNs, sizeof(m_samplingRateInNs));
    }

private:
    void WriteEnd();

    unsigned int GenerateMetadataId();

    unsigned int GetMetadataId(EventPipeEvent &event);

    unsigned int GetStackId(EventPipeEventInstance &instance);

    void SaveMetadataId(EventPipeEvent &event, unsigned int metadataId);

    void WriteEventToBlock(EventPipeEventInstance &instance,
                           unsigned int metadataId,
                           ULONGLONG captureThreadId = 0,
                           unsigned int sequenceNumber = 0,
                           unsigned int stackId = 0,
                           BOOL isSortedEvent = TRUE);

    // The format to serialize
    EventPipeSerializationFormat m_format;

    // The object responsible for serialization.
    FastSerializer *m_pSerializer;

    EventPipeEventBlock *m_pBlock;
    EventPipeMetadataBlock *m_pMetadataBlock;
    EventPipeStackBlock *m_pStackBlock;

    // The system time when the file was opened.
    SYSTEMTIME m_fileOpenSystemTime;

    // The timestamp when the file was opened.  Used for calculating file-relative timestamps.
    LARGE_INTEGER m_fileOpenTimeStamp;

    // The frequency of the timestamps used for this file.
    LARGE_INTEGER m_timeStampFrequency;

    unsigned int m_pointerSize;

    unsigned int m_currentProcessId;

    unsigned int m_numberOfProcessors;

    unsigned int m_samplingRateInNs;

    // The serialization which is responsible for making sure only a single event
    // or block of events gets written to the file at once.
    SpinLock m_serializationLock;

    // Hashtable of metadata labels.
    MapSHashWithRemove<EventPipeEvent *, unsigned int> *m_pMetadataIds;

    Volatile<LONG> m_metadataIdCounter;

    unsigned int m_stackIdCounter;
    EventPipeStackHash m_stackHash;
#ifdef DEBUG
    LARGE_INTEGER m_lastSortedTimestamp;
#endif
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_FILE_H__
