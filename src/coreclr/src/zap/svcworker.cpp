// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/**********************************************************************
svcworker.cpp -- logic for the runtime implementation of the native
image service.

Overview:  the runtime implementation is accessed via a local COM
server implemented in ngen.exe.  That server is simply a stub that
loads the most recent runtime and calls into the actual implementation
in this file.  There are three entrypoints in mscorwks.dll that
are called by the local service in ngen.exe:

NGenWorkerRegisterServer -- called to register ngen.exe as the current
  COM server for CLSID_CorSvcWorker
NGenWorkerUnregisterServer -- unregister ngen.exe as the current COM
  server for CLSID_CorSvcWorker
NGenWorkerEmbedding() -- called when COM invoked the COM server with
  the "-Embedding" flag.  Implements the logic for registering the class
  factory for CLSID_CorSvcWorker and controlling the lifetime of the
  COM server.
**********************************************************************/

#include "common.h"


#ifdef FEATURE_APPX
#include "AppXUtil.h"
#endif

ILocalServerLifetime *g_pLocalServerLifetime = NULL;

SvcLogger::SvcLogger()
    : pss(NULL),
      pCorSvcLogger(NULL)
{
}

inline void SvcLogger::CheckInit()
{
    if(pss == NULL)
    {
        StackSString* psstemp = new StackSString();
        StackSString* pssOrig = InterlockedCompareExchangeT(&pss, psstemp, NULL);
        if(pssOrig)
            delete psstemp;
    }
}

SvcLogger::~SvcLogger()
{
    if (pCorSvcLogger)
    {
//        pCorSvcLogger->Release();
        pCorSvcLogger = NULL;
    }
    if (pss)
        delete pss;
}

void SvcLogger::ReleaseLogger()
{
    if (pCorSvcLogger)
    {
        pCorSvcLogger->Release();
        pCorSvcLogger = NULL;
    }
}

void SvcLogger::Printf(const CHAR *format, ...)
{
    StackSString s;

    va_list args;
    va_start(args, format);
    s.VPrintf(format, args);
    va_end(args);

    if (pCorSvcLogger)
    {
        LogHelper(s);
    }
    else
    {
        wprintf( W("%s"), s.GetUnicode() );
    }
}

void SvcLogger::SvcPrintf(const CHAR *format, ...)
{
    StackSString s;

    va_list args;
    va_start(args, format);
    s.VPrintf(format, args);
    va_end(args);

    LogHelper(s);
}

void SvcLogger::Printf(const WCHAR *format, ...)
{
    StackSString s;

    va_list args;
    va_start(args, format);
    s.VPrintf(format, args);
    va_end(args);

    if (pCorSvcLogger)
    {
        LogHelper(s);
    }
    else
    {
        wprintf( W("%s"), s.GetUnicode() );
    }
}

void SvcLogger::Printf(CorSvcLogLevel logLevel, const WCHAR *format, ...)
{
    StackSString s;

    va_list args;
    va_start(args, format);
    s.VPrintf(format, args);
    va_end(args);

    if (pCorSvcLogger)
    {
        LogHelper(s, logLevel);
    }
    else
    {
        wprintf( W("%s"), s.GetUnicode());
    }
}

void SvcLogger::SvcPrintf(const WCHAR *format, ...)
{
    StackSString s;

    va_list args;
    va_start(args, format);
    s.VPrintf(format, args);
    va_end(args);

    LogHelper(s);
}

void SvcLogger::Log(const WCHAR *message, CorSvcLogLevel logLevel)
{
    LogHelper(StackSString(message), logLevel);
}

void SvcLogger::LogHelper(SString s, CorSvcLogLevel logLevel)
{
    CheckInit();
    pss->Append(s);

    // Does s contain a newline?
    SString::Iterator i = pss->Begin();
    if (pss->FindASCII(i, "\n"))
    {
        if (pCorSvcLogger)
        {
            BSTRHolder bstrHolder(::SysAllocString(pss->GetUnicode()));
            // Can't use the IfFailThrow macro here because in checked
            // builds that macros will try to log an error message
            // that will recursively return to this method.
            HRESULT hr = pCorSvcLogger->Log(logLevel, bstrHolder);
            if (FAILED(hr))
                ThrowHR(hr);
        }
        pss->Clear();
    }
}

void SvcLogger::SetSvcLogger(ICorSvcLogger *pCorSvcLoggerArg)
{
    ReleaseLogger();
    this->pCorSvcLogger = pCorSvcLoggerArg;
    if (pCorSvcLoggerArg)
    {
        pCorSvcLogger->AddRef();
    }
}

BOOL SvcLogger::HasSvcLogger()
{
    return (this->pCorSvcLogger != NULL);
}

ICorSvcLogger* SvcLogger::GetSvcLogger()
{
    return pCorSvcLogger;
}


namespace
{
    SvcLogger *g_SvcLogger = NULL;
}

// As NGen is currently single-threaded, this function is intentionally not thread safe.
// If necessary, change it into an interlocked function.
SvcLogger *GetSvcLogger()
{
    if (g_SvcLogger == NULL)
    {
        g_SvcLogger = new SvcLogger();
    }
    return g_SvcLogger;
}

BOOL HasSvcLogger()
{
    if (g_SvcLogger != NULL)
    {
        return g_SvcLogger->HasSvcLogger();
    }
    return FALSE;
}

#ifdef CROSSGEN_COMPILE
void SetSvcLogger(ICorSvcLogger *pCorSvcLogger)
{
    GetSvcLogger()->SetSvcLogger(pCorSvcLogger);
}
#endif

