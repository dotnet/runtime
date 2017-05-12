// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef __EVENTPIPE_FILE_H__
#define __EVENTPIPE_FILE_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipe.h"
#include "eventpipeeventinstance.h"
#include "fastserializableobject.h"
#include "fastserializer.h"

class EventPipeFile : public FastSerializableObject
{
    public:

        EventPipeFile(SString &outputFilePath
#ifdef _DEBUG
            ,
            bool lockOnWrite = false
#endif // _DEBUG
        );
        ~EventPipeFile();

        // Write an event to the file.
        void WriteEvent(EventPipeEventInstance &instance);

        // Serialize this object.
        // Not supported - this is the entry object for the trace,
        // which means that the contents hasn't yet been created.
        void FastSerialize(FastSerializer *pSerializer)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(!"This function should never be called!");
        }

        // Get the type name of this object.
        const char* GetTypeName()
        {
            LIMITED_METHOD_CONTRACT;
            return "Microsoft.DotNet.Runtime.EventPipeFile";
        }

    private:

        // Get the metadata address in the file for an event.
        // The return value can be written into the file as a back-pointer to the event metadata.
        StreamLabel GetMetadataLabel(EventPipeEvent &event);

        // Save the metadata address in the file for an event.
        void SaveMetadataLabel(EventPipeEvent &event, StreamLabel label);

        // The object responsible for serialization.
        FastSerializer *m_pSerializer;

        // The system time when the file was opened.
        SYSTEMTIME m_fileOpenSystemTime;

        // The timestamp when the file was opened.  Used for calculating file-relative timestamps.
        LARGE_INTEGER m_fileOpenTimeStamp;

        // The frequency of the timestamps used for this file.
        LARGE_INTEGER m_timeStampFrequency;

        // The forward reference index that marks the beginning of the event stream.
        unsigned int m_beginEventsForwardReferenceIndex;

        // The serialization which is responsible for making sure only a single event
        // or block of events gets written to the file at once.
        SpinLock m_serializationLock;

        // Hashtable of metadata labels.
        MapSHashWithRemove<EventPipeEvent*, StreamLabel> *m_pMetadataLabels;

#ifdef _DEBUG
        bool m_lockOnWrite;
#endif // _DEBUG
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_FILE_H__
