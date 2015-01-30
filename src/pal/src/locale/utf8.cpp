//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    unicode/utf8.c

Abstract:
    Functions to encode and decode UTF-8 strings

Revision History:



--*/

#include "pal/utf8.h"
#include "pal/dbgmsg.h"
#include "pal/unicode_data.h"

//
//  Constant Declarations.
//

#define ASCII                 0x007f

#define UTF8_2_MAX            0x07ff  // max UTF8 2-byte sequence (32 * 64 = 2048)
#define UTF8_1ST_OF_2         0xc0    // 110x xxxx
#define UTF8_1ST_OF_3         0xe0    // 1110 xxxx
#define UTF8_1ST_OF_4         0xf0    // 1111 xxxx
#define UTF8_TRAIL            0x80    // 10xx xxxx

#define HIGHER_6_BIT(u)       ((u) >> 12)
#define MIDDLE_6_BIT(u)       (((u) & 0x0fc0) >> 6)
#define LOWER_6_BIT(u)        ((u) & 0x003f)

#define BIT7(a)               ((a) & 0x80)
#define BIT6(a)               ((a) & 0x40)

#define HIGH_SURROGATE_START  0xd800
#define HIGH_SURROGATE_END    0xdbff
#define LOW_SURROGATE_START   0xdc00
#define LOW_SURROGATE_END     0xdfff


////////////////////////////////////////////////////////////////////////////
//
//  UTF8ToUnicode
//
//  Maps a UTF-8 character string to its wide character string counterpart.
//
////////////////////////////////////////////////////////////////////////////

int UTF8ToUnicode(
    LPCSTR lpSrcStr,
    int cchSrc,
    LPWSTR lpDestStr,
    int cchDest,
    DWORD dwFlags
    )
{
    int nTB = 0;                   // # trail bytes to follow
    int cchWC = 0;                 // # of Unicode code points generated
    CONST BYTE* pUTF8 = (CONST BYTE*)lpSrcStr;
    DWORD dwUnicodeChar = 0;       // Our character with room for full surrogate char
    BOOL bSurrogatePair = FALSE;   // Indicate we're collecting a surrogate pair
    BOOL bCheckInvalidBytes = (dwFlags & MB_ERR_INVALID_CHARS);
    BYTE UTF8;

    // Note that we can't test destination buffer length here because we may have to
    // iterate through thousands of broken characters which won't be output, even though
    // the buffer has no more room.
    while (cchSrc--)
    {
        //
        //  See if there are any trail bytes.
        //
        if (BIT7(*pUTF8) == 0)
        {
            //
            //  Found ASCII.
            //
            if (cchDest)
            {
                // In this function always test buffer size before using it
                if (cchWC >= cchDest)
                {
                    // Error: Buffer too small, we didn't process this character
                    SetLastError(ERROR_INSUFFICIENT_BUFFER);
                    return (0);
                }
                lpDestStr[cchWC] = (WCHAR)*pUTF8;
            }
            nTB = bSurrogatePair = 0;
            cchWC++;
        }
        else if (BIT6(*pUTF8) == 0)
        {
            //
            //  Found a trail byte.
            //  Note : Ignore the trail byte if there was no lead byte.
            //
            if (nTB != 0)
            {
                //
                //  Decrement the trail byte counter.
                //
                nTB--;

                // Add room for trail byte and add the trail byte falue
                dwUnicodeChar <<= 6;
                dwUnicodeChar |= LOWER_6_BIT(*pUTF8);

                // If we're done then we may need to store the data
                if (nTB == 0)
                {
                    if (bSurrogatePair)
                    {
                        if (cchDest)
                        {
                            if ((cchWC + 1) >= cchDest)
                            {
                                // Error: Buffer too small, we didn't process this character
                                SetLastError(ERROR_INSUFFICIENT_BUFFER);
                                return (0);
                            }                                

                            lpDestStr[cchWC]   = (WCHAR)
                                                 (((dwUnicodeChar - 0x10000) >> 10) + HIGH_SURROGATE_START);

                            lpDestStr[cchWC+1] = (WCHAR)
                                                 ((dwUnicodeChar - 0x10000)%0x400 + LOW_SURROGATE_START);
                        }

                        //
                        //  End of sequence.  Advance the output counter, turn off surrogateness
                        //
                        cchWC += 2;
                        bSurrogatePair = FALSE;
                    }
                    else
                    {
                        if (cchDest)
                        {
                            
                            if (cchWC >= cchDest)
                            {
                                // Error: Buffer too small, we didn't process this character
                                SetLastError(ERROR_INSUFFICIENT_BUFFER);
                                return (0);
                            }

                            lpDestStr[cchWC] = (WCHAR)dwUnicodeChar;
                        }

                        //
                        //  End of sequence.  Advance the output counter.
                        //
                        cchWC++;
                    }
                      
                }

            }
            else
            {
                if (bCheckInvalidBytes) 
                {
                    SetLastError(ERROR_NO_UNICODE_TRANSLATION);
                    return (0);
                }
                
                // error - not expecting a trail byte. That is, there is a trailing byte without leading byte.
                bSurrogatePair = FALSE;
            }
        }
        else
        {
            //
            //  Found a lead byte.
            //
            if (nTB > 0)
            {
                // error - A leading byte before the previous sequence is completed.
                if (bCheckInvalidBytes) 
                {
                    SetLastError(ERROR_NO_UNICODE_TRANSLATION);
                    return (0);
                }
                //
                //  Error - previous sequence not finished.
                //
                nTB = 0;
                bSurrogatePair = FALSE;
                // Put this character back so that we can start over another sequence.
                cchSrc++;
                pUTF8--;
            }
            else
            {
                //
                //  Calculate the number of bytes to follow.
                //  Look for the first 0 from left to right.
                //
                UTF8 = *pUTF8;
                while (BIT7(UTF8) != 0)
                {
                    UTF8 <<= 1;
                    nTB++;
                }

                // Recover the data from the byte
                UTF8 >>= nTB;

                //
                // Check for non-shortest form.
                // 
                switch (nTB)
                {
                    case 1:
                        nTB = 0;
                        break;
                    case 2:
                        // Make sure that bit 8 ~ bit 11 is not all zero.
                        // 110XXXXx 10xxxxxx
                        if ((*pUTF8 & 0x1e) == 0)
                        {
                            nTB = 0;
                        }
                        break;
                    case 3:
                        // Look ahead to check for non-shortest form.
                        // 1110XXXX 10Xxxxxx 10xxxxxx
                        if (cchSrc >= 2)
                        {
                            if (((*pUTF8 & 0x0f) == 0) && (*(pUTF8 + 1) & 0x20) == 0)
                            {
                                nTB = 0;
                            }
                        }
                        break;
                    case 4:                    
                        //
                        // This is a surrogate unicode pair
                        //
                        if (cchSrc >= 3)
                        {
                            WORD word = (((WORD)*pUTF8) << 8) | *(pUTF8 + 1);
                            // Look ahead to check for non-shortest form.
                            // 11110XXX 10XXxxxx 10xxxxxx 10xxxxxx                        
                            // Check if the 5 X bits are all zero.
                            // 0x0730 == 00000111 00110000
                            if ( (word & 0x0730) == 0 ||
                                  // If the 21st bit is 1, we have extra work
                                  ( (word & 0x0400) == 0x0400 &&
                                     // The 21st bit is 1.
                                     // Make sure that the resulting Unicode is within the valid surrogate range.
                                     // The 4 byte code sequence can hold up to 21 bits, and the maximum valid code point range
                                     // that Unicode (with surrogate) could represent are from U+000000 ~ U+10FFFF.
                                     // Therefore, if the 21 bit (the most significant bit) is 1, we should verify that the 17 ~ 20
                                     // bit are all zero.
                                     // I.e., in 11110XXX 10XXxxxx 10xxxxxx 10xxxxxx,
                                     // XXXXX can only be 10000.    
                                     // 0x0330 = 0000 0011 0011 0000
                                    (word & 0x0330) != 0 ) )
                            {
                                // Not shortest form
                                nTB = 0;
                            }                              
                            else
                            { 
                                // A real surrogate pair
                                bSurrogatePair = TRUE;
                            }
                        }                        
                        break;
                    default:                    
                        // 
                        // If the bits is greater than 4, this is an invalid
                        // UTF8 lead byte.
                        //
                        nTB = 0;
                        break;
                }

                if (nTB != 0) 
                {
                    //
                    //  Store the value from the first byte and decrement
                    //  the number of bytes to follow.
                    //
                    dwUnicodeChar = UTF8;
                    nTB--;
                } else 
                {
                    if (bCheckInvalidBytes) 
                    {
                        SetLastError(ERROR_NO_UNICODE_TRANSLATION);
                        return (0);
                    }
                }
            }
        }
        pUTF8++;
    }

    if ((bCheckInvalidBytes && nTB != 0) || (cchWC == 0)) 
    {
        // About (cchWC == 0):
        // Because we now throw away non-shortest form, it is possible that we generate 0 chars.
        // In this case, we have to set error to ERROR_NO_UNICODE_TRANSLATION so that we conform
        // to the spec of MultiByteToWideChar.
        SetLastError(ERROR_NO_UNICODE_TRANSLATION);
        return (0);
    }

    //
    //  Return the number of Unicode characters written.
    //
    return (cchWC);
}

////////////////////////////////////////////////////////////////////////////
//
//  UnicodeToUTF8
//
//  Maps a Unicode character string to its UTF-8 string counterpart.
//
////////////////////////////////////////////////////////////////////////////

int UnicodeToUTF8(
    LPCWSTR lpSrcStr,
    int cchSrc,
    LPSTR lpDestStr,
    int cchDest)
{
    LPCWSTR lpWC = lpSrcStr;
    int     cchU8 = 0;                // # of UTF8 chars generated
    DWORD   dwSurrogateChar;
    WCHAR   wchHighSurrogate = 0;
    BOOL    bHandled;


    while ((cchSrc--) && ((cchDest == 0) || (cchU8 < cchDest)))
    {
        bHandled = FALSE;

        //
        // Check if high surrogate is available
        //
        if ((*lpWC >= HIGH_SURROGATE_START) && (*lpWC <= HIGH_SURROGATE_END))
        {
            if (cchDest)
            {
                // Another high surrogate, then treat the 1st as normal
                // Unicode character.
                if (wchHighSurrogate)
                {
                    if ((cchU8 + 2) < cchDest)
                    {
                        lpDestStr[cchU8++] = UTF8_1ST_OF_3 | HIGHER_6_BIT(wchHighSurrogate);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | MIDDLE_6_BIT(wchHighSurrogate);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | LOWER_6_BIT(wchHighSurrogate);
                    }
                    else
                    {
                        // not enough buffer
                        cchSrc++;
                        break;
                    }
                }
            }
            else
            {
                cchU8 += 3;
            }
            wchHighSurrogate = *lpWC;
            bHandled = TRUE;
        }

        if (!bHandled && wchHighSurrogate)
        {
            if ((*lpWC >= LOW_SURROGATE_START) && (*lpWC <= LOW_SURROGATE_END))
            {
                 // wheee, valid surrogate pairs

                 if (cchDest)
                 {
                     if ((cchU8 + 3) < cchDest)
                     {
                         dwSurrogateChar = (((wchHighSurrogate-0xD800) << 10) + (*lpWC - 0xDC00) + 0x10000);

                         lpDestStr[cchU8++] = (UTF8_1ST_OF_4 |
                                               (unsigned char)(dwSurrogateChar >> 18));           // 3 bits from 1st byte

                         lpDestStr[cchU8++] =  (UTF8_TRAIL |
                                                (unsigned char)((dwSurrogateChar >> 12) & 0x3f)); // 6 bits from 2nd byte

                         lpDestStr[cchU8++] = (UTF8_TRAIL |
                                               (unsigned char)((dwSurrogateChar >> 6) & 0x3f));   // 6 bits from 3rd byte

                         lpDestStr[cchU8++] = (UTF8_TRAIL |
                                               (unsigned char)(0x3f & dwSurrogateChar));          // 6 bits from 4th byte
                     }
                     else
                     {
                        // not enough buffer
                        cchSrc++;
                        break;
                     }
                 }
                 else
                 {
                     // we already counted 3 previously (in high surrogate)
                     cchU8 ++;
                 }

                 bHandled = TRUE;
            }
            else
            {
                 // Bad Surrogate pair : ERROR
                 // Just process wchHighSurrogate , and the code below will
                 // process the current code point
                 if (cchDest)
                 {
                     if ((cchU8 + 2) < cchDest)
                     {
                        lpDestStr[cchU8++] = UTF8_1ST_OF_3 | HIGHER_6_BIT(wchHighSurrogate);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | MIDDLE_6_BIT(wchHighSurrogate);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | LOWER_6_BIT(wchHighSurrogate);
                     }
                     else
                     {
                        // not enough buffer
                        cchSrc++;
                        break;
                     }
                 }
            }

            wchHighSurrogate = 0;
        }

        if (!bHandled)
        {
            if (*lpWC <= ASCII)
            {
                //
                //  Found ASCII.
                //
                if (cchDest)
                {
                    if (cchU8 < cchDest) 
                    {
                        lpDestStr[cchU8] = (char)*lpWC;
                    } 
                    else 
                    {
                        //
                        //  Error - buffer too small.
                        //
                        cchSrc++;
                        break;
                    }
                }
                cchU8++;
            }
            else if (*lpWC <= UTF8_2_MAX)
            {
                //
                //  Found 2 byte sequence if < 0x07ff (11 bits).
                //
                if (cchDest)
                {
                    if ((cchU8 + 1) < cchDest)
                    {
                        //
                        //  Use upper 5 bits in first byte.
                        //  Use lower 6 bits in second byte.
                        //
                        lpDestStr[cchU8++] = UTF8_1ST_OF_2 | (*lpWC >> 6);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | LOWER_6_BIT(*lpWC);
                    }
                    else
                    {
                        //
                        //  Error - buffer too small.
                        //
                        cchSrc++;
                        break;
                    }
                }
                else
                {
                    cchU8 += 2;
                }
            }
            else
            {
                //
                //  Found 3 byte sequence.
                //
                if (cchDest)
                {
                    if ((cchU8 + 2) < cchDest)
                    {
                        //
                        //  Use upper  4 bits in first byte.
                        //  Use middle 6 bits in second byte.
                        //  Use lower  6 bits in third byte.
                        //
                        lpDestStr[cchU8++] = UTF8_1ST_OF_3 | HIGHER_6_BIT(*lpWC);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | MIDDLE_6_BIT(*lpWC);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | LOWER_6_BIT(*lpWC);
                    }
                    else
                    {
                        //
                        //  Error - buffer too small.
                        //
                        cchSrc++;
                        break;
                    }
                }
                else
                {
                    cchU8 += 3;
                }
            }
        }

        lpWC++;
    }

    //
    // If the last character was a high surrogate, then handle it as a normal
    // unicode character.
    //
    if ((cchSrc < 0) && (wchHighSurrogate != 0))
    {
        if (cchDest)
        {
            if ((cchU8 + 2) < cchDest)
            {
                lpDestStr[cchU8++] = UTF8_1ST_OF_3 | HIGHER_6_BIT(wchHighSurrogate);
                lpDestStr[cchU8++] = UTF8_TRAIL    | MIDDLE_6_BIT(wchHighSurrogate);
                lpDestStr[cchU8++] = UTF8_TRAIL    | LOWER_6_BIT(wchHighSurrogate);
            }
            else
            {
                cchSrc++;
            }
        }
    }

    //
    //  Make sure the destination buffer was large enough.
    //
    if (cchDest && (cchSrc >= 0))
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        return (0);
    }

    //
    //  Return the number of UTF-8 characters written.
    //
    return (cchU8);
}
