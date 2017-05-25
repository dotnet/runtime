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
    unsigned int length,
    LPCGUID pActivityId,
    LPCGUID pRelatedActivityId)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    m_debugEventStart = 0xDEADBEEF;
    m_debugEventEnd = 0xCAFEBABE;
#endif // _DEBUG
    m_pEvent = &event;
    m_threadID = threadID;
    if(pActivityId != NULL)
    {
        m_activityId = *pActivityId;
    }
    else
    {
        m_activityId = {0};
    }
    if(pRelatedActivityId != NULL)
    {
        m_relatedActivityId = *pRelatedActivityId;
    }
    else
    {
        m_relatedActivityId = {0};
    }

    m_pData = pData;
    m_dataLength = length;
    QueryPerformanceCounter(&m_timeStamp);

    if(event.NeedStack())
    {
        EventPipe::WalkManagedStackForCurrentThread(m_stackContents);
    }

#ifdef _DEBUG
    EnsureConsistency();
#endif // _DEBUG
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

LARGE_INTEGER EventPipeEventInstance::GetTimeStamp() const
{
    LIMITED_METHOD_CONTRACT;

    return m_timeStamp;
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

void EventPipeEventInstance::FastSerialize(FastSerializer *pSerializer, StreamLabel metadataLabel)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef EVENTPIPE_EVENT_MARKER
    // Useful for diagnosing serialization bugs.
    const unsigned int value = 0xDEADBEEF;
    pSerializer->WriteBuffer((BYTE*)&value, sizeof(value));
#endif

    // Calculate the size of the total payload so that it can be written to the file.
    unsigned int payloadLength =
        sizeof(metadataLabel) +
        sizeof(m_threadID) +            // Thread ID
        sizeof(m_timeStamp) +           // TimeStamp
        sizeof(m_activityId) +          // Activity ID
        sizeof(m_relatedActivityId) +   // Related Activity ID
        sizeof(m_dataLength) +          // Data payload length
        m_dataLength +                  // Event payload data
        sizeof(unsigned int) +          // Prepended stack payload size in bytes
        m_stackContents.GetSize();      // Stack payload size

    // Write the size of the event to the file.
    pSerializer->WriteBuffer((BYTE*)&payloadLength, sizeof(payloadLength));

    // Write the metadata label.
    pSerializer->WriteBuffer((BYTE*)&metadataLabel, sizeof(metadataLabel));

    // Write the thread ID.
    pSerializer->WriteBuffer((BYTE*)&m_threadID, sizeof(m_threadID));

    // Write the timestamp.
    pSerializer->WriteBuffer((BYTE*)&m_timeStamp, sizeof(m_timeStamp));

    // Write the activity id.
    pSerializer->WriteBuffer((BYTE*)&m_activityId, sizeof(m_activityId));

    // Write the related activity id.
    pSerializer->WriteBuffer((BYTE*)&m_relatedActivityId, sizeof(m_relatedActivityId));

    // Write the data payload size.
    pSerializer->WriteBuffer((BYTE*)&m_dataLength, sizeof(m_dataLength));

    // Write the event data payload.
    if(m_dataLength > 0)
    {
        pSerializer->WriteBuffer(m_pData, m_dataLength);
    }

    // Write the size of the stack in bytes.
    unsigned int stackSize = m_stackContents.GetSize();
    pSerializer->WriteBuffer((BYTE*)&stackSize, sizeof(stackSize));

    // Write the stack if present.
    if(stackSize > 0)
    {
        pSerializer->WriteBuffer(m_stackContents.GetPointer(), stackSize);
    }
}

#ifdef _DEBUG
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
#endif

void EventPipeEventInstance::SetTimeStamp(LARGE_INTEGER timeStamp)
{
    LIMITED_METHOD_CONTRACT;

    m_timeStamp = timeStamp;
}

#ifdef _DEBUG
bool EventPipeEventInstance::EnsureConsistency()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Validate event start.
    _ASSERTE(m_debugEventStart == 0xDEADBEEF);

    // Validate event end.
    _ASSERTE(m_debugEventEnd == 0xCAFEBABE);

    return true;
}
#endif // _DEBUG

SampleProfilerEventInstance::SampleProfilerEventInstance(EventPipeEvent &event, Thread *pThread, BYTE *pData, unsigned int length)
    :EventPipeEventInstance(event, pThread->GetOSThreadId(), pData, length, NULL /* pActivityId */, NULL /* pRelatedActivityId */)
{
    LIMITED_METHOD_CONTRACT;
}

#endif // FEATURE_PERFTRACING
