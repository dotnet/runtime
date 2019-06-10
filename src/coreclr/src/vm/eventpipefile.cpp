// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipeblock.h"
#include "eventpipeeventinstance.h"
#include "eventpipefile.h"
#include "sampleprofiler.h"

#ifdef FEATURE_PERFTRACING


StackHashEntry* StackHashEntry::CreateNew(StackContents* pStack, ULONG id, ULONG hash)
{
    LIMITED_METHOD_CONTRACT;

    StackHashEntry* pEntry = (StackHashEntry*) new (nothrow) BYTE[offsetof(StackHashEntry, StackBytes) + pStack->GetSize()];
    if (pEntry == NULL)
    {
        return NULL;
    }
    pEntry->Id = id;
    pEntry->Hash = hash;
    pEntry->StackSizeInBytes = pStack->GetSize();
    memcpy_s(pEntry->StackBytes, pStack->GetSize(), pStack->GetPointer(), pStack->GetSize());
    return pEntry;
}

StackHashKey StackHashEntry::GetKey() const
{
    LIMITED_METHOD_CONTRACT;
    StackHashKey key((BYTE*)StackBytes, StackSizeInBytes, Hash);
    return key;
}

StackHashKey::StackHashKey(StackContents* pStack) :
    pStackBytes(pStack->GetPointer()),
    Hash(HashBytes(pStack->GetPointer(), pStack->GetSize())),
    StackSizeInBytes(pStack->GetSize())
{}

StackHashKey::StackHashKey(BYTE* pStackBytes, ULONG stackSizeInBytes, ULONG hash) :
    pStackBytes(pStackBytes),
    Hash(hash),
    StackSizeInBytes(stackSizeInBytes)
{}

DWORD GetFileVersion(EventPipeSerializationFormat format)
{
    LIMITED_METHOD_CONTRACT;
    switch(format)
    {
    case EventPipeSerializationFormat::NetPerfV3:
        return 3;
    case EventPipeSerializationFormat::NetTraceV4:
        return 4;
    default:
        _ASSERTE(!"Unrecognized EventPipeSerializationFormat");
        return 0;
    }
}

DWORD GetFileMinVersion(EventPipeSerializationFormat format)
{
    LIMITED_METHOD_CONTRACT;
    switch (format)
    {
    case EventPipeSerializationFormat::NetPerfV3:
        return 0;
    case EventPipeSerializationFormat::NetTraceV4:
        return 4;
    default:
        _ASSERTE(!"Unrecognized EventPipeSerializationFormat");
        return 0;
    }
}

EventPipeFile::EventPipeFile(StreamWriter *pStreamWriter, EventPipeSerializationFormat format) :
    FastSerializableObject(GetFileVersion(format), GetFileMinVersion(format), format >= EventPipeSerializationFormat::NetTraceV4)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    m_format = format;
    m_pBlock = new EventPipeEventBlock(100 * 1024, format);
    m_pMetadataBlock = new EventPipeMetadataBlock(100 * 1024);
    m_pStackBlock = new EventPipeStackBlock(100 * 1024);

    // File start time information.
    GetSystemTime(&m_fileOpenSystemTime);
    QueryPerformanceCounter(&m_fileOpenTimeStamp);
    QueryPerformanceFrequency(&m_timeStampFrequency);

    m_pointerSize = TARGET_POINTER_SIZE;

    m_currentProcessId = GetCurrentProcessId();

    SYSTEM_INFO sysinfo = {};
    GetSystemInfo(&sysinfo);
    m_numberOfProcessors = sysinfo.dwNumberOfProcessors;

    m_samplingRateInNs = SampleProfiler::GetSamplingRate();

    bool fSuccess = true;
    if (m_format >= EventPipeSerializationFormat::NetTraceV4)
    {
        const char* pHeader = "Nettrace";
        uint32_t bytesWritten = 0;
        fSuccess = pStreamWriter->Write(pHeader, 8, bytesWritten) && bytesWritten == 8;
    }
    if (fSuccess)
    {
        // Create the file stream and write the FastSerialization header.
        m_pSerializer = new FastSerializer(pStreamWriter);
    }
    else
    {
        m_pSerializer = nullptr;
    }

    m_serializationLock.Init(LOCK_TYPE_DEFAULT);

    m_pMetadataIds = new MapSHashWithRemove<EventPipeEvent*, unsigned int>();

    // Start at 0 - The value is always incremented prior to use, so the first ID will be 1.
    m_metadataIdCounter = 0;

    // Start at 0 - The value is always incremented prior to use, so the first ID will be 1.
    m_stackIdCounter = 0;

#ifdef DEBUG
    QueryPerformanceCounter(&m_lastSortedTimestamp);
#endif

    // Write the first object to the file.
    m_pSerializer->WriteObject(this);
}

EventPipeFile::~EventPipeFile()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pBlock != NULL && m_pSerializer != NULL)
        WriteEnd();

    for (EventPipeStackHash::Iterator pCur = m_stackHash.Begin(); pCur != m_stackHash.End(); pCur++)
    {
        delete *pCur;
    }

    delete m_pBlock;
    delete m_pMetadataBlock;
    delete m_pStackBlock;
    delete m_pSerializer;
    delete m_pMetadataIds;
}

EventPipeSerializationFormat EventPipeFile::GetSerializationFormat() const
{
    LIMITED_METHOD_CONTRACT;
    return m_format;
}

bool EventPipeFile::HasErrors() const
{
    LIMITED_METHOD_CONTRACT;
    return (m_pSerializer == nullptr) || m_pSerializer->HasWriteErrors();
}

void EventPipeFile::WriteEvent(EventPipeEventInstance &instance, ULONGLONG captureThreadId, unsigned int sequenceNumber, BOOL isSortedEvent)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef DEBUG
    _ASSERTE(instance.GetTimeStamp()->QuadPart >= m_lastSortedTimestamp.QuadPart);
    if (isSortedEvent)
    {
        m_lastSortedTimestamp = *(instance.GetTimeStamp());
    }
#endif

    unsigned int stackId = 0;
    if (m_format >= EventPipeSerializationFormat::NetTraceV4)
    {
        stackId = GetStackId(instance);
    }

    // Check to see if we've seen this event type before.
    // If not, then write the event metadata to the event stream first.
    unsigned int metadataId = GetMetadataId(*instance.GetEvent());
    if(metadataId == 0)
    {
        metadataId = GenerateMetadataId();

        EventPipeEventInstance* pMetadataInstance = EventPipe::BuildEventMetadataEvent(instance, metadataId);

        WriteEventToBlock(*pMetadataInstance, 0); // metadataId=0 breaks recursion and represents the metadata event.

        SaveMetadataId(*instance.GetEvent(), metadataId);

        delete[] pMetadataInstance->GetData();
        delete pMetadataInstance;
    }

    WriteEventToBlock(instance, metadataId, captureThreadId, sequenceNumber, stackId, isSortedEvent);
}

void EventPipeFile::WriteSequencePoint(EventPipeSequencePoint* pSequencePoint)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pSequencePoint != nullptr);
    }
    CONTRACTL_END;

    if (m_format < EventPipeSerializationFormat::NetTraceV4)
    {
        // sequence points aren't used in NetPerf format
        return;
    }

    Flush(FlushAllBlocks);
    EventPipeSequencePointBlock sequencePointBlock(pSequencePoint);
    m_pSerializer->WriteObject(&sequencePointBlock);

    // stack cache resets on sequence points
    m_stackIdCounter = 0;
    for (EventPipeStackHash::Iterator pCur = m_stackHash.Begin(); pCur != m_stackHash.End(); pCur++)
    {
        delete *pCur;
    }
    m_stackHash.RemoveAll();
}

void EventPipeFile::Flush(FlushFlags flags)
{
    // Write existing buffer to the stream/file regardless of whether it is full or not.
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    // we write current blocks to the disk, whether they are full or not
    if ((m_pMetadataBlock->GetBytesWritten() != 0) && ((flags & FlushMetadataBlock) != 0))
    {
        _ASSERTE(m_format >= EventPipeSerializationFormat::NetTraceV4);
        m_pSerializer->WriteObject(m_pMetadataBlock);
        m_pMetadataBlock->Clear();
    }
    if ((m_pStackBlock->GetBytesWritten() != 0) && ((flags & FlushStackBlock) != 0))
    {
        _ASSERTE(m_format >= EventPipeSerializationFormat::NetTraceV4);
        m_pSerializer->WriteObject(m_pStackBlock);
        m_pStackBlock->Clear();
    }
    if ((m_pBlock->GetBytesWritten() != 0) && ((flags & FlushEventBlock) != 0))
    {
        m_pSerializer->WriteObject(m_pBlock);
        m_pBlock->Clear();
    }
}

void EventPipeFile::WriteEnd()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    Flush();

    // "After the last EventBlock is emitted, the stream is ended by emitting a NullReference Tag which indicates that there are no more objects in the stream to read."
    // see https://github.com/Microsoft/perfview/blob/master/src/TraceEvent/EventPipe/EventPipeFormat.md for more
    m_pSerializer->WriteTag(FastSerializerTags::NullReference);
}

void EventPipeFile::WriteEventToBlock(EventPipeEventInstance &instance, 
                                      unsigned int metadataId,
                                      ULONGLONG captureThreadId,
                                      unsigned int sequenceNumber,
                                      unsigned int stackId,
                                      BOOL isSortedEvent)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    instance.SetMetadataId(metadataId);

    // If we are flushing events we need to flush metadata and stacks as well
    // to ensure referenced metadata/stacks were written to the file before the
    // event which referenced them.
    FlushFlags flags = FlushAllBlocks;
    EventPipeEventBlockBase* pBlock = m_pBlock;
    if(metadataId == 0 && m_format >= EventPipeSerializationFormat::NetTraceV4)
    {
        flags = FlushMetadataBlock;
        pBlock = m_pMetadataBlock;
    }

    if (pBlock->WriteEvent(instance, captureThreadId, sequenceNumber, stackId, isSortedEvent))
        return; // the block is not full, we added the event and continue

    // we can't write this event to the current block (it's full)
    // so we write what we have in the block to the serializer
    Flush(flags);

    bool result = pBlock->WriteEvent(instance, captureThreadId, sequenceNumber, stackId, isSortedEvent);

    _ASSERTE(result == true); // we should never fail to add event to a clear block (if we do the max size is too small)
}

unsigned int EventPipeFile::GenerateMetadataId()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // PAL does not support 32 bit InterlockedIncrement, so we are using the LONG version and cast to int
    // https://github.com/dotnet/coreclr/blob/master/src/pal/inc/pal.h#L4159
    // it's ok because the metadataId will never be bigger than 32 bit
    return (unsigned int)InterlockedIncrement(&m_metadataIdCounter);
}

unsigned int EventPipeFile::GetMetadataId(EventPipeEvent &event)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    unsigned int metadataId;
    if(m_pMetadataIds->Lookup(&event, &metadataId))
    {
        _ASSERTE(metadataId != 0);
        return metadataId;
    }

    return 0;
}

void EventPipeFile::SaveMetadataId(EventPipeEvent &event, unsigned int metadataId)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(metadataId > 0);
    }
    CONTRACTL_END;

    // If a pre-existing metadata label exists, remove it.
    unsigned int oldId;
    if(m_pMetadataIds->Lookup(&event, &oldId))
        m_pMetadataIds->Remove(&event);

    // Add the metadata label.
    m_pMetadataIds->Add(&event, metadataId);
}

unsigned int EventPipeFile::GetStackId(EventPipeEventInstance &instance)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(m_format >= EventPipeSerializationFormat::NetTraceV4);
    }
    CONTRACTL_END;

    unsigned int stackId = 0;
    StackHashEntry* pEntry = NULL;
    StackHashKey key(instance.GetStack());
    if (NULL == (pEntry = m_stackHash.Lookup(key)))
    {
        stackId = ++m_stackIdCounter;

        pEntry = StackHashEntry::CreateNew(instance.GetStack(), stackId, key.Hash);
        if (pEntry != NULL)
        {
            EX_TRY
            {
                m_stackHash.Add(pEntry);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions);
        }

        if (m_pStackBlock->WriteStack(stackId, instance.GetStack()))
            return stackId;

        // we can't write this stack to the current block (it's full)
        // so we write what we have in the block to the serializer
        Flush(FlushStackBlock);

        bool result = m_pStackBlock->WriteStack(stackId, instance.GetStack());
        _ASSERTE(result == true); // we should never fail to add event to a clear block (if we do the max size is too small)
    }
    else
    {
        stackId = pEntry->Id;
    }

    return stackId;
}

#endif // FEATURE_PERFTRACING
