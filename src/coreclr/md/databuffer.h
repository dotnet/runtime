// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DataBuffer.h
//

//
// Class code:DataBuffer provides secure access to a block of memory.
//
// ======================================================================================

#pragma once

#include "external.h"

// --------------------------------------------------------------------------------------
//
// This class provides secure access to a block of memory.
//
class DataBuffer
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
    inline DataBuffer();
    // Creates memory block (pbData, of size cbSize).
    inline DataBuffer(
        _In_reads_bytes_(cbSize) BYTE  *pbData,
                            UINT32 cbSize);
    // Creates memory block copy.
    inline DataBuffer(
        const DataBuffer &source);
    // Initializes memory block to empty data. The object could be already initialzied.
    inline void Clear();
    // Initializes memory block to data (pbData, of size cbSize). The object should be empty before.
    inline void Init(
        _In_reads_bytes_(cbSize) BYTE  *pbData,
                            UINT32 cbSize);

    //
    // Getters
    //

    // Reads data of type T without skipping the read data (returns pointer to the type in *ppTypeData).
    // Returns FALSE if there's not enough data (of size T) in the blob, doesn't initialize the pointer
    // *ppTypeData then.
    // Returns TRUE otherwise, fills *ppTypeData with the "read" type start, but doesn't move the memory
    // block (doesn't skip the "read" data).
    template<class T>
    __checkReturn
    inline BOOL PeekData(
        _Outptr_ T **ppTypeData);
    // Reads data of type T at offset nOffset without skipping the read data (returns pointer to the type in
    // *ppTypeData).
    // Returns FALSE if there's not enough data (of size T) at offset nOffset in the buffer, doesn't
    // initialize the pointer *ppTypeData then.
    // Returns TRUE otherwise, fills *ppTypeData with the type start, but doesn't move the memory block
    // (doesn't skip any "read" data).
    template<class T>
    __checkReturn
    inline BOOL PeekDataAt(
                    UINT32 nOffset,
        _Outptr_ T    **ppTypeData);
    // Reads data of type T and skips the data (instead of reading the bytes, returns pointer to the type in
    // *ppTypeData).
    // Returns FALSE if there's not enough data (of size T) in the blob, doesn't initialize the pointer
    // *ppTypeData then.
    // Returns TRUE otherwise, fills *ppTypeData with the "read" type start and moves the memory block
    // behind the "read" type.
    template<class T>
    __checkReturn
    inline BOOL GetData(
        _Outptr_ T **ppTypeData);
    // Reads data of size cbDataSize and skips the data (instead of reading the bytes, returns pointer to
    // the bytes in *ppbDataPointer).
    // Returns FALSE if there's not enough data in the blob, doesn't initialize the pointer *ppbDataPointer
    // then.
    // Returns TRUE otherwise, fills *ppbDataPointer with the "read" data start and moves the memory block
    // behind the "read" data.
    __checkReturn
    inline BOOL GetDataOfSize(
                                 UINT32 cbDataSize,
        _Out_writes_bytes_(cbDataSize) BYTE **ppbDataPointer);

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
        { return m_pbData + m_cbSize; }
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
    inline BOOL TruncateToExactSize(UINT32 cbSize);
    // Truncates the buffer by size (cbSize).
    // Returns FALSE if there's less than cbSize data represented.
    // Returns TRUE otherwise and truncates the represented data size by cbSize.
    __checkReturn
    inline BOOL TruncateBySize(UINT32 cbSize);

    // Skips the buffer to exact size (cbSize).
    // Returns FALSE if there's less than cbSize data represented.
    // Returns TRUE otherwise and skips data at the beggining, so that the result has size cbSize.
    __checkReturn
    inline BOOL SkipToExactSize(UINT32 cbSize);

private:
    //
    // Helpers
    //

    // Skips 'cbSize' bytes in the represented memory block. The caller is responsible for making sure that
    // the represented memory block contains at least 'cbSize' bytes, otherwise there will be a security
    // issue.
    // Should be used only internally, never call it from outside of this class.
    inline void SkipBytes_InternalInsecure(UINT32 cbSize);

};  // class DataBuffer

#include "databuffer.inl"
