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

class EventPipeFile final : public FastSerializableObject
{
public:
    EventPipeFile(StreamWriter *pStreamWriter);
    ~EventPipeFile();

    void WriteEvent(EventPipeEventInstance &instance);
    void Flush();
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

    void SaveMetadataId(EventPipeEvent &event, unsigned int metadataId);

    void WriteToBlock(EventPipeEventInstance &instance, unsigned int metadataId);

    // The object responsible for serialization.
    FastSerializer *m_pSerializer;

    EventPipeBlock *m_pBlock;

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
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_FILE_H__
