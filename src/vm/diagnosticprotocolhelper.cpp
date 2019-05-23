// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "fastserializer.h"
#include "diagnosticprotocolhelper.h"
#include "diagnosticsipc.h"
#include "diagnosticsprotocol.h"
#if defined(FEATURE_PROFAPI_ATTACH_DETACH) && !defined(DACCESS_COMPILE)
#include "profilinghelper.h"
#include "profilinghelper.inl"
#endif // defined(FEATURE_PROFAPI_ATTACH_DETACH) && !defined(DACCESS_COMPILE)

#ifdef FEATURE_PERFTRACING

static void WriteStatus(uint64_t result, IpcStream* pStream)
{
    uint32_t nBytesWritten = 0;
    bool fSuccess = pStream->Write(&result, sizeof(result), nBytesWritten);
    if (fSuccess)
    {
        fSuccess = pStream->Flush();
    }
}

#ifdef FEATURE_PAL

void DiagnosticProtocolHelper::GenerateCoreDump(IpcStream* pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    if (pStream == nullptr)
        return;

    HRESULT hr = S_OK;

    // TODO: Read within a loop.
    uint8_t buffer[IpcStreamReadBufferSize] { };
    uint32_t nNumberOfBytesRead = 0;
    bool fSuccess = pStream->Read(buffer, sizeof(buffer), nNumberOfBytesRead);
    if (fSuccess)
    {
        // The protocol buffer is defined as:
        //   string - dumpName (UTF16)
        //   int - dumpType
        //   int - diagnostics
        // returns
        //   ulong - status
        LPCWSTR pwszDumpName;
        INT dumpType;
        INT diagnostics;

        uint8_t *pBufferCursor = buffer;
        uint32_t bufferLen = nNumberOfBytesRead;

        if (TryParseString(pBufferCursor, bufferLen, pwszDumpName) &&
            TryParse(pBufferCursor, bufferLen, dumpType) &&
            TryParse(pBufferCursor, bufferLen, diagnostics))
        {
            MAKE_UTF8PTR_FROMWIDE_NOTHROW(szDumpName, pwszDumpName);
            if (szDumpName != nullptr)
            {
                if (!PAL_GenerateCoreDump(szDumpName, dumpType, diagnostics))
                {
                    hr = E_FAIL;
                }
            }
            else 
            {
                hr = E_OUTOFMEMORY;
            }
        }
        else
        {
            hr = E_INVALIDARG;
        }
    }
    else 
    {
        hr = E_UNEXPECTED;
    }

    WriteStatus(hr, pStream);
    delete pStream;
}

#endif // FEATURE_PAL

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
void DiagnosticProtocolHelper::AttachProfiler(IpcStream *pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pStream != nullptr);
    }
    CONTRACTL_END;

    if (pStream == nullptr)
    {
        return;
    }

    HRESULT hr = S_OK;
    uint8_t buffer[IpcStreamReadBufferSize] { };
    uint32_t nNumberOfBytesRead = 0;
    uint32_t dwAttachTimeout = 0;
    CLSID profilerGuid = { };
    uint32_t cbProfilerPath = 0;
    NewArrayHolder<WCHAR> pwszProfilerPath = nullptr;
    uint32_t cbClientData = 0;
    NewArrayHolder<uint8_t> pClientData = nullptr;

    uint8_t *pBufferCursor = 0;
    uint32_t bufferLen = 0;
    uint32_t pathSize = 0;
    uint32_t bytesToRead = 0;
    uint32_t bufferBytesRead = 0;
    uint8_t *pClientDataCursor = 0;
    
    bool fSuccess = pStream->Read(buffer, sizeof(buffer), nNumberOfBytesRead);
    if (!fSuccess)
    {
        hr = E_UNEXPECTED;
        goto ErrExit;
    }

    pBufferCursor = buffer;
    bufferLen = nNumberOfBytesRead;
    if (!(TryParse(pBufferCursor, bufferLen, dwAttachTimeout) &&
        TryParse(pBufferCursor, bufferLen, profilerGuid)))
    {
        hr = E_INVALIDARG;
        goto ErrExit;
    }

    if (!TryParse(pBufferCursor, bufferLen, cbProfilerPath) ||
        cbProfilerPath > bufferLen)
    {
        // TODO: A really long path (thousands of characters) could be longer than 
        // the buffer here, if that happens we'll return E_INVALIDARG. The read
        // buffer is 8192 bytes, so has room for 4096 16 bit characters. Minus a few for
        // the header, etc, the realistic max is around 4000 characters.
        hr = E_INVALIDARG;
        goto ErrExit;
    }

    pwszProfilerPath = new (nothrow) WCHAR[cbProfilerPath];
    if (pwszProfilerPath == nullptr)
    {
        hr = E_INVALIDARG;
        goto ErrExit;
    }

    pathSize = cbProfilerPath * sizeof(WCHAR);
    memcpy(pwszProfilerPath, pBufferCursor, pathSize);
    bufferLen -= pathSize;
    pBufferCursor += pathSize;
        
    if (!TryParse(pBufferCursor, bufferLen, cbClientData))
    {
        hr = E_INVALIDARG;
        goto ErrExit;
    }

    pClientData = new (nothrow) uint8_t[cbClientData];
    if (pClientData == nullptr)
    {
        hr = E_OUTOFMEMORY;
        goto ErrExit;
    }

    bufferBytesRead = 0;
    pClientDataCursor = pClientData;
    // TODO: get rid of this ad-hoc byte[] parsing code
    while (bufferBytesRead < cbClientData)
    {
        if (bufferLen == 0)
        {
            // Client data was bigger than the buffer, need to read more
            fSuccess = pStream->Read(buffer, sizeof(buffer), nNumberOfBytesRead);
            if (!fSuccess)
            {
                hr = E_UNEXPECTED;
                goto ErrExit;
            }

            pBufferCursor = buffer;
            bufferLen = nNumberOfBytesRead;
        }

        bytesToRead = min((cbClientData - bufferBytesRead), bufferLen);
        memcpy(pClientDataCursor, pBufferCursor, bytesToRead);
        pClientDataCursor += bytesToRead;
        
        _ASSERTE(bytesToRead <= bufferLen && "bytesToRead > bufferLen means we overran the buffer");
        bufferLen -= bytesToRead;
        bufferBytesRead += bytesToRead;
    }

    _ASSERTE(bufferBytesRead == cbClientData && "bufferBytesRead > cbClientData means we read too far");

    if (cbClientData == 0)
    {
        pClientData = nullptr;
    }

    if (!g_profControlBlock.fProfControlBlockInitialized)
    {
        hr = CORPROF_E_RUNTIME_UNINITIALIZED;
        goto ErrExit;
    }

    hr = ProfilingAPIUtility::LoadProfilerForAttach(&profilerGuid,
                                                    pwszProfilerPath,
                                                    pClientData,
                                                    cbClientData,
                                                    dwAttachTimeout);
ErrExit:
    WriteStatus(hr, pStream);
    delete pStream;
}
#endif // FEATURE_PROFAPI_ATTACH_DETACH

#endif // FEATURE_PERFTRACING
