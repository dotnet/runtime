// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DataBlob.inl
//

//
// Class code:MetaData::DataBlob provides secure access to a block of memory from MetaData (i.e. with fixed
// endianness).
//
// ======================================================================================

#pragma once

#include "datablob.h"
#include "compressedinteger.h"

#include "debug_metadata.h"

namespace MetaData
{

// --------------------------------------------------------------------------------------
//
// Creates empty memory block.
//
inline
DataBlob::DataBlob()
{
    Clear();
} // DataBlob::DataBlob

// --------------------------------------------------------------------------------------
//
// Creates memory block (pbData, of size cbSize).
//
inline
DataBlob::DataBlob(
    _In_reads_bytes_(cbSize) BYTE  *pbData,
                        UINT32 cbSize)
{
    m_pbData = pbData;
    m_cbSize = cbSize;
} // DataBlob::DataBlob

// --------------------------------------------------------------------------------------
//
// Creates memory block copy.
//
inline
DataBlob::DataBlob(
    const DataBlob &source)
{
    m_pbData = source.m_pbData;
    m_cbSize = source.m_cbSize;
} // DataBlob::DataBlob

#ifdef HOST_64BIT
    #define const_pbBadFood (((BYTE *)NULL) + 0xbaadf00dbaadf00d)
#else //!HOST_64BIT
    #define const_pbBadFood (((BYTE *)NULL) + 0xbaadf00d)
#endif //!HOST_64BIT

// --------------------------------------------------------------------------------------
//
// Initializes memory block to empty data. The object could be already initialized.
//
inline
void
DataBlob::Clear()
{
    m_cbSize = 0;
    // For debugging purposes let's put invalid non-NULL pointer here
    INDEBUG_MD(m_pbData = const_pbBadFood);
} // DataBlob::Clear

#undef const_pbBadFood

// --------------------------------------------------------------------------------------
//
// Initializes memory block to data (pbData, of size cbSize). The object should be empty before.
//
inline
void
DataBlob::Init(
    _In_reads_bytes_(cbSize) BYTE  *pbData,
                        UINT32 cbSize)
{
    m_pbData = pbData;
    m_cbSize = cbSize;
} // DataBlob::Init

// --------------------------------------------------------------------------------------
//
// #PeekUx_Functions
//
// Reads the U1/U2/U4/U8 from the data blob without skipping the read data.
// Returns FALSE if there's not enough data in the blob, doesn't initialize the value '*pnValue' then.
// Returns TRUE otherwise, fills *pnValue, but doesn't move the memory block (doesn't skip the read data).
//

// --------------------------------------------------------------------------------------
//
// See code:#PeekUx_Functions above.
//
__checkReturn
_Success_(return)
inline
BOOL
DataBlob::PeekU1(_Out_ BYTE *pnValue) const
{
    if (m_cbSize < sizeof(BYTE))
    {
        return FALSE;
    }
    *pnValue = *m_pbData;
    return TRUE;
} // DataBlob::PeekU1

// --------------------------------------------------------------------------------------
//
// See code:#PeekUx_Functions above.
//
__checkReturn
_Success_(return)
inline
BOOL
DataBlob::PeekU2(_Out_ UINT16 *pnValue) const
{
    if (m_cbSize < sizeof(UINT16))
    {
        return FALSE;
    }
    *pnValue = GET_UNALIGNED_VAL16(m_pbData);
    return TRUE;
} // DataBlob::PeekU2

// --------------------------------------------------------------------------------------
//
// See code:#PeekUx_Functions above.
//
__checkReturn
_Success_(return)
inline
BOOL
DataBlob::PeekU4(_Out_ UINT32 *pnValue) const
{
    if (m_cbSize < sizeof(UINT32))
    {
        return FALSE;
    }
    *pnValue = GET_UNALIGNED_VAL32(m_pbData);
    return TRUE;
} // DataBlob::PeekU4

// --------------------------------------------------------------------------------------
//
// See code:#PeekUx_Functions above.
//
__checkReturn
_Success_(return)
inline
BOOL
DataBlob::PeekU8(_Out_ UINT64 *pnValue) const
{
    if (m_cbSize < sizeof(UINT64))
    {
        return FALSE;
    }
    *pnValue = GET_UNALIGNED_VAL64(m_pbData);
    return TRUE;
} // DataBlob::PeekU8

// --------------------------------------------------------------------------------------
//
// #GetUx_Functions
//
// Reads the U1/U2/U4/U8 from the data blob and skips the read data.
// Returns FALSE if there's not enough data in the blob, doesn't initialize the value '*pnValue' then.
// Returns TRUE otherwise, fills *pnValue and moves the memory block behind the read data.
//

// --------------------------------------------------------------------------------------
//
// See code:#GetUx_Functions above.
//
__checkReturn
_Success_(return)
inline
BOOL
DataBlob::GetU1(_Out_ BYTE *pnValue)
{
    if (m_cbSize < sizeof(BYTE))
    {
        return FALSE;
    }
    *pnValue = *m_pbData;
    SkipBytes_InternalInsecure(sizeof(BYTE));
    return TRUE;
} // DataBlob::GetU1

// --------------------------------------------------------------------------------------
//
// See code:#GetUx_Functions above.
//
__checkReturn
_Success_(return)
inline
BOOL
DataBlob::GetU2(_Out_ UINT16 *pnValue)
{
    if (m_cbSize < sizeof(UINT16))
    {
        return FALSE;
    }
    *pnValue = GET_UNALIGNED_VAL16(m_pbData);
    SkipBytes_InternalInsecure(sizeof(UINT16));
    return TRUE;
} // DataBlob::GetU2

// --------------------------------------------------------------------------------------
//
// See code:#GetUx_Functions above.
//
__checkReturn
_Success_(return)
inline
BOOL
DataBlob::GetU4(_Out_ UINT32 *pnValue)
{
    if (m_cbSize < sizeof(UINT32))
    {
        return FALSE;
    }
    *pnValue = GET_UNALIGNED_VAL32(m_pbData);
    SkipBytes_InternalInsecure(sizeof(UINT32));
    return TRUE;
} // DataBlob::GetU4

// --------------------------------------------------------------------------------------
//
// See code:#GetUx_Functions above.
//
__checkReturn
_Success_(return)
inline
BOOL
DataBlob::GetU8(_Out_ UINT64 *pnValue)
{
    if (m_cbSize < sizeof(UINT64))
    {
        return FALSE;
    }
    *pnValue = GET_UNALIGNED_VAL64(m_pbData);
    SkipBytes_InternalInsecure(sizeof(UINT64));
    return TRUE;
} // DataBlob::GetU8

// --------------------------------------------------------------------------------------
//
// Reads compressed integer (1, 2 or 4 bytes of format code:CompressedInteger#Format) from the data blob
// and skips the read data.
// Returns FALSE if there's not enough data in the blob or the compression is invalid (starts with byte
// 111? ????), doesn't initialize the value *pnValue then.
// Returns TRUE otherwise, fills *pnValue and moves the memory block behind the read data.
//
__checkReturn
inline
BOOL
DataBlob::GetCompressedU(_Out_ UINT32 *pnValue)
{
    UINT32 cbCompressedValueSize_Ignore;
    return GetCompressedU(pnValue, &cbCompressedValueSize_Ignore);
} // DataBlob::GetCompressedU

// --------------------------------------------------------------------------------------
//
// Reads compressed integer (1, 2 or 4 bytes of format code:CompressedInteger#Format - returns the size
// in *pcbCompressedValueSize) from the data blob without skipping the read data.
// Returns FALSE if there's not enough data in the blob or the compression is invalid (starts with byte
// 111? ????), doesn't initialize the value *pnValue nor the size of the compressed value
// *pcbCompressedValueSize then.
// Returns TRUE otherwise, fills *pnValue and *pcbCompressedValueSize (with number 1,2 or 4), but
// doesn't move the memory block (doesn't skip the read data).
//
__checkReturn
_Success_(return)
inline
BOOL
DataBlob::PeekCompressedU(
    _Out_ UINT32 *pnValue,
    _Out_ UINT32 *pcbCompressedValueSize)
{
    // This algorithm has to be in sync with code:CompressedInteger#Format encoding definition.
    //
    // Note that this algorithm accepts technically invalid encodings, e.g.
    // encoding of value 0 is accepted as 0000 0000 (0x00, valid) and 1000 0000 0000 000 (0x8000, invalid).

    // Is there at least 1 byte?
    if (m_cbSize < 1)
    {   // The data blob is empty, there's not compressed integer stored
        return FALSE;
    }
    if ((*m_pbData & 0x80) == 0x00)
    {   // 0??? ????
        // The value is compressed into 1 byte
        *pnValue = (UINT32)(*m_pbData);
        *pcbCompressedValueSize = 1;
        return TRUE;
    }
    // 1??? ????

    if ((*m_pbData & 0x40) == 0x00)
    {   // 10?? ????
        // The value is compressed into 2 bytes
        if (m_cbSize < 2)
        {   // The data blob is too short and doesn't contain 2 bytes needed for storing compressed integer
            return FALSE;
        }
        *pnValue =
            ((*m_pbData & 0x3f) << 8) |
            *(m_pbData + 1);
        *pcbCompressedValueSize = 2;
        return TRUE;
    }
    // 11?? ????

    if ((*m_pbData & 0x20) == 0x00)
    {   // 110? ????
        // The value is compressed into 4 bytes
        if (m_cbSize < 4)
        {   // The data blob is too short and doesn't contain 4 bytes needed for storing compressed integer
            return FALSE;
        }
        *pnValue =
            ((*m_pbData & 0x1f) << 24) |
            (*(m_pbData + 1) << 16) |
            (*(m_pbData + 2) << 8) |
            *(m_pbData + 3);
        *pcbCompressedValueSize = 4;
        return TRUE;
    }
    // 111? ????
    // Invalid encoding of the compressed integer
    return FALSE;
} // DataBlob::PeekCompressedU

// --------------------------------------------------------------------------------------
//
// Reads compressed integer (1, 2 or 4 bytes of format code:CompressedInteger#Format - returns the size
// in *pcbCompressedValueSize) from the data blob and skips the read data.
// Returns FALSE if there's not enough data in the blob or the compression is invalid (starts with byte
// 111? ????), doesn't initialize the value *pnValue nor the size of the compressed value
// *pcbCompressedValueSize then.
// Returns TRUE otherwise, fills *pnValue and *pcbCompressedValueSize (with number 1,2 or 4) and moves
// the memory block behind the read data.
//
__checkReturn
inline
BOOL
DataBlob::GetCompressedU(
    _Out_ UINT32 *pnValue,
    _Out_ UINT32 *pcbCompressedValueSize)
{
    // Read the compressed integer from withou skipping the read data
    BOOL fReadResult = PeekCompressedU(
        pnValue,
        pcbCompressedValueSize);
    // Was the compressed integer read?
    if (fReadResult)
    {   // The compressed integer was read
        // Skip the read data
        SkipBytes_InternalInsecure(*pcbCompressedValueSize);
    }
    // Return the (original) read result
    return fReadResult;
} // DataBlob::GetCompressedU

// --------------------------------------------------------------------------------------
//
// Reads data of size cbDataSize and skips the data (instead of reading the bytes, returns the data as
// *pData).
// Returns FALSE if there's not enough data in the blob, clears *pData then.
// Returns TRUE otherwise, fills *pData with the "read" data and moves the memory block behind the
// "read" data.
//
__checkReturn
inline
BOOL
DataBlob::GetDataOfSize(
          UINT32    cbDataSize,
    _Out_ DataBlob *pData)
{
    if (m_cbSize < cbDataSize)
    {   // There's not enough data in the memory block
        pData->Clear();
        return FALSE;
    }
    // Fill the "read" data
    pData->Init(m_pbData, cbDataSize);
    SkipBytes_InternalInsecure(cbDataSize);
    return TRUE;
} // DataBlob::GetDataOfSize

/*
// --------------------------------------------------------------------------------------
//
// Checks if there's at least cbDataSize1 + cbDataSize2 bytes in the represented memory block (and that
// the sum doesn't overflow).
// Returns TRUE if there's >= cbDataSize1 + cbDataSize2 bytes.
// Returns FALSE otherwise and if cbDataSize1 + cbDataSize2 overflows.
//
inline
BOOL
DataBlob::ContainsData_2Parts(
    UINT32 cbDataSize1,
    UINT32 cbDataSize2) const
{
    S_UINT32 cbDataSize = S_UINT32(cbDataSize1) + S_UITN32(cbDataSize2);
    if (cbDataSize.IsOverflow())
    {
        return FALSE;
    }
    return (cbDataSize.Value() <= m_cbSize);
} // DataBlob::ContainsData
*/

// --------------------------------------------------------------------------------------
//
// Truncates the buffer to exact size (cbSize).
// Returns FALSE if there's less than cbSize data represented.
// Returns TRUE otherwise and truncates the represented data size to cbSize.
//
__checkReturn
inline
BOOL
DataBlob::TruncateToExactSize(UINT32 cbSize)
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
} // DataBlob::TruncateToExactSize

// --------------------------------------------------------------------------------------
//
// Truncates the buffer by size (cbSize).
// Returns FALSE if there's less than cbSize data represented.
// Returns TRUE otherwise and truncates the represented data size by cbSize.
//
__checkReturn
inline
BOOL
DataBlob::TruncateBySize(UINT32 cbSize)
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
} // DataBlob::TruncateBySize

#ifdef _DEBUG
// --------------------------------------------------------------------------------------
//
// Returns U1 value at offset (nOffset). Fires an assert if the offset is behind the end of represented
// data.
//
inline
BYTE
DataBlob::Debug_GetByteAtOffset(UINT32 nOffset) const
{
    _ASSERTE(nOffset < m_cbSize);
    return m_pbData[nOffset];
} // DataBlob::Debug_GetByteAtOffset
#endif //_DEBUG

// --------------------------------------------------------------------------------------
//
// Writes compressed integer (1, 2 or 4 bytes of format code:CompressedInteger#Format) to the data blob
// and skips the written data.
// Returns FALSE if there's not enough data in the blob or the value cannot be encoded as compressed
// integer (bigger than code:CompressedInteger::const_Max).
// Returns TRUE on success and moves the memory block behind the written data.
//
__checkReturn
inline
BOOL
DataBlob::StoreCompressedU(UINT32 nValue)
{
    if (nValue <= CompressedInteger::const_Max1Byte)
    {   // The value fits into 1 byte
        if (m_cbSize < 1)
        {   // The data blob is empty, we cannot store compressed integer as 1 byte
            return FALSE;
        }
        *m_pbData = (BYTE)nValue;
        SkipBytes_InternalInsecure(1);
        return TRUE;
    }
    if (nValue <= CompressedInteger::const_Max2Bytes)
    {   // The value fits into 2 bytes
        if (m_cbSize < 2)
        {   // The data blob is too short, we cannot store compressed integer as 2 bytes
            return FALSE;
        }
        *m_pbData = (BYTE)(nValue >> 8) | 0x80;
        *(m_pbData + 1) = (BYTE)(nValue & 0xff);
        SkipBytes_InternalInsecure(2);
        return TRUE;
    }
    if (nValue <= CompressedInteger::const_Max4Bytes)
    {   // The value fits into 4 bytes
        if (m_cbSize < 4)
        {   // The data blob is too short, we cannot store compressed integer as 4 bytes
            return FALSE;
        }
        *m_pbData = (BYTE)(nValue >> 24) | 0xC0;
        *(m_pbData + 1) = (BYTE)((nValue >> 16) & 0xff);
        *(m_pbData + 2) = (BYTE)((nValue >> 8) & 0xff);
        *(m_pbData + 3) = (BYTE)(nValue & 0xff);
        SkipBytes_InternalInsecure(4);
        return TRUE;
    }
    // The value cannot be encoded as compressed integer
    return FALSE;
} // DataBlob::StoreCompressedU

// --------------------------------------------------------------------------------------
//
// Writes data from *pSource to the data blob and skips the written data.
// Returns FALSE if there's not enough data in the blob.
// Returns TRUE on success and moves memory block behind the written data.
//
__checkReturn
inline
BOOL
DataBlob::StoreData(_In_ const DataBlob *pSource)
{
    // Check that we have enough space to store the *pSource data
    if (m_cbSize < pSource->m_cbSize)
    {   // There's not enough space to store *pSource data
        return FALSE;
    }
    // Copy the *pSource data to the data blob
    memcpy(m_pbData, pSource->m_pbData, pSource->m_cbSize);
    // Move the data blob behind copied/written data *pSource
    m_pbData += pSource->m_cbSize;
    m_cbSize -= pSource->m_cbSize;

    return TRUE;
} // DataBlob::StoreData

// --------------------------------------------------------------------------------------
//
// Skips cbSize bytes in the represented memory block. The caller is responsible for making sure that the
// represented memory block contains at least cbSize bytes, otherwise there will be a security issue.
// Should be used only internally, never call it from outside of this class.
//
inline
void
DataBlob::SkipBytes_InternalInsecure(UINT32 cbSize)
{
    // The caller is responsible for this check, just double check here
    _ASSERTE(m_cbSize >= cbSize);
    // Move the memory block by 'cbSize' bytes
    m_pbData += cbSize;
    m_cbSize -= cbSize;
} // DataBlob::SkipBytes_InternalInsecure

};  // namespace MetaData
