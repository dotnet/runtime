// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DataBuffer.inl
//

//
// Class code:DataBuffer provides secure access to a block of memory.
//
// ======================================================================================

#pragma once

#include "databuffer.h"

// --------------------------------------------------------------------------------------
//
// Creates empty memory block.
//
inline
DataBuffer::DataBuffer()
{
    Clear();
} // DataBuffer::DataBuffer

// --------------------------------------------------------------------------------------
//
// Creates memory block (pbData, of size cbSize).
//
inline
DataBuffer::DataBuffer(
    _In_reads_bytes_(cbSize) BYTE  *pbData,
                        UINT32 cbSize)
{
    m_pbData = pbData;
    m_cbSize = cbSize;
} // DataBuffer::DataBuffer

// --------------------------------------------------------------------------------------
//
// Creates memory block copy.
//
inline
DataBuffer::DataBuffer(
    const DataBuffer &source)
{
    m_pbData = source.m_pbData;
    m_cbSize = source.m_cbSize;
} // DataBuffer::DataBuffer

#ifdef HOST_64BIT
    #define const_pbBadFood (((BYTE *)NULL) + 0xbaadf00dbaadf00d)
#else //!HOST_64BIT
    #define const_pbBadFood (((BYTE *)NULL) + 0xbaadf00d)
#endif //!HOST_64BIT

// --------------------------------------------------------------------------------------
//
// Initializes memory block to empty data. The object could be already initialzied.
//
inline
void
DataBuffer::Clear()
{
    m_cbSize = 0;
    // For debugging purposes let's put invalid non-NULL pointer here
    INDEBUG_MD(m_pbData = const_pbBadFood);
} // DataBuffer::Clear

#undef const_pbBadFood

// --------------------------------------------------------------------------------------
//
// Initializes memory block to data (pbData, of size cbSize). The object should be empty before.
//
inline
void
DataBuffer::Init(
    _In_reads_bytes_(cbSize) BYTE  *pbData,
                        UINT32 cbSize)
{
    _ASSERTE(IsEmpty());

    m_pbData = pbData;
    m_cbSize = cbSize;
} // DataBuffer::Init

// --------------------------------------------------------------------------------------
//
// Reads data of type T without skipping the read data (returns pointer to the type in *ppTypeData).
// Returns FALSE if there's not enough data (of size T) in the blob, doesn't initialize the pointer
// *ppTypeData then.
// Returns TRUE otherwise, fills *ppTypeData with the "read" type start, but doesn't move the memory
// block (doesn't skip the "read" data).
//
template<class T>
__checkReturn
inline
BOOL
DataBuffer::PeekData(
    _Outptr_ T **ppTypeData)
{
    if (m_cbSize < sizeof(T))
    {   // There's not enough data in the memory block
        return FALSE;
    }
    // Fill the start of the "read" type
    *ppTypeData = reinterpret_cast<T *>(m_pbData);
    return TRUE;
} // DataBuffer::PeekData

// --------------------------------------------------------------------------------------
//
// Reads data of type T at offset nOffset without skipping the read data (returns pointer to the type in
// *ppTypeData).
// Returns FALSE if there's not enough data (of size T) at offset nOffset in the buffer, doesn't
// initialize the pointer *ppTypeData then.
// Returns TRUE otherwise, fills *ppTypeData with the type start, but doesn't move the memory block
// (doesn't skip any "read" data).
template<class T>
__checkReturn
inline
BOOL
DataBuffer::PeekDataAt(
                UINT32 nOffset,
    _Outptr_ T    **ppTypeData)
{
    if (m_cbSize < nOffset)
    {   // The offset is not in the memory block
        return FALSE;
    }
    if ((m_cbSize - nOffset) < sizeof(T))
    {   // The type is not fully in the memory block
        return FALSE;
    }
    // Fill the start of the "read" type
    *ppTypeData = reinterpret_cast<T *>(m_pbData + nOffset);
    return TRUE;
} // DataBuffer::PeekDataAt

// --------------------------------------------------------------------------------------
//
// Reads data of type T and skips the data (instead of reading the bytes, returns pointer to the type in
// *ppTypeData).
// Returns FALSE if there's not enough data (of size T) in the blob, doesn't initialize the pointer
// *ppTypeData then.
// Returns TRUE otherwise, fills *ppTypeData with the "read" type start and moves the memory block
// behind the "read" type.
//
template<class T>
__checkReturn
inline
BOOL
DataBuffer::GetData(
    _Outptr_ T **ppTypeData)
{
    if (m_cbSize < sizeof(T))
    {   // There's not enough data in the memory block
        return FALSE;
    }
    // Fill the start of the "read" type
    *ppTypeData = reinterpret_cast<T *>(m_pbData);
    SkipBytes_InternalInsecure(sizeof(T));
    return TRUE;
} // DataBuffer::GetData

// --------------------------------------------------------------------------------------
//
// Reads data of size cbDataSize and skips the data (instead of reading the bytes, returns pointer to
// the bytes in *ppbDataPointer).
// Returns FALSE if there's not enough data in the blob, doesn't initialize the pointer *ppbDataPointer
// then.
// Returns TRUE otherwise, fills *ppbDataPointer with the "read" data start and moves the memory block
// behind the "read" data.
//
__checkReturn
inline
BOOL
DataBuffer::GetDataOfSize(
                             UINT32 cbDataSize,
    _Out_writes_bytes_(cbDataSize) BYTE **ppbDataPointer)
{
    if (m_cbSize < cbDataSize)
    {   // There's not enough data in the memory block
        return FALSE;
    }
    // Fill the start of the "read" data
    *ppbDataPointer = m_pbData;
    SkipBytes_InternalInsecure(cbDataSize);
    return TRUE;
} // DataBuffer::GetDataOfSize

// --------------------------------------------------------------------------------------
//
// Truncates the buffer to exact size (cbSize).
// Returns FALSE if there's less than cbSize data represented.
// Returns TRUE otherwise and truncates the represented data size to cbSize.
//
__checkReturn
inline
BOOL
DataBuffer::TruncateToExactSize(UINT32 cbSize)
{
    // Check if there's at least cbSize data present
    if (m_cbSize < cbSize)
    {   // There's less than cbSize data present
        // Fail the operation
        return FALSE;
    }
    // Truncate represented data to size cbSize
    m_cbSize = cbSize;
    return TRUE;
} // DataBuffer::TruncateToExactSize

// --------------------------------------------------------------------------------------
//
// Truncates the buffer by size (cbSize).
// Returns FALSE if there's less than cbSize data represented.
// Returns TRUE otherwise and truncates the represented data size by cbSize.
//
__checkReturn
inline
BOOL
DataBuffer::TruncateBySize(UINT32 cbSize)
{
    // Check if there's at least cbSize data present
    if (m_cbSize < cbSize)
    {   // There's less than cbSize data present
        // Fail the operation
        return FALSE;
    }
    // Truncate represented data by size cbSize
    m_cbSize -= cbSize;
    return TRUE;
} // DataBuffer::TruncateBySize

// --------------------------------------------------------------------------------------
//
// Skips the buffer to size (cbSize).
// Returns FALSE if there's less than cbSize data represented.
// Returns TRUE otherwise and skips data at the beggining, so that the result has size cbSize.
//
__checkReturn
inline
BOOL
DataBuffer::SkipToExactSize(UINT32 cbSize)
{
    // Check if there's at least cbSize data present
    if (m_cbSize < cbSize)
    {   // There's less than cbSize data present
        // Fail the operation
        return FALSE;
    }
    SkipBytes_InternalInsecure(m_cbSize - cbSize);
    return TRUE;
} // DataBuffer::SkipToExactSize

// --------------------------------------------------------------------------------------
//
// Skips 'cbSize' bytes in the represented memory block. The caller is responsible for making sure that the
// represented memory block contains at least 'cbSize' bytes, otherwise there will be a security issue.
// Should be used only internally, never call it from outside of this class.
//
inline
void
DataBuffer::SkipBytes_InternalInsecure(UINT32 cbSize)
{
    // The caller is responsible for this check, just double check here
    _ASSERTE(m_cbSize >= cbSize);
    // Move the memory block by 'cbSize' bytes
    m_pbData += cbSize;
    m_cbSize -= cbSize;
} // DataBuffer::SkipBytes_InternalInsecure
