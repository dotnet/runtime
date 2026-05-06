// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// DDMarshalUtil.cpp


#ifndef _DDMarshal_Util_h
#define _DDMarshal_Util_h

#include "dacdbiinterface.h"
typedef IDacDbiInterface::StackWalkHandle StackWalkHandle;
typedef IDacDbiInterface::HeapWalkHandle HeapWalkHandle;
typedef IDacDbiInterface::IStringHolder IStringHolder;

#include "stringcopyholder.h"

// @dbgtodo  Mac - cleanup the buffer classes here. (are there pre-existing classes we could use instead?)
// These ultimately get included in the signature for IDacDbiMarshalStub::DoRequest.
// Key is that this helps serialize remote sized structures like IStringHolder and DacDbiArrayList<T>.
class BaseBuffer
{
public:
    BaseBuffer()
    {
        m_idx = 0;
        m_pBuffer = NULL;
        m_size = 0;
    }

protected:
    DWORD m_idx;
    DWORD m_size;
    BYTE * m_pBuffer;
};

class WriteBuffer : public BaseBuffer
{
public:
    friend class ReadBuffer;

    WriteBuffer()
    {
        // @dbgtodo  Mac - do something smarter than always allocate 1000 bytes.
        m_size = 1000;
        m_pBuffer = new BYTE[m_size];
    }
    ~WriteBuffer()
    {
        if (m_pBuffer != NULL)
        {
            delete [] m_pBuffer;
            m_pBuffer = NULL;
        }
    }

    // Write to the buffer, and grow if needed.
    void WriteBlob(const void * pData, DWORD cbLength)
    {
        _ASSERTE(m_pBuffer != NULL);
        EnsureSize(cbLength);
        memcpy(&m_pBuffer[m_idx], pData, cbLength);
        m_idx += cbLength;
    }

    void EnsureSize(DWORD cbLength)
    {
        DWORD cbSizeNeeded = m_idx + cbLength;
        _ASSERTE(cbSizeNeeded <= 128000); // sanity checking...
        while(cbSizeNeeded >= m_size)
        {
            DWORD cbNewSize = (m_size * 3) / 2; // grow by 1.5
            BYTE * pNewBuffer = new BYTE[cbNewSize];
            memcpy(pNewBuffer, m_pBuffer, m_idx);

            _ASSERTE(m_pBuffer != NULL);
            delete [] m_pBuffer;

            m_size = cbNewSize;
            m_pBuffer = pNewBuffer;
        }
    }

    void WriteString(const WCHAR * pString)
    {
        bool fIsNull = (pString == NULL ? true : false);
        EnsureSize(sizeof(fIsNull));
        memcpy(&m_pBuffer[m_idx], &fIsNull, sizeof(fIsNull));
        m_idx += sizeof(fIsNull);

        if (!fIsNull)
        {
            _ASSERTE(pString != NULL);
            DWORD len = (DWORD) u16_strlen(pString);
            DWORD cbCopy = (len + 1) * sizeof(WCHAR);

            EnsureSize(cbCopy);
            memcpy(&m_pBuffer[m_idx], pString, cbCopy);
            m_idx += cbCopy;
        }
    }

    // Gets access to the raw buffer - does not transfer ownership of the memory
    void GetRawPtr(PBYTE * ppBuffer, DWORD * pcbUsed)
    {
        *ppBuffer = m_pBuffer;
        *pcbUsed = m_idx;
    }
};

// Read-only stream access to memory blob.
class ReadBuffer : public BaseBuffer
{
public:
    ReadBuffer()
    {
        m_fDeleteOnClose = false;
    }
    ~ReadBuffer()
    {
        if (m_fDeleteOnClose)
        {
            delete [] m_pBuffer;
        }
    }

    // Create on existing stream
    void Open(BYTE * pStream, DWORD cbLength)
    {
        _ASSERTE(m_pBuffer == NULL);
        _ASSERTE(m_idx == 0);
        m_pBuffer = pStream;
        m_size = cbLength;
    }
    void OpenAndOwn(BYTE * pStream, DWORD cbLength)
    {
        Open(pStream, cbLength);
        m_fDeleteOnClose = true;
    }

    // Get a reader for the range written by a Writer
    // This steal's the writer's buffer. The Writer object is dead after this.
    void Open(WriteBuffer * pBuffer)
    {
        _ASSERTE(m_pBuffer == NULL);
        _ASSERTE(m_idx == 0);
        m_size = pBuffer->m_idx;
        m_pBuffer = pBuffer->m_pBuffer;


        pBuffer->m_pBuffer = NULL;
        m_fDeleteOnClose = true;
    }

    bool IsAtEnd()
    {
        return (m_idx == m_size);
    }
    void ReadBlob(void * pData, DWORD cbLength)
    {
        _ASSERTE(m_idx + cbLength <= m_size);
        memcpy(pData, &m_pBuffer[m_idx], cbLength);
        m_idx += cbLength;
    }
    DWORD Length()
    {
        return m_size;
    }

    // Return a pointer to the string and mvoe the index.
    const WCHAR * ReadString()
    {
        bool fIsNull = *reinterpret_cast<bool *>(&m_pBuffer[m_idx]);
        m_idx += sizeof(fIsNull);

        if (fIsNull)
        {
            return NULL;
        }
        else
        {
            const WCHAR * pString = (WCHAR*) &m_pBuffer[m_idx];
            DWORD len = (DWORD) u16_strlen(pString);
            m_idx += (len + 1) * sizeof(WCHAR); // skip past null
            _ASSERTE(m_idx <= m_size);
            return pString;
        }
    }

protected:
    bool m_fDeleteOnClose;
};



//
// Writers
//
template<class T> inline
void WriteToBuffer(WriteBuffer * p, T & data)
{
    p->WriteBlob(&data, sizeof(T));
}

inline
void WriteToBuffer(WriteBuffer * p, StackWalkHandle & h)
{
    p->WriteBlob(&h, sizeof(StackWalkHandle));
}

inline
void WriteCookie(WriteBuffer * p, BYTE cookie)
{
#if _DEBUG
    WriteToBuffer(p, cookie);
#endif
}


template<class T> inline
void WriteToBuffer(WriteBuffer * p, T * pData)
{
    p->WriteBlob(pData, sizeof(T));
}

inline
void WriteToBuffer(WriteBuffer * p, StringCopyHolder * pString)
{
    const WCHAR * pData = NULL;
    if (pString->IsSet())
    {
        pData = *pString; // gets raw data
    }
    p->WriteString(pData);
    WriteCookie(p, 0x1F);
}

template<class T> inline
void WriteToBuffer(WriteBuffer * p, DacDbiArrayList<T> * pList)
{
    _ASSERTE(pList != NULL);
    WriteCookie(p, 0xCD);

    int count = pList->Count();
    WriteToBuffer(p, count);

    if (count == 0) return;

    // Write raw data.
    for(int i = 0; i < count; i++)
    {
        const T * pElement = &((*pList)[i]);
        WriteToBuffer(p, pElement);
    }
    WriteCookie(p, 0xAB);
}

template<class T> inline
void WriteToBuffer(WriteBuffer * p, DacDbiArrayList<T> & list)
{
    WriteToBuffer(p, &list);
}

inline
void WriteToBuffer(WriteBuffer * p, NativeVarData * pData)
{
    WriteCookie(p, 0xD1);
    p->WriteBlob(pData, sizeof(NativeVarData));
    WriteToBuffer(p, pData->m_offsetInfo);
}

inline
void WriteToBuffer(WriteBuffer * p, SequencePoints  * pData)
{
    WriteCookie(p, 0xD2);
    p->WriteBlob(pData, sizeof(SequencePoints));
    WriteToBuffer(p, pData->m_map);
}
inline
void WriteToBuffer(WriteBuffer * p, ClassInfo * pData)
{
    WriteCookie(p, 0xD3);
    p->WriteBlob(pData, sizeof(ClassInfo));
    WriteToBuffer(p, pData->m_fieldList);
}

//-----------------------------------------------------------------------------
//
// Readers
//
template<class T> inline
void ReadFromBuffer(ReadBuffer * p, T & data)
{
    p->ReadBlob(&data, sizeof(T));
}

inline
void ReadCookie(ReadBuffer * p, BYTE cookieExpected)
{
#if _DEBUG
    BYTE cookie;
    ReadFromBuffer(p, cookie);
    _ASSERTE(cookie = cookieExpected);
#endif
}


inline
void ReadFromBuffer(ReadBuffer * p, StackWalkHandle & h)
{
    p->ReadBlob(&h, sizeof(StackWalkHandle));
}

template<class T> inline
void ReadFromBuffer(ReadBuffer * p, T * pData)
{
    // Used to copy-back a By-ref / out parameter
    p->ReadBlob(pData, sizeof(T));
}

inline
void ReadFromBuffer(ReadBuffer * p, IStringHolder * pString)
{
    const WCHAR *pData = p->ReadString();
    // AssignCopy() can handle a NULL string.
    pString->AssignCopy(pData);
    ReadCookie(p, 0x1F);
}

template<class T> inline
void ReadFromBuffer(ReadBuffer * p, DacDbiArrayList<T> * pList)
{
    _ASSERTE(pList != NULL);

    ReadCookie(p, 0xCD);

    // Alloc() will attempt to free the old pointer.
    // if this was blit copied, the pointer is trashed.  So we need to safely clear that
    // pointer to prepare it to be copied.
    pList->PrepareForDeserialize();

    int count;
    ReadFromBuffer(p, count);

    pList->Alloc(count);
    if (count == 0)
    {
        return;
    }

    // Read raw data.
    for(int i = 0; i < count; i++)
    {
        T * pElement = &((*pList)[i]);
        ReadFromBuffer(p, pElement);
    }
    ReadCookie(p, 0xAB);
}

template<class T> inline
void ReadFromBuffer(ReadBuffer * p, DacDbiArrayList<T> & list)
{
    ReadFromBuffer(p, &list);
}

inline
void ReadFromBuffer(ReadBuffer * p, NativeVarData * pData)
{
    ReadCookie(p, 0xD1);
    p->ReadBlob(pData, sizeof(NativeVarData));
    ReadFromBuffer(p, &pData->m_offsetInfo);
}

inline
void ReadFromBuffer(ReadBuffer * p, SequencePoints  * pData)
{
    ReadCookie(p, 0xD2);
    p->ReadBlob(pData, sizeof(SequencePoints));
    ReadFromBuffer(p, &pData->m_map);
}

inline
void ReadFromBuffer(ReadBuffer * p, ClassInfo * pData)
{
    ReadCookie(p, 0xD3);
    p->ReadBlob(pData, sizeof(ClassInfo));
    ReadFromBuffer(p, &pData->m_fieldList);
}




#endif  // _DDMarshal_Util_h

