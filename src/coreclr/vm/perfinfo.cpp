// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: perfinfo.cpp
//

#include "common.h"

#if defined(FEATURE_PERFMAP) && !defined(DACCESS_COMPILE)
#include "perfinfo.h"
#include "pal.h"

PerfInfo::PerfInfo(int pid)
  : m_Stream(nullptr)
{
    LIMITED_METHOD_CONTRACT;

    SString tempPath;
    if (!WszGetTempPath(tempPath))
    {
        return;
    }

    SString path;
    path.Printf("%Sperfinfo-%d.map", tempPath.GetUnicode(), pid);
    OpenFile(path);
}

// Logs image loads into the process' perfinfo-%d.map file
void PerfInfo::LogImage(PEFile* pFile, WCHAR* guid)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(pFile != nullptr);
        PRECONDITION(guid != nullptr);
    } CONTRACTL_END;

    SString value;
    const SString& path = pFile->GetPath();
    if (path.IsEmpty())
    {
        return;
    }

    SIZE_T baseAddr = 0;
    if (pFile->IsReadyToRun())
    {
        PEImageLayout *pLoadedLayout = pFile->GetLoaded();
        if (pLoadedLayout)
        {
            baseAddr = (SIZE_T)pLoadedLayout->GetBase();
        }
    }

    value.Printf("%S%c%S%c%p", path.GetUnicode(), sDelimiter, guid, sDelimiter, baseAddr);

    SString command;
    command.Printf("%s", "ImageLoad");
    WriteLine(command, value);

}

// Writes a command line, with "type" being the type of command, with "value" as the command's corresponding instructions/values. This is to be used to log specific information, e.g. LogImage
void PerfInfo::WriteLine(SString& type, SString& value)
{

    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    if (m_Stream == nullptr)
    {
        return;
    }

    SString line;
    line.Printf("%S%c%S%c\n",
            type.GetUnicode(), sDelimiter, value.GetUnicode(), sDelimiter);

    EX_TRY
    {
        StackScratchBuffer scratch;
        const char* strLine = line.GetANSI(scratch);
        ULONG inCount = line.GetCount();
        ULONG outCount;

        m_Stream->Write(strLine, inCount, &outCount);

        if (inCount != outCount)
        {
            // error encountered
        }
    }
    EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
}

// Opens a file ready to be written in.
void PerfInfo::OpenFile(SString& path)
{
    STANDARD_VM_CONTRACT;

    m_Stream = new (nothrow) CFileStream();

    if (m_Stream != nullptr)
    {
        HRESULT hr = m_Stream->OpenForWrite(path.GetUnicode());
        if (FAILED(hr))
        {
            delete m_Stream;
            m_Stream = nullptr;
        }
    }
}

PerfInfo::~PerfInfo()
{
    LIMITED_METHOD_CONTRACT;

    delete m_Stream;
    m_Stream = nullptr;
}


#endif










