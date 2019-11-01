// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipejsonfile.h"
#include "typestring.h"

#ifdef _DEBUG
#ifdef FEATURE_PERFTRACING

EventPipeJsonFile::EventPipeJsonFile(SString &outFilePath)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_writeErrorEncountered = false;
    m_pFileStream = new CFileStream();
    if(FAILED(m_pFileStream->OpenForWrite(outFilePath)))
    {
        delete(m_pFileStream);
        m_pFileStream = NULL;
        return;
    }

    QueryPerformanceCounter(&m_fileOpenTimeStamp);

    SString fileStart(W("{\n\"StackSource\" : {\n\"Samples\" : [\n"));
    Write(fileStart);
}

EventPipeJsonFile::~EventPipeJsonFile()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(m_pFileStream != NULL)
    {
        if(!m_writeErrorEncountered)
        {
            SString closingString(W("]}}"));
            Write(closingString);
        }

        delete(m_pFileStream);
        m_pFileStream = NULL;
    }
}

void EventPipeJsonFile::WriteEvent(EventPipeEventInstance &instance)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END; 

    instance.SerializeToJsonFile(this);
}

void EventPipeJsonFile::WriteEvent(LARGE_INTEGER timeStamp, DWORD threadID, SString &message, StackContents &stackContents)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END; 

    if(m_pFileStream == NULL || m_writeErrorEncountered)
    {
        return;
    }

    // Format the call stack.
    SString strCallStack;
    FormatCallStack(stackContents, strCallStack);

    // Convert the timestamp from a QPC value to a trace-relative timestamp.
    double millisecondsSinceTraceStart = 0.0;
    if(timeStamp.QuadPart != m_fileOpenTimeStamp.QuadPart)
    {
        LARGE_INTEGER elapsedNanoseconds;
        elapsedNanoseconds.QuadPart = timeStamp.QuadPart - m_fileOpenTimeStamp.QuadPart;
        millisecondsSinceTraceStart = elapsedNanoseconds.QuadPart / 1000000.0;
    }

    StackScratchBuffer scratch;
    SString threadFrame;
    threadFrame.Printf("Thread (%d)", threadID);
    SString event;
    event.Printf("{\"Time\" : \"%f\", \"Metric\" : \"1\",\n\"Stack\": [\n\"%s\",\n%s\"%s\"]},", millisecondsSinceTraceStart, message.GetANSI(scratch), strCallStack.GetANSI(scratch), threadFrame.GetANSI(scratch));
    Write(event);
}

void EventPipeJsonFile::Write(SString &str)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackScratchBuffer scratch;
    const char * charStr = str.GetANSI(scratch);

    EX_TRY
    {
        ULONG inCount = str.GetCount();
        ULONG outCount;

        m_pFileStream->Write(charStr, inCount, &outCount);

        if(inCount != outCount)
        {
            m_writeErrorEncountered = true;
        }
    }
    EX_CATCH
    {
        m_writeErrorEncountered = true;
    }
    EX_END_CATCH(SwallowAllExceptions);
}

void EventPipeJsonFile::FormatCallStack(StackContents &stackContents, SString &resultStr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END; 

    StackScratchBuffer scratch;
    SString frameStr;
    
    for(unsigned int i=0; i<stackContents.GetLength(); i++)
    {
        // Get the method declaration string.
        MethodDesc *pMethod = stackContents.GetMethod(i);
        _ASSERTE(pMethod != NULL);

        SString mAssemblyName;
        mAssemblyName.SetUTF8(pMethod->GetLoaderModule()->GetAssembly()->GetSimpleName());
        SString fullName;
        TypeString::AppendMethodInternal(
            fullName,
            pMethod,
            TypeString::FormatNamespace | TypeString::FormatSignature);

        frameStr.Printf("\"%s!%s\",\n", mAssemblyName.GetANSI(scratch), fullName.GetANSI(scratch));
        resultStr.Append(frameStr);
    }
}

#endif // _DEBUG
#endif // FEATURE_PERFTRACING
