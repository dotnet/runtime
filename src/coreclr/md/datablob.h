// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DataBlob.h
//

//
// Class code:MetaData::DataBlob provides secure access to a block of memory from MetaData (i.e. with fixed
// endianness).
//
// ======================================================================================

#pragma once

#include "external.h"

namespace MetaData
{

// --------------------------------------------------------------------------------------
//
// This class provides secure access to a block of memory.
//
class DataBlob
{
private:
    //
    // Private data
    //

    // The memory block of size code:m_cbSize. Can be non-NULL even if code:m_cbSize is 0.
    __field_bcount(m_cbSize)
    BYTE  *m_pbData;
    // Size of the memory block starting at code:m_pbData. If it is 0, then value of code:m_pbData can be
    // anything (incl. NULL).
    UINT32 m_cbSize;

public:
    //
    // Initialization
    //

    // Creates empty memory block.
    inline DataBlob();
    // Creates memory block (pbData, of size cbSize).
    inline DataBlob(
        _In_reads_bytes_opt_(cbSize) BYTE  *pbData,
                                UINT32 cbSize);
    // Creates memory block copy.
    inline DataBlob(
        const DataBlob &source);
    // Initializes memory block to empty data. The object could be already initialized.
    inline void Clear();
    // Initializes memory block to data (pbData, of size cbSize). The object should be empty before.
    inline void Init(
        _In_reads_bytes_opt_(cbSize) BYTE  *pbData,
                                UINT32 cbSize);

    //
    // Getters
    //

    //#PeekUx_Functions
    // Reads the U1/U2/U4/U8 from the data blob without skipping the read data.
    // Returns FALSE if there's not enough data in the blob, doesn't initialize the value '*pnValue' then.
    // Returns TRUE otherwise, fills *pnValue, but doesn't move the memory block (doesn't skip the read
    // data).
    __checkReturn __success(return) inline BOOL PeekU1(_Out_ BYTE   *pnValue) const;
    __checkReturn __success(return) inline BOOL PeekU2(_Out_ UINT16 *pnValue) const;
    __checkReturn __success(return) inline BOOL PeekU4(_Out_ UINT32 *pnValue) const;
    __checkReturn __success(return) inline BOOL PeekU8(_Out_ UINT64 *pnValue) const;

    //#GetUx_Functions
    // Reads the U1/U2/U4/U8 from the data blob and skips the read data.
    // Returns FALSE if there's not enough data in the blob, doesn't initialize the value '*pnValue' then.
    // Returns TRUE otherwise, fills *pnValue and moves the memory block behind the read data.
    __checkReturn __success(return) inline BOOL GetU1(_Out_ BYTE   *pnValue);
    __checkReturn __success(return) inline BOOL GetU2(_Out_ UINT16 *pnValue);
    __checkReturn __success(return) inline BOOL GetU4(_Out_ UINT32 *pnValue);
    __checkReturn __success(return) inline BOOL GetU8(_Out_ UINT64 *pnValue);

    // Reads compressed integer (1, 2 or 4 bytes of format code:CompressedInteger#Format - returns the size
    // in *pcbCompressedValueSize) from the data blob without skipping the read data.
    // Returns FALSE if there's not enough data in the blob or the compression is invalid (starts with byte
    // 111? ????), doesn't initialize the value *pnValue nor the size of the compressed value
    // *pcbCompressedValueSize then.
    // Returns TRUE otherwise, fills *pnValue and *pcbCompressedValueSize (with number 1,2 or 4), but
    // doesn't move the memory block (doesn't skip the read data).
    __checkReturn
    __success(return)
    inline BOOL PeekCompressedU(
        _Out_ UINT32 *pnValue,
        _Out_ UINT32 *pcbCompressedValueSize);
    // Reads compressed integer (1, 2 or 4 bytes of format code:CompressedInteger#Format) from the data blob
    // and skips the read data.
    // Returns FALSE if there's not enough data in the blob or the compression is invalid (starts with byte
    // 111? ????), doesn't initialize the value *pnValue then.
    // Returns TRUE otherwise, fills *pnValue and moves the memory block behind the read data.
    __checkReturn
    __success(return)
    inline BOOL GetCompressedU(_Out_ UINT32 *pnValue);
    // Reads compressed integer (1, 2 or 4 bytes of format code:CompressedInteger#Format - returns the size
    // in *pcbCompressedValueSize) from the data blob and skips the read data.
    // Returns FALSE if there's not enough data in the blob or the compression is invalid (starts with byte
    // 111? ????), doesn't initialize the value *pnValue nor the size of the compressed value
    // *pcbCompressedValueSize then.
    // Returns TRUE otherwise, fills *pnValue and *pcbCompressedValueSize (with number 1,2 or 4) and moves
    // the memory block behind the read data.
    __checkReturn
    __success(return)
    inline BOOL GetCompressedU(
        _Out_ UINT32 *pnValue,
        _Out_ UINT32 *pcbCompressedValueSize);

    // Reads data of size cbDataSize and skips the data (instead of reading the bytes, returns the data as
    // *pData).
    // Returns FALSE if there's not enough data in the blob, clears *pData then.
    // Returns TRUE otherwise, fills *pData with the "read" data and moves the memory block behind the
    // "read" data.
    __checkReturn
    __success(return)
    inline BOOL GetDataOfSize(
              UINT32    cbDataSize,
        _Out_ DataBlob *pData);

    // Checks if there's at least cbDataSize bytes in the represented memory block.
    // Returns TRUE if there's >= cbDataSize bytes. Returns FALSE otherwise.
    inline BOOL ContainsData(UINT32 cbDataSize) const
        { return cbDataSize <= m_cbSize; }
/*
    // Checks if there's at least cbDataSize1 + cbDataSize2 bytes in the represented memory block (and that
    // the sum doesn't overflow).
    // Returns TRUE if there's >= cbDataSize1 + cbDataSize2 bytes.
    // Returns FALSE otherwise and if cbDataSize1 + cbDataSize2 overflows.
    inline BOOL ContainsData_2Parts(
        UINT32 cbDataSize1,
        UINT32 cbDataSize2) const;
    // Checks if there's valid compressed integer (1, 2 or 4 bytes of format
    // code:DataBlob#CompressedIntegerFormat) in the data blob.
    // Returns:
    //  * 0 ... if there's valid compressed integer.
    //  * 1 ... if there's not enough data in the data blob, but the encoding is correct.
    //  * 2 ... if the integer encoding is invalid (starts with byte 111x xxxx byte), but there are at least
    //          4 bytes in the data blob left.
    //  * 3 ... if there's not enough data in the data blob and the integer encoding is invalid (starts with
    //          111x xxx byte).
    inline int ValidateCompressedU() const;
*/

    // Returns TRUE if the represented memory is empty.
    inline BOOL IsEmpty() const
        { return (m_cbSize == 0); }
    // Gets pointer to the represented data buffer (can be random pointer if size of the data is 0).
    // Note: Should be used exceptionally. Try to use other operations instead.
    inline BYTE *GetDataPointer()
        { return m_pbData; }
    // Gets pointer to the represented data buffer (can be random pointer if size of the data is 0).
    // Note: Should be used exceptionally. Try to use other operations instead.
    inline const BYTE *GetDataPointer() const
        { return m_pbData; }
    // Gets pointer right behind the represented data buffer (can be random pointer if size of the data is
    // 0).
    inline const BYTE *GetDataPointerBehind() const
        { return ((m_cbSize == 0) ? NULL : (m_pbData + m_cbSize)); }
    // Gets the size of represented memory.
    inline UINT32 GetSize() const
        { return m_cbSize; }
    //BOOL SkipBytes(UINT32 cbSize);

public:
    //
    // Operations
    //

    // Truncates the buffer to exact size (cbSize).
    // Returns FALSE if there's less than cbSize data represented.
    // Returns TRUE otherwise and truncates the represented data size to cbSize.
    __checkReturn
    __success(return)
    inline BOOL TruncateToExactSize(UINT32 cbSize);
    // Truncates the buffer by size (cbSize).
    // Returns FALSE if there's less than cbSize data represented.
    // Returns TRUE otherwise and truncates the represented data size by cbSize.
    __checkReturn
    __success(return)
    inline BOOL TruncateBySize(UINT32 cbSize);

#ifdef _DEBUG
    // Returns U1 value at offset (nOffset). Fires an assert if the offset is behind the end of represented
    // data.
    inline BYTE Debug_GetByteAtOffset(UINT32 nOffset) const;
#endif //_DEBUG

public:
    //
    // Setters
    //

    // Writes compressed integer (1, 2 or 4 bytes of format code:CompressedInteger#Format) to the data blob
    // and skips the written data.
    // Returns FALSE if there's not enough data in the blob or the value cannot be encoded as compressed
    // integer (bigger than code:CompressedInteger::const_Max).
    // Returns TRUE on success and moves the memory block behind the written data.
    __checkReturn
    __success(return)
    inline BOOL StoreCompressedU(UINT32 nValue);

    // Writes data from *pSource to the data blob and skips the written data.
    // Returns FALSE if there's not enough data in the blob.
    // Returns TRUE on success and moves memory block behind the written data.
    __checkReturn
    __success(return)
    inline BOOL StoreData(_In_ const DataBlob *pSource);

private:
    //
    // Helpers
    //

    // Skips cbSize bytes in the represented memory block. The caller is responsible for making sure that
    // the represented memory block contains at least cbSize bytes, otherwise there will be a security
    // issue.
    // Should be used only internally, never call it from outside of this class.
    inline void SkipBytes_InternalInsecure(UINT32 cbSize);

};  // class DataBlob

};  // namespace MetaData

#include "datablob.inl"
