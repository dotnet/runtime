// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This module contains the routines to implement the Marvin32 checksum function
//
//

#include "common.h"
#include "marvin32.h"

//
// See the symcrypt.h file for documentation on what the various functions do.
//

//
// Round rotation amounts. This array is optimized away by the compiler
// as we inline all our rotations.
//
static const int rotate[4] = {
    20, 9, 27, 19, 
};


#define ROL32( x, n ) _rotl( (x), (n) )
#define ROR32( x, n ) _rotr( (x), (n) )

#define BLOCK( a, b ) \
{\
    b ^= a; a = ROL32( a, rotate[0] );\
    a += b; b = ROL32( b, rotate[1] );\
    b ^= a; a = ROL32( a, rotate[2] );\
    a += b; b = ROL32( b, rotate[3] );\
}



HRESULT
SymCryptMarvin32ExpandSeed(   
    __out               PSYMCRYPT_MARVIN32_EXPANDED_SEED    pExpandedSeed,
    __in_ecount(cbSeed) PCBYTE                              pbSeed,
                        SIZE_T                              cbSeed )
{
    HRESULT retVal = S_OK;

    if( cbSeed != SYMCRYPT_MARVIN32_SEED_SIZE )
    {
        retVal =E_INVALIDARG;
        goto cleanup;
    }
    pExpandedSeed->s[0] = LOAD_LSBFIRST32( pbSeed );
    pExpandedSeed->s[1] = LOAD_LSBFIRST32( pbSeed + 4 );

cleanup:
    return retVal;
}


VOID
SymCryptMarvin32Init(   _Out_   PSYMCRYPT_MARVIN32_STATE            pState,
                        _In_    PCSYMCRYPT_MARVIN32_EXPANDED_SEED   pExpandedSeed)
{
    pState->chain = *pExpandedSeed;
    pState->dataLength = 0;
    pState->pSeed = pExpandedSeed;

    *(ULONG *) &pState->buffer[4] = 0; // wipe the last 4 bytes of the buffer.
}

VOID
SymCryptMarvin32AppendBlocks(
    _Inout_                 PSYMCRYPT_MARVIN32_CHAINING_STATE   pChain,
    _In_reads_( cbData )    PCBYTE                              pbData,
                            SIZE_T                              cbData )
{
    ULONG s0 = pChain->s[0];
    ULONG s1 = pChain->s[1];

    SIZE_T bytesInFirstBlock = cbData & 0xc;        // 0, 4, 8, or 12

    pbData += bytesInFirstBlock;
    cbData -= bytesInFirstBlock;

    switch( bytesInFirstBlock )
    {
    case 0: // This handles the cbData == 0 case too
        while( cbData > 0 )
        {
            pbData += 16;
            cbData -= 16;

            s0 += LOAD_LSBFIRST32( pbData - 16 );
            BLOCK( s0, s1 );
    case 12:
            s0 += LOAD_LSBFIRST32( pbData - 12 );
            BLOCK( s0, s1 );
    case 8:
            s0 += LOAD_LSBFIRST32( pbData -  8 );
            BLOCK( s0, s1 );
    case 4:
            s0 += LOAD_LSBFIRST32( pbData -  4 );
            BLOCK( s0, s1 );
        }
    }

    pChain->s[0] = s0;
    pChain->s[1] = s1;
}

VOID
SymCryptMarvin32Append(_Inout_                    SYMCRYPT_MARVIN32_STATE * state,
_In_reads_bytes_(cbData) PCBYTE                    pbData,
SIZE_T               cbData)
{
    ULONG bytesInBuffer = state->dataLength;

    state->dataLength += (ULONG)cbData;    // We only keep track of the last 2 bits...

    //
    // Truncate bytesInBuffer so that we never have an integer overflow.
    //
    bytesInBuffer &= SYMCRYPT_MARVIN32_INPUT_BLOCK_SIZE - 1;

    //
    // If previous data in buffer, buffer new input and transform if possible.
    //
    if (bytesInBuffer > 0)
    {
        SIZE_T freeInBuffer = SYMCRYPT_MARVIN32_INPUT_BLOCK_SIZE - bytesInBuffer;
        if (cbData < freeInBuffer)
        {
            //
            // All the data will fit in the buffer.
            // We don't do anything here. 
            // As cbData < INPUT_BLOCK_SIZE the bulk data processing is skipped,
            // and the data will be copied to the buffer at the end
            // of this code.
        }
        else {
            //
            // Enough data to fill the whole buffer & process it
            //
            memcpy(&state->buffer[bytesInBuffer], pbData, freeInBuffer);
            pbData += freeInBuffer;
            cbData -= freeInBuffer;
            SymCryptMarvin32AppendBlocks(&state->chain, state->buffer, SYMCRYPT_MARVIN32_INPUT_BLOCK_SIZE);

            //
            // Set bytesInBuffer to zero to ensure that the trailing data in the
            // buffer will be copied to the right location of the buffer below.
            //
            bytesInBuffer = 0;
        }
    }

    //
    // Internal buffer is empty; process all remaining whole blocks in the input
    //
    if (cbData >= SYMCRYPT_MARVIN32_INPUT_BLOCK_SIZE)
    {
        SIZE_T cbDataRoundedDown = cbData & ~(SIZE_T)(SYMCRYPT_MARVIN32_INPUT_BLOCK_SIZE - 1);
        SymCryptMarvin32AppendBlocks(&state->chain, pbData, cbDataRoundedDown);
        pbData += cbDataRoundedDown;
        cbData -= cbDataRoundedDown;
    }

    //
    // buffer remaining input if necessary.
    //
    if (cbData > 0)
    {
        memcpy(&state->buffer[bytesInBuffer], pbData, cbData);
    }

}

VOID
SymCryptMarvin32Result( 
     _Inout_                                        PSYMCRYPT_MARVIN32_STATE    pState,
     _Out_writes_( SYMCRYPT_MARVIN32_RESULT_SIZE )  PBYTE                       pbResult )
{
    SIZE_T bytesInBuffer = ( pState->dataLength) & 0x3;

    //
    // Wipe four bytes in the buffer.
    // Doing this first ensures that this write is aligned when the input was of
    // length 0 mod 4. 
    // The buffer is 8 bytes long, so we never overwrite anything else.
    //
    *(ULONG *) &pState->buffer[bytesInBuffer] = 0;

    //
    // The buffer is never completely full, so we can always put the first
    // padding byte in.
    //
    pState->buffer[bytesInBuffer++] = 0x80;

    //
    // Process the final block
    //
    SymCryptMarvin32AppendBlocks( &pState->chain, pState->buffer, 8 );

    STORE_LSBFIRST32( pbResult    , pState->chain.s[0] );
    STORE_LSBFIRST32( pbResult + 4, pState->chain.s[1] );

    //
    // Wipe only those things that we need to wipe.
    //

    *(ULONG *) &pState->buffer[0] = 0;
    pState->dataLength = 0;

    pState->chain = *pState->pSeed;
}


VOID
SymCryptMarvin32( 
  __in                                           PCSYMCRYPT_MARVIN32_EXPANDED_SEED   pExpandedSeed,
  __in_ecount(cbData)                            PCBYTE                              pbData,
                                                 SIZE_T                              cbData,
  __out_ecount(SYMCRYPT_MARVIN32_RESULT_SIZE)    PBYTE                               pbResult)
//
// To reduce the per-computation overhead, we have a dedicated code here instead of the whole Init/Append/Result stuff.
//
{
    ULONG tmp;

    ULONG s0 = pExpandedSeed->s[0];
    ULONG s1 = pExpandedSeed->s[1];
    
    while( cbData > 7 )
    {
        s0 += LOAD_LSBFIRST32( pbData );
        BLOCK( s0, s1 );
        s0 += LOAD_LSBFIRST32( pbData + 4 );
        BLOCK( s0, s1 );
        pbData += 8;
        cbData -= 8;
    }

    switch( cbData )
    {
    default:
    case 4: s0 += LOAD_LSBFIRST32( pbData ); BLOCK( s0, s1 ); pbData += 4;
    case 0: tmp = 0x80; break;

    case 5: s0 += LOAD_LSBFIRST32( pbData ); BLOCK( s0, s1 ); pbData += 4;
    case 1: tmp = 0x8000 | pbData[0]; break;

    case 6: s0 += LOAD_LSBFIRST32( pbData ); BLOCK( s0, s1 ); pbData += 4;
    case 2: tmp = 0x800000 | LOAD_LSBFIRST16( pbData ); break;

    case 7: s0 += LOAD_LSBFIRST32( pbData ); BLOCK( s0, s1 ); pbData += 4;
    case 3: tmp = LOAD_LSBFIRST16( pbData ) | (pbData[2] << 16) | 0x80000000; break;
    }
    s0 += tmp;


    BLOCK( s0, s1 );
    BLOCK( s0, s1 );

    STORE_LSBFIRST32( pbResult    , s0 );
    STORE_LSBFIRST32( pbResult + 4, s1 );
}
