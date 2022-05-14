// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
//
inline
unsigned            BitPosition(unsigned value)
{
    _ASSERTE((value != 0) && ((value & (value-1)) == 0));
    DWORD index;
    BitScanForward(&index, value);
    return index;
}


#ifdef TARGET_64BIT
//------------------------------------------------------------------------
// BitPosition: Return the position of the single bit that is set in 'value'.
//
// Return Value:
//    The position (0 is LSB) of bit that is set in 'value'
//
// Notes:
//    'value' must have exactly one bit set.
//
inline
unsigned            BitPosition(unsigned __int64 value)
{
    _ASSERTE((value != 0) && ((value & (value-1)) == 0));
    DWORD index;
    BitScanForward64(&index, value);
    return index;
}
#endif // TARGET_64BIT

#endif
