// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: bignum.h
//

//

#ifndef _BIGNUM_H_
#define _BIGNUM_H_

#include <clrtypes.h>

class BigNum
{
public:
    BigNum();
    BigNum(UINT32 value);
    BigNum(UINT64 value);
    ~BigNum();

    BigNum & operator=(const BigNum &rhs);

    static UINT32 LogBase2(UINT32 val);
    static UINT32 LogBase2(UINT64 val);

    static int Compare(const BigNum& lhs, const BigNum& rhs);

    static void ShiftLeft(UINT64 input, UINT32 shift, BigNum& output);
    static void Pow10(int exp, BigNum& result);
    static void PrepareHeuristicDivide(BigNum* pDividend, BigNum* divisor);
    static UINT32 HeuristicDivide(BigNum* pDividend, const BigNum& divisor);
    static void Multiply(const BigNum& lhs, UINT32 value, BigNum& result);
    static void Multiply(const BigNum& lhs, const BigNum& rhs, BigNum& result);

    bool IsZero() const;

    void Multiply(UINT32 value);
    void Multiply(const BigNum& value);
    void Multiply10();
    void ShiftLeft(UINT32 shift);
    void SetUInt32(UINT32 value);
    void SetUInt64(UINT64 value);
    void SetZero();
    void ExtendBlock(UINT32 blockValue);
    void ExtendBlocks(UINT32 blockValue, UINT32 blockCount);

private:

    static const UINT32 BIGSIZE = 35;
    static const UINT32 UINT32POWER10NUM = 8;
    static const UINT32 BIGPOWER10NUM = 6;
    static constexpr UINT32 m_power10UInt32Table[UINT32POWER10NUM] = 
    {
            1,          // 10^0
            10,         // 10^1
            100,        // 10^2
            1000,       // 10^3
            10000,      // 10^4
            100000,     // 10^5
            1000000,    // 10^6
            10000000,   // 10^7
    };
    static BigNum m_power10BigNumTable[BIGPOWER10NUM];

    static class StaticInitializer
    {
    public:
        StaticInitializer()
        {
            // 10^8
            m_power10BigNumTable[0].m_len = (UINT32)1;
            m_power10BigNumTable[0].m_blocks[0] = (UINT32)100000000;

            // 10^16
            m_power10BigNumTable[1].m_len = (UINT32)2;
            m_power10BigNumTable[1].m_blocks[0] = (UINT32)0x6fc10000;
            m_power10BigNumTable[1].m_blocks[1] = (UINT32)0x002386f2;

            // 10^32
            m_power10BigNumTable[2].m_len = (UINT32)4;
            m_power10BigNumTable[2].m_blocks[0] = (UINT32)0x00000000;
            m_power10BigNumTable[2].m_blocks[1] = (UINT32)0x85acef81;
            m_power10BigNumTable[2].m_blocks[2] = (UINT32)0x2d6d415b;
            m_power10BigNumTable[2].m_blocks[3] = (UINT32)0x000004ee;

            // 10^64
            m_power10BigNumTable[3].m_len = (UINT32)7;
            m_power10BigNumTable[3].m_blocks[0] = (UINT32)0x00000000;
            m_power10BigNumTable[3].m_blocks[1] = (UINT32)0x00000000;
            m_power10BigNumTable[3].m_blocks[2] = (UINT32)0xbf6a1f01;
            m_power10BigNumTable[3].m_blocks[3] = (UINT32)0x6e38ed64;
            m_power10BigNumTable[3].m_blocks[4] = (UINT32)0xdaa797ed;
            m_power10BigNumTable[3].m_blocks[5] = (UINT32)0xe93ff9f4;
            m_power10BigNumTable[3].m_blocks[6] = (UINT32)0x00184f03;

            // 10^128
            m_power10BigNumTable[4].m_len = (UINT32)14;
            m_power10BigNumTable[4].m_blocks[0] = (UINT32)0x00000000;
            m_power10BigNumTable[4].m_blocks[1] = (UINT32)0x00000000;
            m_power10BigNumTable[4].m_blocks[2] = (UINT32)0x00000000;
            m_power10BigNumTable[4].m_blocks[3] = (UINT32)0x00000000;
            m_power10BigNumTable[4].m_blocks[4] = (UINT32)0x2e953e01;
            m_power10BigNumTable[4].m_blocks[5] = (UINT32)0x03df9909;
            m_power10BigNumTable[4].m_blocks[6] = (UINT32)0x0f1538fd;
            m_power10BigNumTable[4].m_blocks[7] = (UINT32)0x2374e42f;
            m_power10BigNumTable[4].m_blocks[8] = (UINT32)0xd3cff5ec;
            m_power10BigNumTable[4].m_blocks[9] = (UINT32)0xc404dc08;
            m_power10BigNumTable[4].m_blocks[10] = (UINT32)0xbccdb0da;
            m_power10BigNumTable[4].m_blocks[11] = (UINT32)0xa6337f19;
            m_power10BigNumTable[4].m_blocks[12] = (UINT32)0xe91f2603;
            m_power10BigNumTable[4].m_blocks[13] = (UINT32)0x0000024e;

            // 10^256
            m_power10BigNumTable[5].m_len = (UINT32)27;
            m_power10BigNumTable[5].m_blocks[0] = (UINT32)0x00000000;
            m_power10BigNumTable[5].m_blocks[1] = (UINT32)0x00000000;
            m_power10BigNumTable[5].m_blocks[2] = (UINT32)0x00000000;
            m_power10BigNumTable[5].m_blocks[3] = (UINT32)0x00000000;
            m_power10BigNumTable[5].m_blocks[4] = (UINT32)0x00000000;
            m_power10BigNumTable[5].m_blocks[5] = (UINT32)0x00000000;
            m_power10BigNumTable[5].m_blocks[6] = (UINT32)0x00000000;
            m_power10BigNumTable[5].m_blocks[7] = (UINT32)0x00000000;
            m_power10BigNumTable[5].m_blocks[8] = (UINT32)0x982e7c01;
            m_power10BigNumTable[5].m_blocks[9] = (UINT32)0xbed3875b;
            m_power10BigNumTable[5].m_blocks[10] = (UINT32)0xd8d99f72;
            m_power10BigNumTable[5].m_blocks[11] = (UINT32)0x12152f87;
            m_power10BigNumTable[5].m_blocks[12] = (UINT32)0x6bde50c6;
            m_power10BigNumTable[5].m_blocks[13] = (UINT32)0xcf4a6e70;
            m_power10BigNumTable[5].m_blocks[14] = (UINT32)0xd595d80f;
            m_power10BigNumTable[5].m_blocks[15] = (UINT32)0x26b2716e;
            m_power10BigNumTable[5].m_blocks[16] = (UINT32)0xadc666b0;
            m_power10BigNumTable[5].m_blocks[17] = (UINT32)0x1d153624;
            m_power10BigNumTable[5].m_blocks[18] = (UINT32)0x3c42d35a;
            m_power10BigNumTable[5].m_blocks[19] = (UINT32)0x63ff540e;
            m_power10BigNumTable[5].m_blocks[20] = (UINT32)0xcc5573c0;
            m_power10BigNumTable[5].m_blocks[21] = (UINT32)0x65f9ef17;
            m_power10BigNumTable[5].m_blocks[22] = (UINT32)0x55bc28f2;
            m_power10BigNumTable[5].m_blocks[23] = (UINT32)0x80dcc7f7;
            m_power10BigNumTable[5].m_blocks[24] = (UINT32)0xf46eeddc;
            m_power10BigNumTable[5].m_blocks[25] = (UINT32)0x5fdcefce;
            m_power10BigNumTable[5].m_blocks[26] = (UINT32)0x000553f7;
        }
    } m_initializer;

    UINT32 m_blocks[BIGSIZE];
    UINT32 m_len;
};


#endif
