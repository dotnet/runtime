// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: grisu3.cpp
//

//

#include "grisu3.h"
#include <check.h>
#include <math.h>

// 1/lg(10)
const double Grisu3::D_1_LOG2_10 = 0.30102999566398120;

constexpr UINT32 Grisu3::m_cachedPowerOfTen[CACHED_POWER_OF_TEN_NUM]; 
constexpr PowerOfTen Grisu3::m_cachedPowers[CACHED_POWER_NUM];

bool Grisu3::Run(double value, int count, int* dec, int* sign, wchar_t* digits)
{
    // ========================================================================================================================================
    // This implementation is based on the paper: http://www.cs.tufts.edu/~nr/cs257/archive/florian-loitsch/printf.pdf
    // You must read this paper to fully understand the code.
    //
    // Deviation: Instead of generating shortest digits, we generate the digits according to the input count.
    // Therefore, we do not need m+ and m- which are used to determine the exact range of values.
    // ======================================================================================================================================== 
    //
    // Overview:
    //
    // The idea of Grisu3 is to leverage additional bits and cached power of ten to produce the digits.
    // We need to create a handmade floating point data structure DiyFp to extend the bits of double.
    // We also need to cache the powers of ten for digits generation. By choosing the correct index of powers
    // we need to start with, we can eliminate the expensive big num divide operation.
    //
    // Grisu3 is imprecision for some numbers. Fortunately, the algorithm itself can determine that and give us
    // a success/fail flag. We may fall back to other algorithms (For instance, Dragon4) if it fails.
    //
    // w: the normalized DiyFp from the input value.
    // mk: The index of the cached powers.
    // cmk: The cached power.
    // D: Product: w * cmk.
    // kappa: A factor used for generating digits. See step 5 of the Grisu3 procedure in the paper.

    // Handle sign bit.
    if (_signbit(value) != 0)
    {
        value = -value;
        *sign = 1;
    }
    else
    {
        *sign = 0;
    }

    // Step 1: Determine the normalized DiyFp w.

    DiyFp w;
    DiyFp::GenerateNormalizedDiyFp(value, w);

    // Step 2: Find the cached power of ten.

    // Compute the proper index mk.
    int mk = KComp(w.e() + DiyFp::SIGNIFICAND_LENGTH);

    // Retrieve the cached power of ten.
    DiyFp cmk;
    int decimalExponent;
    CachedPower(mk, &cmk, &decimalExponent);

    // Step 3: Scale the w with the cached power of ten.

    DiyFp D;
    DiyFp::Multiply(w, cmk, D);

    // Step 4: Generate digits.

    int kappa;
    int length;
    bool isSuccess = DigitGen(D, count, digits, &length, &kappa);
    if (isSuccess)
    {
        digits[count] = 0;
        *dec = length - decimalExponent + kappa;
    }

    return isSuccess;
}

bool Grisu3::RoundWeed(wchar_t* buffer,
    int len,
    UINT64 rest,
    UINT64 tenKappa,
    UINT64 ulp,
    int* kappa)
{
    _ASSERTE(rest < tenKappa);

    // 1. tenKappa <= ulp: we don't have an idea which way to round.
    // 2. Even if tenKappa > ulp, but if tenKappa <= 2 * ulp we cannot find the way to round.
    // Note: to prevent overflow, we need to use tenKappa - ulp <= ulp.
    if (tenKappa <= ulp || tenKappa - ulp <=  ulp)
    {
        return false;
    }

    // tenKappa >= 2 * (rest + ulp). We should round down.
    // Note: to prevent overflow, we need to check if tenKappa > 2 * rest as a prerequisite.
    if ((tenKappa - rest > rest) && (tenKappa - 2 * rest >= 2 * ulp))
    {
        return true;
    }

    // tenKappa <= 2 * (rest - ulp). We should round up.
    // Note: to prevent overflow, we need to check if rest > ulp as a prerequisite.
    if ((rest > ulp) && (tenKappa <= (rest - ulp) || (tenKappa - (rest - ulp) <= (rest - ulp))))
    {
        // Find all 9s from end to start.
        buffer[len - 1]++;
        for (int i = len - 1; i > 0; --i)
        {
            if (buffer[i] != L'0' + 10)
            {
                // We end up a number less than 9.
                break;
            }

            // Current number becomes 0 and add the promotion to the next number.
            buffer[i] = L'0';
            buffer[i - 1]++;
        }

        if (buffer[0] == L'0' + 10)
        {
            // First number is '0' + 10 means all numbers are 9.
            // We simply make the first number to 1 and increase the kappa.
            buffer[0] = L'1';
            (*kappa) += 1;
        }

        return true;
    }

    return false;
}

bool Grisu3::DigitGen(const DiyFp& mp, int count, wchar_t* buffer, int* len, int* K)
{
    // Split the input mp to two parts. Part 1 is integral. Part 2 can be used to calculate
    // fractional.
    //
    // mp: the input DiyFp scaled by cached power.
    // K: final kappa.
    // p1: part 1.
    // p2: part 2.

    _ASSERTE(count > 0);
    _ASSERTE(buffer != NULL);
    _ASSERTE(len != NULL);
    _ASSERTE(K != NULL);
    _ASSERTE(mp.e() >= ALPHA && mp.e() <= GAMA);

    UINT64 ulp = 1;
    DiyFp one = DiyFp(static_cast<UINT64>(1) << -mp.e(), mp.e());
    UINT32 p1 = static_cast<UINT32>(mp.f() >> -one.e());
    UINT64 p2 = mp.f() & (one.f() - 1);

    // When p2 (fractional part) is zero, we can predicate if p1 is good to produce the numbers in requested digit count:
    //
    // - When requested digit count >= 11, p1 is not be able to exhaust the count as 10^(11 - 1) > UINT32_MAX >= p1.
    // - When p1 < 10^(count - 1), p1 is not be able to exhaust the count.
    // - Otherwise, p1 may have chance to exhaust the count.
    if (p2 == 0 && (count >= 11 || p1 < m_cachedPowerOfTen[count - 1]))
    {
        return false;
    }

    // Note: The code in the paper simply assignes div to TEN9 and kappa to 10 directly.
    // That means we need to check if any leading zero of the generated
    // digits during the while loop, which hurts the performance.
    //
    // Now if we can estimate what the div and kappa, we do not need to check the leading zeros.
    // The idea is to find the biggest power of 10 that is less than or equal to the given number.
    // Then we don't need to worry about the leading zeros and we can get 10% performance gain.
    int length = 0;
    int kappa;
    UINT32 div;
    BiggestPowerTenLessThanOrEqualTo(p1, DiyFp::SIGNIFICAND_LENGTH - (-one.e()), &div, &kappa);
    ++kappa;

    // Produce integral.
    while (kappa > 0)
    {
        int d = p1 / div;
        buffer[length++] = L'0' + d;
        --count;

        p1 %= div;
        --kappa;

        if (count == 0)
        {
            break;
        }

        div /= 10;
    }

    // End up here if we already exhausted the digit count.
    if (count == 0)
    {
        UINT64 rest = (static_cast<UINT64>(p1) << -one.e()) + p2;

        *len = length;
        *K = kappa;

        return RoundWeed(buffer,
            length,
            rest,
            static_cast<UINT64>(div) << -one.e(),
            ulp,
            K);
    }

    // We have to generate digits from part2 if we have requested digit count left
    // and part2 is greater than ulp.
    while (count > 0 && p2 > ulp)
    {
        p2 *= 10;

        int d = static_cast<int>(p2 >> -one.e());
        buffer[length++] = L'0' + d;
        --count;

        p2 &= one.f() - 1;
        --kappa;

        ulp *= 10;
    }

    // If we haven't exhausted the requested digit counts, the Grisu3 algorithm fails.
    if (count != 0)
    {
        return false;
    }

    *len = length;
    *K = kappa;

    return RoundWeed(buffer, length, p2, one.f(), ulp, K);
}

int Grisu3::KComp(int e)
{
    return static_cast<int>(ceil((ALPHA - e + DiyFp::SIGNIFICAND_LENGTH - 1) * D_1_LOG2_10));
}

void Grisu3::CachedPower(int k, DiyFp* cmk, int* decimalExponent)
{
    _ASSERTE(cmk != NULL);
    _ASSERTE(decimalExponent != NULL);

    int index = (POWER_OFFSET + k - 1) / POWER_DECIMAL_EXPONENT_DISTANCE + 1;
    PowerOfTen cachedPower = m_cachedPowers[index];

    cmk->SetSignificand(cachedPower.significand);
    cmk->SetExponent(cachedPower.binaryExponent);
    *decimalExponent = cachedPower.decimalExponent;
}

// Returns the biggest power of ten that is less than or equal to the given number.
void Grisu3::BiggestPowerTenLessThanOrEqualTo(UINT32 number,
                            int bits,
                            UINT32 *power,
                            int *exponent)
{
    switch (bits)
    {
    case 32:
    case 31:
    case 30:
        if (TEN9 <= number)
        {
            *power = TEN9;
            *exponent = 9;
            break;
        }
    case 29:
    case 28:
    case 27:
        if (TEN8 <= number)
        {
            *power = TEN8;
            *exponent = 8;
            break;
        }
    case 26:
    case 25:
    case 24:
        if (TEN7 <= number)
        {
            *power = TEN7;
            *exponent = 7;
            break;
        }
    case 23:
    case 22:
    case 21:
    case 20:
        if (TEN6 <= number)
        {
            *power = TEN6;
            *exponent = 6;
            break;
        }
    case 19:
    case 18:
    case 17:
        if (TEN5 <= number)
        {
            *power = TEN5;
            *exponent = 5;
            break;
        }
    case 16:
    case 15:
    case 14:
        if (TEN4 <= number)
        {
            *power = TEN4;
            *exponent = 4;
            break;
        }
    case 13:
    case 12:
    case 11:
    case 10:
        if (1000 <= number)
        {
            *power = 1000;
            *exponent = 3;
            break;
        }
    case 9:
    case 8:
    case 7:
        if (100 <= number)
        {
            *power = 100;
            *exponent = 2;
            break;
        }
    case 6:
    case 5:
    case 4:
        if (10 <= number)
        {
            *power = 10;
            *exponent = 1;
            break;
        }
    case 3:
    case 2:
    case 1:
        if (1 <= number)
        {
            *power = 1;
            *exponent = 0;
            break;
        }
    case 0:
        *power = 0;
        *exponent = -1;
        break;
    default:
        *power = 0;
        *exponent = 0;
        UNREACHABLE();
    }
}
