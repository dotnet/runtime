// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipeeventinstance.h"
#include "eventpipejsonfile.h"
#include "fastserializer.h"
#include "sampleprofiler.h"

#ifdef FEATURE_PERFTRACING

EventPipeEventInstance::EventPipeEventInstance(
    EventPipeEvent &event,
    DWORD threadID,
    BYTE *pData,
    unsigned int length)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pEvent = &event;
    m_threadID = threadID;
    m_pData = pData;
    m_dataLength = length;
    QueryPerformanceCounter(&m_timeStamp);

    if(event.NeedStack())
    {
        EventPipe::WalkManagedStackForCurrentThread(m_stackContents);
    }
}

StackContents* EventPipeEventInstance::GetStack()
{
    LIMITED_METHOD_CONTRACT;

    return &m_stackContents;
}

EventPipeEvent* EventPipeEventInstance::GetEvent() const
{
    LIMITED_METHOD_CONTRACT;

    return m_pEvent;
}

BYTE* EventPipeEventInstance::GetData() const
{
    LIMITED_METHOD_CONTRACT;

    return m_pData;
}

unsigned int EventPipeEventInstance::GetLength() const
{
    LIMITED_METHOD_CONTRACT;

    return m_dataLength;
}

unsigned int EventPipeEventInstance::GetThreadId() const
{
    LIMITED_METHOD_CONTRACT;

    return m_threadID;
}

LARGE_INTEGER EventPipeEventInstance::GetTimestamp() const
{
    LIMITED_METHOD_CONTRACT;

    return m_timeStamp;
}

void EventPipeEventInstance::FastSerialize(FastSerializer *pSerializer, StreamLabel metadataLabel)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    // Useful for diagnosing serialization bugs.
    const unsigned int value = 0xDEADBEEF;
    pSerializer->WriteBuffer((BYTE*)&value, sizeof(value));
#endif

    // Calculate the size of the total payload so that it can be written to the file.
    unsigned int payloadLength =
        sizeof(metadataLabel) +
        sizeof(m_threadID) +        // Thread ID
        sizeof(m_timeStamp) +       // TimeStamp
        m_dataLength +              // Event payload data length
        m_stackContents.GetSize();  // Stack payload size

    // Write the size of the event to the file.
    pSerializer->WriteBuffer((BYTE*)&payloadLength, sizeof(payloadLength));

    // Write the metadata label.
    pSerializer->WriteBuffer((BYTE*)&metadataLabel, sizeof(metadataLabel));

    // Write the thread ID.
    pSerializer->WriteBuffer((BYTE*)&m_threadID, sizeof(m_threadID));

    // Write the timestamp.
    pSerializer->WriteBuffer((BYTE*)&m_timeStamp, sizeof(m_timeStamp));

    // Write the event data payload.
    if(m_dataLength > 0)
    {
        pSerializer->WriteBuffer(m_pData, m_dataLength);
    }

    // Write the stack if present.
    if(m_stackContents.GetSize() > 0)
    {
        pSerializer->WriteBuffer(m_stackContents.GetPointer(), m_stackContents.GetSize());
    }
}

void EventPipeEventInstance::SerializeToJsonFile(EventPipeJsonFile *pFile)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(pFile == NULL)
    {
        return;
    }

    EX_TRY
    {
        const unsigned int guidSize = 39;
        WCHAR wszProviderID[guidSize];
        if(!StringFromGUID2(m_pEvent->GetProvider()->GetProviderID(), wszProviderID, guidSize))
        {
            wszProviderID[0] = '\0';
        }

        // Strip off the {}.
        StackScratchBuffer scratch;
        SString guidStr(&wszProviderID[1], guidSize-3);

        SString message;
        message.Printf("Provider=%s/EventID=%d/Version=%d", guidStr.GetANSI(scratch), m_pEvent->GetEventID(), m_pEvent->GetEventVersion());
        pFile->WriteEvent(m_timeStamp, m_threadID, message, m_stackContents);
    }
    EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
}

SampleProfilerEventInstance::SampleProfilerEventInstance(Thread *pThread)
    :EventPipeEventInstance(*SampleProfiler::s_pThreadTimeEvent, pThread->GetOSThreadId(), NULL, 0)
{
    LIMITED_METHOD_CONTRACT;
}

#endif // FEATURE_PERFTRACING
