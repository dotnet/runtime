// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_EVENTINSTANCE_H__
#define __EVENTPIPE_EVENTINSTANCE_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipe.h"
#include "eventpipeevent.h"
#include "fastserializableobject.h"
#include "fastserializer.h"

class EventPipeEventInstance
{
    // Declare friends.
    friend EventPipeConfiguration;

public:

    EventPipeEventInstance(EventPipeEvent &event, DWORD threadID, BYTE *pData, unsigned int length, LPCGUID pActivityId, LPCGUID pRelatedActivityId);

    // Get the event associated with this instance.
    EventPipeEvent* GetEvent() const;

    // Get the stack contents object to either read or write to it.
    StackContents* GetStack();

    // Get the timestamp.
    LARGE_INTEGER GetTimeStamp() const;

    // Get a pointer to the data payload.
    BYTE* GetData() const;

    // Get the length of the data.
    unsigned int GetLength() const;

    // Serialize this object using FastSerialization.
    void FastSerialize(FastSerializer *pSerializer, StreamLabel metadataLabel);

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
    DWORD m_threadID;
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

// A specific type of event instance for use by the SampleProfiler.
// This is needed because the SampleProfiler knows how to walk stacks belonging
// to threads other than the current thread.
class SampleProfilerEventInstance : public EventPipeEventInstance
{

public:

    SampleProfilerEventInstance(EventPipeEvent &event, Thread *pThread, BYTE *pData, unsigned int length);
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_EVENTINSTANCE_H__
