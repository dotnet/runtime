// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef MARVIN32_INCLUDED
#define MARVIN32_INCLUDED


#include "common.h"
#include "windows.h"

//
// Pointer-const typedefs:
//
// These definitions are missing from the standard Windows declarations.
// Should probably be moved to a central typedef file.
//
typedef const BYTE      * PCBYTE;
typedef const USHORT    * PCUSHORT;
typedef const ULONG     * PCULONG;
typedef const ULONGLONG * PCULONGLONG;
typedef const VOID      * PCVOID;



//
// MARVIN32
//

#define SYMCRYPT_MARVIN32_RESULT_SIZE       (8)
#define SYMCRYPT_MARVIN32_SEED_SIZE         (8)
#define SYMCRYPT_MARVIN32_INPUT_BLOCK_SIZE  (4)

// These macros only support little-endian machines with unaligned access
#define LOAD_LSBFIRST16( p ) ( *(USHORT    *)(p))
#define LOAD_LSBFIRST32( p ) ( *(ULONG     *)(p))
#define STORE_LSBFIRST32( p, x ) ( *(ULONG     *)(p) =        (x) )

// Disable the warning about padding the struct on amd64
#pragma warning(push)
#pragma warning(disable:4324)

typedef struct _SYMCRYPT_MARVIN32_EXPANDED_SEED
{
    ULONG   s[2];
} SYMCRYPT_MARVIN32_EXPANDED_SEED, *PSYMCRYPT_MARVIN32_EXPANDED_SEED;

typedef SYMCRYPT_MARVIN32_EXPANDED_SEED SYMCRYPT_MARVIN32_CHAINING_STATE, *PSYMCRYPT_MARVIN32_CHAINING_STATE;
typedef const SYMCRYPT_MARVIN32_EXPANDED_SEED * PCSYMCRYPT_MARVIN32_EXPANDED_SEED;

typedef struct _SYMCRYPT_MARVIN32_STATE
{
    BYTE                                buffer[8];  // 4 bytes of data, 4 more bytes for final padding
    SYMCRYPT_MARVIN32_CHAINING_STATE    chain;      // chaining state 
    PCSYMCRYPT_MARVIN32_EXPANDED_SEED   pSeed;      // 
    ULONG                               dataLength; // length of the data processed so far, mod 2^32
} SYMCRYPT_MARVIN32_STATE, *PSYMCRYPT_MARVIN32_STATE;
typedef const SYMCRYPT_MARVIN32_STATE *PCSYMCRYPT_MARVIN32_STATE;
#pragma warning(pop)

//
// Function declarations
//
HRESULT SymCryptMarvin32ExpandSeed(
        __out               PSYMCRYPT_MARVIN32_EXPANDED_SEED    pExpandedSeed,
        __in_ecount(cbSeed) PCBYTE                              pbSeed,
                            SIZE_T                              cbSeed);

VOID SymCryptMarvin32Init(_Out_   PSYMCRYPT_MARVIN32_STATE            pState,
        _In_    PCSYMCRYPT_MARVIN32_EXPANDED_SEED   pExpandedSeed);

VOID SymCryptMarvin32Result(
        _Inout_                                        PSYMCRYPT_MARVIN32_STATE    pState,
        _Out_  PBYTE                       pbResult);

VOID SymCryptMarvin32Append(_Inout_                    SYMCRYPT_MARVIN32_STATE * state,
        _In_reads_bytes_(cbData) PCBYTE                    pbData,
        SIZE_T               cbData);

VOID SymCryptMarvin32(
        __in                                            PCSYMCRYPT_MARVIN32_EXPANDED_SEED   pExpandedSeed,
        __in_ecount(cbData)                             PCBYTE                              pbData,
                                                        SIZE_T                              cbData,
        __out_ecount(SYMCRYPT_MARVIN32_RESULT_SIZE)     PBYTE                               pbResult);
#endif // MARVIN32_INCLUDED
