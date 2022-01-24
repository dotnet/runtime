// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CompressedInteger.h
//

//
// Class code:MetaData::CompressedInteger provides secure access to a compressed integer (as defined in CLI
// ECMA specification). The integer is compressed into 1, 2 or 4 bytes. See code:CompressedInteger#Format
// for full format description.
//
// ======================================================================================

#pragma once

#include "external.h"

namespace MetaData
{

// --------------------------------------------------------------------------------------
//
// This class provides secure access to a compressed integer (as defined in CLI ECMA specification). The
// integer is compressed into 1, 2 or 4 bytes. See code:CompressedInteger#Format for full format description.
//
class CompressedInteger
{
// #Format
//
// The format/encoding of compressed integer is (defined in ECMA CLI specification):
//  The encoding is 1, 2 or 4 bytes long and depends on the first byte value. If the first byte is (binary):
//    * 0xxx xxxx ... then it's 1 byte long and the value is 0xxx xxxx.
//    * 10xx xxxx ... then it's 2 bytes long and the value is 00xx xxxx yyyy yyyy, where yyyy yyyy is the
//                    second byte. Though values smaller than code:const_Max1Byte are technically invalid
//                    when encoded with 2 bytes.
//    * 110x xxxx ... then it's 4 bytes long and the value is 000x xxxx yyyy yyyy zzzz zzzz wwww wwww, where
//                    yyyy yyyy is the 2nd byte, zzzz zzzz is the 3rd byte and wwww wwww is the 4th byte.
//                    Though values smaller than code:const_Max2Bytes are technically invalid when encoded
//                    with 4 bytes.
//    * 111x xxxx ... then it's invalid encoding.
//
// Note: Some encodings are invalid, but CLR accepts them (see code:DataBlob::GetCompressedU),
//  e.g. 1000 0000 0000 0000 (0x8000) encodes 0 while correct/valid encoding is 0000 0000 (0x00).
//
private:
    // This class has only static methods and shouldn't be instantiated.
    CompressedInteger() {}

public:
    static const UINT32 const_MaxEncodingSize = 4;

    static const UINT32 const_Max1Byte  = 0x7f;
    static const UINT32 const_Max2Bytes = 0x3fff;
    static const UINT32 const_Max4Bytes = 0x1fffffff;

    static const UINT32 const_Max = const_Max4Bytes;

public:
    //
    // Operations
    //

    // Returns TRUE if the value (nValue) fits into 1-byte, 2-bytes or 4-bytes encoding and fills
    // *pcbEncodingSize with 1, 2 or 4.
    // Returns FALSE if the value cannot be encoded as compressed integer, doesn't fill *pcbEncodingSize
    // then.
    __checkReturn
    __success(return)
    static inline BOOL GetEncodingSize(
              UINT32  nValue,
        _Out_ UINT32 *pcbEncodingSize);
    // Returns TRUE if the value (nValue) fits into 1-byte, 2-bytes or 4-bytes encoding and fills
    // *pcbEncodingSize with 1, 2 or 4 and *pnEncodedValue with the encoded value.
    // Returns FALSE if the value cannot be encoded as compressed integer, doesn't fill *pcbEncodingSize
    // nor *pnEncodedValue then.
    __success(return)
    static inline BOOL Encode(
              UINT32  nValue,
        _Out_ UINT32 *pnEncodedValue,
        _Out_ UINT32 *pcbEncodingSize);

};  // class CompressedInteger

};  // namespace MetaData

#include "compressedinteger.inl"
