// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __EVENTPIPE_JSONFILE_H__
#define __EVENTPIPE_JSONFILE_H__

#ifdef _DEBUG
#ifdef FEATURE_PERFTRACING

#include "common.h"
#include "eventpipe.h"
#include "eventpipeeventinstance.h"
#include "fstream.h"

class EventPipeJsonFile
{
    public:
        EventPipeJsonFile(SString &outFilePath);
        ~EventPipeJsonFile();

        // Write an event instance.
        void WriteEvent(EventPipeEventInstance &instance);

        // Write an event with the specified message and stack.
        void WriteEvent(LARGE_INTEGER timeStamp, DWORD threadID, SString &message, StackContents &stackContents);

    private:

        // Write a string to the file.
        void Write(SString &str);

        // Format the input callstack for printing.
        void FormatCallStack(StackContents &stackContents, SString &resultStr);

        // The output file stream.
        CFileStream *m_pFileStream;

        // Keep track of if an error has been encountered while writing.
        bool m_writeErrorEncountered;

        // File-open timestamp for use when calculating event timestamps.
        LARGE_INTEGER m_fileOpenTimeStamp;
};

#endif // FEATURE_PERFTRACING
#endif // _DEBUG

#endif // __EVENTPIPE_JSONFILE_H__
