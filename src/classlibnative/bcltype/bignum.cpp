// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: bignum.cpp
//

//

#include "bignum.h"
#include <intrin.h>

constexpr UINT32 BigNum::m_power10UInt32Table[UINT32POWER10NUM];
BigNum BigNum::m_power10BigNumTable[BIGPOWER10NUM];
BigNum::StaticInitializer BigNum::m_initializer;

BigNum::BigNum()
    :m_len(0) // Note: we do not zeroing m_blocks due to performance.
{
}

BigNum::BigNum(UINT32 value)
{
    SetUInt32(value);
}

BigNum::BigNum(UINT64 value)
{
    SetUInt64(value);
}

BigNum::~BigNum()
{
}

BigNum& BigNum::operator=(const BigNum &rhs)
{
    memcpy(m_blocks, rhs.m_blocks, ((UINT32)rhs.m_len) * sizeof(UINT32));
    m_len = rhs.m_len;

    return *this;
}

int BigNum::Compare(const BigNum& lhs, const BigNum& rhs)
{
    _ASSERTE(lhs.m_len <= BIGSIZE);
    _ASSERTE(rhs.m_len <= BIGSIZE);

    int lenDiff = (int)lhs.m_len - (int)rhs.m_len;
    if (lenDiff != 0)
    {
        return lenDiff;
    }

    if (lhs.m_len == 0)
    {
        _ASSERTE(rhs.m_len == 0);

        return 0;
    }

    for (INT32 currentIndex = lhs.m_len - 1; currentIndex >= 0; --currentIndex)
    {
        INT64 diff = (INT64)(lhs.m_blocks[currentIndex]) - (INT64)(rhs.m_blocks[currentIndex]);
        if (diff != 0)
        {
            return diff > 0 ? 1 : -1;
        }
    }

    return 0;
}

void BigNum::ShiftLeft(UINT64 input, UINT32 shift, BigNum& output)
{
    if (shift == 0)
    {
        return;
    }

    UINT32 shiftBlocks = shift / 32;
    UINT32 remaningToShiftBits = shift % 32;

    if (shiftBlocks > 0)
    {
        // If blocks shifted, we should fill the corresponding blocks with zero.
        output.ExtendBlocks(0, shiftBlocks);
    }

    if (remaningToShiftBits == 0)
    {
        // We shift 32 * n (n >= 1) bits. No remaining bits.
        output.ExtendBlock((UINT32)(input & 0xFFFFFFFF));

        UINT32 highBits = (UINT32)(input >> 32);
        if (highBits != 0)
        {
            output.ExtendBlock(highBits);
        }
    }
    else
    {
        // Extract the high position bits which would be shifted out of range.
        UINT32 highPositionBits = (UINT32)input >> (64 - remaningToShiftBits);

        // Shift the input. The result should be stored to current block.
        UINT64 shiftedInput = input << remaningToShiftBits;
        output.ExtendBlock(shiftedInput & 0xFFFFFFFF);

        UINT32 highBits = (UINT32)(input >> 32);
        if (highBits != 0)
        {
            output.ExtendBlock(highBits);
        }

        if (highPositionBits != 0)
        {
            // If the high position bits is not 0, we should store them to next block.
            output.ExtendBlock(highPositionBits);
        }
    }
}

void BigNum::ShiftLeft(UINT32 shift)
{
    if (m_len == 0 || shift == 0)
    {
        return;
    }

    UINT32 shiftBlocks = shift / 32;
    UINT32 shiftBits = shift % 32;

    // Process blocks high to low so that we can safely process in place
    int inLength = m_len;

    // Check if the shift is block aligned
    if (shiftBits == 0)
    {
        // Copy blocks from high to low
        UINT32* pInCurrent = m_blocks + inLength;
        UINT32* pOutCurrent = pInCurrent + shiftBlocks;
        while (pInCurrent >= m_blocks)
        {
            *pOutCurrent = *pInCurrent;

            --pInCurrent;
            --pOutCurrent;
        }

        m_len += shiftBlocks;

        // Zero the remaining low blocks
        memset(m_blocks, 0, shiftBlocks);
    }
    else
    {
        // We need to shift partial blocks
        int inBlockIdx = inLength - 1;
        UINT32 outBlockIdx = inLength + shiftBlocks;

        _ASSERTE(outBlockIdx < BIGSIZE);

        // Set the length to hold the shifted blocks
        m_len = outBlockIdx + 1;

        // Output the initial blocks
        const UINT32 lowBitsShift = (32 - shiftBits);
        UINT32 highBits = 0;
        UINT32 block = m_blocks[inBlockIdx];
        UINT32 lowBits = block >> lowBitsShift;
        while (inBlockIdx > 0)
        {
            m_blocks[outBlockIdx] = highBits | lowBits;
            highBits = block << shiftBits;

            --inBlockIdx;
            --outBlockIdx;

            block = m_blocks[inBlockIdx];
            lowBits = block >> lowBitsShift;
        }

        // Output the final blocks
        m_blocks[outBlockIdx] = highBits | lowBits;
        m_blocks[outBlockIdx - 1] = block << shiftBits;

        // Zero the remaining low blocks
        memset(m_blocks, 0, shiftBlocks * sizeof(UINT32));

        // Check if the terminating block has no set bits
        if (m_blocks[m_len - 1] == 0)
        {
            --m_len;
        }
    }
}

void BigNum::Pow10(int exp, BigNum& result)
{
    // We leverage two arrays - m_power10UInt32Table and m_power10BigNumTable to speed up the 
    // pow10 calculation.
    //
    // m_power10UInt32Table stores the results of 10^0 to 10^7.
    // m_power10BigNumTable stores the results of 10^8, 10^16, 10^32, 10^64, 10^128 and 10^256.
    //
    // For example, let's say exp = (111111)2. We can split the exp to two parts, one is small exp, 
    // which 10^smallExp can be represented as UINT32, another part is 10^bigExp, which must be represented as BigNum. 
    // So the result should be 10^smallExp * 10^bigExp.
    //
    // Calculate 10^smallExp is simple, we just lookup the 10^smallExp from m_power10UInt32Table. 
    // But here's a bad news: although UINT32 can represent 10^9, exp 9's binary representation is 1001. 
    // That means 10^(1011), 10^(1101), 10^(1111) all cannot be stored as UINT32, we cannot easily say something like: 
    // "Any bits <= 3 is small exp, any bits > 3 is big exp". So instead of involving 10^8, 10^9 to m_power10UInt32Table, 
    // consider 10^8 and 10^9 as a bigNum, so they fall into m_power10BigNumTable. Now we can have a simple rule: 
    // "Any bits <= 3 is small exp, any bits > 3 is big exp".
    //
    // For (111111)2, we first calculate 10^(smallExp), which is 10^(7), now we can shift right 3 bits, prepare to calculate the bigExp part, 
    // the exp now becomes (000111)2.
    //
    // Apparently the lowest bit of bigExp should represent 10^8 because we have already shifted 3 bits for smallExp, so m_power10BigNumTable[0] = 10^8.
    // Now let's shift exp right 1 bit, the lowest bit should represent 10^(8 * 2) = 10^16, and so on...
    //
    // That's why we just need the values of m_power10BigNumTable be power of 2.
    //
    // More details of this implementation can be found at: https://github.com/dotnet/coreclr/pull/12894#discussion_r128890596

    BigNum temp1;
    BigNum temp2;

    BigNum* pCurrentTemp = &temp1;
    BigNum* pNextTemp = &temp2;

    // Extract small exp. 
    UINT32 smallExp = exp & 0x7;
    pCurrentTemp->SetUInt32(m_power10UInt32Table[smallExp]);

    exp >>= 3;
    UINT32 idx = 0;

    while (exp != 0)
    {
        // If the current bit is set, multiply it with the corresponding power of 10
        if (exp & 1)
        {
            // Multiply into the next temporary
            Multiply(*pCurrentTemp, m_power10BigNumTable[idx], *pNextTemp);

            // Swap to the next temporary
            BigNum* t = pNextTemp;
            pNextTemp = pCurrentTemp;
            pCurrentTemp = t;
        }

        // Advance to the next bit
        ++idx;
        exp >>= 1;
    }

    result = *pCurrentTemp;
}

void BigNum::PrepareHeuristicDivide(BigNum* pDividend, BigNum* pDivisor)
{
    UINT32 hiBlock = pDivisor->m_blocks[pDivisor->m_len - 1];
    if (hiBlock < 8 || hiBlock > 429496729)
    {
        // Inspired by http://www.ryanjuckett.com/programming/printing-floating-point-numbers/
        // Perform a bit shift on all values to get the highest block of the divisor into
        // the range [8,429496729]. We are more likely to make accurate quotient estimations
        // in heuristicDivide() with higher divisor values so
        // we shift the divisor to place the highest bit at index 27 of the highest block.
        // This is safe because (2^28 - 1) = 268435455 which is less than 429496729. This means
        // that all values with a highest bit at index 27 are within range.
        UINT32 hiBlockLog2 = LogBase2(hiBlock);
        UINT32 shift = (59 - hiBlockLog2) % 32;

        pDivisor->ShiftLeft(shift);
        pDividend->ShiftLeft(shift);
    }
}

UINT32 BigNum::HeuristicDivide(BigNum* pDividend, const BigNum& divisor)
{
    UINT32 len = divisor.m_len;
    if (pDividend->m_len < len)
    {
        return 0;
    }

    const UINT32* pFinalDivisorBlock = divisor.m_blocks + len - 1;
    UINT32* pFinalDividendBlock = pDividend->m_blocks + len - 1;

    // This is an estimated quotient. Its error should be less than 2.
    // Reference inequality:
    // a/b - floor(floor(a)/(floor(b) + 1)) < 2
    UINT32 quotient = *pFinalDividendBlock / (*pFinalDivisorBlock + 1);

    if (quotient != 0)
    {
        // Now we use our estimated quotient to update each block of dividend.
        // dividend = dividend - divisor * quotient
        const UINT32 *pDivisorCurrent = divisor.m_blocks;
        UINT32 *pDividendCurrent = pDividend->m_blocks;

        UINT64 borrow = 0;
        UINT64 carry = 0;
        do
        {
            UINT64 product = (UINT64)*pDivisorCurrent * (UINT64)quotient + carry;
            carry = product >> 32;

            UINT64 difference = (UINT64)*pDividendCurrent - (product & 0xFFFFFFFF) - borrow;
            borrow = (difference >> 32) & 1;

            *pDividendCurrent = difference & 0xFFFFFFFF;

            ++pDivisorCurrent;
            ++pDividendCurrent;
        } while (pDivisorCurrent <= pFinalDivisorBlock);

        // Remove all leading zero blocks from dividend
        while (len > 0 && pDividend->m_blocks[len - 1] == 0)
        {
            --len;
        }

        pDividend->m_len = len;
    }

    // If the dividend is still larger than the divisor, we overshot our estimate quotient. To correct,
    // we increment the quotient and subtract one more divisor from the dividend (Because we guaranteed the error range).
    if (BigNum::Compare(*pDividend, divisor) >= 0)
    {
        ++quotient;

        // dividend = dividend - divisor
        const UINT32 *pDivisorCur = divisor.m_blocks;
        UINT32 *pDividendCur = pDividend->m_blocks;

        UINT64 borrow = 0;
        do
        {
            UINT64 difference = (UINT64)*pDividendCur - (UINT64)*pDivisorCur - borrow;
            borrow = (difference >> 32) & 1;

            *pDividendCur = difference & 0xFFFFFFFF;

            ++pDivisorCur;
            ++pDividendCur;
        } while (pDivisorCur <= pFinalDivisorBlock);

        // Remove all leading zero blocks from dividend
        while (len > 0 && pDividend->m_blocks[len - 1] == 0)
        {
            --len;
        }

        pDividend->m_len = len;
    }

    return quotient;
}

void BigNum::Multiply(UINT32 value)
{
    Multiply(*this, value, *this);
}

void BigNum::Multiply(const BigNum& value)
{
    BigNum temp;
    BigNum::Multiply(*this, value, temp);

    memcpy(m_blocks, temp.m_blocks, ((UINT32)temp.m_len) * sizeof(UINT32));
    m_len = temp.m_len;
}

void BigNum::Multiply(const BigNum& lhs, UINT32 value, BigNum& result)
{
    if (lhs.IsZero() || value == 1)
    {
        result = lhs;

        return;
    }

    if (value == 0)
    {
        result.SetZero();

        return;
    }

    const UINT32* pCurrent = lhs.m_blocks;
    const UINT32* pEnd = pCurrent + lhs.m_len;
    UINT32* pResultCurrent = result.m_blocks;

    UINT64 carry = 0;
    while (pCurrent != pEnd)
    {
        UINT64 product = (UINT64)(*pCurrent) * (UINT64)value + carry;
        carry = product >> 32;
        *pResultCurrent = (UINT32)(product & 0xFFFFFFFF);

        ++pResultCurrent;
        ++pCurrent;
    }

    if (carry != 0)
    {
        _ASSERTE(lhs.m_len + 1 <= BIGSIZE);
        *pResultCurrent = (UINT32)carry;
        result.m_len += lhs.m_len + 1;
    }
}

void BigNum::Multiply(const BigNum& lhs, const BigNum& rhs, BigNum& result)
{
    if (lhs.IsZero() || (rhs.m_len == 1 && rhs.m_blocks[0] == 1))
    {
        result = lhs;

        return;
    }

    if (rhs.IsZero())
    {
        result.SetZero();

        return;
    }

    const BigNum* pLarge = NULL;
    const BigNum* pSmall = NULL;
    if (lhs.m_len < rhs.m_len)
    {
        pSmall = &lhs;
        pLarge = &rhs;
    }
    else
    {
        pSmall = &rhs;
        pLarge = &lhs;
    }

    UINT32 maxResultLength = pSmall->m_len + pLarge->m_len;
    _ASSERTE(maxResultLength <= BIGSIZE);

    // Zero out result internal blocks.
    memset(result.m_blocks, 0, sizeof(UINT32) * BIGSIZE);

    const UINT32* pLargeBegin = pLarge->m_blocks;
    const UINT32* pLargeEnd = pLarge->m_blocks + pLarge->m_len;

    UINT32* pResultStart = result.m_blocks;
    const UINT32* pSmallCurrent = pSmall->m_blocks;
    const UINT32* pSmallEnd = pSmallCurrent + pSmall->m_len;

    while (pSmallCurrent != pSmallEnd)
    {
        // Multiply each block of large BigNum.
        if (*pSmallCurrent != 0)
        {
            const UINT32* pLargeCurrent = pLargeBegin;
            UINT32* pResultCurrent = pResultStart;
            UINT64 carry = 0;

            do
            {
                UINT64 product = (UINT64)(*pResultCurrent) + (UINT64)(*pSmallCurrent) * (UINT64)(*pLargeCurrent) + carry;
                carry = product >> 32;
                *pResultCurrent = (UINT32)(product & 0xFFFFFFFF);

                ++pResultCurrent;
                ++pLargeCurrent;
            } while (pLargeCurrent != pLargeEnd);

            *pResultCurrent = (UINT32)(carry & 0xFFFFFFFF);
        }

        ++pSmallCurrent;
        ++pResultStart;
    }

    if (maxResultLength > 0 && result.m_blocks[maxResultLength - 1] == 0)
    {
        result.m_len = maxResultLength - 1;
    }
    else
    {
        result.m_len = maxResultLength;
    }
}

void BigNum::Multiply10()
{
    if (IsZero())
    {
        return;
    }

    const UINT32* pCurrent = m_blocks;
    const UINT32* pEnd = pCurrent + m_len;
    UINT32* pResultCurrent = m_blocks;

    UINT64 carry = 0;
    while (pCurrent != pEnd)
    {
        UINT64 product = ((UINT64)(*pCurrent) << 3) +  ((UINT64)(*pCurrent) << 1) + carry;
        carry = product >> 32;
        *pResultCurrent = (UINT32)(product & 0xFFFFFFFF);

        ++pResultCurrent;
        ++pCurrent;
    }

    if (carry != 0)
    {
        _ASSERTE(m_len + 1 <= BIGSIZE);
        *pResultCurrent = (UINT32)carry;
        m_len += 1;
    }
}

bool BigNum::IsZero() const
{
    return m_len == 0;
}

void BigNum::SetUInt32(UINT32 value)
{
    m_len = 1;
    m_blocks[0] = value;
}

void BigNum::SetUInt64(UINT64 value)
{
    m_blocks[0] = (UINT32)(value & 0xFFFFFFFF);
    m_blocks[1] = (UINT32)(value >> 32);
    m_len = (m_blocks[1] == 0) ? 1 : 2;
}

void BigNum::SetZero()
{
    m_len = 0;
}

void BigNum::ExtendBlock(UINT32 blockValue)
{
    m_blocks[m_len] = blockValue;
    ++m_len;
}

void BigNum::ExtendBlocks(UINT32 blockValue, UINT32 blockCount)
{
    _ASSERTE(blockCount > 0);

    if (blockCount == 1)
    {
        ExtendBlock(blockValue);

        return;
    }

    memset(m_blocks + m_len, 0, (blockCount - 1) * sizeof(UINT32));
    m_len += blockCount;
    m_blocks[m_len - 1] = blockValue;
}

UINT32 BigNum::LogBase2(UINT32 value)
{
    _ASSERTE(value != 0);

    DWORD r;
    BitScanReverse(&r, (DWORD)value);

    return (UINT32)r;
}

UINT32 BigNum::LogBase2(UINT64 value)
{
    _ASSERTE(value != 0);

#if defined(_TARGET_X86_) && !defined(FEATURE_PAL)
    UINT64 temp = value >> 32;
    if (temp != 0)
    {
        return 32 + LogBase2((UINT32)temp);
    }

    return LogBase2((UINT32)value);
#else
    DWORD r;
    BitScanReverse64(&r, (DWORD64)value);

    return (UINT32)r;
#endif
}
