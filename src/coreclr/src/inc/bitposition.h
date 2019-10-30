// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _BITPOSITION_H_
#define _BITPOSITION_H_

//------------------------------------------------------------------------
// BitPosition: Return the position of the single bit that is set in 'value'.
//
// Return Value:
//    The position (0 is LSB) of bit that is set in 'value'
//
// Notes:
//    'value' must have exactly one bit set.
//    The algorithm is as follows:
//    - PRIME is a prime bigger than sizeof(unsigned int), which is not of the
//      form 2^n-1.
//    - Taking the modulo of 'value' with this will produce a unique hash for all
//      powers of 2 (which is what "value" is).
//    - Entries in hashTable[] which are -1 should never be used. There
//      should be PRIME-8*sizeof(value) entries which are -1 .
//
inline
unsigned            BitPosition(unsigned value)
{
    _ASSERTE((value != 0) && ((value & (value-1)) == 0));
#if defined(_ARM_) && defined(__llvm__)
    // use intrinsic functions for arm32
    // this is applied for LLVM only but it may work for some compilers
    DWORD index = __builtin_clz(__builtin_arm_rbit(value));
#elif !defined(_AMD64_)
    const unsigned PRIME = 37;

    static const char hashTable[PRIME] =
    {
        -1,  0,  1, 26,  2, 23, 27, -1,  3, 16,
        24, 30, 28, 11, -1, 13,  4,  7, 17, -1,
        25, 22, 31, 15, 29, 10, 12,  6, -1, 21,
        14,  9,  5, 20,  8, 19, 18
    };

    _ASSERTE(PRIME >= 8*sizeof(value));
    _ASSERTE(sizeof(hashTable) == PRIME);


    unsigned hash   = value % PRIME;
    unsigned index  = hashTable[hash];
    _ASSERTE(index != (unsigned char)-1);
#else
    // not enabled for x86 because BSF is extremely slow on Atom
    // (15 clock cycles vs 1-3 on any other Intel CPU post-P4)
    DWORD index;
    BitScanForward(&index, value);
#endif
    return index;
}

#endif
