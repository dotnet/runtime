// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _SIGBUILDER_H_
#define _SIGBUILDER_H_

#include "contract.h"

//
// Simple signature builder
//

class SigBuilder
{
    PBYTE m_pBuffer;
    DWORD m_dwLength;
    DWORD m_dwAllocation;

    // Preallocate space for small signatures
    BYTE m_prealloc[64];

    // Grow the buffer to get at least cbMin of free space
    void Grow(SIZE_T cbMin);

    // Ensure that the buffer has at least cbMin of free space
    FORCEINLINE void Ensure(SIZE_T cb)
    {
        if (m_dwAllocation - m_dwLength < cb)
            Grow(cb);
    }

public:
    SigBuilder()
        : m_pBuffer(m_prealloc), m_dwLength(0), m_dwAllocation(sizeof(m_prealloc))
    {
        LIMITED_METHOD_CONTRACT;
    }

    ~SigBuilder();

    SigBuilder(DWORD cbPreallocationSize);

    PVOID GetSignature(DWORD * pdwLength)
    {
        LIMITED_METHOD_CONTRACT;
        *pdwLength = m_dwLength;
        return m_pBuffer;
    }

    DWORD GetSignatureLength()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwLength;
    }

    void AppendByte(BYTE b);

    void AppendData(ULONG data);

    void AppendElementType(CorElementType etype)
    {
        WRAPPER_NO_CONTRACT;
        AppendByte(static_cast<BYTE>(etype));
    }

    void AppendToken(mdToken tk);

    void AppendPointer(void * ptr)
    {
        WRAPPER_NO_CONTRACT;
        AppendBlob(&ptr, sizeof(ptr));
    }

    void AppendBlob(const PVOID pBlob, SIZE_T cbBlob);
};

#endif // _SIGBUILDER_H_
