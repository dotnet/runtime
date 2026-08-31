// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CompressedInteger.inl
//

//
// Class code:MetaData::CompressedInteger provides secure access to a compressed integer (as defined in CLI
// ECMA specification). The integer is compressed into 1, 2 or 4 bytes. See code:CompressedInteger#Format
// for full format description.
//
// ======================================================================================

#pragma once

#include "compressedinteger.h"

namespace MetaData
{

// --------------------------------------------------------------------------------------
//
// Returns TRUE if the value (nValue) fits into 1-byte, 2-bytes or 4-bytes encoding and fills
// *pcbEncodingSize with 1, 2 or 4.
// Returns FALSE if the value cannot be encoded as compressed integer, doesn't fill *pcbEncodingSize then.
//
__checkReturn
//static
inline
BOOL
CompressedInteger::GetEncodingSize(
          UINT32  nValue,
    _Out_ UINT32 *pcbEncodingSize)
{
    // Does it fit into 1-byte encoding?
    if (nValue <= const_Max1Byte)
    {   // The value fits into 1 byte (binary format 0xxx xxxx)
        *pcbEncodingSize = 1;
        return TRUE;
    }
    // Does it fit into 2-bytes encoding?
    if (nValue <= const_Max2Bytes)
    {   // The value fits into 2 bytes (binary format 10xx xxxx yyyy yyyy)
        *pcbEncodingSize = 2;
        return TRUE;
    }
    // Does it fit into 4-bytes encoding?
    if (nValue <= const_Max4Bytes)
    {   // The value fits into 4 bytes (binary format 110x xxxx yyyy yyyy zzzz zzzz wwww wwww)
        *pcbEncodingSize = 4;
        return TRUE;
    }
    // The value cannot be encoded as compressed integer
    return FALSE;
} // CompressedInteger::GetEncodingSize

// --------------------------------------------------------------------------------------
//
// Returns TRUE if the value (nValue) fits into 1-byte, 2-bytes or 4-bytes encoding and fills
// *pcbEncodingSize with 1, 2 or 4 and *pnEncodedValue with the encoded value.
// Returns FALSE if the value cannot be encoded as compressed integer, doesn't fill *pcbEncodingSize
// nor *pnEncodedValue then.
//
__checkReturn
//static
inline
BOOL
CompressedInteger::Encode(
          UINT32  nValue,
    _Out_ UINT32 *pnEncodedValue,
    _Out_ UINT32 *pcbEncodingSize)
{
    // Does it fit into 1-byte encoding?
    if (nValue <= const_Max1Byte)
    {   // The value fits into 1 byte (binary format 0xxx xxxx)
        *pnEncodedValue = nValue;
        *pcbEncodingSize = 1;
        return TRUE;
    }
    // Does it fit into 2-bytes encoding?
    if (nValue <= const_Max2Bytes)
    {   // The value fits into 2 bytes (binary format 10xx xxxx yyyy yyyy)
        *pnEncodedValue = 0x8000 | nValue;
        *pcbEncodingSize = 2;
        return TRUE;
    }
    // Does it fit into 4-bytes encoding?
    if (nValue <= const_Max4Bytes)
    {   // The value fits into 4 bytes (binary format 110x xxxx yyyy yyyy zzzz zzzz wwww wwww)
        *pnEncodedValue = 0xC0000000 | nValue;
        *pcbEncodingSize = 4;
        return TRUE;
    }
    // The value cannot be encoded as compressed integer
    return FALSE;
} // CompressedInteger::Encode

};  // namespace MetaData
