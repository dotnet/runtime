// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: perfinfo.cpp
//

#include "common.h"

#if defined(FEATURE_PERFMAP) && !defined(DACCESS_COMPILE)
#include "perfinfo.h"
#include "pal.h"

PerfInfo::PerfInfo(int pid, const char* basePath)
  : m_Stream(nullptr)
{
    LIMITED_METHOD_CONTRACT;

    SString path;
    path.Printf("%s/perfinfo-%d.map", basePath, pid);
    OpenFile(path);
}

// Logs image loads into the process' perfinfo-%d.map file
void PerfInfo::LogImage(PEAssembly* pPEAssembly, CHAR* guid)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(pPEAssembly != nullptr);
        PRECONDITION(guid != nullptr);
    } CONTRACTL_END;

    // Nothing to log if the assembly path isn't present.
    SString path{ pPEAssembly->GetPath() };
    if (path.IsEmpty())
    {
        return;
    }

    SIZE_T baseAddr = 0;
    if (pPEAssembly->IsReadyToRun())
    {
        PEImageLayout *pLoadedLayout = pPEAssembly->GetLoadedLayout();
        if (pLoadedLayout)
        {
            baseAddr = (SIZE_T)pLoadedLayout->GetBase();
        }
    }

    SString value;
    value.Printf("%s%c%s%c%p", path.GetUTF8(), sDelimiter, guid, sDelimiter, baseAddr);

    SString command{ SString::Literal, "ImageLoad" };
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
    line.Printf("%s%c%s%c\n",
            type.GetUTF8(), sDelimiter, value.GetUTF8(), sDelimiter);

    EX_TRY
    {
        const char* strLine = line.GetUTF8();
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










