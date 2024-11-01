// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef NIBBLEMAPMACROS_H_
#define NIBBLEMAPMACROS_H_

///////////////////////////////////////////////////////////////////////
////   some mmgr stuff for JIT, especially for jit code blocks
///////////////////////////////////////////////////////////////////////
//
// In order to quickly find the start of a jit code block
// we keep track of all those positions via a map.
// Each entry in this map represents 32 byte (a bucket) of the code heap.
// We make the assumption that no two code-blocks can start in
// the same 32byte bucket;
// Additionally we assume that every code header is DWORD aligned.
// Because we cannot guarantee that jitblocks always start at
// multiples of 32 bytes we cannot use a simple bitmap; instead we
// use a nibble (4 bit) per bucket and encode the offset of the header
// inside the bucket (in DWORDS). Each 32-bit DWORD represents 256 bytes
// in the mapped code region. In order to make initialization
// easier we add one to the real offset, a nibble-value of zero
// means that there is no header start in the resp. bucket.
// To have constant time reads, we also store relative pointers
// in DWORDs which represent code regions which are completed
// covered by a function. This is indicated by the nibble value
// in the lowest bits of the DWORD having a value > 8.
// 
// Pointers are encoded in DWORDS by using the top 28 bits as normal.
// The bottom 4 bits are read as a nibble (the value must be greater than 8
// to identify the DWORD as a pointer) which encodes the final 2 bits of
// information. 

#if defined(HOST_64BIT)
// TODO: bump up the windows CODE_ALIGN to 16 and iron out any nibble map bugs that exist.
# define CODE_ALIGN             4
# define LOG2_CODE_ALIGN        2
#else
# define CODE_ALIGN             sizeof(DWORD)                                // 4 byte boundry
# define LOG2_CODE_ALIGN        2
#endif
#define NIBBLE_MASK             0xfu
#define NIBBLE_SIZE             4                                            // 4 bits
#define LOG2_NIBBLE_SIZE        2
#define NIBBLES_PER_DWORD       (2 * sizeof(DWORD))                          // 8 (4-bit) nibbles per dword
#define NIBBLES_PER_DWORD_MASK  (NIBBLES_PER_DWORD - 1)                      // 7
#define LOG2_NIBBLES_PER_DWORD  3
#define BYTES_PER_BUCKET        (NIBBLES_PER_DWORD * CODE_ALIGN)             // 32 bytes per bucket
#define LOG2_BYTES_PER_BUCKET   (LOG2_CODE_ALIGN + LOG2_NIBBLES_PER_DWORD)   //  5 bits per bucket
#define MASK_BYTES_PER_BUCKET   (BYTES_PER_BUCKET - 1)                       // 31
#define BYTES_PER_DWORD         (NIBBLES_PER_DWORD * BYTES_PER_BUCKET)       // 256 bytes per dword
#define LOG2_BYTES_PER_DWORD    (LOG2_NIBBLES_PER_DWORD + LOG2_BYTES_PER_BUCKET) // 8 bits per dword
#define HIGHEST_NIBBLE_BIT      (32 - NIBBLE_SIZE)                           // 28 (i.e 32 - 4)
#define HIGHEST_NIBBLE_MASK     (NIBBLE_MASK << HIGHEST_NIBBLE_BIT)          // 0xf0000000

#define ADDR2POS(x)                      ((x) >> LOG2_BYTES_PER_BUCKET)
#define ADDR2OFFS(x)            (DWORD)  ((((x) & MASK_BYTES_PER_BUCKET) >> LOG2_CODE_ALIGN) + 1)
#define POSOFF2ADDR(pos, of)    (size_t) (((pos) << LOG2_BYTES_PER_BUCKET) + (((of) - 1) << LOG2_CODE_ALIGN))
#define HEAP2MAPSIZE(x)                  (((x) / (BYTES_PER_DWORD) + 1) * sizeof(DWORD))
#define POS2SHIFTCOUNT(x)       (DWORD)  (HIGHEST_NIBBLE_BIT - (((x) & NIBBLES_PER_DWORD_MASK) << LOG2_NIBBLE_SIZE))
#define POS2MASK(x)             (DWORD) ~(HIGHEST_NIBBLE_MASK >> (((x) & NIBBLES_PER_DWORD_MASK) << LOG2_NIBBLE_SIZE))

inline DWORD Pos2ShiftCount(size_t pos)
{
    return HIGHEST_NIBBLE_BIT - ((pos & NIBBLES_PER_DWORD_MASK) << LOG2_NIBBLE_SIZE);
}

inline size_t GetDwordIndex(size_t relativePointer)
{
    return relativePointer >> LOG2_BYTES_PER_DWORD;
}

inline size_t GetNibbleIndex(size_t relativePointer)
{
    return (relativePointer >> (LOG2_BYTES_PER_BUCKET)) & NIBBLES_PER_DWORD_MASK;
}

inline size_t NibbleToRelativeAddress(size_t dwordIndex, size_t nibbleIndex, DWORD nibbleValue)
{
    return (dwordIndex << LOG2_BYTES_PER_DWORD) + (nibbleIndex << LOG2_BYTES_PER_BUCKET) + ((nibbleValue - 1) << LOG2_CODE_ALIGN);
}

inline DWORD GetNibble(DWORD dword, size_t nibbleIndex)
{
    return (dword >> POS2SHIFTCOUNT(nibbleIndex)) & NIBBLE_MASK;
}

inline bool IsPointer(DWORD dword)
{
    return (dword & NIBBLE_MASK) > 8;
}

inline DWORD EncodePointer(size_t relativePointer)
{
    return (DWORD) ((relativePointer & ~NIBBLE_MASK) + (((relativePointer & NIBBLE_MASK) >> 2) + 9));
}

inline size_t DecodePointer(DWORD dword)
{
    return (size_t) ((dword & ~NIBBLE_MASK) + (((dword & NIBBLE_MASK) - 9) << 2));
}

//------------------------------------------------------------------------
// BitScanForward: Search the mask data from least significant bit (LSB) to the most significant bit
// (MSB) for a set bit (1)
//
// Arguments:
//    value - the value
//
// Return Value:
//    0 if the mask is zero; nonzero otherwise.
//
FORCEINLINE size_t NibbleBitScanForward(DWORD value)
{
    assert(value != 0);

#if defined(_MSC_VER)
    unsigned long result;
    ::_BitScanForward(&result, value);
    return static_cast<size_t>(result);
#else
    int32_t result = __builtin_ctz(value);
    return static_cast<size_t>(result);
#endif
}

inline bool FindPreviousNibble(DWORD dword, size_t &nibbleIndex, DWORD &nibble)
{
    if(!dword)
    {
        return false;
    }

    size_t ctz = NibbleBitScanForward(dword);
    nibbleIndex = (31 - ctz) / 4;
    nibble = GetNibble(dword, nibbleIndex);
    _ASSERTE(nibble);
    return true;
}

#endif  // NIBBLEMAPMACROS_H_
