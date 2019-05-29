// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipeconfiguration.h"
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
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    m_debugEventStart = 0xDEADBEEF;
    m_debugEventEnd = 0xCAFEBABE;
#endif // _DEBUG
    m_pEvent = &event;
    m_threadID = threadID;
    if (pActivityId != NULL)
    {
        m_activityId = *pActivityId;
    }
    else
    {
        m_activityId = {0};
    }
    if (pRelatedActivityId != NULL)
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
    _ASSERTE(m_timeStamp.QuadPart > 0);
#ifdef _DEBUG
    EnsureConsistency();
#endif // _DEBUG
}

void EventPipeEventInstance::EnsureStack(const EventPipeSession &session)
{
    if (m_pEvent->NeedStack() && !session.RundownEnabled())
    {
        EventPipe::WalkManagedStackForCurrentThread(m_stackContents);
    }
}

unsigned int EventPipeEventInstance::GetAlignedTotalSize() const
{
    CONTRACT(unsigned int)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(RETVAL % ALIGNMENT_SIZE == 0);
    }
    CONTRACT_END;

    // Calculate the size of the total payload so that it can be written to the file.
    unsigned int payloadLength =
        sizeof(m_metadataId) +          // Metadata ID
        sizeof(m_threadID) +            // Thread ID
        sizeof(m_timeStamp) +           // TimeStamp
        sizeof(m_activityId) +          // Activity ID
        sizeof(m_relatedActivityId) +   // Related Activity ID
        sizeof(m_dataLength) +          // Data payload length
        m_dataLength +                  // Event payload data
        sizeof(unsigned int) +          // Prepended stack payload size in bytes
        m_stackContents.GetSize();      // Stack payload size

    // round up to ALIGNMENT_SIZE bytes
    if (payloadLength % ALIGNMENT_SIZE != 0)
    {
        payloadLength += ALIGNMENT_SIZE - (payloadLength % ALIGNMENT_SIZE);
    }

    RETURN payloadLength;
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
        StackScratchBuffer scratch;
        SString providerName = m_pEvent->GetProvider()->GetProviderName();

        SString message;
        message.Printf("Provider=%s/EventID=%d/Version=%d", providerName.GetANSI(scratch), m_pEvent->GetEventID(), m_pEvent->GetEventVersion());
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

#endif // FEATURE_PERFTRACING
