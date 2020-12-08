// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#if defined(_MSC_VER)
#pragma hdrstop
#endif // defined(_MSC_VER)

// Table of primes and their magic-number-divide constant.
// For more info see the book "Hacker's Delight" chapter 10.9 "Unsigned Division by Divisors >= 1"
// These were selected by looking for primes, each roughly twice as big as the next, having
// 32-bit magic numbers, (because the algorithm for using 33-bit magic numbers is slightly slower).

#include "jithashtable.h"

// Table of primes and their magic-number-divide constant.
// For more info see the book "Hacker's Delight" chapter 10.9 "Unsigned Division by Divisors >= 1"
// These were selected by looking for primes, each roughly twice as big as the next, having
// 32-bit magic numbers, (because the algorithm for using 33-bit magic numbers is slightly slower).

// clang-format off
const JitPrimeInfo jitPrimeInfo[]
{
    JitPrimeInfo(9,         0x38e38e39, 1),
    JitPrimeInfo(23,        0xb21642c9, 4),
    JitPrimeInfo(59,        0x22b63cbf, 3),
    JitPrimeInfo(131,       0xfa232cf3, 7),
    JitPrimeInfo(239,       0x891ac73b, 7),
    JitPrimeInfo(433,       0x975a751,  4),
    JitPrimeInfo(761,       0x561e46a5, 8),
    JitPrimeInfo(1399,      0xbb612aa3, 10),
    JitPrimeInfo(2473,      0x6a009f01, 10),
    JitPrimeInfo(4327,      0xf2555049, 12),
    JitPrimeInfo(7499,      0x45ea155f, 11),
    JitPrimeInfo(12973,     0x1434f6d3, 10),
    JitPrimeInfo(22433,     0x2ebe18db, 12),
    JitPrimeInfo(46559,     0xb42bebd5, 15),
    JitPrimeInfo(96581,     0xadb61b1b, 16),
    JitPrimeInfo(200341,    0x29df2461, 15),
    JitPrimeInfo(415517,    0xa181c46d, 18),
    JitPrimeInfo(861719,    0x4de0bde5, 18),
    JitPrimeInfo(1787021,   0x9636c46f, 20),
    JitPrimeInfo(3705617,   0x4870adc1, 20),
    JitPrimeInfo(7684087,   0x8bbc5b83, 22),
    JitPrimeInfo(15933877,  0x86c65361, 23),
    JitPrimeInfo(33040633,  0x40fec79b, 23),
    JitPrimeInfo(68513161,  0x7d605cd1, 25),
    JitPrimeInfo(142069021, 0xf1da390b, 27),
    JitPrimeInfo(294594427, 0x74a2507d, 27),
    JitPrimeInfo(733045421, 0x5dbec447, 28),
};
// clang-format on
