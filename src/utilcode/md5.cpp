// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// md5.cpp
//

// 

#include "stdafx.h"

#include <stdlib.h>
#include "stdmacros.h"
#include "md5.h"
#include "contract.h"

void MD5::Init(BOOL fConstructed)
    {
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    
    // These two fields are read only, and so initialization thereof can be 
    // omitted on the second and subsequent hashes using this same instance.
    //
    if (!fConstructed)
        {
        memset(m_padding, 0, 64);
        m_padding[0]=0x80;
        }

    m_cbitHashed = 0;
    m_cbData     = 0;
    u.m_a = 0x67452301;   // magic
    u.m_b = 0xefcdab89;   //      ... constants
    u.m_c = 0x98badcfe;   //              ... per
    u.m_d = 0x10325476;   //                      .. RFC1321
    }


void MD5::HashMore(const void* pvInput, ULONG cbInput)
// Hash the additional data into the state
    {
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    const BYTE* pbInput = (const BYTE*)pvInput;

    m_cbitHashed += (((ULONGLONG)cbInput) <<3);

    ULONG cbRemaining = 64 - m_cbData;
    if (cbInput < cbRemaining)
        {
        // It doesn't fill up the buffer, so just store it
        memcpy(&m_data[m_cbData], pbInput, cbInput);
        m_cbData += cbInput;
        }
    else
        {
        // It does fill up the buffer. Fill up all that it will take
        memcpy(&m_data[m_cbData], pbInput, cbRemaining);

        // Hash the now-full buffer
        MD5Transform(m_state, (ULONG*)&m_data[0]);
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:22019) // Suppress this OACR warning 22019: 
                               //     'cbInput-=cbRemaining' may be greater than 'cbInput'. This can be caused by integer underflow. 
                               //     This could yield an incorrect loop index 'cbInput>=64'
                               // We only enter the else clause here if cbInput >= cbRemaining
#endif
        cbInput -= cbRemaining;
#ifdef _PREFAST_
#pragma warning(pop)
#endif
        pbInput += cbRemaining;

        // Hash the data in 64-byte runs, starting just after what we've copied
        while (cbInput >= 64)
            {
            if (IS_ALIGNED(pbInput, sizeof(ULONG)))
                {
                MD5Transform(m_state, (ULONG*)pbInput);
                }
            else
                {
                ULONG inputCopy[64 / sizeof(ULONG)];
                memcpy(inputCopy, pbInput, sizeof(inputCopy));
                MD5Transform(m_state, inputCopy);
                }
            pbInput += 64;
            cbInput -= 64;
            }

        // Store the tail of the input into the buffer
        memcpy(&m_data[0], pbInput, cbInput);
        m_cbData = cbInput;
        }
    }


void MD5::GetHashValue(MD5HASHDATA* phash)
// Finalize the hash by appending the necessary padding and length count. Then
// return the final hash value.
    {
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    union {
        ULONGLONG cbitHashed;
        BYTE      rgb[8];
        }u;

    // Remember how many bits there were in the input data
    u.cbitHashed = m_cbitHashed;

    // Calculate amount of padding needed. Enough so total byte count hashed is 56 mod 64
    ULONG cbPad = (m_cbData < 56 ? 56-m_cbData : 120-m_cbData);

    // Hash the padding
    HashMore(&m_padding[0], cbPad);

    // Hash the (before padding) bit length
    HashMore(&u.rgb[0], 8);

    // Return the hash value
    memcpy(phash, &this->u.m_a, 16);
    }




    ////////////////////////////////////////////////////////////////
    //
    // ROTATE_LEFT should be a macro that updates its first operand
    // with its present value rotated left by the amount of its 
    // second operand, which is always a constant.
    // 
    // One way to portably do it would be
    //
    //      #define ROL(x, n)        (((x) << (n)) | ((x) >> (32-(n))))
    //      #define ROTATE_LEFT(x,n) (x) = ROL(x,n)
    //
    // but our compiler has an intrinsic!

    #if (defined(_X86_) || defined(_ARM_)) && defined(PLATFORM_UNIX)
    #define ROL(x, n)        (((x) << (n)) | ((x) >> (32-(n))))
    #define ROTATE_LEFT(x,n) (x) = ROL(x,n)
    #else
    #define ROTATE_LEFT(x,n) (x) = _lrotl(x,n)
    #endif

    ////////////////////////////////////////////////////////////////
    //
    // Constants used in each of the various rounds

    #define MD5_S11 7
    #define MD5_S12 12
    #define MD5_S13 17
    #define MD5_S14 22
    #define MD5_S21 5
    #define MD5_S22 9
    #define MD5_S23 14
    #define MD5_S24 20
    #define MD5_S31 4
    #define MD5_S32 11
    #define MD5_S33 16
    #define MD5_S34 23
    #define MD5_S41 6
    #define MD5_S42 10
    #define MD5_S43 15
    #define MD5_S44 21

    ////////////////////////////////////////////////////////////////
    //
    // The core twiddle functions

//  #define F(x, y, z) (((x) & (y)) | ((~x) & (z)))         // the function per the standard
    #define F(x, y, z) ((((z) ^ (y)) & (x)) ^ (z))          // an alternate encoding

//  #define G(x, y, z) (((x) & (z)) | ((y) & (~z)))         // the function per the standard
    #define G(x, y, z) ((((x) ^ (y)) & (z)) ^ (y))          // an alternate encoding

    #define H(x, y, z) ((x) ^ (y) ^ (z))

    #define I(x, y, z) ((y) ^ ((x) | (~z)))

    #define AC(ac)  ((ULONG)(ac))
    
    ////////////////////////////////////////////////////////////////

    #define FF(a, b, c, d, x, s, ac) { \
        (a) += F (b,c,d) + (x) + (AC(ac)); \
        ROTATE_LEFT (a, s); \
        (a) += (b); \
        }
    
    ////////////////////////////////////////////////////////////////
    
    #define GG(a, b, c, d, x, s, ac) { \
        (a) += G (b,c,d) + (x) + (AC(ac)); \
        ROTATE_LEFT (a, s); \
        (a) += (b); \
        }

    ////////////////////////////////////////////////////////////////

    #define HH(a, b, c, d, x, s, ac) { \
        (a) += H (b,c,d) + (x) + (AC(ac)); \
        ROTATE_LEFT (a, s); \
        (a) += (b); \
        }
    
    ////////////////////////////////////////////////////////////////
    
    #define II(a, b, c, d, x, s, ac) { \
        (a) += I (b,c,d) + (x) + (AC(ac)); \
        ROTATE_LEFT (a, s); \
        (a) += (b); \
        }

    void __stdcall MD5Transform(ULONG state[4], const ULONG* data)
        {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;

        _ASSERTE(IS_ALIGNED(data, sizeof(ULONG)));

        ULONG a=state[0];
        ULONG b=state[1];
        ULONG c=state[2];
        ULONG d=state[3];

        // Round 1
        FF (a, b, c, d, data[ 0], MD5_S11, 0xd76aa478); // 1
        FF (d, a, b, c, data[ 1], MD5_S12, 0xe8c7b756); // 2 
        FF (c, d, a, b, data[ 2], MD5_S13, 0x242070db); // 3 
        FF (b, c, d, a, data[ 3], MD5_S14, 0xc1bdceee); // 4 
        FF (a, b, c, d, data[ 4], MD5_S11, 0xf57c0faf); // 5 
        FF (d, a, b, c, data[ 5], MD5_S12, 0x4787c62a); // 6 
        FF (c, d, a, b, data[ 6], MD5_S13, 0xa8304613); // 7 
        FF (b, c, d, a, data[ 7], MD5_S14, 0xfd469501); // 8 
        FF (a, b, c, d, data[ 8], MD5_S11, 0x698098d8); // 9 
        FF (d, a, b, c, data[ 9], MD5_S12, 0x8b44f7af); // 10 
        FF (c, d, a, b, data[10], MD5_S13, 0xffff5bb1); // 11 
        FF (b, c, d, a, data[11], MD5_S14, 0x895cd7be); // 12 
        FF (a, b, c, d, data[12], MD5_S11, 0x6b901122); // 13 
        FF (d, a, b, c, data[13], MD5_S12, 0xfd987193); // 14 
        FF (c, d, a, b, data[14], MD5_S13, 0xa679438e); // 15 
        FF (b, c, d, a, data[15], MD5_S14, 0x49b40821); // 16 

        // Round 2
        GG (a, b, c, d, data[ 1], MD5_S21, 0xf61e2562); // 17 
        GG (d, a, b, c, data[ 6], MD5_S22, 0xc040b340); // 18 
        GG (c, d, a, b, data[11], MD5_S23, 0x265e5a51); // 19 
        GG (b, c, d, a, data[ 0], MD5_S24, 0xe9b6c7aa); // 20 
        GG (a, b, c, d, data[ 5], MD5_S21, 0xd62f105d); // 21 
        GG (d, a, b, c, data[10], MD5_S22,  0x2441453); // 22 
        GG (c, d, a, b, data[15], MD5_S23, 0xd8a1e681); // 23 
        GG (b, c, d, a, data[ 4], MD5_S24, 0xe7d3fbc8); // 24 
        GG (a, b, c, d, data[ 9], MD5_S21, 0x21e1cde6); // 25 
        GG (d, a, b, c, data[14], MD5_S22, 0xc33707d6); // 26 
        GG (c, d, a, b, data[ 3], MD5_S23, 0xf4d50d87); // 27 
        GG (b, c, d, a, data[ 8], MD5_S24, 0x455a14ed); // 28 
        GG (a, b, c, d, data[13], MD5_S21, 0xa9e3e905); // 29 
        GG (d, a, b, c, data[ 2], MD5_S22, 0xfcefa3f8); // 30 
        GG (c, d, a, b, data[ 7], MD5_S23, 0x676f02d9); // 31 
        GG (b, c, d, a, data[12], MD5_S24, 0x8d2a4c8a); // 32 

        // Round 3
        HH (a, b, c, d, data[ 5], MD5_S31, 0xfffa3942); // 33 
        HH (d, a, b, c, data[ 8], MD5_S32, 0x8771f681); // 34 
        HH (c, d, a, b, data[11], MD5_S33, 0x6d9d6122); // 35 
        HH (b, c, d, a, data[14], MD5_S34, 0xfde5380c); // 36 
        HH (a, b, c, d, data[ 1], MD5_S31, 0xa4beea44); // 37 
        HH (d, a, b, c, data[ 4], MD5_S32, 0x4bdecfa9); // 38 
        HH (c, d, a, b, data[ 7], MD5_S33, 0xf6bb4b60); // 39 
        HH (b, c, d, a, data[10], MD5_S34, 0xbebfbc70); // 40 
        HH (a, b, c, d, data[13], MD5_S31, 0x289b7ec6); // 41 
        HH (d, a, b, c, data[ 0], MD5_S32, 0xeaa127fa); // 42 
        HH (c, d, a, b, data[ 3], MD5_S33, 0xd4ef3085); // 43 
        HH (b, c, d, a, data[ 6], MD5_S34,  0x4881d05); // 44 
        HH (a, b, c, d, data[ 9], MD5_S31, 0xd9d4d039); // 45 
        HH (d, a, b, c, data[12], MD5_S32, 0xe6db99e5); // 46 
        HH (c, d, a, b, data[15], MD5_S33, 0x1fa27cf8); // 47 
        HH (b, c, d, a, data[ 2], MD5_S34, 0xc4ac5665); // 48 

        // Round 4
        II (a, b, c, d, data[ 0], MD5_S41, 0xf4292244); // 49 
        II (d, a, b, c, data[ 7], MD5_S42, 0x432aff97); // 50 
        II (c, d, a, b, data[14], MD5_S43, 0xab9423a7); // 51 
        II (b, c, d, a, data[ 5], MD5_S44, 0xfc93a039); // 52 
        II (a, b, c, d, data[12], MD5_S41, 0x655b59c3); // 53 
        II (d, a, b, c, data[ 3], MD5_S42, 0x8f0ccc92); // 54 
        II (c, d, a, b, data[10], MD5_S43, 0xffeff47d); // 55 
        II (b, c, d, a, data[ 1], MD5_S44, 0x85845dd1); // 56 
        II (a, b, c, d, data[ 8], MD5_S41, 0x6fa87e4f); // 57 
        II (d, a, b, c, data[15], MD5_S42, 0xfe2ce6e0); // 58 
        II (c, d, a, b, data[ 6], MD5_S43, 0xa3014314); // 59 
        II (b, c, d, a, data[13], MD5_S44, 0x4e0811a1); // 60 
        II (a, b, c, d, data[ 4], MD5_S41, 0xf7537e82); // 61 
        II (d, a, b, c, data[11], MD5_S42, 0xbd3af235); // 62 
        II (c, d, a, b, data[ 2], MD5_S43, 0x2ad7d2bb); // 63 
        II (b, c, d, a, data[ 9], MD5_S44, 0xeb86d391); // 64 

        state[0] += a;
        state[1] += b;
        state[2] += c;
        state[3] += d;
        }
