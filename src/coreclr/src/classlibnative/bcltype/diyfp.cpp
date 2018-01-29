// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: diyfp.cpp
//

//

#include "diyfp.h"
#include "fp.h"

void DiyFp::Minus(const DiyFp& rhs)
{
    _ASSERTE(m_e == rhs.e());
    _ASSERTE(m_f >= rhs.f());

    m_f -= rhs.f();
}

void DiyFp::Minus(const DiyFp& left, const DiyFp& right, DiyFp& result)
{
    result = left;
    result.Minus(right);
}

void DiyFp::Multiply(const DiyFp& rhs)
{
    UINT64 m32 = 0xFFFFFFFF;

    UINT64 a = m_f >> 32;
    UINT64 b = m_f & m32;
    UINT64 c = rhs.f() >> 32;
    UINT64 d = rhs.f() & m32;

    UINT64 ac = a * c;
    UINT64 bc = b * c;
    UINT64 ad = a * d;
    UINT64 bd = b * d;

    UINT64 tmp = (bd >> 32) + (ad & m32) + (bc & m32);
    tmp += 1U << 31;

    m_f = ac + (ad >> 32) + (bc >> 32) + (tmp >> 32);
    m_e = m_e + rhs.e() + SIGNIFICAND_LENGTH;
}

void DiyFp::Multiply(const DiyFp& left, const DiyFp& right, DiyFp& result)
{
    result = left;
    result.Multiply(right);
}

void DiyFp::GenerateNormalizedDiyFp(double value, DiyFp& result)
{
    _ASSERTE(value > 0.0);

    UINT64 f = 0;
    int e = 0;
    ExtractFractionAndBiasedExponent(value, &f, &e);

    UINT64 normalizeBit = (UINT64)1 << 52;
    while ((f & normalizeBit) == 0)
    {
        f <<= 1;
        --e;
    }

    int lengthDiff = DiyFp::SIGNIFICAND_LENGTH - 53;
    f <<= lengthDiff;
    e -= lengthDiff;

    result.SetSignificand(f);
    result.SetExponent(e);
}