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
// In order to speed up "backwards scanning" we start numbering
// nibbles inside a DWORD from the highest bits (28..31). Because
// of that we can scan backwards inside the DWORD with right shifts.
//
// To have constant time reads, we store pointers relative to the map
// base in DWORDs which represent code regions that are completely
// covered by a function. A DWORD is a pointer if the nibble value
// in the lowest bits of the DWORD have a value > 8.
//
// Pointers are encoded in DWORDS by using the top 28 bits as normal.
// The bottom 4 bits are read as a nibble (the value must be greater than 8
// to identify the DWORD as a pointer) which encodes the final 2 bits of
// information.
//
///////////////////////////////////////////////////////////////////////
////                         Set Algorithm
///////////////////////////////////////////////////////////////////////
//
// 1. Write encoded nibble at offset corresponding to PC.
// 2. If codeSize completely covers one or more subsequent DWORDs,
//    insert relative pointers into each covered DWORD.
//
///////////////////////////////////////////////////////////////////////
////                        Delete Algorithm
///////////////////////////////////////////////////////////////////////
//
// 1. Delete the nibble corresponding to the PC.
// 2. Delete all following pointers which match the offset of PC.
//    We must check the pointers refer to the PC because there may
//    one or more subsequent nibbles in the DWORD. In that case the
//    following pointers would not refer to PC but a different offset.
//
///////////////////////////////////////////////////////////////////////
////                        Read Algorithm
///////////////////////////////////////////////////////////////////////
//
// 1. Look up DWORD representing given PC.
// 2. If DWORD is a pointer, then return pointer + mapBase.
// 3. If nibble corresponding to PC is initialized and the value
//    it represents precedes the PC return that value.
// 4. Find the first preceding initialized nibble in the DWORD.
//    If found, return the value the nibble represents.
// 5. Execute steps 2 and 4 on the proceeding DWORD.
//    If this DWORD does not contain a pointer or any initialized nibbles,
//    then we must not be in a function and can return an nullptr.
//
///////////////////////////////////////////////////////////////////////
////                        Concurrency
///////////////////////////////////////////////////////////////////////
//
// Writes to the nibblemap (set and delete) require holding a critical
// section and therefore can not be done concurrently. Reads can be done
// without a lock and can occur at any time.
//
// The read algorithm is designed so that as long as no tearing occurs
// on a DWORD, the read will always be valid. This is because the read
// only depends on a single DWORD. Either the first if that contains a
// pointer/preceeding initialized nibble, or second if that contains a
// pointer/nibble. Given that DWORDs are 32-bits and aligned to 4-byte
// boundaries, these reads should not tear.
//

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

namespace NibbleMap
{
    FORCEINLINE bool IsPointer(DWORD dword)
    {
        return (dword & NIBBLE_MASK) > 8;
    }

    FORCEINLINE DWORD EncodePointer(size_t relativePointer)
    {
        return (DWORD) ((relativePointer & ~NIBBLE_MASK) + (((relativePointer & NIBBLE_MASK) >> 2) + 9));
    }

    FORCEINLINE size_t DecodePointer(DWORD dword)
    {
        return (size_t) ((dword & ~NIBBLE_MASK) + (((dword & NIBBLE_MASK) - 9) << 2));
    }
}


#endif  // NIBBLEMAPMACROS_H_
