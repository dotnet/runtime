// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "stdafx.h"
#include "sigbuilder.h"
#include "ex.h"

void SigBuilder::AppendByte(BYTE b)
{
    STANDARD_VM_CONTRACT;

    Ensure(1);
    m_pBuffer[m_dwLength++] = b;
}

void SigBuilder::AppendData(ULONG data)
{
    STANDARD_VM_CONTRACT;

    //
    // Inlined logic from CorSigCompressData
    //

    if (data <= 0x7F)
    {
        Ensure(1);
        m_pBuffer[m_dwLength++] = BYTE(data);
        return;
    }

    if (data <= 0x3FFF)
    {
        Ensure(2);

        DWORD dwLength = m_dwLength;
        BYTE * pBuffer = m_pBuffer;

        pBuffer[dwLength] = BYTE((data >> 8) | 0x80);
        pBuffer[dwLength+1] = BYTE(data);

        m_dwLength = dwLength + 2;
        return;
    }

    if (data <= 0x1FFFFFFF)
    {
        Ensure(4);

        DWORD dwLength = m_dwLength;
        BYTE * pBuffer = m_pBuffer;

        pBuffer[dwLength] = BYTE((data >> 24) | 0xC0);
        pBuffer[dwLength+1] = BYTE(data >> 16);
        pBuffer[dwLength+2] = BYTE(data >> 8);
        pBuffer[dwLength+3] = BYTE(data);

        m_dwLength = dwLength + 4;
        return;
    }

    // We currently can only represent to 0x1FFFFFFF.
    ThrowHR(COR_E_OVERFLOW);
}

void SigBuilder::AppendToken(mdToken tk)
{
    STANDARD_VM_CONTRACT;

    //
    // Inlined logic from CorSigCompressToken
    //

    RID         rid = RidFromToken(tk);
    ULONG32     ulTyp = TypeFromToken(tk);

    _ASSERTE(rid <= 0x3FFFFFF);
    rid = (rid << 2);

    // TypeDef is encoded with low bits 00
    // TypeRef is encoded with low bits 01
    // TypeSpec is encoded with low bits 10
    // BaseType is encoded with low bit 11
    //
    if (ulTyp == CorSigDecodeTokenType(0))
    {
        // make the last two bits 00
        // nothing to do
    }
    else if (ulTyp == CorSigDecodeTokenType(1))
    {
        // make the last two bits 01
        rid |= 0x1;
    }
    else if (ulTyp == CorSigDecodeTokenType(2))
    {
        // make last two bits 0
        rid |= 0x2;
    }
    else if (ulTyp == CorSigDecodeTokenType(3))
    {
        rid |= 0x3;
    }
    else
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    AppendData(rid);
}

void SigBuilder::AppendBlob(const PVOID pBlob, SIZE_T cbBlob)
{
    STANDARD_VM_CONTRACT;

    Ensure(cbBlob);
    memcpy(m_pBuffer + m_dwLength, pBlob, cbBlob);
    m_dwLength += (DWORD)cbBlob;
}

void SigBuilder::Grow(SIZE_T cbMin)
{
    STANDARD_VM_CONTRACT;

    DWORD dwNewAllocation = max(m_dwLength + (DWORD)cbMin, 2 * m_dwAllocation);

    // Overflow checks
    if (dwNewAllocation < m_dwLength || (dwNewAllocation - m_dwLength) < cbMin)
        ThrowOutOfMemory();

    BYTE * pNewAllocation = new BYTE[dwNewAllocation];
    memcpy(pNewAllocation, m_pBuffer, m_dwLength);

    BYTE * pOldAllocation = m_pBuffer;

    m_pBuffer = pNewAllocation;
    m_dwAllocation = dwNewAllocation;

    if (pOldAllocation != m_prealloc)
        delete [] pOldAllocation;
}

SigBuilder::~SigBuilder()
{
    if (m_pBuffer != m_prealloc)
        delete [] m_pBuffer;
}

SigBuilder::SigBuilder(DWORD cbPreallocationSize)
{
    STANDARD_VM_CONTRACT;

    m_dwLength = 0;
    if (cbPreallocationSize <= sizeof(m_prealloc))
    {
        m_pBuffer = m_prealloc;
        m_dwAllocation = sizeof(m_prealloc);
    }
    else
    {
        m_pBuffer = new BYTE[cbPreallocationSize];
        m_dwAllocation = cbPreallocationSize;
    }
}
