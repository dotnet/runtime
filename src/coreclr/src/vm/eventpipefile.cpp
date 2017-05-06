// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipeconfiguration.h"
#include "eventpipefile.h"

#ifdef FEATURE_PERFTRACING

EventPipeFile::EventPipeFile(SString &outputFilePath)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pSerializer = new FastSerializer(outputFilePath, *this);
    m_serializationLock.Init(LOCK_TYPE_DEFAULT);
    m_pMetadataLabels = new MapSHashWithRemove<EventPipeEvent*, StreamLabel>();

    // File start time information.
    GetSystemTime(&m_fileOpenSystemTime);
    QueryPerformanceCounter(&m_fileOpenTimeStamp);
    QueryPerformanceFrequency(&m_timeStampFrequency);

    // Write a forward reference to the beginning of the event stream.
    // This also allows readers to know where the event stream ends and skip it if needed.
    m_beginEventsForwardReferenceIndex = m_pSerializer->AllocateForwardReference();
    m_pSerializer->WriteForwardReference(m_beginEventsForwardReferenceIndex);

    // Write the header information into the file.

    // Write the current date and time.
    m_pSerializer->WriteBuffer((BYTE*)&m_fileOpenSystemTime, sizeof(m_fileOpenSystemTime));

    // Write FileOpenTimeStamp
    m_pSerializer->WriteBuffer((BYTE*)&m_fileOpenTimeStamp, sizeof(m_fileOpenTimeStamp));

    // Write ClockFrequency
    m_pSerializer->WriteBuffer((BYTE*)&m_timeStampFrequency, sizeof(m_timeStampFrequency));
}

EventPipeFile::~EventPipeFile()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Mark the end of the event stream.
    StreamLabel currentLabel = m_pSerializer->GetStreamLabel();

    // Define the event start forward reference.
    m_pSerializer->DefineForwardReference(m_beginEventsForwardReferenceIndex, currentLabel);

    // Close the serializer.
    if(m_pSerializer != NULL)
    {
        delete(m_pSerializer);
        m_pSerializer = NULL;
    }
}

void EventPipeFile::WriteEvent(EventPipeEventInstance &instance)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Take the serialization lock.
    SpinLockHolder _slh(&m_serializationLock);

    // Check to see if we've seen this event type before.
    // If not, then write the event metadata to the event stream first.
    StreamLabel metadataLabel = GetMetadataLabel(*instance.GetEvent());
    if(metadataLabel == 0)
    {
        EventPipeEventInstance* pMetadataInstance = EventPipe::GetConfiguration()->BuildEventMetadataEvent(*instance.GetEvent());

        metadataLabel = m_pSerializer->GetStreamLabel();
        pMetadataInstance->FastSerialize(m_pSerializer, (StreamLabel)0); // 0 breaks recursion and represents the metadata event.

        SaveMetadataLabel(*instance.GetEvent(), metadataLabel);

        delete (pMetadataInstance->GetData());
        delete (pMetadataInstance);
    }

    // Write the event to the stream.
    instance.FastSerialize(m_pSerializer, metadataLabel);
}

StreamLabel EventPipeFile::GetMetadataLabel(EventPipeEvent &event)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    StreamLabel outLabel;
    if(m_pMetadataLabels->Lookup(&event, &outLabel))
    {
        _ASSERTE(outLabel != 0);
        return outLabel;
    }

    return 0;
}

void EventPipeFile::SaveMetadataLabel(EventPipeEvent &event, StreamLabel label)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(label > 0);
    }
    CONTRACTL_END;

    // If a pre-existing metadata label exists, remove it.
    StreamLabel outLabel;
    if(m_pMetadataLabels->Lookup(&event, &outLabel))
    {
        m_pMetadataLabels->Remove(&event);
    }

    // Add the metadata label.
    m_pMetadataLabels->Add(&event, label);
}

#endif // FEATURE_PERFTRACING
