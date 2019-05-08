// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "fastserializer.h"
#include "diagnosticprotocolhelper.h"
#include "diagnosticsipc.h"
#include "diagnosticsprotocol.h"

#ifdef FEATURE_PERFTRACING
#ifdef FEATURE_PAL

static void WriteStatus(uint64_t result, IpcStream* pStream)
{
    uint32_t nBytesWritten = 0;
    bool fSuccess = pStream->Write(&result, sizeof(result), nBytesWritten);
    if (fSuccess)
    {
        fSuccess = pStream->Flush();
    }
}

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
#endif // FEATURE_PERFTRACING
