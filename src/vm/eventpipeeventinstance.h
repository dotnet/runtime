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

public:

    EventPipeEventInstance(EventPipeEvent &event, DWORD threadID, BYTE *pData, unsigned int length);

    // Get the event associated with this instance.
    EventPipeEvent* GetEvent() const;

    // Get the stack contents object to either read or write to it.
    StackContents* GetStack();

    // Get a pointer to the data payload.
    BYTE* GetData() const;

    // Get the length of the data.
    unsigned int GetLength() const;

    // Get the thread id of the event.
    unsigned int GetThreadId() const;

    // Get the timestamp of the event.
    LARGE_INTEGER GetTimestamp() const;

    // Serialize this object using FastSerialization.
    void FastSerialize(FastSerializer *pSerializer, StreamLabel metadataLabel);

    // Serialize this event to the JSON file.
    void SerializeToJsonFile(EventPipeJsonFile *pFile);

protected:

    EventPipeEvent *m_pEvent;
    DWORD m_threadID;
    LARGE_INTEGER m_timeStamp;

    BYTE *m_pData;
    unsigned int m_dataLength;
    StackContents m_stackContents;
};

// A specific type of event instance for use by the SampleProfiler.
// This is needed because the SampleProfiler knows how to walk stacks belonging
// to threads other than the current thread.
class SampleProfilerEventInstance : public EventPipeEventInstance
{

public:

    SampleProfilerEventInstance(Thread *pThread);
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_EVENTINSTANCE_H__
