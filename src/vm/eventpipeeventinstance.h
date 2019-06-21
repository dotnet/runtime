// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_EVENTINSTANCE_H__
#define __EVENTPIPE_EVENTINSTANCE_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipe.h"
#include "eventpipeevent.h"
#include "eventpipesession.h"
#include "eventpipeblock.h"
#include "eventpipethread.h"
#include "fastserializableobject.h"
#include "fastserializer.h"

class EventPipeJsonFile;

class EventPipeEventInstance
{
    // Declare friends.
    friend EventPipeConfiguration;

public:

    EventPipeEventInstance(EventPipeEvent &event, 
                           unsigned int procNumber,
                           ULONGLONG threadID,
                           BYTE *pData,
                           unsigned int length,
                           LPCGUID pActivityId,
                           LPCGUID pRelatedActivityId);

    void EnsureStack(const EventPipeSession &session);

    StackContents* GetStack()
    {
        LIMITED_METHOD_CONTRACT;

        return &m_stackContents;
    }

    EventPipeEvent* GetEvent() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_pEvent;
    }

    const LARGE_INTEGER* GetTimeStamp() const
    {
        LIMITED_METHOD_CONTRACT;

        return &m_timeStamp;
    }

    unsigned int GetMetadataId() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_metadataId;
    }

    void SetMetadataId(unsigned int metadataId)
    {
        LIMITED_METHOD_CONTRACT;

        m_metadataId = metadataId;
    }

    unsigned int GetProcNumber() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_procNumber;
    }

    DWORD GetThreadId32() const
    {
        LIMITED_METHOD_CONTRACT;

        return (DWORD)m_threadId;
    }

    ULONGLONG GetThreadId64() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_threadId;
    }

    const GUID* GetActivityId() const
    {
        LIMITED_METHOD_CONTRACT;

        return &m_activityId;
    }

    const GUID* GetRelatedActivityId() const
    {
        LIMITED_METHOD_CONTRACT;

        return &m_relatedActivityId;
    }

    const BYTE* GetData() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_pData;
    }

    unsigned int GetDataLength() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_dataLength;
    }

    unsigned int GetStackSize() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_stackContents.GetSize();
    }

    unsigned int GetAlignedTotalSize(EventPipeSerializationFormat format) const;

#ifdef _DEBUG
    // Serialize this event to the JSON file.
    void SerializeToJsonFile(EventPipeJsonFile *pFile);

    bool EnsureConsistency();
#endif // _DEBUG

protected:

#ifdef _DEBUG
    unsigned int m_debugEventStart;
#endif // _DEBUG

    EventPipeEvent *m_pEvent;
    unsigned int m_metadataId;
    unsigned int m_procNumber;
    ULONGLONG m_threadId;
    LARGE_INTEGER m_timeStamp;
    GUID m_activityId;
    GUID m_relatedActivityId;

    BYTE *m_pData;
    unsigned int m_dataLength;
    StackContents m_stackContents;

#ifdef _DEBUG
    unsigned int m_debugEventEnd;
#endif // _DEBUG

private:

    // This is used for metadata events by EventPipeConfiguration because
    // the metadata event is created after the first instance of the event
    // but must be inserted into the file before the first instance of the event.
    void SetTimeStamp(LARGE_INTEGER timeStamp);
};

typedef MapSHash<EventPipeThreadSessionState *, unsigned int> ThreadSequenceNumberMap;

// A point in time marker that is used as a boundary when emitting events.
// The events in a Nettrace file are not emitted in a fully sorted order
// but we do guarantee that all events before a sequence point are emitted
// prior to any events after the sequence point
struct EventPipeSequencePoint
{
    // Entry in EventPipeBufferManager m_sequencePointList 
    SLink m_Link;

    // The timestamp the sequence point was captured
    LARGE_INTEGER TimeStamp;
    ThreadSequenceNumberMap ThreadSequenceNumbers;

    EventPipeSequencePoint();
    ~EventPipeSequencePoint();
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_EVENTINSTANCE_H__
