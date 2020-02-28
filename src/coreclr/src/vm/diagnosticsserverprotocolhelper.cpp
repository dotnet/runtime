// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "diagnosticsserverprotocolhelper.h"
#include "diagnosticsipc.h"
#include "diagnosticsprotocol.h"

const DiagnosticsServerAdvertiseCommandPayload* DiagnosticsServerAdvertiseCommandPayload::TryParse(BYTE* lpBuffer, uint16_t& BufferSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(lpBuffer != nullptr);
    }
    CONTRACTL_END;

    NewHolder<DiagnosticsServerAdvertiseCommandPayload> payload = new (nothrow) DiagnosticsServerAdvertiseCommandPayload;
    if (payload == nullptr)
    {
        // OOM
        return nullptr;
    }

    payload->incomingBuffer = lpBuffer;
    uint8_t* pBufferCursor = payload->incomingBuffer;
    uint32_t bufferLen = BufferSize;
    if (!::TryParse(pBufferCursor, bufferLen, payload->pid) ||
        !::TryParse(pBufferCursor, bufferLen, payload->hash))
    {
        return nullptr;
    }

    return payload;
}